using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using System.IO;
using Syroot.BinaryData;

namespace RRUnpacker.Headers
{
    // Base Info from https://github.com/emoose/xbox-reversing/ and https://github.com/xenia-project/xenia/blob/HEAD/src/xenia/kernel/util/xex2_info.h
    // Full credits to respective authors
    // Support just for grabbing the file descriptor header entry

    public class XEX2Header
    {
        public const int FileHeaderSize = 0x18;

        public string Magic { get; set; }
        public XEX2ModuleFlags ModuleFlags { get; set; }
        public uint HeaderSize { get; set; }
        public uint SizeOfDiscardableHeaders { get; set; }
        public uint SecurityOffset { get; set; }
        public uint HeaderCount { get; set; }
        public XEX2OptHeader[] Headers { get; set; }

        public static XEX2Header Read(string fileName)
        {
            using var fs = File.Open(fileName, FileMode.Open);
            using var bs = new BinaryStream(fs, ByteConverter.Big);

            if (fs.Length < FileHeaderSize)
                return null;

            var header = new XEX2Header();
            header.Magic = bs.ReadString(4, Encoding.ASCII);
            if (header.Magic != "XEX2")
                return null;

            header.ModuleFlags = (XEX2ModuleFlags)bs.ReadUInt32();
            header.HeaderSize = bs.ReadUInt32();
            header.SizeOfDiscardableHeaders = bs.ReadUInt32();
            header.SecurityOffset = bs.ReadUInt32();
            header.HeaderCount = bs.ReadUInt32();

            if (fs.Length < header.HeaderSize + header.HeaderCount * 8)
                return null; // Corrupt?

            header.Headers = new XEX2OptHeader[header.HeaderCount];
            for (int i = 0; i < header.HeaderCount; i++)
            {
                var optHeader = new XEX2OptHeader();
                header.Headers[i] = optHeader;
                optHeader.Read(bs);

                using (var seek = bs.TemporarySeek(optHeader.Offset, SeekOrigin.Begin))
                {
                    if (optHeader.Key == XEX2ImageKeyType.FileDataDescriptor)
                    {
                        var fd = new XEX2FileDataDescriptor();
                        fd.Read(bs);
                        optHeader.Value = fd;
                    }
                }
            }

            return header;
        }

        public XEX2OptHeader GetImageHeaderInfoByKey(XEX2ImageKeyType type)
        {
            foreach (var header in Headers)
            {
                if (header.Key == type)
                    return header;
            }

            return null;
        }
    }

    public interface IXEX2OptHeaderValue
    {

    }

    public class XEX2OptHeader
    {
        public XEX2ImageKeyType Key { get; set; }
        public uint Offset { get; set; }

        public IXEX2OptHeaderValue Value { get; set; }

        public void Read(BinaryStream bs)
        {
            Key = (XEX2ImageKeyType)bs.ReadUInt32();
            Offset = bs.ReadUInt32();
        }
    }

    public class XEX2FileDataDescriptor : IXEX2OptHeaderValue
    {
        public uint InfoSize { get; set; }
        public XEX2CompressionType CompressionType { get; set; }
        public XEX2EncryptionType EncryptionType { get; set; }

        public void Read(BinaryStream bs)
        {
            InfoSize = bs.ReadUInt32();
            EncryptionType = (XEX2EncryptionType)bs.ReadUInt16();
            CompressionType = (XEX2CompressionType)bs.ReadUInt16();
        }
    }

    public enum XEX2CompressionType : ushort
    {
        Compressed,
        Uncompressed,
    }

    public enum XEX2EncryptionType : ushort
    {
        Decrypted,
        Encrypted,
    }

    public enum XEX2ModuleFlags : uint
    {
        XEX_MODULE_TITLE = 0x00000001,
        XEX_MODULE_EXPORTS_TO_TITLE = 0x00000002,
        XEX_MODULE_SYSTEM_DEBUGGER = 0x00000004,
        XEX_MODULE_DLL_MODULE = 0x00000008,
        XEX_MODULE_MODULE_PATCH = 0x00000010,
        XEX_MODULE_PATCH_FULL = 0x00000020,
        XEX_MODULE_PATCH_DELTA = 0x00000040,
        XEX_MODULE_USER_MODE = 0x00000080,
    };

    public enum XEX2ImageKeyType
    {
        SizeOfHeaders = 0x00000101, // XEX0-only? seems it should always match SizeOfHeaders in main XEX header
        XexSections = 0x000001FF, // XEX3F-only? 1434 seems to create this
                                  //  ModuleFlags_XEX0            = 0x00000201, // stores XEX flags (title/system, exe/dll) in XEX0 executables
        OriginalBaseAddress_XEX3F = 0x00000201,
        HeaderSectionTable = 0x000002FF,
        FileDataDescriptor = 0x000003FF,
        BaseReference = 0x00000405,
        DeltaPatchDescriptor = 0x000005FF,
        KeyVaultPrivs_Alt = 0x00004004,
        KeyVaultPrivs = 0x000040FF,
        TimeRange_Alt = 0x00004104,
        TimeRange = 0x000041FF,
        ConsoleIdTable = 0x000042FF,
        DiscProfileID = 0x00004304,
        BoundingPath = 0x000080FF,
        BuildVersions_XEX3F = 0x00008102,
        DeviceId = 0x00008105,
        OriginalBaseAddress = 0x00010001,
        ExecutionID_XEX0 = 0x00010005,
        EntryPoint = 0x00010100,
        FastcapEnabled_XEX2D = 0x00010200,
        PEBase = 0x00010201,
        Imports_OldKey = 0x000102FF, // XEX25 key
        PEExports_XEX2D = 0x00010300,
        //  SPAFileName_XEX0            = 0x000103FF,
        Imports = 0x000103FF,
        PEExports_XEX1 = 0x00010400,
        StackSize_XEX25 = 0x00010400, // XEX25 key
        TLSData_OldKey = 0x00010504, // XEX25 key
        VitalStats = 0x00018002,
        CallcapImports = 0x00018102,
        FastcapEnabled = 0x00018200,
        PEModuleName = 0x000183FF,
        BuildVersions = 0x000200FF,
        TLSData = 0x00020104,
        BuildVersions_OldKey = 0x000201FF, // XEX25 key
        StackSize = 0x00020200,
        FSCacheSize = 0x00020301,
        XapiHeapSize = 0x00020401,
        PageHeapSizeFlags = 0x00028002,
        Privileges = 0x00030000,
        Privileges_32 = 0x00030100, // privilege IDs 32 onward
        Privileges_64 = 0x00030200, // privilege IDs 64 onward
        ExecutionID = 0x00040006,
        ExecutionID_XEX25 = 0x00040008,
        ServiceIDList = 0x000401FF,
        WorkspaceSize = 0x00040201,
        GameRatings = 0x00040310,
        SpaName = 0x000403FF, // XEX2D only?
        LANKey = 0x00040404,
        MicrosoftLogo = 0x000405FF,
        MultidiskMediaIDs = 0x000406FF,
        AlternateTitleIDs = 0x000407FF,
        AdditionalTitleMemory = 0x00040801,
        IsExecutable = 0x000E0001, // XEX3F only? maybe means NoExports?
        ImportsByName = 0x00E10302,
        ExportsByName = 0x00E10402,
        UserModeImportDeps = 0x00E105FF,
    };
}
