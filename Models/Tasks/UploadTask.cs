using SMBClient.Utils;
using SMBLibrary.Client;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Threading.Tasks;

namespace SMBClient.Models
{
    public class UploadTask : TaskBase
    {
        private readonly IEnumerable<FileSystemInfo> fileSystemItems;
        private readonly string dstPath;

        public UploadTask(SMBFileShare smbFileShare, IEnumerable<FileSystemInfo> fileSystemItems, string dstPath) : base(smbFileShare)
        {
            this.fileSystemItems = fileSystemItems;
            this.dstPath = dstPath;
        }
        protected override void CreateOperations()
        {
            foreach (var fileSystemItem in fileSystemItems)
            {
                if (fileSystemItem is DirectoryInfo srcDirectory)
                {
                    CreateOperationsForDirectory(srcDirectory, Path.Combine(dstPath, fileSystemItem.Name));
                }
                else if (fileSystemItem is FileInfo srcFile)
                {
                    AddOperation(new UploadFileOperation(srcFile, Path.Combine(dstPath, fileSystemItem.Name)));
                }
                else
                {
                    throw new InvalidOperationException();
                }
            }
        }

        private void CreateOperationsForDirectory(DirectoryInfo srcDirectory, string dstPath)
        {
            AddOperation(new CreateDirectoryOperation(dstPath));

            foreach (var directory in srcDirectory.EnumerateDirectories())
            {
                CreateOperationsForDirectory(directory, Path.Combine(dstPath, directory.Name));
            }
            foreach (var file in srcDirectory.EnumerateFiles())
            {
                AddOperation(new UploadFileOperation(file, Path.Combine(dstPath, file.Name)));
            }
        }
    }
}
