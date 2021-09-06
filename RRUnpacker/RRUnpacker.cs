using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;
using System.Buffers;

using Syroot.BinaryData;

using RRUnpacker.TOC;

namespace RRUnpacker
{
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

				Console.WriteLine($"[{i + 1}/{containerInfo.Count}] {container.Name}");

				// Grab container data
				byte[] containerData;
				if (container.Compressed)
				{
					fs.Position = (long)container.SectorOffset * RRConsts.BlockSize;
					long compOff = RRConsts.BlockSize - (container.CompressedSize % RRConsts.BlockSize);
					fs.Position += compOff;

					containerData = ArrayPool<byte>.Shared.Rent((int)container.UncompressedSize);
					Span<byte> containerDecompressed = containerData.AsSpan(0, (int)container.UncompressedSize);
					RRDecompressor.Decompress(fs, container.CompressedSize, containerDecompressed, container.UncompressedSize);
				}
				else
				{
					int containerSize = container.SectorSize * RRConsts.BlockSize;
					fs.Position = (long)container.SectorOffset * RRConsts.BlockSize;
					containerData = ArrayPool<byte>.Shared.Rent(containerSize);
					fs.Read(containerData, 0, containerSize);
				}

				using var ms = new MemoryStream(containerData);
				using var bs = new BinaryStream(ms);

				// Extract files
				for (int index = container.FileDescriptorEntryIndexStart; index < container.FileDescriptorEntryIndexEnd; index++)
				{
					RRFileDescriptor file = fileInfo[index];
					Console.WriteLine($"- {container.Name} -> {file.Name}");

					bs.Position = file.OffsetWithinContainer;

					// Could be better, lazy
					byte[] fileData = bs.ReadBytes((int)file.FileSizeWithinContainer);
					File.WriteAllBytes(Path.Combine(containerDir, file.Name), fileData);
				}

				ArrayPool<byte>.Shared.Return(containerData);
				i++;
			}

			Console.WriteLine("Done.");
		}
	}
}
