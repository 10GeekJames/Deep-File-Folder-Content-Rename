
using System.IO.Compression;

namespace AbcSharp.Tool.DeepRenamer;
public class Renamer
{
    private ICollection<FileChange> _filesTouched;
    private string _archivePath;
    private string _workingDirectory;
    private string _outputArchive;
    private readonly string _sevenZipPath;

    public Renamer(string archivePath, string sevenZipPath)
    {
        if (sevenZipPath == "")
        {
            sevenZipPath = "7zr.exe";
        }

        _filesTouched = new List<FileChange>();
        _archivePath = archivePath;
        _workingDirectory = Path.Combine("c:\\temp\\renamer\\", Path.GetFileNameWithoutExtension(archivePath));

        _outputArchive = Path.Combine(Path.GetDirectoryName(archivePath),
            $"{Path.GetFileNameWithoutExtension(archivePath)}_renamed{Path.GetExtension(archivePath)}");
        _sevenZipPath = sevenZipPath;

        if (!ValidateSetup()) throw new Exception("Could not validate setup");

        UnpackArchive();
    }

    public void Run(string fromKeyword, string toKeyword)
    {
        ProcessFiles(fromKeyword, toKeyword);
    }

    public void Finish()
    {
        CreateNewArchive();
        CleanUp();
        GenerateReport();
    }

    private bool ValidateSetup()
    {
        if (File.Exists(_outputArchive))
        {
            File.Delete(_outputArchive);
        }

        if (Directory.Exists(_workingDirectory))
        {
            Directory.Delete(_workingDirectory, true);
        }

        return true;
    }

    private void UnpackArchive()
    {
        Directory.CreateDirectory(_workingDirectory);

        if (_archivePath.EndsWith(".7z", StringComparison.OrdinalIgnoreCase))
        {
            // Handle .7z files using 7zr.exe
            var args = $"x \"{_archivePath}\" -o\"{_workingDirectory}\" -y";
            var processInfo = new ProcessStartInfo
            {
                FileName = _sevenZipPath,
                Arguments = args,
                RedirectStandardOutput = true,
                UseShellExecute = false,
                CreateNoWindow = true
            };

            using (var process = Process.Start(processInfo))
            {
                process.WaitForExit();
                if (process.ExitCode != 0)
                {
                    throw new InvalidOperationException($"7zr.exe failed to unpack {_archivePath}. Exit code: {process.ExitCode}");
                }
            }
        }
        else if (_archivePath.EndsWith(".zip", StringComparison.OrdinalIgnoreCase))
        {
            // Handle .zip files using .NET's built-in functionality
            ZipFile.ExtractToDirectory(_archivePath, _workingDirectory);
        }
        else
        {
            throw new NotSupportedException("Unsupported file format. Only .zip and .7z are supported.");
        }
    }

    private void ProcessFiles(string fromKeyword, string toKeyword)
    {
        UpdateFileContents(fromKeyword, toKeyword);
        UpdateFileNames(fromKeyword, toKeyword);
        UpdateFolderNames(fromKeyword, toKeyword);
    }

    private void UpdateFileContents(string fromKeyword, string toKeyword)
    {
        var files = Directory.EnumerateFiles(_workingDirectory, "*", SearchOption.AllDirectories)
                             .OrderByDescending(f => new FileInfo(f).Length);

        foreach (var file in files)
        {
            try
            {
                var content = File.ReadAllText(file);
                if (content.Contains(fromKeyword, StringComparison.Ordinal))
                {
                    content = ReplaceCaseSensitive(content, fromKeyword, toKeyword);
                    File.WriteAllText(file, content);

                    LogFileChange(file);
                }
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException($"Failed to process file {file}: {ex.Message}", ex);
            }
        }
    }

    private void UpdateFileNames(string fromKeyword, string toKeyword)
    {
        var files = Directory.EnumerateFiles(_workingDirectory, "*", SearchOption.AllDirectories)
                             .Where(file => Path.GetFileName(file).Contains(fromKeyword, StringComparison.Ordinal))
                             .OrderByDescending(f => new FileInfo(f).Length);

        foreach (var file in files)
        {
            var newFileName = ReplaceCaseSensitive(Path.GetFileName(file), fromKeyword, toKeyword);
            if (newFileName != Path.GetFileName(file))
            {
                var newPath = Path.Combine(Path.GetDirectoryName(file), newFileName);
                try
                {
                    File.Move(file, newPath);
                    LogFileChange(file);
                }
                catch (Exception ex)
                {
                    throw new InvalidOperationException($"Failed to rename file {file} to {newFileName}: {ex.Message}", ex);
                }
            }
        }
    }

    private void UpdateFolderNames(string fromKeyword, string toKeyword)
    {
        var directories = Directory.EnumerateDirectories(_workingDirectory, "*", SearchOption.AllDirectories)
                                   .Where(directory => new DirectoryInfo(directory).Name.Contains(fromKeyword, StringComparison.Ordinal))
                                   .OrderByDescending(d => d.Length);
        while(directories.Count() > 0)
        {
            var directory = directories.First();
            var parentPath = Directory.GetParent(directory).FullName;
            var dirName = new DirectoryInfo(directory).Name;
            var fullPath = Path.Combine(parentPath, dirName);

            var lastIndex = fullPath.LastIndexOf(fromKeyword, StringComparison.Ordinal);

            if (lastIndex >= 0)
            {
                var newFullPath = fullPath.Remove(lastIndex, fromKeyword.Length).Insert(lastIndex, toKeyword);

                if (newFullPath != fullPath)
                {
                    try
                    {
                        Directory.Move(fullPath, newFullPath);
                        LogFileChange(fullPath);
                    }
                    catch (Exception ex)
                    {
                        throw new InvalidOperationException($"Failed to rename directory {fullPath} to {newFullPath}: {ex.Message}", ex);
                    }
                }
            }
            directories = Directory.EnumerateDirectories(_workingDirectory, "*", SearchOption.AllDirectories)
                .Where(directory => new DirectoryInfo(directory).Name.Contains(fromKeyword, StringComparison.Ordinal))
                .OrderByDescending(d => d.Length);
        }
    }

    private string ReplaceCaseSensitive(string input, string fromKeyword, string toKeyword)
    {
        return input.Replace(fromKeyword, toKeyword);
    }

    private void LogFileChange(string filePath)
    {
        _filesTouched.Add(new FileChange
        {
            FileName = Path.GetFileName(filePath),
            FileType = Path.GetExtension(filePath),
            FolderName = Path.GetDirectoryName(filePath)
        });
    }

    private void CreateNewArchive()
    {
        if (_outputArchive.EndsWith(".7z", StringComparison.OrdinalIgnoreCase))
        {
            // Handle .7z files using 7zr.exe
            var args = $"a \"{_outputArchive}\" \"{_workingDirectory}\\*\" -y";
            var processInfo = new ProcessStartInfo
            {
                FileName = _sevenZipPath,
                Arguments = args,
                RedirectStandardOutput = true,
                UseShellExecute = false,
                CreateNoWindow = true
            };

            using (var process = Process.Start(processInfo))
            {
                process.WaitForExit();
                if (process.ExitCode != 0)
                {
                    throw new InvalidOperationException($"7zr.exe failed to create archive {_outputArchive}. Exit code: {process.ExitCode}");
                }
            }
        }
        else if (_outputArchive.EndsWith(".zip", StringComparison.OrdinalIgnoreCase))
        {
            // Handle .zip files using .NET's built-in functionality
            ZipFile.CreateFromDirectory(_workingDirectory, _outputArchive);
        }
        else
        {
            throw new NotSupportedException("Unsupported file format for the output archive.");
        }
    }

    private void CleanUp()
    {
        if (Directory.Exists(_workingDirectory))
        {
            Directory.Delete(_workingDirectory, true);
        }
    }

    private void GenerateReport()
    {
        string reportPath = Path.Combine(Path.GetDirectoryName(_archivePath), $"{Path.GetFileNameWithoutExtension(_archivePath)}_report.xlsx");

        if (File.Exists(reportPath))
        {
            File.Delete(reportPath);
        }

        OfficeOpenXml.ExcelPackage.LicenseContext = OfficeOpenXml.LicenseContext.NonCommercial;

        using (var excelPackage = new OfficeOpenXml.ExcelPackage(new FileInfo(reportPath)))
        {
            var filesWorksheet = excelPackage.Workbook.Worksheets.Add("Files Touched");

            filesWorksheet.Cells["A1"].Value = "File Name";
            filesWorksheet.Cells["B1"].Value = "File Type";
            filesWorksheet.Cells["C1"].Value = "Folder Name";

            int rowIndex = 2;
            foreach (var change in _filesTouched)
            {
                filesWorksheet.Cells[rowIndex, 1].Value = change.FileName;
                filesWorksheet.Cells[rowIndex, 2].Value = change.FileType;
                filesWorksheet.Cells[rowIndex, 3].Value = change.FolderName;
                rowIndex++;
            }

            filesWorksheet.Cells.AutoFitColumns();

            excelPackage.Save();
        }
    }
}