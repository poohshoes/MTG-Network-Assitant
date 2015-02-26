using GameName1;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace KCLidgrenDebug
{
    class Program
    {
        static void Main()
        {
            if (true)
            {
                //if (!Debugger.IsAttached)
                //    Debugger.Launch();
                //Debugger.Break();
                if (Directory.GetCurrentDirectory() == "D:\\Projects\\MTG Network Play\\KCLidgrenDebug\\bin\\Debug")
                {
                    string fileDirectory = Directory.GetCurrentDirectory();
                    string copyDirectoryName = "C:\\Users\\Ian\\Desktop\\MTGMultiPlay";
                    DirectoryCopy(Directory.GetCurrentDirectory(), copyDirectoryName);

                    string ourSettings, otherSettings;
                    if (true)// Attach to Server == true
                    {
                        ourSettings = "server";
                        otherSettings = "client\nlocalhost";
                    }
                    else
                    {
                        ourSettings = "client\nlocalhost";
                        otherSettings = "server";
                    }

                    ourSettings += "\n56542";
                    ourSettings += "\n1900";
                    ourSettings += "\n1000";

                    otherSettings += "\n56542";
                    otherSettings += "\n1900";
                    otherSettings += "\n1000";

                    File.WriteAllText("networkSettings.txt", ourSettings);
                    File.WriteAllText(Path.Combine(copyDirectoryName, "networkSettings.txt"), otherSettings);

                    var startInfo = new ProcessStartInfo()
                    {
                        WorkingDirectory = copyDirectoryName,
                        FileName = Path.Combine(copyDirectoryName, "GameName1.exe")
                    };
                    Process.Start(startInfo);
                }
            }

            var game = new Game1();
            game.Run();

        }

        private static void DirectoryCopy(string sourceDriveName, string destinationDirectoryName)
        {
            // Get the subdirectories for the specified directory.
            DirectoryInfo sourceDirectory = new DirectoryInfo(sourceDriveName);

            if (!sourceDirectory.Exists)
            {
                throw new DirectoryNotFoundException("Source directory does not exist or could not be found: " + sourceDriveName);
            }

            // If the destination directory doesn't exist, create it. 
            if (!Directory.Exists(destinationDirectoryName))
            {
                Directory.CreateDirectory(destinationDirectoryName);
            }

            // Get the files in the directory and copy them to the new location.
            FileInfo[] files = sourceDirectory.GetFiles();
            foreach (FileInfo file in files)
            {
                string destinationPath = Path.Combine(destinationDirectoryName, file.Name);
                file.CopyTo(destinationPath, true);
                FileInfo existingDestinationFile = new FileInfo(destinationPath);
                if (!existingDestinationFile.Exists || file.LastWriteTime > existingDestinationFile.LastWriteTime)
                {
                    file.CopyTo(destinationPath, true);
                }
            }

            foreach (DirectoryInfo subdir in sourceDirectory.GetDirectories())
            {
                string subdirectoryDestinationName = Path.Combine(destinationDirectoryName, subdir.Name);
                DirectoryCopy(subdir.FullName, subdirectoryDestinationName);
            }
        }
    }
}
