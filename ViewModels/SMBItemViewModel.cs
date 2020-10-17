using SMBClient.Models;
using SMBClient.Utils;
using System;

namespace SMBClient.ViewModels
{
    public class SMBItemViewModel
    {
        public bool IsDirectory { get; }
        public string FileName { get; }
        public string FilePath { get; }
        public DateTime LastWriteTime { get; }
        public string Size { get; }
        public SMBItem SMBItem { get; }

        public SMBItemViewModel(SMBItem smbItem)
        {
            IsDirectory = smbItem.IsDirectory;
            FileName = smbItem.Name;
            FilePath = smbItem.Path;
            LastWriteTime = smbItem.Info.LastWriteTime.ToLocalTime();
            Size = IsDirectory ? string.Empty : ByteSizeFormatter.Format((ulong)smbItem.Size);
            SMBItem = smbItem;
        }

        /* design time */
        public SMBItemViewModel(bool isDirectory, string fileName, string filePath, DateTime lastWriteTime, string size)
        {
            IsDirectory = isDirectory;
            FileName = fileName;
            FilePath = filePath;
            LastWriteTime = lastWriteTime;
            Size = size;
            SMBItem = null!;
        }
    }
}
