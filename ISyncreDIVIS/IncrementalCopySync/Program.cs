using System;
using System.Globalization;
using System.IO;
using System.Linq;

namespace MyApp
{
    internal class Program
    {
        private static int MaxCopiesAllowed = 3;
        private static double pocetZkopirovanych = 0;
        private static double pocetStejnych = 0;

        static void Main(string[] args)
        {
            var (sourceDir, destinationDir, NumbCopiesAllowed, LOGFile) = sourceDirectory();

            MaxCopiesAllowed = int.Parse(NumbCopiesAllowed);

            if (!Directory.Exists(sourceDir))
            {
                Console.WriteLine("The configuration file does not lead to source directory.");
                Console.ReadLine();
                return;
            }
            if (!Directory.Exists(destinationDir))
            {
                Console.WriteLine("The configuration file does not lead to destination directory.");
                Console.ReadLine();
                return;
            }
            if (!File.Exists(LOGFile))
            {
                Console.WriteLine("The configuration file does not lead to LOGFile.");
                Console.ReadLine();
                return;
            }

            CopyAndCompareFiles(sourceDir, destinationDir, LOGFile);
            
            File.AppendAllText(LOGFile, $"Number of copied files: {pocetZkopirovanych}, Number of same files {pocetStejnych}. " + DateTime.Now.ToString() + Environment.NewLine);

            Console.WriteLine("Operation completed successfully.");
        }

        static void CopyAndCompareFiles(string sourceDir, string destinationDir, string LOGFile)
        {
            Directory.CreateDirectory(destinationDir);

            foreach (string filePath in Directory.GetFiles(sourceDir))
            {
                string fileName = Path.GetFileName(filePath);
                string baseName = Path.GetFileNameWithoutExtension(fileName);
                string extension = Path.GetExtension(fileName);

                var recentFile = GetMostRecentFile(destinationDir, baseName, extension);

                if (recentFile != null)
                {
                    DateTime sourceModified = File.GetLastWriteTime(filePath);
                    DateTime recentModified = File.GetLastWriteTime(recentFile.FullName);

                    if (sourceModified > recentModified)
                    {
                        string newFileName = GetUniqueFileName(destinationDir, baseName, extension);
                        string newDestFilePath = Path.Combine(destinationDir, newFileName);
                        File.Copy(filePath, newDestFilePath, true);
                        Console.WriteLine($"Newer file copied with name: {newFileName}");
                        File.AppendAllText(LOGFile, $"Newer file copied with name: {newFileName} "+ DateTime.Now.ToString() + Environment.NewLine);
                        ManageFileCopies(destinationDir, baseName, extension);
                        pocetZkopirovanych++;
                    }
                    else
                    {
                        Console.WriteLine($"No action taken for {fileName}. Destination has the latest version.");
                        File.AppendAllText(LOGFile, $"No action taken for {fileName}. Destination has the latest version. " + DateTime.Now.ToString() + Environment.NewLine);
                        pocetStejnych++;
                    }
                }
                else
                {
                    string destFilePath = Path.Combine(destinationDir, fileName);
                    File.Copy(filePath, destFilePath, true);
                    Console.WriteLine($"File copied: {fileName}");
                }
            }
             
            foreach (string dirPath in Directory.GetDirectories(sourceDir))
            {
                string destDirPath = Path.Combine(destinationDir, Path.GetFileName(dirPath));
                CopyAndCompareFiles(dirPath, destDirPath, LOGFile);
            }
        }

        static FileInfo? GetMostRecentFile(string directory, string baseName, string extension)
        {
            var matchingFiles = Directory.GetFiles(directory, $"{baseName}-*{extension}")
                .Select(file => new FileInfo(file))
                .OrderByDescending(file => file.LastWriteTime)
                .ToList();

            string originalFile = Path.Combine(directory, $"{baseName}{extension}");
            if (File.Exists(originalFile))
            {
                matchingFiles.Add(new FileInfo(originalFile));
                matchingFiles = matchingFiles.OrderByDescending(file => file.LastWriteTime).ToList();
            }

            return matchingFiles.FirstOrDefault();
        }

        static string GetUniqueFileName(string directory, string baseName, string extension)
        {
            int index = 1;
            string newFileName;
            do
            {
                newFileName = $"{baseName}-{index}{extension}";
                index++;
            } while (File.Exists(Path.Combine(directory, newFileName)));
            return newFileName;
        }

        static void ManageFileCopies(string directory, string baseName, string extension)
        {
            var matchingFiles = Directory.GetFiles(directory, $"{baseName}-*{extension}")
                .Select(file => new FileInfo(file))
                .OrderByDescending(file => file.LastWriteTime)
                .ToList();

            string originalFile = Path.Combine(directory, $"{baseName}{extension}");
            if (File.Exists(originalFile))
            {
                matchingFiles.Add(new FileInfo(originalFile));
                matchingFiles = matchingFiles.OrderByDescending(file => file.LastWriteTime).ToList();
            }

            while (matchingFiles.Count > MaxCopiesAllowed)
            {
                var oldestFile = matchingFiles.Last();
                Console.WriteLine($"Deleting old file: {oldestFile.Name}");
                oldestFile.Delete();
                matchingFiles.RemoveAt(matchingFiles.Count - 1);
            }
        }

        static (string, string, string, string) sourceDirectory()
        {
            string baseDirectory = Path.GetDirectoryName(System.Reflection.Assembly.GetExecutingAssembly().Location);
            //string relativePath = Path.Combine(baseDirectory, @"..\..\..\Configuration\Configuration.txt"); //IDE
            string relativePath = Path.Combine(baseDirectory, @"Configuration\Configuration.txt"); //published
            string absolutePath = Path.GetFullPath(relativePath);
            string sourceDir = "";
            string destinationDir = "";
            string MaxCopiesAllowed = "3";
            string LOGFile = "";

            if (File.Exists(absolutePath))
            {
                string[] lines = File.ReadAllLines(absolutePath);
                foreach (var line in lines)
                {
                    if (line.StartsWith("LOGFile:"))
                    {
                        LOGFile = line.Substring("LOGFile:".Length).Trim();
                        Console.WriteLine("LogFile Directory is: " + LOGFile);
                        TextWriter tw = new StreamWriter(LOGFile + DateOnly.FromDateTime(DateTime.Now).ToString("o", CultureInfo.InvariantCulture) + ".log");
                        LOGFile += DateOnly.FromDateTime(DateTime.Now).ToString("o", CultureInfo.InvariantCulture) + ".log";
                        tw.Close();
                        File.AppendAllText(LOGFile, $"LOGFile Created;" + DateTime.Now.ToString() + Environment.NewLine);
                    }

                    if (line.StartsWith("sourceDir:"))
                    {
                        sourceDir = line.Substring("sourceDir:".Length).Trim();
                        Console.WriteLine("Source Directory Path: " + sourceDir);
                        File.AppendAllText(LOGFile, $"Source Directory is {sourceDir}" + DateTime.Now.ToString() + Environment.NewLine);
                    }

                    if (line.StartsWith("destinationDir:"))
                    {
                        destinationDir = line.Substring("destinationDir:".Length).Trim();
                        Console.WriteLine("Destination Directory Path: " + destinationDir);
                        File.AppendAllText(LOGFile, $"Destination directory is {destinationDir}" + DateTime.Now.ToString() + Environment.NewLine);
                    }

                    if (line.StartsWith("MaxCopiesAllowed:"))
                    {
                        MaxCopiesAllowed = line.Substring("MaxCopiesAllowed:".Length).Trim();
                        Console.WriteLine("Number of allowed copies is: " + MaxCopiesAllowed);
                        File.AppendAllText(LOGFile, $"Number of allowed copies is: {MaxCopiesAllowed}" + DateTime.Now.ToString() + Environment.NewLine);
                    }

                }
            }
            else
            {
                Console.WriteLine("File not found: " + absolutePath);
            }

            return (sourceDir, destinationDir, MaxCopiesAllowed, LOGFile);
        }
    }
}
