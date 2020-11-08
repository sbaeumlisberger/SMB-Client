using SMBLibrary;
using SMBLibrary.Client;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using FileAttributes = SMBLibrary.FileAttributes;

namespace SMBClient.Models
{
    public class SMBWriteStream : Stream, IDisposable
    {
        public override bool CanRead => false;

        public override bool CanSeek => true;

        public override bool CanWrite => true;

        public override long Length => throw new NotSupportedException();

        public override long Position { get; set; }

        private readonly ISMBFileStore fileStore;
        private readonly int maxWriteSize;
        private readonly object fileHandle;    

        public SMBWriteStream(ISMBFileStore fileStore, int maxWriteSize, string path) 
        {
            this.fileStore = fileStore;
            this.maxWriteSize = maxWriteSize;

            NTStatus status = fileStore.CreateFile(out fileHandle, out FileStatus _, path, AccessMask.GENERIC_WRITE | AccessMask.SYNCHRONIZE, FileAttributes.Normal, ShareAccess.None, CreateDisposition.FILE_CREATE, CreateOptions.FILE_NON_DIRECTORY_FILE | CreateOptions.FILE_SYNCHRONOUS_IO_ALERT, null);

            if (status != NTStatus.STATUS_SUCCESS)
            {
                throw new IOException($"Failed to write to file: {status}");
            }
        }

        void IDisposable.Dispose() 
        {
            fileStore.CloseFile(fileHandle);
        }

        public override void Flush()
        {
            
        }

        public override int Read(byte[] buffer, int offset, int count)
        {
            throw new NotSupportedException();
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
            if (offset == 0 && count == buffer.Length && count < maxWriteSize)
            {
                NTStatus status = fileStore.WriteFile(out int numberOfBytesWritten, fileHandle, Position, buffer);
                if (status != NTStatus.STATUS_SUCCESS)
                {
                    throw new IOException($"Failed to write to file: {status}");
                }
            }
            else
            {
                int totalNumberOfBytesWritten = 0;
                while (totalNumberOfBytesWritten < count)
                {
                    int numberOfBytesToWrite = Math.Min(count - totalNumberOfBytesWritten, maxWriteSize);
                    byte[] bytesToWrite = new byte[numberOfBytesToWrite];
                    Array.Copy(buffer, totalNumberOfBytesWritten, bytesToWrite, 0, numberOfBytesToWrite); 
                    NTStatus status = fileStore.WriteFile(out int numberOfBytesWritten, fileHandle, Position, bytesToWrite);                               
                    if (status != NTStatus.STATUS_SUCCESS)
                    {
                        throw new IOException($"Failed to write to file: {status}");
                    }                
                    totalNumberOfBytesWritten += numberOfBytesWritten;
                    Position += numberOfBytesWritten;
                }
            }
        }
    }
}
