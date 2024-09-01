using Microsoft.Win32;
using System.Windows;
namespace AbcSharp.Tool.DeepRenamer.UI.Wpf
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {

        public MainWindow()
        {
            InitializeComponent();
        }
     
        private void SelectZipfile_Click(object sender, RoutedEventArgs e)
        {
            OpenFileDialog openFileDialog = new OpenFileDialog();

            // Set filters and options (optional)
            openFileDialog.Filter = "Zip Files (*.zip;*.7z)|*.zip;*.7z";
            openFileDialog.Title = "Select a file";
            openFileDialog.InitialDirectory = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments);

            // Show the file dialog
            if (openFileDialog.ShowDialog() == true) // Check if the user selected a file
            {
                // Display the selected file path
                CurrentArchive.Content = openFileDialog.FileName;
                ShowZipSelector(false);
            }
        }

        private void RenameTheWorld_Click(object sender, RoutedEventArgs e) {
            var renamer = new Renamer(CurrentArchive.Content.ToString(), "");

            foreach(var item in ModificationsToMake.Text.Split(Environment.NewLine))
            {
                var splitItem = item.Split("|");

                if (IncludeCaseSchewing.IsChecked == true) {
                    var runList = new List<string>();
                    runList.Add((splitItem[0])[0].ToString().ToUpper() + splitItem[0].ToString().Substring(1) + "|" + (splitItem[1])[0].ToString().ToUpper() + splitItem[1].ToString().Substring(1));
                    runList.Add((splitItem[0])[0].ToString().ToLower() + splitItem[0].ToString().Substring(1) + "|" + (splitItem[1])[0].ToString().ToLower() + splitItem[1].ToString().Substring(1));                                        
                    runList.Add(splitItem[0].ToString().ToUpper() + "|" + splitItem[1].ToString().ToUpper());
                    runList.Add(splitItem[0].ToString().ToLower() + "|" + splitItem[1].ToString().ToLower());

                    foreach(var runItem in runList.Distinct())
                    {
                        var splitRunItem = runItem.Split("|");
                        renamer.Run(splitRunItem[0], splitRunItem[1]);
                    }

                    continue;
                }

                renamer.Run(item.Split("|")[0], item.Split("|")[1]);
            }

            renamer.Finish();
            Console.WriteLine("Processing completed.");
        }        

        private void ShowZipSelector(bool show) {
            if (show)
            {
                SelectZipfile.Visibility = Visibility.Visible;
                LeftDecorate.Visibility = Visibility.Hidden;
                RightDecorate.Visibility = Visibility.Hidden;
                CurrentArchive.Visibility = Visibility.Hidden;
                RenameTheWorld.Visibility = Visibility.Hidden;
                ModificationsToMake.Visibility = Visibility.Hidden;
                IncludeCaseSchewing.Visibility = Visibility.Hidden;

                return;
            }

            SelectZipfile.Visibility = Visibility.Hidden;
            LeftDecorate.Visibility = Visibility.Visible;
            RightDecorate.Visibility = Visibility.Visible;
            CurrentArchive.Visibility = Visibility.Visible;
            RenameTheWorld.Visibility = Visibility.Visible;
            ModificationsToMake.Visibility = Visibility.Visible;
            IncludeCaseSchewing.Visibility = Visibility.Visible;

        }        


    }
}