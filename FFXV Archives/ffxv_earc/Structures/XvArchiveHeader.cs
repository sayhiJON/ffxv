using System;
using System.Collections.Generic;
using System.Text;

//* non-default
using System.Runtime.InteropServices;
using System.IO;

namespace ffxv_earc.Structures {
    [StructLayout(LayoutKind.Explicit, Pack = 1), Serializable]
    public struct XvArchiveHeader {
        public const int EXPECTED_TAG = 1178686019;
        /*
            struct SQEX::Luminous::AssetManager::LmArcInterface::ArcHeader
            {
              unsigned int tag;
              unsigned __int16 minor;
              unsigned __int16 major;
              unsigned int count;
              unsigned int blockSize;
              unsigned int tocStart;
              unsigned int nameStart;
              unsigned int fullPathStart;
              unsigned int dataStart;
              unsigned int flags;
              unsigned int chunkSize;
              unsigned __int64 hash;
              char _pad[16];
            };

            enum SQEX::Luminous::AssetManager::LmArcInterface::ArcHeaderFlags
            {
              ARCHEADER_HASLOOSEDATA = 0x1,
              ARCHEADER_HASLOCALEDATA = 0x2,
              ARCHEADER_DEBUGARCHIVE = 0x4,
            };
        */

        [FieldOffset(0)]
        public uint Tag;
        [FieldOffset(4)]
        public ushort Minor;
        [FieldOffset(6)]
        public ushort Major;
        [FieldOffset(8)]
        public uint Count;
        [FieldOffset(12)]
        public uint BlockSize;
        [FieldOffset(16)]
        public uint OffsetToc;
        [FieldOffset(20)]
        public uint OffsetName;
        [FieldOffset(24)]
        public uint OffsetFullPath;
        [FieldOffset(28)]
        public uint OffsetData;
        [FieldOffset(32)]
        public XvArchiveHeaderFlags Flags;
        [FieldOffset(36)]
        public uint ChunkSize;
        [FieldOffset(40)]
        public ulong Hash;
        [FieldOffset(48)]
        public ulong Padding_1;
        [FieldOffset(56)]
        public ulong Padding_2;

        //* added fields not related to underlying struct
        [FieldOffset(65)]
        public bool EncryptedEntryHeaders;

        public XvArchiveHeader(uint tag, ushort minor, ushort major, uint count, uint blockSize, uint offsetToc, uint offsetName, uint offsetFullPath, uint offsetData, XvArchiveHeaderFlags flags, uint chunkSize, ulong hash) {
            this.Tag = tag;
            this.Minor = minor;
            this.Major = major;
            this.Count = count;
            this.BlockSize = blockSize;
            this.OffsetToc = offsetToc;
            this.OffsetName = offsetName;
            this.OffsetFullPath = offsetFullPath;
            this.OffsetData = offsetData;
            this.Flags = flags;
            this.ChunkSize = chunkSize;
            this.Hash = hash;
            this.Padding_1 = 0;
            this.Padding_2 = 0;
            this.EncryptedEntryHeaders = false;
        }

        public static XvArchiveHeader Deserialize(BinaryReader reader) {
            //* get the archive header
            XvArchiveHeader header = new XvArchiveHeader {
                Tag             = reader.ReadUInt32(),
                Minor           = reader.ReadUInt16(),
                Major           = reader.ReadUInt16(),
                Count           = reader.ReadUInt32(),
                BlockSize       = reader.ReadUInt32(),
                OffsetToc       = reader.ReadUInt32(),
                OffsetName      = reader.ReadUInt32(),
                OffsetFullPath  = reader.ReadUInt32(),
                OffsetData      = reader.ReadUInt32(),
                Flags           = (XvArchiveHeaderFlags) reader.ReadUInt32(),
                ChunkSize       = reader.ReadUInt32(),
                Hash            = reader.ReadUInt64(),
                Padding_1       = reader.ReadUInt64(),
                Padding_2       = reader.ReadUInt64()
            };

            //* see if the entry headers are encrypted
            header.EncryptedEntryHeaders = (header.Major & 0x8000) > 0;

            if (header.EncryptedEntryHeaders)
                //* adjust the major if entry headers are encrypted
                header.Major ^= 0x8000;

            return header;
        }
    }

    [Flags]
    public enum XvArchiveHeaderFlags : uint {
        None            = 0,
        HasLooseData    = 1,
        HasLocaleData   = 2,
        DebugArchive    = 4,
        Encrypted       = 8,
    }
}
