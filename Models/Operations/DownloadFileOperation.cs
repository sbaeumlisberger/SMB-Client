using SMBClient.Utils;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Threading.Tasks;

namespace SMBClient.Models
{
    public class DownloadFileOperation : IOperation
    {
        public SMBItem SMBItem { get; }

        public string DstPath { get; }

        public long ProgressAmount => SMBItem.Size + 1;

        public DownloadFileOperation(SMBItem smbItem, string dstPath)
        {
            if (smbItem.IsDirectory)
            {
                throw new ArgumentException("Must not be a directory", nameof(smbItem));
            }
            if (!Path.IsPathFullyQualified(dstPath)) 
            {
                throw new ArgumentException("Must be a fully qualified path", nameof(dstPath));
            }
            SMBItem = smbItem;
            DstPath = dstPath;
        }

        public void Execute(SMBFileShare smbFileShare, Progress? progress = null)
        {
            if (File.Exists(DstPath)) 
            {
                throw new IOException($"{DstPath} already exists");
            }
            int bufferSize = (int)smbFileShare.MaxReadSize;
            using var srcStream = smbFileShare.OpenRead(SMBItem.Path);
            using var dstStream = File.OpenWrite(DstPath);
            dstStream.SetLength(0);
            dstStream.Position = 0;
            srcStream.CopyTo(dstStream, bufferSize, progress);
            progress?.Report(1);
        }
    }
}
