using SMBClient.Utils;
using System;
using System.Collections.Generic;
using System.Text;

namespace SMBClient.Models
{
    public class CopyFileOperation : IOperation
    {
        public SMBItem Item { get; }
        public string DstPath { get; }

        public long ProgressAmount => Item.Size + 1;

        public CopyFileOperation(SMBItem item, string dstPath)
        {
            Item = item;
            DstPath = dstPath;
        }

        public void Execute(SMBFileShare smbFileShare, Progress progress)
        {
            using var srcStream = smbFileShare.OpenRead(Item.Path);
            using var dstStream = smbFileShare.OpenWrite(DstPath);
            int bufferSize = (int)Math.Min(smbFileShare.MaxReadSize, smbFileShare.MaxWriteSize);
            srcStream.CopyTo(dstStream, bufferSize, progress);
            progress.Report(1);
        }
    }
}
