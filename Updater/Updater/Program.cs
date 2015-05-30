using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;

namespace Updater
{
    class Program
    {
        // Note(ian): When we update this we need to manually copy it to the debug folder of the games build directory.
        static void Main(string[] args)
        {
            try
            {
                Console.WriteLine("Downloading...");
                WebClient client = new WebClient();
                string zipFileName = "Temp\\MTGNetPlay.zip";
                if (File.Exists(zipFileName))
                {
                    File.Delete(zipFileName);
                }
                string directoryName = Path.GetDirectoryName(zipFileName);
                if (!Directory.Exists(directoryName)) 
                {
                    Directory.CreateDirectory(directoryName);
                }
                client.DownloadFile("http://www.hernblog.com/MTGNetPlay.zip", zipFileName);

                Console.WriteLine("Extracting...");
                // Note(ian): We can't use this because it won't replace files.
                //ZipFile.ExtractToDirectory(zipFileName, ".");
                ZipArchive zipArchive = ZipFile.OpenRead(zipFileName);
                foreach (ZipArchiveEntry entry in zipArchive.Entries)
                {
                    string entryDirectory = Path.GetDirectoryName(entry.FullName);
                    if (entryDirectory != "" && !Directory.Exists(entryDirectory))
                    {
                        Directory.CreateDirectory(entryDirectory);
                    }
                    entry.ExtractToFile(entry.FullName, true);
                }

                Console.WriteLine("Starting up Game");
                Process.Start("MTGGame.exe");
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
                        exception.Message + Environment.NewLine +
                        exception.StackTrace + Environment.NewLine + Environment.NewLine;
                    File.AppendAllText("updaterCrashLog.txt", output);
                }
            }
        }
    }
}
