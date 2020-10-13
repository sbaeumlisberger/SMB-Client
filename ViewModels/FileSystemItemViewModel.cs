using SMBClient.Utils;
using SMBLibrary;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using FileAttributes = SMBLibrary.FileAttributes;

namespace SMBClient.ViewModels
{
    public class FileSystemItemViewModel
    {
        public bool IsDirectory { get; }
        public string FileName { get; }
        public string FilePath { get; }
        public DateTime LastWriteTime { get; }
        public string Size { get; }

        public FileSystemItemViewModel(string parentPath, FileDirectoryInformation fdi)
        {
            IsDirectory = fdi.FileAttributes.HasFlag(FileAttributes.Directory);
            FileName = fdi.FileName;
            FilePath = Path.Combine(parentPath, fdi.FileName);
            LastWriteTime = fdi.LastWriteTime.ToLocalTime();
            Size = IsDirectory ? string.Empty : ByteSizeFormatter.Format((ulong)fdi.EndOfFile);
        }

        public FileSystemItemViewModel(bool isDirectory, string fileName, string filePath, DateTime lastWriteTime, string size)
        {
            IsDirectory = isDirectory;
            FileName = fileName;
            FilePath = filePath;
            LastWriteTime = lastWriteTime;
            Size = size;
        }
    }
}
