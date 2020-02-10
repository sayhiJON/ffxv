using System;
using System.Collections.Generic;
using System.Text;

//* non-default
using System.IO;

namespace ffxv_earc {
    public static class Extensions {
        public static string ReadString(this BinaryReader reader, int length) {
            return new string(reader.ReadChars(length));
        }

        public static string ReadStringNullTerminated(this BinaryReader reader) {
            StringBuilder builder = new StringBuilder();

            char next = reader.ReadChar();

            while (next != 0x00) {
                builder.Append(next);
                next = reader.ReadChar();
            }

            return builder.ToString();
        }
    }
}
