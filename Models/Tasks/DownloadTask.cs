﻿using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Threading.Tasks;

namespace SMBClient.Models
{
    public class DownloadTask : TaskBase
    {
        private readonly IEnumerable<SMBItem> items;
        private readonly string dstPath;

        public DownloadTask(SMBFileShare smbFileShare, IEnumerable<SMBItem> items, string dstPath) : base(smbFileShare)
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
                    AddOperation(new DownloadFileOperation(item, Path.Combine(dstPath, item.Name)));
                }
            }
        }

        public void CreateOperationsForDirectory(SMBItem smbItem, string dstPath)
        {
            AddOperation(new CreateDownloadDirectoryOperation(dstPath));

            foreach (var item in smbFileShare.RetrieveItems(smbItem))
            {
                if (item.IsDirectory)
                {
                    CreateOperationsForDirectory(item, Path.Combine(dstPath, item.Name));
                }
                else
                {
                    AddOperation(new DownloadFileOperation(item, Path.Combine(dstPath, item.Name)));
                }
            }
        }
    }
}
