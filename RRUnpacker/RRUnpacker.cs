using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;
using System.Buffers;
using System.IO.Compression;

using Syroot.BinaryData;

using CommunityToolkit.HighPerformance.Buffers;
using CommunityToolkit.HighPerformance;

using RRUnpacker.TOC;
using RRUnpacker.Decompressors;
using RRUnpacker.Entities;

namespace RRUnpacker;

public class RRUnpacker<TToC> where TToC : ITableOfContents
{
	public TToC TOC { get; set; }

	private string _outputPath;
	private string _inputPath;

	public RRUnpacker(string inputPath, string outputPath)
	{
		_inputPath = inputPath;
		_outputPath = outputPath;
	}

	public void SetToc(TToC toc)
	{
		TOC = toc;
	}

	public void ExtractContainers()
	{
		Console.WriteLine("Starting to unpack..");

		using var fs = File.Open(_inputPath, FileMode.Open);
		Directory.CreateDirectory(_outputPath);

		int i = 0;

		string fileName = Path.GetFileNameWithoutExtension(_inputPath);
		List<RRContainerDescriptor> containerInfo = TOC.GetContainers(fileName);
		List<RRFileDescriptor> fileInfo = TOC.GetFiles(fileName);

		foreach (var container in containerInfo)
		{
			string containerDir = Path.Combine(_outputPath, container.Name);
			Directory.CreateDirectory(containerDir);

            long containerOffset = container.SectorOffset * RRConsts.BlockSize;
            fs.Position = containerOffset;

			// Grab container data
			MemoryOwner<byte> containerData;

            if (container.CompressionType != RRCompressionType.None)
			{
                Console.WriteLine($"[{i + 1}/{containerInfo.Count}] {container.Name} [{container.CompressionType}] @ 0x{containerOffset:X}, {container.CompressionType}, compSize: 0x{container.CompressedSize:X}, decSize: 0x{container.UncompressedSize:X}");

                long compOff = RRConsts.BlockSize - (container.CompressedSize % RRConsts.BlockSize);
				fs.Position += compOff;

				containerData = MemoryOwner<byte>.Allocate((int)container.UncompressedSize);

				switch (container.CompressionType)
				{
					case RRCompressionType.RRLZ:
						RRLZDecompressor.Decompress(fs, container.CompressedSize, containerData.Span, container.UncompressedSize);
						break;

					case RRCompressionType.Zlib:
						{
							using MemoryOwner<byte> compressedSpan = MemoryOwner<byte>.Allocate((int)container.CompressedSize);
							fs.ReadExactly(compressedSpan.Span);

							using var memStream = compressedSpan.AsStream();
							ZLibStream zlibStream = new ZLibStream(memStream, CompressionMode.Decompress);
							zlibStream.ReadExactly(containerData.Span);
						}
						break;
				}
				
			}
			else
			{
                uint containerSize = container.SectorSize * RRConsts.BlockSize;
				fs.Position = (long)container.SectorOffset * RRConsts.BlockSize;

                Console.WriteLine($"[{i + 1}/{containerInfo.Count}] {container.Name} @ raw 0x{containerOffset:X}, size: 0x{containerSize:X}");

                containerData = MemoryOwner<byte>.Allocate((int)containerSize);
				fs.ReadExactly(containerData.Span);
			}

			using var ms = containerData.AsStream();
			using var bs = new BinaryStream(ms);

			// Extract files
			for (int index = container.FileDescriptorEntryIndexStart; index < container.FileDescriptorEntryIndexEnd; index++)
			{
				RRFileDescriptor file = fileInfo[index];
				Console.WriteLine($"- {container.Name} @ 0x{containerOffset:X}[0x{file.OffsetWithinContainer:X}..0x{file.OffsetWithinContainer+file.FileSizeWithinContainer:X}] -> {file.Name}");

				bs.Position = file.OffsetWithinContainer;

				using MemoryOwner<byte> outputFileData = MemoryOwner<byte>.Allocate((int)file.FileSizeWithinContainer);
				bs.ReadExactly(outputFileData.Span);

				string outputFileName = Path.Combine(containerDir, file.Name);
				Directory.CreateDirectory(Path.GetDirectoryName(outputFileName)!);

                File.WriteAllBytes(outputFileName, outputFileData.Span);
			}

			containerData.Dispose();
			i++;
		}

		Console.WriteLine($"Done. Files have been extracted at: {_outputPath}");
	}
}
