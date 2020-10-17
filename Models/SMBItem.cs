using SMBLibrary;
using System;
using System.Collections.Generic;
using System.Text;

namespace SMBClient.Models
{
    public class SMBItem
    {
        public string Name { get; }
        public string Path { get; }
        public bool IsDirectory { get; }
        public long Size { get; }
        public FileDirectoryInformation Info { get; }

        public SMBItem(FileDirectoryInformation fdi, string parentPath)
        {
            Name = fdi.FileName;
            Path = System.IO.Path.Combine(parentPath, fdi.FileName);
            IsDirectory = fdi.FileAttributes.HasFlag(FileAttributes.Directory);
            Size = fdi.EndOfFile;
            Info = fdi;
        }
    }
}
