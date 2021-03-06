﻿using System;
using System.Collections.Generic;
using System.Text;

//* non-default
using System.IO;
using System.Runtime.InteropServices;

namespace ffxv_earc.Structures {
    /// <summary>
    /// A struct representing the layout and data of an entry header for a Final Fantasy XV archive.
    /// </summary>
    [StructLayout(LayoutKind.Explicit, Pack = 1), Serializable]
    public struct XvEntryHeader {
        /// <summary>
        /// The offset of the <see cref="IV"/> used to encrypt / decrypt the entry data.
        /// </summary>
        public const uint IV_OFFSET = 33;

        /// <summary>
        /// The hash used in encryption and decryption of the <see cref="XvEntryHeader"/>. Generated by the FNV hashing algorithm. Combination of name and type.
        /// </summary>
        [FieldOffset(0)]
        public ulong Hash;

        /// <summary>
        /// The size (in bytes) of the file.
        /// </summary>
        [FieldOffset(8)]
        public uint Size;

        /// <summary>
        /// The size (in bytes) of the data when compressed or encrypted.
        /// </summary>
        [FieldOffset(12)]
        public uint CompressedSize;

        /// <summary>
        /// The flags of this particular <see cref="XvEntryHeader"/> (see <see cref="XvEntryHeaderFlags"/>).
        /// </summary>
        [FieldOffset(16)]
        public XvEntryHeaderFlags Flags;

        /// <summary>
        /// The offset of the name of the file for this entry.
        /// </summary>
        [FieldOffset(20)]
        public uint OffsetName;

        /// <summary>
        /// The offset of the data of the file for this entry.
        /// </summary>
        [FieldOffset(24)]
        public ulong OffsetData;

        /// <summary>
        /// The offset of the path of the file for this entry (misleading).
        /// </summary>
        [FieldOffset(32)]
        public uint OffsetFullPath;

        /// <summary>
        /// The localization type of this entry.
        /// </summary>
        [FieldOffset(36)]
        public byte LocalizeType;

        /// <summary>
        /// The locale of this entry.
        /// </summary>
        [FieldOffset(37)]
        public byte LocalizeLocale;

        /// <summary>
        /// 
        /// </summary>
        [FieldOffset(38)]
        public ushort Key;


        //* added fields not related to underlying struct

        /// <summary>
        /// The rolling key created after encrypting or decrypting this entry.
        /// </summary>
        [FieldOffset(40)]
        public ulong RollingKey;

        /// <summary>
        /// The IV used in encrypting or decrypting the data of this entry.
        /// </summary>
        [FieldOffset(48)]
        public byte[] IV; //* 16

        /// <summary>
        /// The name of this entry.
        /// </summary>
        [FieldOffset(64)]
        public string Name;

        /// <summary>
        /// The path of this entry (misleading).
        /// </summary>
        [FieldOffset(192)]
        public string Path;

        /// <summary>
        /// 
        /// </summary>
        /// <param name="reader"></param>
        /// <returns></returns>
        public static XvEntryHeader Deserialize(BinaryReader reader) {
            XvEntryHeader header = ReadHeader(reader);

            if ((header.Flags & XvEntryHeaderFlags.SafeHeader) != XvEntryHeaderFlags.SafeHeader)
                throw new InvalidDataException("SAFE HEADER FLAG NOT SET BUT WRONG DESERIALIZE CALLED");

            return header;
        }

        /// <summary>
        /// Deserializes the <see cref="XvEntryHeader"/> from the underlying <see cref="Stream"/> of the specified <see cref="BinaryReader"/>.
        /// </summary>
        /// <param name="reader">The <see cref="BinaryReader"/> to read from.</param>
        /// <param name="key">The hash used to decrypt <see cref="XvEntryHeader"/> information.</param>
        /// <returns>A new <see cref="XvEntryHeader"/> populated with the entry's data.</returns>
        public static XvEntryHeader Deserialize(BinaryReader reader, ulong key) {
            //* get the basic entry information
            XvEntryHeader header = ReadHeader(reader);
            //* set the key incase it's not updated
            header.RollingKey = key;

            //* if the archive encrypts the meta data, decrypt it
            //* thanks to daxxy
            if ((header.Flags & XvEntryHeaderFlags.SafeHeader) != XvEntryHeaderFlags.SafeHeader) {
                ulong   fileSizeKey     = (key * XvArchive.ENTRY_KEY) ^ header.Hash,
                        dataOffsetKey   = (fileSizeKey * XvArchive.ENTRY_KEY) ^ ~(header.Hash);

                uint    uncompressedKey = (uint)(fileSizeKey >> 32),
                        compressedKey   = (uint)(fileSizeKey & 0xFFFFFFFF);

                header.Size ^= uncompressedKey;
                header.CompressedSize ^= compressedKey;
                header.OffsetData ^= dataOffsetKey;

                header.RollingKey = dataOffsetKey;

                long returnPosition = reader.BaseStream.Position;

                reader.BaseStream.Seek((long)(header.OffsetData + header.CompressedSize - IV_OFFSET), SeekOrigin.Begin);
                header.IV = reader.ReadBytes(16);
                header.CompressedSize -= IV_OFFSET;

                reader.BaseStream.Seek(returnPosition, SeekOrigin.Begin);
            }

            return header;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="reader"></param>
        /// <returns></returns>
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

    /// <summary>
    /// 
    /// </summary>
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
