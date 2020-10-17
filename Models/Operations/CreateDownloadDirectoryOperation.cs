using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace SMBClient.Models
{
    public class CreateDownloadDirectoryOperation : IOperation
    {
        public string Path { get; }

        public long ProgressAmount  => 1;

        public CreateDownloadDirectoryOperation(string path)
        {
            Path = path;
        }

        public void Execute(SMBFileShare smbFileShare, Progress progress)
        {
            Directory.CreateDirectory(Path);
            progress.Report(ProgressAmount);
        }
    }
}
