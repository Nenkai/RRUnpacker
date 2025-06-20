using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;

using Syroot.BinaryData;
using System.Diagnostics.CodeAnalysis;

namespace RRUnpacker.Decompressors;

    public static class RRLZDecompressor
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

			uint val = (uint)(b2 << 8 | b1);
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
				inputStream.ReadExactly(outputBufferStream.Slice(0, size));

				outputBufferStream = outputBufferStream.Slice(size);
				curOffset += size;
			}

			if (inputStream.Position == startPos + compressedSize && outputBufferStream.IsEmpty)
				return true;
		}
	}

    public static bool DecompressWithHeader(byte[] data, [NotNullWhen(true)] out byte[]? decompressedData)
    {
		using MemoryStream inputStream = new(data);
        BinaryStream bs = new(inputStream, ByteConverter.Big);
        return DecompressWithHeader(bs, out decompressedData);
    }

    public static bool DecompressWithHeader(Stream inputStream, [NotNullWhen(true)] out byte[]? decompressedData)
	{
        BinaryStream bs = new(inputStream, ByteConverter.Big);
        uint tag = bs.ReadUInt32();

        // If you're looking for this in disassembly; look for a function that reads the first 2 bytes (since ppc instructions are only 4 bytes long)
        // It's at 8000c860 in main.dol (Go Vacation Wii Europe)
        // The overlay handler is at 8008da90
        if (tag != 0x5A3F2E00)
            throw new InvalidDataException("Not a valid compressed file.");

        uint unkBytesToSkip = bs.ReadUInt32();
        uint compressedSize = bs.ReadUInt32();
        uint uncompressedSize = bs.ReadUInt32();
        bs.Position += 0x10;
        bs.Position += unkBytesToSkip;

        byte[] outData = new byte[uncompressedSize];
        bool res = Decompress(bs, compressedSize, outData, uncompressedSize);

		decompressedData = res ? outData : null;
		return res;
    }
}
