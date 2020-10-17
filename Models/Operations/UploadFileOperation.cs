using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace SMBClient.Models
{
    public class UploadFileOperation : IOperation
    {
        public FileInfo File { get; }

        public string DstPath { get; }

        public long ProgressAmount => File.Length + 1;

        public UploadFileOperation(FileInfo file, string dstPath)
        {
            File = file;
            DstPath = dstPath;
        }

        public void Execute(SMBFileShare smbFileShare, Progress progress)
        {
            using (Stream fileStream = File.OpenRead())
            {
                smbFileShare.CreateFile(DstPath, fileStream, progress);
                progress.Report(1);
            }
        }
    }
}
