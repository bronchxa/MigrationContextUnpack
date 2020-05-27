using MigrationContextUnpack.Sources.Tracing;
using System;
using System.IO;
using System.Text;

namespace MigrationContextUnpack.Sources.Windows
{
    public class Filesystem
    {
        private static void folderCopyRecurse(DirectoryInfo srcFolder, DirectoryInfo destFolder)
        {
            Directory.CreateDirectory(destFolder.FullName);

            foreach(FileInfo fileInfo in srcFolder.GetFiles())
            {
                fileInfo.CopyTo(Path.Combine(destFolder.FullName, fileInfo.Name), true);
            }

            foreach(DirectoryInfo directoryInfo in srcFolder.GetDirectories())
            {
                DirectoryInfo subFolder = destFolder.CreateSubdirectory(directoryInfo.Name);
                folderCopyRecurse(directoryInfo, subFolder);
            }
        }

        public static void CreateFolder(string folder, bool deleteFirst= false) 
        {
            if (deleteFirst && Directory.Exists(folder))
            {
                Logger.Log("Removing {0} folder.", folder, LogLevel.Debug);
                Directory.Delete(folder, true);
                if (Directory.Exists(folder)) throw new ApplicationException("Could not remove " + folder + " folder!");
                Logger.Log("Done.", folder, LogLevel.Debug);
            }
            if (!Directory.Exists(folder))
            {
                Logger.Log("Creating {0} folder.", folder, LogLevel.Debug);
                Directory.CreateDirectory(folder);
                if (!Directory.Exists(folder)) throw new ApplicationException("Could not create " + folder + " folder!");
                Logger.Log("Done", LogLevel.Debug);
                return;
            }
            Logger.Log("Noting to do, folder {0} already exists.", folder);
        }
        public static void FolderCopy(string srcFolder, string destFolder, bool removefirst = false) 
        {
            if (!Directory.Exists(srcFolder)) throw new DirectoryNotFoundException("Folder " + srcFolder + " not found!");

            if (Directory.Exists(destFolder) && removefirst)
            {
                Logger.Log("Removing {0} folder first.", LogLevel.Debug);
                Directory.Delete(destFolder, true);
                if (Directory.Exists(destFolder)) throw new ApplicationException("Could not remove " + destFolder + " folder!");
                Logger.Log("Done.", LogLevel.Debug);
            }

            if (!Directory.Exists(destFolder))
            {
                Logger.Log("Destination folder {0} not found, creating it.", LogLevel.Debug);
                Directory.CreateDirectory(destFolder);
                if (!Directory.Exists(destFolder)) throw new ApplicationException("Could not create " + destFolder + " folder!");
                Logger.Log("Done.", LogLevel.Debug);

            }

            Logger.Log("Copying {0} folder to {1}", srcFolder, destFolder, LogLevel.Debug);
            Filesystem.folderCopyRecurse(new DirectoryInfo(srcFolder), new DirectoryInfo(destFolder));
            Logger.Log("Done.", LogLevel.Debug);
        }

        public static void UpdateTextFile(string filePath, string contents, bool showContents = false)
        {
            Logger.Log("Updating {0} file.", filePath, LogLevel.Debug);
            File.AppendAllText(filePath, contents, Encoding.UTF8);
            if (!File.Exists(filePath)) throw new ApplicationException("Could not create " + filePath + " file!");
            if (!showContents || string.IsNullOrEmpty(contents))
            {
                Logger.Log("Done.");
                return;
            }
            foreach (string line in contents.Split(Environment.NewLine.ToCharArray(), StringSplitOptions.RemoveEmptyEntries))
            {
                Logger.Log("   {0}", line, LogLevel.Debug);
            }
        }
        public static void CreateTextFile(string filePath, string contents, bool showContents = false)
        {
            if (File.Exists(filePath))
            {
                Logger.Log("Deleting {0} file.", filePath, LogLevel.Debug);
                File.Delete(filePath);
                if (File.Exists(filePath)) throw new ApplicationException("Could not delete " + filePath + " file!");
                Logger.Log("Done.", LogLevel.Debug);
            }
            UpdateTextFile(filePath, contents, showContents);
        }
        public static void CopyFile(string srcFile, string destFile, bool overwrite = true)
        {
            if (File.Exists(destFile) && !overwrite) return;
            if (!File.Exists(srcFile)) throw new FileNotFoundException("File to copy " + srcFile + " not found!");
            Logger.Log("Copying {0} file to {1} (overwrite: {2})", srcFile, destFile, overwrite.ToString(), LogLevel.Debug);
            File.Copy(srcFile, destFile, overwrite);
            if (!File.Exists(destFile)) throw new FileNotFoundException("Could not copy file!");
        }
    }
}
