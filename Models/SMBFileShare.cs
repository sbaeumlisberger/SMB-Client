using SMBClient.Utils;
using SMBLibrary;
using SMBLibrary.Client;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net;
using System.Threading.Tasks;
using FileAttributes = SMBLibrary.FileAttributes;

namespace SMBClient.Models
{
    public class SMBFileShare
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
                    throw new Exception("Could not connect!");
                }

                NTStatus status = client.Login(string.Empty, username, password);
                if (status != NTStatus.STATUS_SUCCESS)
                {
                    throw new Exception("Could not login!");
                }

                ISMBFileStore fileStore = client.TreeConnect(share, out status);
                if (status != NTStatus.STATUS_SUCCESS)
                {
                    throw new Exception("Could not access share!");
                }

                return new SMBFileShare(client, fileStore);

            }).ConfigureAwait(false);
        }

        public List<FileDirectoryInformation> GetFilesAndDirectories(string path)
        {
            NTStatus status = fileStore.CreateFile(out object directoryHandle, out FileStatus fileStatus, path, AccessMask.GENERIC_READ, FileAttributes.Directory, ShareAccess.Read | ShareAccess.Write, CreateDisposition.FILE_OPEN, CreateOptions.FILE_DIRECTORY_FILE, null);
            if (status == NTStatus.STATUS_SUCCESS)
            {
                fileStore.QueryDirectory(out var fileList, directoryHandle, "*", FileInformationClass.FileDirectoryInformation);
                fileStore.CloseFile(directoryHandle);
                return fileList.Cast<FileDirectoryInformation>().Where(fdi => fdi.FileName.Trim('.').Length > 0).ToList();
            }
            else
            {
                Debug.WriteLine($"Could not retrieve files and directories: {status}");
                return new List<FileDirectoryInformation>();
            }
        }

        public void UploadFile(string srcPath, string dstPath)
        {
            using (Stream fileStream = File.OpenRead(srcPath))
            {
                CreateFile(dstPath, fileStream);
            }
        }

        public void UploadDirectory(string srcPath, string dstPath)
        {
            CreateDirectory(dstPath);

            foreach (var directoryPath in Directory.EnumerateDirectories(srcPath))
            {
                UploadDirectory(directoryPath, Path.Combine(dstPath, Path.GetFileName(directoryPath)));
            }
            foreach (var filePath in Directory.EnumerateFiles(srcPath))
            {
                UploadFile(filePath, Path.Combine(dstPath, Path.GetFileName(filePath)));
            }
        }

        public void CopyFile(string srcPath, string dstPath)
        {
            using (Stream memoryStream = new MemoryStream())
            {
                ReadFile(srcPath, memoryStream);
                CreateFile(dstPath, memoryStream);
            }
        }

        public void CopyDirectory(string srcPath, string dstPath)
        {
            CreateDirectory(dstPath);

            foreach (var fdi in GetFilesAndDirectories(srcPath))
            {
                if (fdi.FileAttributes.HasFlag(FileAttributes.Directory))
                {
                    CopyDirectory(Path.Combine(srcPath, fdi.FileName), Path.Combine(dstPath, fdi.FileName));
                }
                else
                {
                    CopyFile(Path.Combine(srcPath, fdi.FileName), Path.Combine(dstPath, fdi.FileName));
                }
            }
        }

        public void DownloadFile(string srcPath, string dstPath)
        {
            using (Stream dstStream = File.OpenWrite(dstPath))
            {
                ReadFile(srcPath, dstStream);
            }
        }

        public void DownloadDirectory(string srcPath, string dstPath)
        {
            Directory.CreateDirectory(dstPath);
            foreach (var fdi in GetFilesAndDirectories(srcPath))
            {
                if (fdi.FileAttributes.HasFlag(FileAttributes.Directory))
                {
                    DownloadDirectory(Path.Combine(srcPath, fdi.FileName), Path.Combine(dstPath, fdi.FileName));
                }
                else
                {
                    DownloadFile(Path.Combine(srcPath, fdi.FileName), Path.Combine(dstPath, fdi.FileName));
                }
            }
        }

        public void ReadFile(string srcPath, Stream dstStream)
        {
            dstStream.SetLength(0);
            dstStream.Position = 0;

            NTStatus status = fileStore.CreateFile(out object fileHandle, out FileStatus fileStatus, srcPath, AccessMask.GENERIC_READ | AccessMask.SYNCHRONIZE, FileAttributes.Normal, ShareAccess.Read, CreateDisposition.FILE_OPEN, CreateOptions.FILE_NON_DIRECTORY_FILE | CreateOptions.FILE_SYNCHRONOUS_IO_ALERT, null);

            if (status == NTStatus.STATUS_SUCCESS)
            {
                byte[] data;
                long bytesRead = 0;
                while (true)
                {
                    status = fileStore.ReadFile(out data, fileHandle, bytesRead, (int)client.MaxReadSize);
                    if (status != NTStatus.STATUS_SUCCESS && status != NTStatus.STATUS_END_OF_FILE)
                    {
                        throw new IOException("Failed to read from file");
                    }
                    if (status == NTStatus.STATUS_END_OF_FILE || data.Length == 0)
                    {
                        break;
                    }
                    bytesRead += data.Length;
                    dstStream.Write(data, 0, data.Length);
                }
                dstStream.Flush();
                fileStore.CloseFile(fileHandle);
            }
        }

        public void CreateFile(string dstPath, Stream content)
        {
            NTStatus status = fileStore.CreateFile(out object fileHandle, out FileStatus fileStatus, dstPath, AccessMask.GENERIC_WRITE | AccessMask.SYNCHRONIZE, FileAttributes.Normal, ShareAccess.None, CreateDisposition.FILE_CREATE, CreateOptions.FILE_NON_DIRECTORY_FILE | CreateOptions.FILE_SYNCHRONOUS_IO_ALERT, null);
            if (status == NTStatus.STATUS_SUCCESS)
            {
                byte[] bytesToWrite = ReadStream(content);
                int totalBytesWritten = 0;
                while (totalBytesWritten < bytesToWrite.Length)
                {
                    status = fileStore.WriteFile(out int bytesWritten, fileHandle, totalBytesWritten, bytesToWrite);
                    totalBytesWritten += bytesWritten;
                    if (status != NTStatus.STATUS_SUCCESS)
                    {
                        throw new IOException("Failed to write to file");
                    }
                }
                fileStore.CloseFile(fileHandle);
            }
        }

        public void CreateDirectory(string path)
        {
            NTStatus status = fileStore.CreateFile(out object fileHandle, out FileStatus fileStatus, path, AccessMask.GENERIC_WRITE | AccessMask.SYNCHRONIZE, FileAttributes.Directory, ShareAccess.None, CreateDisposition.FILE_CREATE, CreateOptions.FILE_DIRECTORY_FILE | CreateOptions.FILE_SYNCHRONOUS_IO_ALERT, null);
            if (status == NTStatus.STATUS_SUCCESS)
            {
                fileStore.CloseFile(fileHandle);
            }
        }

        public bool Rename(string path, string newPath)
        {
            NTStatus status = fileStore.CreateFile(out object fileHandle, out FileStatus fileStatus, path, AccessMask.GENERIC_ALL, FileAttributes.Normal, ShareAccess.Read, CreateDisposition.FILE_OPEN, 0, null);
            if (status == NTStatus.STATUS_SUCCESS)
            {
                FileRenameInformationType2 fileRenameInformation = new FileRenameInformationType2();
                fileRenameInformation.FileName = newPath;
                fileRenameInformation.ReplaceIfExists = false;
                status = fileStore.SetFileInformation(fileHandle, fileRenameInformation);
                bool succeeded = (status == NTStatus.STATUS_SUCCESS);
                fileStore.CloseFile(fileHandle);
                return succeeded;
            }
            return false;
        }

        public bool DeleteFile(string path)
        {
            Debug.WriteLine("Delete " + path);
            //if (!path.StartsWith("sebastian")) throw new Exception();

            NTStatus status = fileStore.CreateFile(out object fileHandle, out FileStatus fileStatus, path, AccessMask.GENERIC_WRITE | AccessMask.DELETE | AccessMask.SYNCHRONIZE, FileAttributes.Normal, ShareAccess.None, CreateDisposition.FILE_OPEN, 0, null);
            if (status == NTStatus.STATUS_SUCCESS)
            {
                FileDispositionInformation fileDispositionInformation = new FileDispositionInformation();
                fileDispositionInformation.DeletePending = true;
                status = fileStore.SetFileInformation(fileHandle, fileDispositionInformation);
                bool succeeded = (status == NTStatus.STATUS_SUCCESS);
                fileStore.CloseFile(fileHandle);
                return succeeded;
            }
            return false;
        }

        public bool DeleteDirectory(string path)
        {
            bool succeeded = true;
            foreach (var fdi in GetFilesAndDirectories(path))
            {
                if (fdi.FileAttributes.HasFlag(FileAttributes.Directory))
                {
                    succeeded &= DeleteDirectory(Path.Combine(path, fdi.FileName));
                }
                else
                {
                    succeeded &= DeleteFile(Path.Combine(path, fdi.FileName));
                }
            }
            succeeded &= DeleteFile(path);
            return succeeded;
        }

        private static byte[] ReadStream(Stream stream)
        {
            if (stream is MemoryStream memoryStream)
            {
                return memoryStream.ToArray();
            }
            using (memoryStream = new MemoryStream())
            {
                stream.CopyTo(memoryStream);
                return memoryStream.ToArray();
            }
        }

    }
}
