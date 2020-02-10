using System;

//* non-default
using ffxv_earc;
using System.Runtime.InteropServices;

namespace ffxv_console {
    class Program {
        static void Main(string[] args) {
            string  binmod  = @"C:\Users\jon\Documents\My Games\FINAL FANTASY XV\Steam\76561197970216035\mod\ball_of_steel.ffxvbinmod",
                    earc1   = @"D:\Steam\steamapps\common\FINAL FANTASY XV\datas\character\nh\nh00\autoexternal.earc";

            XvArchive archive = XvArchive.Open(binmod);

            long memory = GC.GetTotalMemory(true);

            XvArchive archive2 = XvArchive.Open(binmod);

            long newmemory = GC.GetTotalMemory(true);

            long difference = newmemory - memory;

            Console.WriteLine($"{difference}");
        }
    }
}
