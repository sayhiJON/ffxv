using System;
using System.Collections.Generic;
using System.Text;

//* non-default
using System.Runtime.InteropServices;
using System.IO;

namespace ffxv_earc.Structures {
    /// <summary>
    /// A struct representing the layout and data of the archive header for Final Fantasy XV.
    /// </summary>
    [StructLayout(LayoutKind.Explicit, Pack = 1), Serializable]
    public struct XvArchiveHeader {
        /// <summary>
        /// Tag expected from a valid archive.
        /// </summary>
        public const int EXPECTED_TAG = 1178686019;
 
        /// <summary>
        /// The tag used to identify the file data.
        /// </summary>
        [FieldOffset(0)]
        public uint Tag;

        /// <summary>
        /// The version minor of the archive format.
        /// </summary>
        [FieldOffset(4)]
        public ushort Minor;

        /// <summary>
        /// The version major of the archive format.
        /// </summary>
        [FieldOffset(6)]
        public ushort Major;

        /// <summary>
        /// The number of files contained in the archive.
        /// </summary>
        [FieldOffset(8)]
        public uint Count;

        /// <summary>
        /// The size of a block of data, used for data alignment.
        /// </summary>
        [FieldOffset(12)]
        public uint BlockSize;

        /// <summary>
        /// The offset of where the table of contents of the archive starts.
        /// </summary>
        [FieldOffset(16)]
        public uint OffsetToc;

        /// <summary>
        /// The offset of where the filenames in the archive start.
        /// </summary>
        [FieldOffset(20)]
        public uint OffsetName;

        /// <summary>
        /// The offset of where the path of the files start (misleading).
        /// </summary>
        [FieldOffset(24)]
        public uint OffsetFullPath;

        /// <summary>
        /// The offset of where the data starts in the archive.
        /// </summary>
        [FieldOffset(28)]
        public uint OffsetData;

        /// <summary>
        /// The flags of the archive (See <see cref="XvArchiveHeaderFlags"/>).
        /// </summary>
        [FieldOffset(32)]
        public XvArchiveHeaderFlags Flags;

        /// <summary>
        /// The chunk size of compressed blocks of a file.
        /// </summary>
        [FieldOffset(36)]
        public uint ChunkSize;

        /// <summary>
        /// The archive hash used for encrypting and decrypting entry headers.
        /// </summary>
        [FieldOffset(40)]
        public ulong Hash;

        /// <summary>
        /// Zero padding (8 bytes).
        /// </summary>
        [FieldOffset(48)]
        public ulong Padding_1;

        /// <summary>
        /// Zero padding (8 bytes).
        /// </summary>
        [FieldOffset(56)]
        public ulong Padding_2;


        //* added fields not related to underlying struct

        /// <summary>
        /// Determines whether entry headers are encrypted.
        /// </summary>
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

        /// <summary>
        /// Deserializes the archive header data into an <see cref="XvArchiveHeader"/>.
        /// </summary>
        /// <param name="reader">The binary reader of the underlying data stream.</param>
        /// <returns>A new <see cref="XvArchiveHeader"/> of the archive.</returns>
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

    /// <summary>
    /// Flags of the archive.
    /// </summary>
    [Flags]
    public enum XvArchiveHeaderFlags : uint {
        None            = 0,
        HasLooseData    = 1,
        HasLocaleData   = 2,
        DebugArchive    = 4,
        Encrypted       = 8,
    }
}
