using System;
using System.Collections.Generic;
using System.Text;

//* non-default
using System.IO;

namespace ffxv_earc {
    public static class Extensions {
        /// <summary>
        /// Reads a string from the underlying stream of the specified length using the encoding of the stream.
        /// </summary>
        /// <param name="reader"></param>
        /// <param name="length">Length of the string to read.</param>
        /// <returns>A new string object containing the characters read from the stream.</returns>
        public static string ReadString(this BinaryReader reader, int length) {
            return new string(reader.ReadChars(length));
        }

        /// <summary>
        /// Reads a null-terminated string from the underlying stream using the encoding of the stream.
        /// </summary>
        /// <param name="reader"></param>
        /// <returns>A new string object containing the characters read from the stream.</returns>
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
