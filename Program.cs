using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Security.Cryptography;
using System.IO;

namespace DuplicateFileDetector
{
    class DuplicateFile
    {
        public string MD5Hash;
        public string Filename;

        public DuplicateFile(string m, string f)
        {
            MD5Hash = m;
            Filename = f;
        }
    }

    class Program
    {
        static List<DuplicateFile> allFiles = new List<DuplicateFile>();

        static string rootFolderOfDuplicates = @"C:\DuplicateDetectorTest";
        static string newFilePath = @"C:\newPath";
        static string allFilesGroupedByHashLogPath = @"C:\allFilesGroupedByHash.log";
        static string movedFilesLogPath = @"C:\movedFiles.log";
        static string deletedFilesLogPath = @"C:\deletedFiles.log";

        static void Main(string[] args)
        {
            // Remove all existing logs so we can start fresh each run.
            ClearLogs();

            // Calculate hashes of all files, store in application object allFiles
            GetAllHashes(rootFolderOfDuplicates);

            // Log all files grouped by hashes
            OutputAllHashes();

            // Will take first file found (with shortest path) and move to new location
            OutputAndMoveDistinctItems();

            // Delete all remaining files
            //WriteLine(String.Format("Deleting all remaining items in {0}, logging all deletions in {1}..", rootFolderOfDuplicates, deletedFilesLogPath));
            //DeleteAllRemainingItems(rootFolderOfDuplicates);
        }
        static void ClearLogs()
        {
            WriteLine("Clearing all logs..");
            if (File.Exists(allFilesGroupedByHashLogPath))
            {
                File.Delete(allFilesGroupedByHashLogPath);
            }
            if (File.Exists(movedFilesLogPath))
            {
                File.Delete(movedFilesLogPath);
            }
            if (File.Exists(deletedFilesLogPath))
            {
                File.Delete(deletedFilesLogPath);
            }
        }

        static void GetAllHashes(string rootDirectory)
        {
            LocateAllFiles(rootDirectory);
        }

        static void OutputAllHashes()
        {
            WriteLine(String.Format("Logging all hashes and files, grouped by hash to {0}", allFilesGroupedByHashLogPath));
            var filesGroupedByHash = from fileGroup in allFiles
                                     orderby fileGroup.MD5Hash
                                     select new { fileGroup.MD5Hash, fileGroup.Filename };

            foreach (var grp in filesGroupedByHash)
            {
                WriteLog(grp.MD5Hash + " " + grp.Filename, allFilesGroupedByHashLogPath);
            }
        }

        static void OutputAndMoveDistinctItems()
        {
            WriteLine(String.Format("Logging all distinct hashes and files and new locations to {0}", movedFilesLogPath));

            var distinctHashesOnly = 
            allFiles
                .GroupBy(i => i.MD5Hash)
                .Select(i => 
                    i.OrderBy(s => s.Filename.Length)
                    .First()
                 )
                .ToList();

            foreach (var grp in distinctHashesOnly)
            {
                MoveFileToNewLocation(grp.Filename);
            }
        }

        static void DeleteAllRemainingItems(string root)
        {
            string[] files = Directory.GetFiles(root);
            foreach (string file in files)
            {
                RemoveItem(file);
            }
            string[] subdirectories = Directory.GetDirectories(root);
            foreach (string folder in subdirectories)
            {
                DeleteAllRemainingItems(folder);
            }
        }

        static void RemoveItem(string filename)
        {
            WriteLog(String.Format("Deleting {0}..", filename), deletedFilesLogPath);
            File.Delete(filename);
        }

        static void MoveFileToNewLocation(string fullPathAndFileName)
        {
            string targetPath = fullPathAndFileName.Replace(rootFolderOfDuplicates, newFilePath);
            string targetDirectory = Path.GetDirectoryName(targetPath);
            if (!Directory.Exists(targetDirectory))
            {
                Directory.CreateDirectory(targetDirectory);
            }
            WriteLog(String.Format("Moving {0} to {1}..", fullPathAndFileName, targetPath), movedFilesLogPath);
            File.Move(fullPathAndFileName, targetPath);
        }

        static void LocateAllFiles(string root)
        {
            string[] files = Directory.GetFiles(root);
            foreach (string file in files)
            {
                ComputeHash(file);
            }
            string[] subdirectories = Directory.GetDirectories(root);
            foreach (string folder in subdirectories)
            {
                LocateAllFiles(folder);
            }
        }

        static void ComputeHash(string file)
        {
            byte[] temporaryHash = ComputeHashFromFile(file);
            allFiles.Add(new DuplicateFile(ByteArrayToString(temporaryHash), file));
        }

        static void WriteLine(string message)
        {
            Console.WriteLine(message);
        }

        static void WriteLog(string line, string logPath)
        {
            if (!File.Exists(logPath))
            {
                using (StreamWriter sw = File.CreateText(logPath))
                {
                    sw.WriteLine(line);
                }
            }
            else
            {
                using (StreamWriter sw = File.AppendText(logPath))
                {
                    sw.WriteLine(line);
                }
            }

        }

        static byte[] ComputeHashFromFile(string fileToCompute)
        {
            using (var md5 = MD5.Create())
            {
                using (var stream = File.OpenRead(fileToCompute))
                {
                    return md5.ComputeHash(stream);
                }
            }
        }

        static string ByteArrayToString(byte[] ba)
        {
            StringBuilder hex = new StringBuilder(ba.Length * 2);
            foreach (byte b in ba)
            {
                hex.AppendFormat("{0:x2}", b);
            }
            return hex.ToString();
        }
    }
}
