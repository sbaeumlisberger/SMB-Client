using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Threading.Tasks;

namespace SMBClient.Models
{
    public class DownloadTask
    {
        private readonly SMBFileShare smbFileShare;
        private readonly bool isDirectory;
        private readonly SMBItem smbItem;
        private readonly string dstPath;

        private readonly List<IOperation> operations = new List<IOperation>();

        private long progressTarget = 0;

        public DownloadTask(SMBFileShare smbFileShare, bool isDirectory, SMBItem smbItem, string dstPath)
        {
            this.smbFileShare = smbFileShare;
            this.isDirectory = isDirectory;
            this.smbItem = smbItem;
            this.dstPath = dstPath;
        }

        public async Task ExecuteAsync(Progress progress)
        {
            await Task.Run(() =>
            {
                CreateOperations();
                progress.Initialize(progressTarget);
                foreach (var operation in operations)
                {
                    operation.Execute(smbFileShare, progress);
                }
            });
        }

        private void CreateOperations()
        {
            if (isDirectory)
            {
                CreateOperations(smbItem, dstPath);
            }
            else 
            {
                AddOperation(new DownloadFileOperation(smbItem, dstPath));
            } 
        }

        public void CreateOperations(SMBItem smbItem, string dstPath)
        {
            AddOperation(new CreateDownloadDirectoryOperation(dstPath));

            foreach (var item in smbFileShare.GetFilesAndDirectories(smbItem))
            {
                if (item.IsDirectory)
                {
                    CreateOperations(item, Path.Combine(dstPath, item.Name));
                }
                else
                {
                    AddOperation(new DownloadFileOperation(item, Path.Combine(dstPath, item.Name)));
                }
            }
        }

        private void AddOperation(IOperation operation) 
        {
            operations.Add(operation);
            progressTarget += operation.ProgressAmount;
        }
    }
}
