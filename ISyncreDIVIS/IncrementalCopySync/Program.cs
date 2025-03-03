﻿using System;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace MyApp
{
    internal class Program
    {
        private static int MaxCopiesAllowed = 3;
        private static double pocetZkopirovanych = 0;
        private static double pocetStejnych = 0;

        static async Task Main(string[] args)
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

            await CopyAndCompareFilesAsync(sourceDir, destinationDir, LOGFile);

            await File.AppendAllTextAsync(LOGFile, $"Number of copied files: {pocetZkopirovanych}, Number of same files {pocetStejnych}. {DateTime.Now}{Environment.NewLine}");

            Console.WriteLine("Operation completed successfully.");
        }

        static async Task CopyAndCompareFilesAsync(string sourceDir, string destinationDir, string LOGFile)
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
                        await CopyAsync(filePath, newDestFilePath, LOGFile);
                        Console.WriteLine($"Newer file copied with name: {newFileName}");
                        await File.AppendAllTextAsync(LOGFile, $"Newer file copied with name: {newFileName} {DateTime.Now}{Environment.NewLine}");
                        ManageFileCopies(destinationDir, baseName, extension);
                        pocetZkopirovanych++;
                    }
                    else
                    {
                        Console.WriteLine($"No action taken for {fileName}. Destination has the latest version.");
                        await File.AppendAllTextAsync(LOGFile, $"No action taken for {fileName}. Destination has the latest version. {DateTime.Now}{Environment.NewLine}");
                        pocetStejnych++;
                    }
                }
                else
                {
                    string destFilePath = Path.Combine(destinationDir, fileName);
                    await CopyAsync(filePath, destFilePath, LOGFile);
                    Console.WriteLine($"File copied: {fileName}");
                }
            }

            foreach (string dirPath in Directory.GetDirectories(sourceDir))
            {
                string destDirPath = Path.Combine(destinationDir, Path.GetFileName(dirPath));
                await CopyAndCompareFilesAsync(dirPath, destDirPath, LOGFile);
            }
        }

        static async Task CopyAsync(string sourceFilePath, string destinationFilePath, string LOGFile)
        {
            try
            {
                const int bufferSize = 81920; // 80 KB buffer size
                using (FileStream sourceStream = new FileStream(sourceFilePath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite, bufferSize, useAsync: true))
                using (FileStream destinationStream = new FileStream(destinationFilePath, FileMode.Create, FileAccess.Write, FileShare.None, bufferSize, useAsync: true))
                {
                    await sourceStream.CopyToAsync(destinationStream);
                }
            }
            catch (Exception ex)
            {
                string log = $"Error copying file: {ex.Message}";
                await File.AppendAllTextAsync(LOGFile, log + Environment.NewLine);
            }
        }

        static FileInfo? GetMostRecentFile(string directory, string baseName, string extension)
        {
            try
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
            catch (Exception ex)
            {
                string log = $"Error in GetMostRecentFile: {ex.Message}";
                var (sourceDir, destinationDir, NumbCopiesAllowed, LOGFile) = sourceDirectory();
                File.AppendAllText(LOGFile, log + Environment.NewLine);
                return null;
            }
        }

        static string GetUniqueFileName(string directory, string baseName, string extension)
        {
            try
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
            catch (Exception ex)
            {
                string log = $"Error in GetUniqueFileName: {ex.Message}";
                var (sourceDir, destinationDir, NumbCopiesAllowed, LOGFile) = sourceDirectory();
                File.AppendAllText(LOGFile, log + Environment.NewLine);
                return "";
            }
        }

        static void ManageFileCopies(string directory, string baseName, string extension)
        {
            try
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
            catch (Exception ex)
            {
                string log = $"Error in ManageFileCopies: {ex.Message}";
                var (sourceDir, destinationDir, NumbCopiesAllowed, LOGFile) = sourceDirectory();
                File.AppendAllText(LOGFile, log + Environment.NewLine);
            }
        }

        static (string, string, string, string) sourceDirectory()
        {
            string baseDirectory = Path.GetDirectoryName(System.Reflection.Assembly.GetExecutingAssembly().Location);
            string relativePath = Path.Combine(baseDirectory, @"Configuration\Configuration.txt"); //published
            //string relativePath = Path.Combine(baseDirectory, @"..\..\..\Configuration\Configuration.txt"); //IDE
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
                        Console.WriteLine($"LogFile Directory is: {LOGFile}");
                        using (TextWriter tw = new StreamWriter(LOGFile + DateOnly.FromDateTime(DateTime.Now).ToString("o", CultureInfo.InvariantCulture) + ".log"))
                        {
                            LOGFile += DateOnly.FromDateTime(DateTime.Now).ToString("o", CultureInfo.InvariantCulture) + ".log";
                        }
                        File.AppendAllText(LOGFile, $"LOGFile Created; {DateTime.Now}{Environment.NewLine}");
                    }

                    if (line.StartsWith("sourceDir:"))
                    {
                        sourceDir = line.Substring("sourceDir:".Length).Trim();
                        Console.WriteLine($"Source Directory Path: {sourceDir}");
                        File.AppendAllText(LOGFile, $"Source Directory is {sourceDir} {DateTime.Now}{Environment.NewLine}");
                    }

                    if (line.StartsWith("destinationDir:"))
                    {
                        destinationDir = line.Substring("destinationDir:".Length).Trim();
                        Console.WriteLine($"Destination Directory Path: {destinationDir}");
                        File.AppendAllText(LOGFile, $"Destination directory is {destinationDir} {DateTime.Now}{Environment.NewLine}");
                    }

                    if (line.StartsWith("MaxCopiesAllowed:"))
                    {
                        MaxCopiesAllowed = line.Substring("MaxCopiesAllowed:".Length).Trim();
                        Console.WriteLine($"Number of allowed copies is: {MaxCopiesAllowed}");
                        File.AppendAllText(LOGFile, $"Number of allowed copies is: {MaxCopiesAllowed} {DateTime.Now}{Environment.NewLine}");
                    }

                }
            }
            else
            {
                Console.WriteLine($"File not found: {absolutePath}");
            }

            return (sourceDir, destinationDir, MaxCopiesAllowed, LOGFile);
        }
    }
}
