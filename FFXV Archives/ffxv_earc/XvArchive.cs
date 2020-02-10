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

        public static readonly byte[] AESKey = new byte[] { 0x9C, 0x6C, 0x5D, 0x41, 0x15, 0x52, 0x3F, 0x17, 0x5A, 0xD3, 0xF8, 0xB7, 0x75, 0x58, 0x1E, 0xCF };

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
        public string PhysicalPath {
            get; set;
        } = string.Empty;

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

        public bool EncryptedEntryHeaders {
            get => this.p_ArchiveHeader.EncryptedEntryHeaders;
            set => this.p_ArchiveHeader.EncryptedEntryHeaders = true;
        }

        private XvArchiveHeader p_ArchiveHeader = new XvArchiveHeader();
        private ulong p_MasterKey = MasterArchiveKeyA;
        private List<XvEntryHeader> p_EntryHeaders = new List<XvEntryHeader>();

        public static XvArchive Open(string filePath) {
            if (!File.Exists(filePath))
                throw new FileNotFoundException();

            XvArchive archive = new XvArchive {
                PhysicalPath = filePath
            };

            using (Stream stream = File.OpenRead(filePath)) {
                using (BinaryReader reader = new BinaryReader(stream)) {
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
                        archive.p_EntryHeaders.Add(entryHeader);

                        rollingKey = entryHeader.OffsetDataKey;
                    }
                }
            }

            return archive;
        }
    }
}
