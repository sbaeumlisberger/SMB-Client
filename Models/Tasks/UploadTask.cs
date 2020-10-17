using SMBClient.Utils;
using SMBLibrary.Client;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Threading.Tasks;

namespace SMBClient.Models
{
    public class UploadTask
    {
        private readonly SMBFileShare smbFileShare;
        private readonly FileSystemInfo fileSystemInfo;
        private readonly string dstPath;

        private readonly List<IOperation> operations = new List<IOperation>();

        private long progressTarget = 0;

        public UploadTask(SMBFileShare smbFileShare, FileSystemInfo fileSystemInfo, string dstPath)
        {
            this.smbFileShare = smbFileShare;
            this.fileSystemInfo = fileSystemInfo;
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
            }).ConfigureAwait(false);
        }

        private void CreateOperations()
        {
            if (fileSystemInfo is DirectoryInfo srcDirectory)
            {
                CreateOperations(srcDirectory, dstPath);
            }
            else if (fileSystemInfo is FileInfo srcFile)
            {
                AddOperation(new UploadFileOperation(srcFile, dstPath));
            }
            else
            {
                throw new InvalidOperationException();
            }
        }

        private void CreateOperations(DirectoryInfo srcDirectory, string dstPath)
        {
            AddOperation(new CreateDirectoryOperation(dstPath));
            foreach (var directory in srcDirectory.EnumerateDirectories())
            {
                CreateOperations(directory, Path.Combine(dstPath, directory.Name));
            }
            foreach (var file in srcDirectory.EnumerateFiles())
            {
                AddOperation(new UploadFileOperation(file, Path.Combine(dstPath, file.Name)));
            }
        }

        private void AddOperation(IOperation operation)
        {
            operations.Add(operation);
            progressTarget += operation.ProgressAmount;
        }

    }
}
