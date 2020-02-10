using System;
using System.Collections.Generic;
using System.Text;

//* non-default
using System.IO;
using System.Runtime.InteropServices;

namespace ffxv_earc.Structures {
    [StructLayout(LayoutKind.Explicit, Pack = 1), Serializable]
    public struct XvEntryHeader {
        public const uint IV_OFFSET = 33;

        /*
            struct SQEX::Luminous::AssetManager::LmArcInterface::ArcCatalogEntry
            {
              unsigned __int64 nameTypeHash;
              unsigned int originalSize;
              unsigned int compressedSize;
              unsigned int flags;
              unsigned int nameStart;
              unsigned __int64 dataStart;
              unsigned int fullPathStart;
              _BYTE localizeType[1];
              _BYTE localizeLocale[1];
              unsigned __int16 key;
            };

            enum SQEX::Luminous::AssetManager::LmArcInterface::ArcFlags
            {
                ARCFLAG_AUTOLOAD = 0x1,
                ARCFLAG_COMPRESSED = 0x2,
                ARCFLAG_REFERENCE = 0x4,
                ARCFLAG_NOEARC = 0x8,
                ARCFLAG_PATCHED = 0x10,
                ARCFLAG_PATCHED_DELETED = 0x20,
            };
        */

        [FieldOffset(0)]
        public ulong Hash;

        [FieldOffset(8)]
        public uint Size;
        [FieldOffset(12)]
        public uint CompressedSize;
        [FieldOffset(16)]
        public XvEntryHeaderFlags Flags;
        [FieldOffset(20)]
        public uint OffsetName;
        [FieldOffset(24)]
        public ulong OffsetData;
        [FieldOffset(32)]
        public uint OffsetFullPath;

        [FieldOffset(36)]
        public byte LocalizeType;
        [FieldOffset(37)]
        public byte LocalizeLocale;

        [FieldOffset(38)]
        public ushort Key;

        //* added fields not related to underlying struct
        [FieldOffset(40)]
        public ulong OffsetDataKey;
        [FieldOffset(48)]
        public byte[] IV; //* 16
        [FieldOffset(64)]
        public string Name;
        [FieldOffset(192)]
        public string Path;

        public static XvEntryHeader Deserialize(BinaryReader reader) {
            XvEntryHeader header = ReadHeader(reader);

            if ((header.Flags & XvEntryHeaderFlags.SafeHeader) != XvEntryHeaderFlags.SafeHeader)
                throw new InvalidDataException("SAFE HEADER FLAG NOT SET BUT WRONG DESERIALIZE CALLED");

            return header;
        }

        public static XvEntryHeader Deserialize(BinaryReader reader, ulong key) {
            //* get the basic entry information
            XvEntryHeader header = ReadHeader(reader);
            //* set the key incase it's not updated
            header.OffsetDataKey = key;

            //* if the archive encrypts the meta data, decrypt it
            //* thanks to daxxy
            if ((header.Flags & XvEntryHeaderFlags.SafeHeader) != XvEntryHeaderFlags.SafeHeader) {
                ulong   fileSizeKey     = (key * XvArchive.MasterFileKey) ^ header.Hash,
                        dataOffsetKey   = (fileSizeKey * XvArchive.MasterFileKey) ^ ~(header.Hash);

                uint    uncompressedKey = (uint)(fileSizeKey >> 32),
                        compressedKey   = (uint)(fileSizeKey & 0xFFFFFFFF);

                header.Size ^= uncompressedKey;
                header.CompressedSize ^= compressedKey;
                header.OffsetData ^= dataOffsetKey;

                header.OffsetDataKey = dataOffsetKey;

                long returnPosition = reader.BaseStream.Position;

                reader.BaseStream.Seek((long)(header.OffsetData + header.CompressedSize - IV_OFFSET), SeekOrigin.Begin);
                header.IV = reader.ReadBytes(16);

                reader.BaseStream.Seek(returnPosition, SeekOrigin.Begin);
            }

            return header;
        }

        private static XvEntryHeader ReadHeader(BinaryReader reader) {
            XvEntryHeader header = new XvEntryHeader {
                Hash            = reader.ReadUInt64(),
                Size            = reader.ReadUInt32(),
                CompressedSize  = reader.ReadUInt32(),
                Flags           = (XvEntryHeaderFlags) reader.ReadUInt32(),
                OffsetName      = reader.ReadUInt32(),
                OffsetData      = reader.ReadUInt64(),
                OffsetFullPath  = reader.ReadUInt32(),
                LocalizeType    = reader.ReadByte(),
                LocalizeLocale  = reader.ReadByte(),
                Key             = reader.ReadUInt16()
            };

            long returnPosition = reader.BaseStream.Position;

            reader.BaseStream.Seek(header.OffsetName, SeekOrigin.Begin);
            header.Name = reader.ReadStringNullTerminated();
            reader.BaseStream.Seek(header.OffsetFullPath, SeekOrigin.Begin);
            header.Path = reader.ReadStringNullTerminated();

            reader.BaseStream.Seek(returnPosition, SeekOrigin.Begin);

            return header;
        }
    }

    [Flags]
    public enum XvEntryHeaderFlags : uint {
        None            = 0,
        Autoload        = 1,
        Compressed      = 2,
        Reference       = 4,
        NoEarc          = 8,
        Patched         = 16,
        PatchedDeleted  = 32,
        Encrypted       = 64,
        SafeHeader      = 128
    }
}
