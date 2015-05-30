using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;
using System.IO.Compression;
using System.Net;

namespace UpdateUploader
{
    class Program
    {
        static void Main(string[] args)
        {
            string pathToZip = Path.GetDirectoryName(System.Reflection.Assembly.GetExecutingAssembly().Location);
            pathToZip = Path.GetFullPath(Path.Combine(pathToZip, "..\\..\\..\\..\\"));
            pathToZip += "KCLidgrenDebug\\bin\\Debug";

            // Get the version number and save a reference file.
            if (!Directory.Exists("Temp"))
            {
                Directory.CreateDirectory("Temp");
            }
            WebClient client = new WebClient();
            client.DownloadFile("http://www.hernblog.com/mtgVersion.txt", "Temp\\mtgVersion.txt");
            int oldVersionNumber = int.Parse(File.ReadAllLines("Temp\\mtgVersion.txt")[0]);
            int newVectionNumber = oldVersionNumber + 1;
            Console.WriteLine(oldVersionNumber + "->" + newVectionNumber);
            string versionNumberPath = Path.Combine(pathToZip, "mtgVersion.txt");
            File.WriteAllText(versionNumberPath, newVectionNumber.ToString());

            // Create the zip file.
            string zippedFileName = "MTGNetPlay.zip";
            File.Delete(zippedFileName);
            using (ZipArchive archive = ZipFile.Open(zippedFileName, ZipArchiveMode.Create))
            {
                Stack<string> directoriesToZip = new Stack<string>();
                directoriesToZip.Push(pathToZip);
                while(directoriesToZip.Count > 0)
                {
                    string currentDirectory = directoriesToZip.Pop();
                    foreach (string newDirectory in Directory.GetDirectories(currentDirectory))
                    {
                        directoriesToZip.Push(newDirectory);
                    }

                    foreach (string file in Directory.GetFiles(currentDirectory))
                    {
                        string entryName = file.Replace(pathToZip + "\\", "");
                        if ((currentDirectory.Substring(currentDirectory.Length - 4) == "Data" &&
                            file.Substring(file.Length - 4) == ".jpg") ||
                            entryName == "crashLog.txt" ||
                            entryName == "starred.txt" ||
                            entryName == "KCLidgrenDebug.vshost.exe.config" ||
                            entryName == "KCLidgrenDebug.vshost.exe.manifest" ||
                            entryName == "MTGGame.exe.config" ||
                            entryName == "MTGGame.vshost.exe" ||
                            entryName == "MTGGame.vshost.exe.config" ||
                            entryName == "MTGGame.vshost.exe.manifest")
                        {
                            // Ignore these files
                        }
                        else
                        {
                            //Console.WriteLine(file + " ");
                            Console.WriteLine(entryName);
                            archive.CreateEntryFromFile(file, entryName);
                        }
                    }
                }
            }

            // Upload the new zip.
            UploadFile(zippedFileName);

            // Upload the new version number.
            Console.WriteLine("Uploading New Version File(" + newVectionNumber + ").");
            UploadFile(versionNumberPath);

            //Console.ReadLine();
        }

        static void UploadFile(string filePath)
        {
            string[] credentials = File.ReadAllLines(Path.GetFullPath(Path.Combine("..\\..\\..\\" + "ftpLogin.txt")));
            string url = "ftp://hernblog.com/"  + Path.GetFileName(filePath);
            FtpWebRequest request = (FtpWebRequest)FtpWebRequest.Create(url);
            request.KeepAlive = false;
            request.Method = WebRequestMethods.Ftp.UploadFile;
            request.Credentials = new NetworkCredential(credentials[0], credentials[1]);
            Stream stream = request.GetRequestStream();
            FileStream fileStream = File.OpenRead(filePath);
            int length = 1024;
            byte[] buffer = new byte[length];
            int bytesRead = 0;
            int totalBytesRead = 0;
            string lastPercent = "";
            do
            {
                bytesRead = fileStream.Read(buffer, 0, length);
                stream.Write(buffer, 0, bytesRead);
                totalBytesRead += bytesRead;
                string currentPercent = ((float)totalBytesRead / (float)fileStream.Length * 100f).ToString("0") + "%";
                if (currentPercent != lastPercent)
                {
                    lastPercent = currentPercent;
                    Console.WriteLine(currentPercent);
                }
            }
            while (bytesRead != 0);
            fileStream.Close();
            stream.Close();
        }
    }
}
