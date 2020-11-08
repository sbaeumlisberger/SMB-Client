using SMBLibrary;
using SMBLibrary.Client;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using FileAttributes = SMBLibrary.FileAttributes;

namespace SMBClient.Models
{
    public class SMBReadStream : Stream, IDisposable
    {
        public override bool CanRead => true;

        public override bool CanSeek => true;

        public override bool CanWrite => false;

        public override long Length => throw new NotSupportedException();

        public override long Position { get; set; }

        private readonly ISMBFileStore fileStore;
        private readonly int maxReadSize;
        private readonly object fileHandle;

        public SMBReadStream(ISMBFileStore fileStore, int maxReadSize, string path) 
        {
            this.fileStore = fileStore;
            this.maxReadSize = maxReadSize;

            NTStatus status = fileStore.CreateFile(out fileHandle, out FileStatus _, path, AccessMask.GENERIC_READ | AccessMask.SYNCHRONIZE, FileAttributes.Normal, ShareAccess.Read, CreateDisposition.FILE_OPEN, CreateOptions.FILE_NON_DIRECTORY_FILE | CreateOptions.FILE_SYNCHRONOUS_IO_ALERT, null);

            if (status != NTStatus.STATUS_SUCCESS)
            {
                throw new IOException($"Failed to read from file: {status}");
            }
        }

        void IDisposable.Dispose() 
        {
            fileStore.CloseFile(fileHandle);
        }

        public override void Flush()
        {
            throw new NotSupportedException();
        }

        public override int Read(byte[] buffer, int offset, int count)
        {
            int totalNumberOfBytesRead = 0;
            while (totalNumberOfBytesRead < count)
            {
                int numberOfBytesToRead = Math.Min(count - totalNumberOfBytesRead, maxReadSize);
                NTStatus status = fileStore.ReadFile(out byte[] smbBuffer, fileHandle, Position, numberOfBytesToRead);
                if (status != NTStatus.STATUS_SUCCESS && status != NTStatus.STATUS_END_OF_FILE)
                {
                    throw new IOException($"Failed to read from file: {status}");
                }
                if (status == NTStatus.STATUS_END_OF_FILE || smbBuffer.Length == 0)
                {
                    break;
                }
                Array.Copy(smbBuffer, 0, buffer, offset + totalNumberOfBytesRead, smbBuffer.Length);
                totalNumberOfBytesRead += smbBuffer.Length;
                Position += smbBuffer.Length;            
            }
            return totalNumberOfBytesRead;
        }

        public override long Seek(long offset, SeekOrigin origin)
        {
            switch (origin) 
            {
                case SeekOrigin.Begin:
                    return Position = offset;
                case SeekOrigin.Current:
                    return Position += offset;
                case SeekOrigin.End:
                    throw new NotSupportedException();
            }
            throw new NotSupportedException();
        }

        public override void SetLength(long value)
        {
            throw new NotSupportedException();
        }

        public override void Write(byte[] buffer, int offset, int count)
        {
            throw new NotSupportedException();
        }
    }
}
