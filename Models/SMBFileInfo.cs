using System;
using System.Collections.Generic;
using System.Text;

namespace SMBClient.Models
{
    public class SMBFileInfo
    {
        public string Path { get; set; }
        public bool IsDirectory { get; set; }

        public SMBFileInfo(string path, bool isDirectory)
        {
            Path = path;
            IsDirectory = isDirectory;
        }
    }
}
