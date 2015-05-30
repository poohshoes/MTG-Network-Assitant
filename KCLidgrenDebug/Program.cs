using GameName1;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;

namespace KCLidgrenDebug
{
    class Program
    {
        static void Main()
        {
            try
            {
                if (!Debugger.IsAttached)
                {
                    if (!Directory.Exists("Temp"))
                    {
                        Directory.CreateDirectory("Temp");
                    }
                    WebClient client = new WebClient();
                    client.DownloadFile("http://www.hernblog.com/mtgVersion.txt", "Temp\\mtgVersion.txt");
                    int onlineVersionNumber = int.Parse(File.ReadAllLines("Temp\\mtgVersion.txt")[0]);
                    int currentVersionNumber = int.Parse(File.ReadAllLines("mtgVersion.txt")[0]);
                    if (onlineVersionNumber != currentVersionNumber)
                    {
                        // We can't update the updater if it's running so we make a copy.
                        CopyDirectoryNotRecursive("Updater", "Temp\\Updater");
                        Process.Start("Temp\\Updater\\Updater.exe");
                        Process.GetCurrentProcess().Kill();
                    }
                }

                if (false)
                {
                    //if (!Debugger.IsAttached)
                    //    Debugger.Launch();
                    //Debugger.Break();
                    if (Directory.GetCurrentDirectory() == "D:\\Projects\\MTGNetworkPlay\\KCLidgrenDebug\\bin\\Debug")
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
            catch (Exception exception)
            {
                if (Debugger.IsAttached)
                {
                    throw;
                }
                else
                {
                    string output = DateTime.Now.ToString() + Environment.NewLine +
                        "Version: " + int.Parse(File.ReadAllLines("mtgVersion.txt")[0]) + Environment.NewLine +
                        exception.Message + Environment.NewLine +
                        exception.StackTrace + Environment.NewLine + Environment.NewLine;
                    File.AppendAllText("crashLog.txt", output);
                }
            }
        }

        private static void CopyDirectoryNotRecursive(string from, string to)
        {
            if (!Directory.Exists(to))
            {
                Directory.CreateDirectory(to);
            }

            foreach (string file in Directory.GetFiles(from))
            {
                File.Copy(file, Path.Combine(to, Path.GetFileName(file)), true);
            }
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
