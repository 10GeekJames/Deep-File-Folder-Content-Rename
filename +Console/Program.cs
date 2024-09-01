using System.Collections.Generic;
using System.Linq;

namespace DeepRename;
class Program
{
    static void Main(string[] args)
    {
        bool varyCasing = true;

        if (args.Length == 0)
        {
            // Console.WriteLine("Please provide the path to the archive file (zip or 7z) as an argument.");
            // return;
            args = [""];
            args[0] = "C:\\r\\Rdy1LastLobby\\AbcSharp.Tool.DeepRenamer\\Test\\ReplaceBobWithJane.7z";
        }

        var archivePath = args[0];

        if (!System.IO.File.Exists(archivePath))
        {
            Console.WriteLine($"The file '{archivePath}' does not exist.");
            return;
        }

        var renamer = new Renamer(archivePath, "7zr.exe");

        while (true)
        {
            Console.WriteLine("What word should I search for?");
            var fromKeyword = Console.ReadLine();

            Console.WriteLine("What word should I replace it with?");
            var toKeyword = Console.ReadLine();

            if (varyCasing)
            {
                var runList = new List<string>();
                runList.Add(fromKeyword[0].ToString().ToUpper() + fromKeyword.Substring(1) + "|" + toKeyword[0].ToString().ToUpper() + toKeyword.Substring(1));
                runList.Add(fromKeyword[0].ToString().ToLower() + fromKeyword.Substring(1) + "|" + toKeyword[0].ToString().ToUpper() + toKeyword.Substring(1));
                
                runList.Add(fromKeyword.ToUpper() + "|" + toKeyword.ToUpper());
                runList.Add(fromKeyword.ToLower() + "|" + toKeyword.ToLower());

                foreach (var runItem in runList.Distinct())
                {
                    var splitRunItem = runItem.Split("|");
                    renamer.Run(splitRunItem[0], splitRunItem[1]);
                }
            } else {
                renamer.Run(fromKeyword, toKeyword);
            }            

            Console.WriteLine("Would you like to process another phrase? (Y/N) [N]");
            var key = Console.ReadKey().Key;
            Console.WriteLine();
            if (key != ConsoleKey.Y)
            {
                break;
            }
        }

        renamer.Finish();

        Console.WriteLine("Processing completed.");
    }
}
