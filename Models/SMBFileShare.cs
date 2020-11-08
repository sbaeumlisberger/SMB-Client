using SMBLibrary;
using SMBLibrary.Client;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using FileAttributes = SMBLibrary.FileAttributes;

namespace SMBClient.Models
{
    public class SMBFileShare : IDisposable
    {
        public uint MaxReadSize => client.MaxReadSize;
        public uint MaxWriteSize => client.MaxWriteSize;

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
        }

        public List<SMBItem> RetrieveItems(SMBItem smbItem)
        {
            return RetrieveItems(smbItem.Path);
        }

        public List<SMBItem> RetrieveItems(string path)
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

        public SMBReadStream OpenRead(string path)
        {
            return new SMBReadStream(fileStore, (int)client.MaxReadSize, path);
        }

        public SMBWriteStream OpenWrite(string path)
        {
            return new SMBWriteStream(fileStore, (int)client.MaxWriteSize, path);
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

        public void Rename(string path, string newName)
        {
            NTStatus status = fileStore.CreateFile(out object fileHandle, out FileStatus fileStatus, path, AccessMask.GENERIC_ALL, FileAttributes.Normal, ShareAccess.Read, CreateDisposition.FILE_OPEN, 0, null);
            if (status != NTStatus.STATUS_SUCCESS)
            {
                throw new IOException($"Failed to rename file: {status}");
            }
            FileRenameInformationType2 fileRenameInformation = new FileRenameInformationType2();
            string parentPath = Path.GetDirectoryName(path) ?? string.Empty;
            fileRenameInformation.FileName = Path.Combine(parentPath, newName);
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


        // TODO ?
        public void DeleteDirectory(string path)
        {
            foreach (var item in RetrieveItems(path))
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
