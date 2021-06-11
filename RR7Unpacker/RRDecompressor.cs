using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;

using Syroot.BinaryData;

namespace RR7Unpacker
{
    public static class RRDecompressor
    {
		/// <summary>
		/// Ridge Racer 7 Decompression Algo
		/// </summary>
		/// <param name="stream"></param>
		/// <param name="compressedSize"></param>
		/// <param name="output"></param>
		/// <param name="uncompressedSize"></param>
		/// <returns></returns>
		public static bool Decompress(Stream stream, uint compressedSize, Span<byte> output, uint uncompressedSize)
		{
			/* if (start > end)
				return false; */

			var inputStream = new BinaryStream(stream);
			long startPos = inputStream.Position;

			Span<byte> outputBufferStream = output;

			int curOffset = 0;

			while (true)
			{
				byte b1 = inputStream.Read1Byte();
				byte b2 = inputStream.Read1Byte();

				uint val = (uint)((b2 << 8) | b1);
				if (val < 0xFF00)
				{
					int size = (b1 & 0x1f) + 0x3;

					for (int i = 0; i < size; i++)
					{
						int endOffset = (int)(val >> 5) + 1;
						int dPos = curOffset - endOffset;
						outputBufferStream[i] = output[dPos + i];
					}

					curOffset += size;
					outputBufferStream = outputBufferStream.Slice(size);
				}
				else
				{
					int size = b1 + 1;

					// Memcpy
					inputStream.Read(outputBufferStream.Slice(0, size));

					outputBufferStream = outputBufferStream.Slice(size);
					curOffset += size;
				}

				if (inputStream.Position == startPos + compressedSize && outputBufferStream.IsEmpty)
					return true;
			}
		}
	}
}
