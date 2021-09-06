using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;

using Syroot.BinaryData;

using RRUnpacker.TOC;

namespace RRUnpacker
{
    public class RR7Patcher : IDisposable
    {
        private RR7TableOfContents _ToC;
        private string _elfPath;
        private FileStream _elfStream;

        private string _datPath;
        private FileStream _datStream;

        public RR7Patcher(RR7TableOfContents toc, string elfPath, string datPath)
        {
            _ToC = toc;
            _elfPath = elfPath;
            _datPath = datPath;
        }

        public void Setup(string modFolder)
        {
            long test = 0;
            for (int i = 0; i < _ToC.ContainerDescriptors.Count; i++)
            {
                if (_ToC.ContainerDescriptors[i].Compressed)
                    test += _ToC.ContainerDescriptors[i].UncompressedSize;
            }

            var highestContainer = _ToC.ContainerDescriptors.OrderByDescending(c => c.SectorOffset).FirstOrDefault();

            long end = (long)(highestContainer.SectorOffset + highestContainer.SectorSize) * RRConsts.BlockSize;
            _datStream = new FileStream(_datPath, FileMode.Open, FileAccess.ReadWrite);
            _elfStream = new FileStream(_elfPath, FileMode.Open, FileAccess.ReadWrite);

            _datStream.Position = end;

            foreach (var file in Directory.EnumerateDirectories(modFolder))
            {
                ProcessFolder(file, modFolder);
            }

            // Hardcoded DAT size, thank you bandai
            _elfStream.Position = 0x620048;
            _elfStream.WriteUInt64((ulong)_datStream.Length, ByteConverter.Big);
        }

        private void ProcessFolder(string containerFolder, string modFolder)
        {
            string gamePath = containerFolder.Substring(modFolder.Length + 1, (containerFolder.Length - modFolder.Length) - 1);
            var container = _ToC.ContainerDescriptors.Find(e => e.Name == gamePath);
            if (container is null)
            {
                Console.WriteLine("Container not found");
                return;
            }

            var filesInContainer = new List<RRFileDescriptor>();
            for (int i = container.FileDescriptorEntryIndexStart; i < container.FileDescriptorEntryIndexEnd; i++)
                filesInContainer.Add(_ToC.FileDescriptors[i]);

            var modFiles = Directory.GetFiles(containerFolder);

            // Verify
            var copy = new List<string>();
            for (int i = 0; i < modFiles.Length; i++)
            {
                string fileGamePath = modFiles[i].Substring(containerFolder.Length + 1, (modFiles[i].Length - containerFolder.Length) - 1);
                copy.Add(fileGamePath);
            }

            // Check the container matches the amount of files
            foreach (var file in filesInContainer)
            {
                if (copy.Contains(file.Name))
                    copy.Remove(file.Name);
                else
                {
                    Console.WriteLine("Unexpected file");
                    return;
                }
            }
            if (copy.Count != 0)
            {
                Console.WriteLine("Mod folder does not match container");
                return;
            }

            // Reorder based on original container file order
            var modFilesOrdered = new string[modFiles.Length];
            var tmp = filesInContainer.Select(e => e.Name).ToList();
            for (int i = 0; i < modFiles.Length; i++)
            {
                string file = modFiles[i];
                string fileGamePath = modFiles[i].Substring(containerFolder.Length + 1, (modFiles[i].Length - containerFolder.Length) - 1);
                int index = tmp.IndexOf(fileGamePath);
                modFilesOrdered[index] = file;
            }

            using var datStreamWriter = new BinaryStream(_datStream, ByteConverter.Big, leaveOpen: true);
            using var elfStreamWriter = new BinaryStream(_elfStream, ByteConverter.Big, leaveOpen: true);

            long basePos = _datStream.Position;
            long lastFilePos = basePos;
            uint containerFileOffset = 0;

            for (int i = 0; i < modFilesOrdered.Length; i++)
            {
                string file = modFilesOrdered[i];
                RRFileDescriptor fileDescriptor = filesInContainer[i];

                byte[] fileData = File.ReadAllBytes(file);
                datStreamWriter.Write(fileData, 0, fileData.Length);
                datStreamWriter.Align(RRConsts.BlockSize, grow: true);

                elfStreamWriter.Position = fileDescriptor.Offset;
                elfStreamWriter.Position += 4; // Skip name ptr
                elfStreamWriter.WriteUInt32((uint)(lastFilePos / RRConsts.BlockSize)); // Sector Offset
                elfStreamWriter.WriteUInt16((ushort)Math.Round(((double)(datStreamWriter.Position - lastFilePos) / RRConsts.BlockSize), MidpointRounding.ToPositiveInfinity)); // Sector Size
                elfStreamWriter.Position += 2;
                elfStreamWriter.WriteUInt32((uint)fileData.Length); // Size in container
                elfStreamWriter.WriteUInt32(containerFileOffset); // Offset in container

                containerFileOffset += (uint)(datStreamWriter.Position - lastFilePos);
                lastFilePos = datStreamWriter.Position;
            }

            datStreamWriter.Align(RRConsts.BlockSize, grow: true);
            long lastFilePosAligned = _datStream.Position;

            elfStreamWriter.Position = container.Offset;
            elfStreamWriter.Position += 4;
            elfStreamWriter.WriteUInt32((uint)(basePos / RRConsts.BlockSize)); // Sector Offset
            elfStreamWriter.WriteUInt16((ushort)((uint)(lastFilePosAligned - basePos) / RRConsts.BlockSize)); // Sector Size
            elfStreamWriter.Position += 2;
            elfStreamWriter.Position += 2;
            elfStreamWriter.WriteUInt16(0); // Not compressed
            elfStreamWriter.WriteUInt32(0); // ZSize, not needed
            elfStreamWriter.WriteUInt32(0); // Size, not needed

        }

        public void Dispose()
        {
            _elfStream.Dispose();
            _datStream.Dispose();
        }
    }
}
