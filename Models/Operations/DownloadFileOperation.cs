using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Threading.Tasks;

namespace SMBClient.Models
{
    public class DownloadFileOperation : IOperation
    {
        public SMBItem SMBItem;
        public string DstPath;

        public long ProgressAmount => SMBItem.Size + 1;

        public DownloadFileOperation(SMBItem smbItem, string dstPath)
        {
            SMBItem = smbItem;
            DstPath = dstPath;
        }
    
        public void Execute(SMBFileShare smbFileShare, Progress progress) 
        {
            using (var dstStream = File.OpenWrite(DstPath))
            {
                dstStream.SetLength(0);
                dstStream.Position = 0;
                smbFileShare.ReadFile(SMBItem.Path, dstStream, progress);
            }
            progress.Report(1);
        }
    }
}
