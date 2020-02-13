using System;

//* non-default
using ffxv_earc;
using ffxv_earc.Structures;
using System.Runtime.InteropServices;
using System.IO;

namespace ffxv_console {
    class Program {
        static void Main(string[] args) {
            string  binmod  = @"C:\Users\jon\Documents\My Games\FINAL FANTASY XV\Steam\76561197970216035\mod\ball_of_steel.ffxvbinmod",
                    earc1   = @"D:\Steam\steamapps\common\FINAL FANTASY XV\datas\character\nh\nh00\autoexternal.earc";

            XvArchive archive = XvArchive.Open(binmod);

            foreach (XvEntryHeader entry in archive.Entries) {
                string  filename    = Path.GetFileName(entry.Path),
                        directory   = Path.GetFullPath(Path.GetDirectoryName(entry.Path));

                if (!Directory.Exists(directory))
                    Directory.CreateDirectory(directory);

                using Stream stream = File.OpenWrite(Path.GetFullPath(entry.Path));
                using BinaryWriter writer = new BinaryWriter(stream);

                byte[] fileData = archive.ExtractToArray(entry);

                if (fileData.Length == 1 && fileData[0] == 0x00)
                    continue;

                writer.Write(fileData, 0, (int)entry.Size);
            }
        }
    }
}
