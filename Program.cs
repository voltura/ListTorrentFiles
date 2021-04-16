using BencodeNET.Parsing;
using BencodeNET.Torrents;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace ListTorrentFiles
{
    internal class Program
    {
        private string torrentDir;
        private readonly string downloadDir = @"E:\Downloads";
        private readonly string moveToDir = @"E:\Downloads_Finished";

        private static void Main()
        {
            Program p = new Program();
            p.Run();
        }

        private void Run()
        {
            Console.WriteLine("Working...");
            torrentDir = GetFinishedTorrentsFolder();
            List<string> activeTorrentFiles = GetActiveTorrentFiles();
            List<string> activeTorrentFolders = GetActiveTorrentFolders();
            string[] allFiles = Directory.GetFiles(downloadDir, "*.*", SearchOption.TopDirectoryOnly);
            string[] allFolders = Directory.GetDirectories(downloadDir, "*", SearchOption.TopDirectoryOnly);
            List<string> allDownloadedFiles = new List<string>(allFiles);
            List<string> allDownloadedFolders = new List<string>(allFolders);
            List<string> filesNotActive = GetFilesOKtoMove(activeTorrentFiles, allDownloadedFiles);
            List<string> foldersNotActive = GetFoldersOKtoMove(activeTorrentFolders, allDownloadedFolders);
            MoveFinishedFiles(filesNotActive);
            MoveFinishedFolders(foldersNotActive);
            DeleteEmptyAndInactiveFolders(foldersNotActive);
            Console.WriteLine("Done.");
        }

        private static string GetFinishedTorrentsFolder()
        {
            string folder = string.Empty;
            var fileName = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
                @"qBitTorrent\qBitTorrent.ini");
            if (!File.Exists(fileName)) return folder;
            foreach (string line in File.ReadAllLines(fileName))
            {
                if (line.StartsWith(@"Downloads\FinishedTorrentExportDir", StringComparison.OrdinalIgnoreCase))
                {
                    folder = line.Split('=')[1].Replace('/', '\\');
                    break;
                }
            }

            return folder;
        }
        private void MoveFinishedFolders(List<string> foldersNotActive)
        {
            List<string> rssSavePaths = GetRssSavePaths();
            // move inactive files
            foreach (string folder in foldersNotActive)
            {
                if (torrentDir.Equals(folder, StringComparison.InvariantCultureIgnoreCase) ||
                    rssSavePaths.Contains(folder, StringComparer.OrdinalIgnoreCase) ||
                    IsSymbolic(folder)) continue;
                string newFullPath = folder.Replace(downloadDir, moveToDir);
                if (!Directory.Exists(moveToDir)) Directory.CreateDirectory(moveToDir);
                Directory.Move(folder, newFullPath);
            }
        }

        private void MoveFinishedFiles(List<string> filesNotActive)
        {
            // move inactive files
            foreach (string file in filesNotActive)
            {
                if (file.EndsWith(".torrent") || IsSymbolic(Path.GetDirectoryName(file))) continue;
                string newFullPath = file.Replace(downloadDir, moveToDir);
                string newDir = Path.GetDirectoryName(newFullPath);
                if (!Directory.Exists(newDir)) Directory.CreateDirectory(newDir);
                File.Move(file, newFullPath);
            }
        }

        private void DeleteEmptyAndInactiveFolders(List<string> foldersNotActive)
        {
            // delete all empty and not active folders
            List<string> rssSavePaths = GetRssSavePaths();
            foreach (string dir in foldersNotActive)
            {
                bool containsFiles = false;
                if (torrentDir.Equals(dir, StringComparison.InvariantCultureIgnoreCase) ||
                    rssSavePaths.Contains(dir, StringComparer.OrdinalIgnoreCase) || IsSymbolic(dir)) continue;
                string[] files = Directory.GetFiles(dir, "*.*", SearchOption.AllDirectories);
                foreach (string fileOrDir in files)
                {
                    if (File.Exists(fileOrDir) && !IsDirectory(fileOrDir))
                    {
                        containsFiles = true;
                        break;
                    }
                }
                if (Directory.Exists(dir) && (!containsFiles))
                {
                    Directory.Delete(dir, true);
                }
            }
        }

        private bool IsSymbolic(string path)
        {
            FileInfo pathInfo = new FileInfo(path);
            bool isSym = pathInfo.Attributes.HasFlag(FileAttributes.ReparsePoint);
            if (isSym)
            {
                return true;
            }

            string[] parts = path.Split(new char[] { '\\' });
            int levels = parts.Length;
            string dir = parts[0];
            for (int i = 1; i < levels; i++)
            {
                FileInfo pathInf = new FileInfo(dir);
                bool isSy = pathInf.Attributes.HasFlag(FileAttributes.ReparsePoint);
                if (isSy)
                {
                    return true;
                }

                dir += @"\" + parts[i];
            }
            return isSym;
        }

        private static bool IsDirectory(string path)
        {
            FileInfo pathInfo = new FileInfo(path);
            return pathInfo.Attributes.HasFlag(FileAttributes.Directory);
        }

        private static List<string> GetRssSavePaths()
        {
            List<string> savePaths = new List<string>();
            // read rss/download_rules.xml get all savepaths
            var fileName = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
                @"qBittorrent\rss\download_rules.json");
            if (!File.Exists(fileName)) return savePaths;
            foreach (string line in File.ReadAllLines(fileName))
            {
                string data = line.Replace('"', ' ').Trim();
                if (data.StartsWith("savePath : ", StringComparison.OrdinalIgnoreCase))
                {
                    data = data.Remove(0, 11).Replace('/', '\\').Trim(new char[] { ' ', ',' });
                    savePaths.Add(data);
                }
            }
            return savePaths;
        }

        private List<string> GetFoldersOKtoMove(List<string> activeTorrentFolders, List<string> allDownloadedFolders)
        {
            List<string> okToMoveFolders = new List<string>();
            List<string> foldersToNotMove = GetRssSavePaths();
            bool isActive;
            foreach (string downloadedFolder in allDownloadedFolders)
            {
                isActive = false;
                foreach (string torrentFolder in activeTorrentFolders)
                {
                    string torrentFolderOnDisk = Path.Combine(downloadDir, torrentFolder);
                    if (downloadedFolder.Equals(torrentFolderOnDisk, StringComparison.OrdinalIgnoreCase))
                    {
                        isActive = true;
                        break;
                    }
                }
                if (!isActive && !foldersToNotMove.Contains(downloadedFolder, StringComparer.OrdinalIgnoreCase))
                {
                    okToMoveFolders.Add(downloadedFolder);
                }
            }
            return okToMoveFolders;
        }

        private List<string> GetFilesOKtoMove(List<string> activeTorrentFiles, List<string> allDownloadedFiles)
        {
            List<string> okToMoveFiles = new List<string>();
            bool isActive;
            foreach (string downloadedFile in allDownloadedFiles)
            {
                isActive = false;
                foreach (string torrentFile in activeTorrentFiles)
                {
                    string fileOnDisk = Path.GetFileName(downloadedFile);
                    if (fileOnDisk.Equals(torrentFile, StringComparison.OrdinalIgnoreCase))
                    {
                        isActive = true;
                        break;
                    }
                }
                if (!isActive)
                {
                    okToMoveFiles.Add(downloadedFile);
                }
            }
            return okToMoveFiles;
        }

        private List<string> GetActiveTorrentFiles()
        {
            List<string> torrentFiles = new List<string>();
            string[] allFiles = Directory.GetFiles(torrentDir, "*.torrent");
            foreach (string file in allFiles)
            {
                BencodeParser parser = new BencodeParser();
                Torrent torrent = parser.Parse<Torrent>(file);
                if (torrent.File is null)
                {
                    MultiFileInfoList files = torrent.Files;
                    if (torrent.Files.DirectoryName != string.Empty)
                    {
                        continue;
                    }
                    else
                    {
                        foreach (MultiFileInfo fileInfo in files)
                        {
                            torrentFiles.Add(fileInfo.FileName);
                        }
                    }
                }
                else
                {
                    SingleFileInfo singleFile = torrent.File;
                    torrentFiles.Add(singleFile.FileName);
                }
            }
            return torrentFiles;
        }


        private List<string> GetActiveTorrentFolders()
        {
            List<string> torrentFolders = new List<string>();
            foreach (string file in Directory.GetFiles(torrentDir, "*.torrent"))
            {
                BencodeParser parser = new BencodeParser();
                Torrent torrent = parser.Parse<Torrent>(file);
                if (torrent.File is null)
                {
                    if (torrent.Files.DirectoryName != string.Empty)
                    {
                        torrentFolders.Add(torrent.Files.DirectoryName);
                    }
                }
            }
            return torrentFolders;
        }
    }
}
