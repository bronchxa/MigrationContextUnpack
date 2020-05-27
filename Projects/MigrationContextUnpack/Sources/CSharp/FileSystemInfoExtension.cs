using System;
using System.IO;

namespace MigrationContextUnpack.Sources.CSharp
{
    public static class FileSystemInfoExtension
    {
        public class FileSystemItemException : CustomException
        {
            public FileSystemItemException(string message) : base(message) {}
        }
        public class FileSystemItemNotFoundException : FileSystemItemException
        {
            public FileSystemItemNotFoundException(FileSystemInfo item)
                : base(string.Format("{0}{1} not found", (item == null ? "FileSystem item" : item.IsDirectory() ? "Folder" : "File"), (item == null ? string.Empty : item.FullName))){ }
            public FileSystemItemNotFoundException(string message) : base(message) { }
        }
        public class FileSystemItemNullException : FileSystemItemException
        {
            public FileSystemItemNullException(): base("FileSystem item cannot be null") { }
        }
        public class FileSystemItemNoItem : FileSystemItemException
        {
            public FileSystemItemNoItem() : base ("No FileSystemInfo object provided") { }
        }

        public static bool IsDirectory(this FileSystemInfo fileSystemItem)
        {
            if (fileSystemItem == null) throw new ArgumentNullException("FileSystem item cannot be null");
            return fileSystemItem.Attributes.HasFlag(FileAttributes.Directory);
        }

        public static string PathWithoutDrive(this FileSystemInfo fileSystemInfo)
        {
            return (fileSystemInfo.FullName.Length > 3) ? fileSystemInfo.FullName.Substring(3) : fileSystemInfo.FullName;
        }
    }
}
