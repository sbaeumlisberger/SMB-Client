using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Threading.Tasks;

namespace SMBClient.Models
{
    public class CopyTask : TaskBase
    {
        private readonly IEnumerable<SMBItem> items;
        private readonly string dstPath;

        public CopyTask(SMBFileShare smbFileShare, IEnumerable<SMBItem> items, string dstPath) : base(smbFileShare)
        {
            this.items = items;
            this.dstPath = dstPath;
        }

        protected override void CreateOperations()
        {
            foreach (var item in items)
            {
                if (item.IsDirectory)
                {
                    CreateOperationsForDirectory(item, Path.Combine(dstPath, item.Name));
                }
                else
                {
                    AddOperation(new CopyFileOperation(item, Path.Combine(dstPath, item.Name)));
                }
            }
        }

        private void CreateOperationsForDirectory(SMBItem item, string dstPath)
        {
            AddOperation(new CreateDirectoryOperation(dstPath));

            foreach (var child in smbFileShare.RetrieveItems(item))
            {
                if (item.IsDirectory)
                {
                    CreateOperationsForDirectory(item, Path.Combine(dstPath, item.Name));
                }
                else
                {
                    AddOperation(new CopyFileOperation(child, Path.Combine(dstPath, item.Name)));
                }
            }
        }
    }
}
