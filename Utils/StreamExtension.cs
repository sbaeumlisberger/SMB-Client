using SMBClient.Models;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace SMBClient.Utils
{
    public static class StreamExtensions
    {
		public static void CopyTo(this Stream fromStream, Stream destination, int bufferSize, Progress? progress)
		{
			byte[] buffer = new byte[bufferSize];

			while (true)
			{
				int numberOfBytesRead = fromStream.Read(buffer, 0, buffer.Length);

				if (numberOfBytesRead == 0)
				{
					return;
				}

				destination.Write(buffer, 0, numberOfBytesRead);
				progress?.Report(numberOfBytesRead);
			}
		}
	}
}
