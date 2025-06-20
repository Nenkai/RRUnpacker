using Syroot.BinaryData;

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RRUnpacker.Headers;

public class U8Archive : IDisposable
{
    private Stream _stream;
    private List<U8Node> _nodes = [];
    public List<string> Nodes = [];

    public static U8Archive Open(string file)
    {
        var stream = File.OpenRead(file);
        return Open(stream);
    }

    public static U8Archive Open(Stream stream)
    {
        var bs = new BinaryStream(stream, ByteConverter.Big);

        var archive = new U8Archive
        {
            _stream = stream
        };
        archive.ReadHeader(bs);
        return archive;
    }

    private void ReadHeader(BinaryStream bs)
    {
        uint tag = bs.ReadUInt32();
        if (tag != 0x55AA382D)
            throw new InvalidDataException($"Not a valid U8 archive. Expected tag 0x55AA382D, got 0x{tag:X8}");

        uint rootNodeOffset = bs.ReadUInt32();
        uint headerSize = bs.ReadUInt32();
        uint dataOffset = bs.ReadUInt32();
        bs.Position = rootNodeOffset;

        var root = new U8Node();
        root.Read(bs);

        // FIXME: This does not support reading directories, but we don't care for that for the archive we're targetting.
        // It doesn't use folders.
        for (int i = 0; i < root.Size - 1; i++)
        {
            var node = new U8Node();
            node.Read(bs);
            _nodes.Add(node);
        }

        long strsOffset = bs.Position;
        for (int i = 0; i < root.Size - 1; i++)
        {
            bs.Position = strsOffset + _nodes[i].NameOffset;
            _nodes[i].Name = bs.ReadString(StringCoding.ZeroTerminated);
        }
    }

    public byte[] GetFile(string path)
    {
        U8Node? node = _nodes.FirstOrDefault(n => n.Name == path) 
            ?? throw new FileNotFoundException($"File '{path}' not found in U8 archive.");

        byte[] data = new byte[node.Size];
        _stream.Position = node.DataOffset;
        _stream.ReadExactly(data, 0, node.Size);
        return data;
    }

    public void Dispose()
    {
        ((IDisposable)_stream).Dispose();
    }

    public class U8Node
    {
        public string Name { get; set; }

        public bool IsDirectory;
        public uint NameOffset;
        public int DataOffset;
        public int Size;

        public void Read(BinaryStream bs)
        {
            uint firstFour = bs.ReadUInt32();
            IsDirectory = (firstFour & 0xFF000000) > 0;
            NameOffset = firstFour & 0x00FFFFFF;
            DataOffset = bs.ReadInt32();
            Size = bs.ReadInt32();
        }
    }
}
