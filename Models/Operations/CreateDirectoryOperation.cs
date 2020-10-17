using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace SMBClient.Models
{
    public class CreateDirectoryOperation : IOperation
    {
        public string Path { get; }

        public long ProgressAmount => 1;

        public CreateDirectoryOperation(string path)
        {
            Path = path;
        }

        public void Execute(SMBFileShare smbFileShare, Progress progress)
        {
            smbFileShare.CreateDirectory(Path);
            progress.Report(ProgressAmount);
        }
    }
}
