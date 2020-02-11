using System;

//* non-default
using System.Collections.Generic;
using System.IO;
using ffxv_earc.Structures;

namespace ffxv_earc {
    public class XvArchive {
        public const ulong  MasterArchiveKeyA   = unchecked(0xCBF29CE484222325),
                            MasterArchiveKeyB   = unchecked(0x40D4CCA269811DAF),
                            MasterFileHash      = unchecked(0x14650FB0739D0383),
                            MasterFileKey       = unchecked(0x100000001B3),
                            MasterChunkKeyA     = unchecked(0x10E64D70C2A29A69),
                            MasterChunkKeyB     = unchecked(0xC63D3dC167E);

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
            set {
                XvArchiveHeaderFlags flags = this.p_ArchiveHeader.Flags;

                //* if nothing is actually changing, do nothing
                if (value == flags)
                    return;

                //* are we setting or removing the encrypted flag?
                if ((value & XvArchiveHeaderFlags.Encrypted) == XvArchiveHeaderFlags.Encrypted && (flags & XvArchiveHeaderFlags.Encrypted) == XvArchiveHeaderFlags.None)
                    //* we're setting the encrypted flag
                    this.p_MasterKey = MasterArchiveKeyB;
                else if ((value & XvArchiveHeaderFlags.Encrypted) == XvArchiveHeaderFlags.None && (flags & XvArchiveHeaderFlags.Encrypted) == XvArchiveHeaderFlags.Encrypted)
                    //* we're removing the encrypted flag
                    this.p_MasterKey = MasterArchiveKeyA;

                this.p_ArchiveHeader.Flags = value;
            }
        }

        /// <summary>
        /// Gets or sets whether the entry headers are encrypted.
        /// </summary>
        public bool EncryptedEntryHeaders {
            get => this.p_ArchiveHeader.EncryptedEntryHeaders;
            set => this.p_ArchiveHeader.EncryptedEntryHeaders = value;
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
        private ulong p_MasterKey = MasterArchiveKeyA;

        public void Extract(int entryIndex) {
            if (entryIndex >= this.Entries.Count)
                throw new IndexOutOfRangeException();

            this.Extract(this.Entries[entryIndex]);
        }

        public void Extract(XvEntryHeader entry) {
            //* make sure the archive still exists for whatever reason
            if (!File.Exists(this.PhysicalPath))
                throw new FileNotFoundException();

            //* we need to open the archive
            using (Stream stream = File.OpenRead(this.PhysicalPath)) {
                using (BinaryReader reader = new BinaryReader(stream)) {

                }
            }
        }

        private static byte[] ReadEntryData(BinaryReader reader, long position, int size) {
            reader.BaseStream.Seek(position, SeekOrigin.Begin);

            return reader.ReadBytes(size);
        }

        /// <summary>
        /// Opens and reads the archive information at the specified file path and stores it in a new <see cref="XvArchive"/>.
        /// </summary>
        /// <param name="filePath">Physical location of archive on the hard drive.</param>
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
                    archive.p_MasterKey = MasterArchiveKeyB;

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
