using System;

//* non-default
using System.Collections.Generic;
using System.IO;
using System.Security.Cryptography;
using ffxv_earc.Structures;
using Joveler.ZLibWrapper;

namespace ffxv_earc {
    public class XvArchive {
        internal const ulong    PRIMARY_KEY         = 0xCBF29CE484222325,
                                SECONDARY_KEY       = 0x40D4CCA269811DAF,
                                ENTRY_HASH          = 0x14650FB0739D0383,
                                ENTRY_KEY           = 0x100000001B3,
                                PRIMARY_CHUNK_KEY   = 0x10E64D70C2A29A69,
                                SECONDARY_CHUNK_KEY = 0xC63D3dC167E;

        /// <summary>
        /// Key used for encryption / decryption of entry data.
        /// </summary>
        public static readonly byte[] AESKey = new byte[] { 0x9C, 0x6C, 0x5D, 0x41, 0x15, 0x52, 0x3F, 0x17, 0x5A, 0xD3, 0xF8, 0xB7, 0x75, 0x58, 0x1E, 0xCF };

        /// <summary>
        /// Gets or sets the location of the archive on the hard drive.
        /// </summary>
        public string PhysicalPath {
            get; set;
        } = string.Empty;

        /// <summary>
        /// Gets or sets the flags of the archive.
        /// </summary>
        public XvArchiveHeaderFlags Flags {
            get => this.p_ArchiveHeader.Flags;
            private set {
                XvArchiveHeaderFlags flags = this.p_ArchiveHeader.Flags;

                //* if nothing is actually changing, do nothing
                if (value == flags)
                    return;

                //* are we setting or removing the encrypted flag?
                if ((value & XvArchiveHeaderFlags.Encrypted) == XvArchiveHeaderFlags.Encrypted && (flags & XvArchiveHeaderFlags.Encrypted) == XvArchiveHeaderFlags.None)
                    //* we're setting the encrypted flag
                    this.p_MasterKey = SECONDARY_KEY;
                else if ((value & XvArchiveHeaderFlags.Encrypted) == XvArchiveHeaderFlags.None && (flags & XvArchiveHeaderFlags.Encrypted) == XvArchiveHeaderFlags.Encrypted)
                    //* we're removing the encrypted flag
                    this.p_MasterKey = PRIMARY_KEY;

                this.p_ArchiveHeader.Flags = value;
            }
        }

        /// <summary>
        /// Gets or sets whether the entry headers are encrypted.
        /// </summary>
        public bool EncryptedEntryHeaders {
            get => this.p_ArchiveHeader.EncryptedEntryHeaders;
            private set => this.p_ArchiveHeader.EncryptedEntryHeaders = value;
        }

        /// <summary>
        /// Gets or sets the header information for entries.
        /// </summary>
        public List<XvEntryHeader> Entries {
            get; set;
        } = new List<XvEntryHeader>();

        /// <summary>
        /// Private archive header for this archive.
        /// </summary>
        private XvArchiveHeader p_ArchiveHeader = new XvArchiveHeader();

        /// <summary>
        /// Key used for encryption / decryption of header information.
        /// </summary>
        private ulong p_MasterKey = PRIMARY_KEY;

        /// <summary>
        /// Extracts the data of the <see cref="XvEntryHeader"/> at the specified index to memory.
        /// </summary>
        /// <param name="entryIndex">Index of the <see cref="XvEntryHeader"/> to be extracted.</param>
        /// <returns>A byte array containing the file data of the <see cref="XvEntryHeader"/>.</returns>
        public byte[] ExtractToArray(int entryIndex) {
            if (entryIndex >= this.Entries.Count)
                throw new IndexOutOfRangeException();

            return this.ExtractToArray(this.Entries[entryIndex]);
        }

        /// <summary>
        /// Extracts the data of the specified <see cref="XvEntryHeader"/> to memory.
        /// </summary>
        /// <param name="entry"><see cref="XvEntryHeader"/> to be extracted.</param>
        /// <returns>A byte array containing the file data of the <see cref="XvEntryHeader"/>. A zero filled array with size 1 means no data.</returns>
        public byte[] ExtractToArray(XvEntryHeader entry) {
            //* make sure the archive still exists for whatever reason
            if (!File.Exists(this.PhysicalPath))
                throw new FileNotFoundException();

            //* we need to open the archive
            using Stream stream = File.OpenRead(this.PhysicalPath);
            using BinaryReader reader = new BinaryReader(stream);

            //* there's basically 3 scenarios here (and one sub-scenario)

            //* 1.) The data is encrypted -- decrypt it and write it out
            //* 2.) The data is compressed -- decompress it and write it out
            //* 3.) The data is not compressed or encrypted -- write it out
            //*     3b.) The data has a size of zero, create the file but write nothing

            //* start with decryption
            if ((entry.Flags & XvEntryHeaderFlags.Encrypted) == XvEntryHeaderFlags.Encrypted)
                return this.DecryptEntryDataToArray(entry, ReadEntryDataToArray(reader, (long)entry.OffsetData, (int)entry.CompressedSize));
            else if ((entry.Flags & XvEntryHeaderFlags.Compressed) == XvEntryHeaderFlags.Compressed)
                return this.DecompressEntryDataToArray(entry, ReadEntryDataToArray(reader, (long)entry.OffsetData, (int)entry.CompressedSize));
            else {
                if (entry.Size == 0)
                    return new byte[1] { 0 };

                return ReadEntryDataToArray(reader, (long)entry.OffsetData, (int)entry.Size);
            }
        }

        /// <summary>
        /// Decrypts an <see cref="XvEntryHeader"/>'s data into a byte array.
        /// </summary>
        /// <param name="entry">The <see cref="XvEntryHeader"/> to decrypt.</param>
        /// <param name="encryptedData">The data to decrypt.</param>
        /// <returns>A byte array of decrypted data.</returns>
        private byte[] DecryptEntryDataToArray(XvEntryHeader entry, byte[] encryptedData) {
            Aes aes = Aes.Create();

            aes.Key     = AESKey;
            aes.IV      = entry.IV;
            aes.Padding = PaddingMode.Zeros;

            using MemoryStream decryptedStream = new MemoryStream();
            using MemoryStream encryptedStream = new MemoryStream(encryptedData);
            using CryptoStream decryptorStream = new CryptoStream(encryptedStream, aes.CreateDecryptor(), CryptoStreamMode.Read);

            decryptorStream.CopyTo(decryptedStream);

            if (!decryptorStream.HasFlushedFinalBlock) {
                decryptorStream.FlushFinalBlock();
                decryptorStream.CopyTo(decryptedStream);
            }

            decryptedStream.Seek(0, SeekOrigin.Begin);

            return decryptedStream.ToArray();
        }

        /// <summary>
        /// Decompresses an <see cref="XvEntryHeader"/>'s data into a byte array.
        /// </summary>
        /// <param name="entry">The <see cref="XvEntryHeader"/> to decompress.</param>
        /// <param name="compressedData">The data to decompress.</param>
        /// <returns>A byte array of decompressed data.</returns>
        private byte[] DecompressEntryDataToArray(XvEntryHeader entry, byte[] compressedData) {
            //* get the number of chunks we have
            uint chunkSize  = this.p_ArchiveHeader.ChunkSize * 1024;
            uint chunks     = entry.Size / chunkSize;

            //* if the integer division wasn't even, add 1 more chunk
            if (entry.Size % chunkSize != 0)
                chunks++;

            using MemoryStream decompressedStream   = new MemoryStream();
            using MemoryStream compressedStream     = new MemoryStream(compressedData);
            using BinaryReader reader               = new BinaryReader(compressedStream);

            for (int index = 0; index < chunks; index++) {
                if (index > 0) {
                    int offset = 4 - (int)(compressedStream.Position % 4);

                    if (offset > 3)
                        offset = 0;

                    compressedStream.Seek(offset, SeekOrigin.Current);
                }

                uint compressedSize = reader.ReadUInt32(),
                        decompressedSize = reader.ReadUInt32();

                if (index == 0 && this.EncryptedEntryHeaders) {
                    ulong   chunkKey        = (PRIMARY_CHUNK_KEY * entry.Key) + SECONDARY_CHUNK_KEY;

                    uint    compressedKey   = (uint)(chunkKey >> 32),
                            uncompressedKey = (uint)(chunkKey & 0xFFFFFFFF);

                    compressedSize      ^= compressedKey;
                    decompressedSize    ^= uncompressedKey;
                }

                using MemoryStream memory = new MemoryStream();

                //* store the chunk of compressed data to a memory stream
                memory.Write(reader.ReadBytes((int)compressedSize), 0, (int)compressedSize);

                //* move to the start of the chunk
                memory.Seek(0, SeekOrigin.Begin);

                //* now decompress it and write it to our file
                using ZLibStream decompressor = new ZLibStream(memory, CompressionMode.Decompress);
                decompressor.CopyTo(decompressedStream);
            }

            decompressedStream.Seek(0, SeekOrigin.Begin);
            return decompressedStream.ToArray();
        }

        /// <summary>
        /// Gets the binary data of an <see cref="XvEntryHeader"/> as stored in the <see cref="XvArchive"/>.
        /// </summary>
        /// <param name="reader">The <see cref="BinaryReader"/> attached to the <see cref="Stream"/> of the <see cref="XvArchive"/>.</param>
        /// <param name="position">The starting position of the data within the <see cref="XvArchive"/></param>
        /// <param name="size">The size of the data to read.</param>
        /// <returns>A byte array containing the unmodified data of an <see cref="XvEntryHeader"/>.</returns>
        private static byte[] ReadEntryDataToArray(BinaryReader reader, long position, int size) {
            reader.BaseStream.Seek(position, SeekOrigin.Begin);

            return reader.ReadBytes(size);
        }

        /// <summary>
        /// Opens and reads the file information at the specified file path and stores it in a new <see cref="XvArchive"/>.
        /// </summary>
        /// <param name="filePath">Physical location of <see cref="XvArchive"/> on the hard drive.</param>
        /// <returns>A new <see cref="XvArchive"/> of the specified archive file.</returns>
        public static XvArchive Open(string filePath) {
            if (!File.Exists(filePath))
                throw new FileNotFoundException();

            XvArchive archive = new XvArchive {
                PhysicalPath = filePath
            };

            using (Stream stream = File.OpenRead(filePath)) {
                using BinaryReader reader = new BinaryReader(stream);
                //* get the archive header
                XvArchiveHeader archiveHeader = XvArchiveHeader.Deserialize(reader);

                if (archiveHeader.Tag != XvArchiveHeader.EXPECTED_TAG)
                    throw new InvalidDataException($"Invalid tag ({archiveHeader.Tag})");

                archive.p_ArchiveHeader = archiveHeader;

                //* set the correct master key
                if ((archive.Flags & XvArchiveHeaderFlags.Encrypted) == XvArchiveHeaderFlags.Encrypted)
                    archive.p_MasterKey = SECONDARY_KEY;

                ulong rollingKey = archive.p_MasterKey ^ archive.p_ArchiveHeader.Hash;

                for (int entryIndex = 0; entryIndex < archive.p_ArchiveHeader.Count; entryIndex++) {
                    XvEntryHeader entryHeader = (archive.EncryptedEntryHeaders) ? XvEntryHeader.Deserialize(reader, rollingKey) : XvEntryHeader.Deserialize(reader);
                    archive.Entries.Add(entryHeader);

                    rollingKey = entryHeader.RollingKey;
                }
            }

            return archive;
        }
    }
}
