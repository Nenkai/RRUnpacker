using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;
using System.Buffers;

using Syroot.BinaryData;

using RRUnpacker.RR6.TOC;

namespace RRUnpacker.RR6
{
	public class RR6Unpacker
	{
		public RR6TableOfContents TOC { get; set; }
		public const int BlockSize = 0x800;

		private string _outputPath;
		public string _inputPath;

		public RR6Unpacker(string inputPath, string outputPath)
		{
			_inputPath = inputPath;
			_outputPath = outputPath;
		}

		public void ReadToc(string elfPath)
		{
			TOC = new RR6TableOfContents(elfPath);
			TOC.Read();
		}

		public void ExtractContainers()
		{
			Console.WriteLine("Starting to unpack..");

			using var fs = File.Open(_inputPath, FileMode.Open);
			Directory.CreateDirectory(_outputPath);

			int i = 0;

			string fileName = Path.GetFileNameWithoutExtension(_inputPath);
			List<RR6ContainerDescriptor> containerInfo = TOC.GetContainerTOCByFileName(fileName);
			List<RR6FileDescriptor> fileInfo = TOC.GetFileTOCByFileName(fileName);

			foreach (var container in containerInfo)
			{
				string containerDir = Path.Combine(_outputPath, container.Name);
				Directory.CreateDirectory(containerDir);

				Console.WriteLine($"[{i + 1}/{containerInfo.Count}] {container.Name}");

				// Grab container data
				byte[] containerData;
				if (container.Compressed)
				{
					fs.Position = (long)container.SectorOffset * BlockSize;
					long compOff = BlockSize - (container.CompressedSize % BlockSize);
					fs.Position += compOff;

					containerData = ArrayPool<byte>.Shared.Rent((int)container.UncompressedSize);
					Span<byte> containerDecompressed = containerData.AsSpan(0, (int)container.UncompressedSize);
					RRDecompressor.Decompress(fs, container.CompressedSize, containerDecompressed, container.UncompressedSize);
				}
				else
				{
					int containerSize = container.SectorSize * BlockSize;
					fs.Position = (long)container.SectorOffset * BlockSize;
					containerData = ArrayPool<byte>.Shared.Rent(containerSize);
					fs.Read(containerData, 0, containerSize);
				}

				using var ms = new MemoryStream(containerData);
				using var bs = new BinaryStream(ms);

				// Extract files
				for (int index = container.FileDescriptorEntryIndexStart; index < container.FileDescriptorEntryIndexEnd; index++)
				{
					RR6FileDescriptor file = fileInfo[index];
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
