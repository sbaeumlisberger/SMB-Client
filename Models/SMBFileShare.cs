using SMBClient.Utils;
using SMBLibrary;
using SMBLibrary.Client;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net;
using System.Threading;
using System.Threading.Tasks;
using FileAttributes = SMBLibrary.FileAttributes;

namespace SMBClient.Models
{
    public class SMBFileShare : IDisposable
    {
        private readonly SMB2Client client;
        private readonly ISMBFileStore fileStore;

        private SMBFileShare(SMB2Client client, ISMBFileStore fileStore)
        {
            this.client = client;
            this.fileStore = fileStore;
        }

        public static async Task<SMBFileShare> ConnectAsync(IPAddress address, string share, string username = "", string password = "")
        {
            return await Task.Run(() =>
            {
                SMB2Client client = new SMB2Client();
                bool isConnected = client.Connect(address, SMBTransportType.DirectTCPTransport);
                if (!isConnected)
                {
                    throw new Exception($"Could not connect to {address}");
                }

                NTStatus status = client.Login(string.Empty, username, password);
                if (status != NTStatus.STATUS_SUCCESS)
                {
                    throw new Exception($"Could not login: {status}");
                }

                ISMBFileStore fileStore = client.TreeConnect(share, out status);
                if (status != NTStatus.STATUS_SUCCESS)
                {
                    throw new Exception($"Could not access share:  {status}");
                }

                return new SMBFileShare(client, fileStore);

            }).ConfigureAwait(false);
        }

        public void Dispose()
        {
            fileStore.Disconnect();
            client.Disconnect();
        }

        public List<SMBItem> GetFilesAndDirectories(SMBItem smbItem)
        {
            return GetFilesAndDirectories(smbItem.Path);
        }

        public List<SMBItem> GetFilesAndDirectories(string path)
        {
            NTStatus status = fileStore.CreateFile(out object directoryHandle, out FileStatus fileStatus, path, AccessMask.GENERIC_READ, FileAttributes.Directory, ShareAccess.Read | ShareAccess.Write, CreateDisposition.FILE_OPEN, CreateOptions.FILE_DIRECTORY_FILE, null);
            if (status != NTStatus.STATUS_SUCCESS)
            {
                throw new IOException($"Could not fetch files and directories: {status}");
            }
            fileStore.QueryDirectory(out var fileList, directoryHandle, "*", FileInformationClass.FileDirectoryInformation);
            fileStore.CloseFile(directoryHandle);
            return fileList.Cast<FileDirectoryInformation>()
                .Where(fdi => fdi.FileName.Trim('.').Length > 0)
                .Select(fdi => new SMBItem(fdi, path))
                .ToList();
        }

        public void CopyFile(string srcPath, string dstPath, Progress progress)
        {
            using (Stream memoryStream = new MemoryStream()) // TODO: copy directly
            {
                ReadFile(srcPath, memoryStream, progress);
                CreateFile(dstPath, memoryStream, progress);
            }
        }

        public void CopyDirectory(string srcPath, string dstPath, Progress progress)
        {
            CreateDirectory(dstPath);

            foreach (var item in GetFilesAndDirectories(srcPath))
            {
                if (progress.IsCanceled)
                {
                    return;
                }
                if (item.IsDirectory)
                {
                    CopyDirectory(item.Path, Path.Combine(dstPath, item.Name), progress);
                }
                else
                {
                    CopyFile(item.Path, Path.Combine(dstPath, item.Name), progress);
                }
            }
        }

        public void ReadFile(string srcPath, Stream dstStream, Progress progress)
        {
            NTStatus status = fileStore.CreateFile(out object fileHandle, out FileStatus fileStatus, srcPath, AccessMask.GENERIC_READ | AccessMask.SYNCHRONIZE, FileAttributes.Normal, ShareAccess.Read, CreateDisposition.FILE_OPEN, CreateOptions.FILE_NON_DIRECTORY_FILE | CreateOptions.FILE_SYNCHRONOUS_IO_ALERT, null);

            if (status != NTStatus.STATUS_SUCCESS)
            {
                throw new IOException($"Failed to read from file: {status}");
            }

            long totalBytesRead = 0;
            while (true)
            {
                status = fileStore.ReadFile(out byte[] data, fileHandle, totalBytesRead, (int)client.MaxReadSize);
                if (status != NTStatus.STATUS_SUCCESS && status != NTStatus.STATUS_END_OF_FILE)
                {
                    throw new IOException($"Failed to read from file: {status}");
                }
                if (status == NTStatus.STATUS_END_OF_FILE || data.Length == 0)
                {
                    break;
                }
                int bytesRead = data.Length;
                totalBytesRead += bytesRead;
                dstStream.Write(data, 0, bytesRead);
                progress.Report(bytesRead);
            }
            dstStream.Flush();
            fileStore.CloseFile(fileHandle); // TODO: finally
        }

        public void CreateFile(string dstPath, Stream content, Progress progress)
        {
            NTStatus status = fileStore.CreateFile(out object fileHandle, out FileStatus fileStatus, dstPath, AccessMask.GENERIC_WRITE | AccessMask.SYNCHRONIZE, FileAttributes.Normal, ShareAccess.None, CreateDisposition.FILE_CREATE, CreateOptions.FILE_NON_DIRECTORY_FILE | CreateOptions.FILE_SYNCHRONOUS_IO_ALERT, null);

            if (status != NTStatus.STATUS_SUCCESS)
            {
                throw new IOException($"Failed to write to file: {status}");
            }

            int totalBytesWrittenCount = 0;
            while (totalBytesWrittenCount < content.Length)
            {
                if (progress.IsCanceled)
                {
                    break;
                }
                int bytesToWriteCount = (int)Math.Min(client.MaxWriteSize, content.Length - totalBytesWrittenCount);
                byte[] bytesToWrite = new byte[bytesToWriteCount];
                content.Read(bytesToWrite, 0, bytesToWriteCount);
                status = fileStore.WriteFile(out int bytesWritten, fileHandle, totalBytesWrittenCount, bytesToWrite);
                totalBytesWrittenCount += bytesWritten;
                progress.Report(bytesWritten);
                if (status != NTStatus.STATUS_SUCCESS)
                {
                    throw new IOException($"Failed to write to file: {status}");
                }
            }
            fileStore.CloseFile(fileHandle); // TODO: finally
        }

        public void CreateDirectory(string path)
        {
            NTStatus status = fileStore.CreateFile(out object fileHandle, out FileStatus fileStatus, path, AccessMask.GENERIC_WRITE | AccessMask.SYNCHRONIZE, FileAttributes.Directory, ShareAccess.None, CreateDisposition.FILE_CREATE, CreateOptions.FILE_DIRECTORY_FILE | CreateOptions.FILE_SYNCHRONOUS_IO_ALERT, null);
            if (status != NTStatus.STATUS_SUCCESS)
            {
                throw new IOException($"Failed to create directory: {status}");
            }
            fileStore.CloseFile(fileHandle);
        }

        public void Rename(string path, string newPath)
        {
            NTStatus status = fileStore.CreateFile(out object fileHandle, out FileStatus fileStatus, path, AccessMask.GENERIC_ALL, FileAttributes.Normal, ShareAccess.Read, CreateDisposition.FILE_OPEN, 0, null);
            if (status != NTStatus.STATUS_SUCCESS)
            {
                throw new IOException($"Failed to rename file: {status}");
            }
            FileRenameInformationType2 fileRenameInformation = new FileRenameInformationType2();
            fileRenameInformation.FileName = newPath;
            fileRenameInformation.ReplaceIfExists = false;
            status = fileStore.SetFileInformation(fileHandle, fileRenameInformation);
            fileStore.CloseFile(fileHandle);
            if (status != NTStatus.STATUS_SUCCESS)
            {
                throw new IOException($"Failed to rename file: {status}");
            }
        }

        public void DeleteFile(string path)
        {
            Debug.WriteLine("Delete " + path);
            //if (!path.StartsWith("sebastian")) throw new Exception();

            NTStatus status = fileStore.CreateFile(out object fileHandle, out FileStatus fileStatus, path, AccessMask.GENERIC_WRITE | AccessMask.DELETE | AccessMask.SYNCHRONIZE, FileAttributes.Normal, ShareAccess.None, CreateDisposition.FILE_OPEN, 0, null);

            if (status != NTStatus.STATUS_SUCCESS)
            {
                throw new IOException($"Failed to delete file: {status}");
            }

            FileDispositionInformation fileDispositionInformation = new FileDispositionInformation();
            fileDispositionInformation.DeletePending = true;
            status = fileStore.SetFileInformation(fileHandle, fileDispositionInformation);

            fileStore.CloseFile(fileHandle);

            if (status != NTStatus.STATUS_SUCCESS)
            {
                throw new IOException($"Failed to delete file: {status}");
            }
        }

        public void DeleteDirectory(string path)
        {
            foreach (var item in GetFilesAndDirectories(path))
            {
                if (item.IsDirectory)
                {
                    DeleteDirectory(item.Path);
                }
                else
                {
                    DeleteFile(item.Path);
                }
            }
            DeleteFile(path);
        }

    }
}
