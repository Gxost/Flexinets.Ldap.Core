using System;
using System.Collections;
using System.Text;
using System.Linq;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace Flexinets.Ldap.Core
{
    public static class Utils
    {
        public static byte[] StringToByteArray(string hex, bool trimWhitespace = true)
        {
            if (trimWhitespace)
            {
                hex = hex.Replace(" ", "");
            }

            int NumberChars = hex.Length;
            byte[] bytes = new byte[NumberChars / 2];
            for (int i = 0; i < NumberChars; i += 2)
            {
                bytes[i / 2] = Convert.ToByte(hex.Substring(i, 2), 16);
            }
            return bytes;
        }

        public static string ByteArrayToString(byte[] bytes)
        {
            var hex = new StringBuilder(bytes.Length * 2);
            foreach (var b in bytes)
            {
                hex.Append($"{b:x2}");
            }
            return hex.ToString();
        }


        /// <summary>
        /// Used for debugging and testing...
        /// </summary>
        /// <param name="bits"></param>
        /// <returns></returns>
        public static string BitsToString(BitArray bits)
        {
            int i = 1;
            var derp = "";
            foreach (var bit in bits)
            {
                derp += Convert.ToInt32(bit);
                if (i % 8 == 0)
                {
                    derp += " ";
                }
                i++;
            }
            return derp.Trim();
        }


        /// <summary>
        /// Convert integer length to a byte array with BER encoding
        /// https://en.wikipedia.org/wiki/X.690#BER_encoding
        /// </summary>
        /// <param name="length"></param>
        /// <returns></returns>
        public static byte[] IntToBerLength(int length)
        {
            // Short notation
            if (length <= 127)
            {
                return new byte[] { (byte)length };
            }
            // Long notation
            else
            {
                var intbytes = BitConverter.GetBytes(length);
                Array.Reverse(intbytes);

                byte intbyteslength = (byte)intbytes.Length;


                var lengthByte = intbyteslength + 128;
                var berBytes = new byte[1 + intbyteslength];
                berBytes[0] = (byte)lengthByte;
                Buffer.BlockCopy(intbytes, 0, berBytes, 1, intbyteslength);
                return berBytes;
            }
        }


        /// <summary>
        /// Convert BER encoded length at offset to an integer
        /// </summary>
        /// <param name="bytes">Byte array</param>
        /// <param name="offset">Offset where the BER encoded length is located</param>
        /// <param name="berByteCount">Number of bytes used to represent BER encoded length</param>
        /// <returns></returns>
        public static int BerLengthToInt(byte[] bytes, int offset, out int berByteCount)
        {
            var stream = new MemoryStream(bytes, offset, bytes.Length - offset, false);
            return BerLengthToInt(stream, out berByteCount);
        }

        /// <summary>
        /// Get a BER length from a stream
        /// </summary>
        /// <param name="stream">Stream at position where BER length should be found</param>
        /// <param name="berByteCount">Number of bytes used to represent BER encoded length</param>
        /// <returns></returns>
        public static int BerLengthToInt(Stream stream, out int berByteCount)
        {
            var berByte = new byte[1];
            _ = stream.Read(berByte, 0, 1);
            if (!IsLongNotation(berByte[0], out int lengthBytesLength))
            {
                berByteCount = 1;
                return berByte[0];
            }

            var lengthBytes = new byte[lengthBytesLength];
            _ = stream.Read(lengthBytes, 0, lengthBytesLength);

            berByteCount = lengthBytesLength + 1;
            return GetLongLength(lengthBytes);
        }

        /// <summary>
        /// Get a BER length from a stream asynchronously
        /// </summary>
        /// <param name="stream">Stream at position where BER length should be found</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>Task returning tuple containing length and byte count</returns>
        public static async Task<(int Length, int ByteCount)> BerLengthToIntAsync(Stream stream, CancellationToken cancellationToken = default)
        {
            var berByte = new byte[1];
            _ = await stream.ReadAsync(berByte, 0, 1, cancellationToken).ConfigureAwait(false);
            if (!IsLongNotation(berByte[0], out int lengthBytesLength))
            {
                return (berByte[0], 1);
            }

            var lengthBytes = new byte[lengthBytesLength];
            _ = await stream.ReadAsync(lengthBytes, 0, lengthBytesLength, cancellationToken).ConfigureAwait(false);

            return (GetLongLength(lengthBytes), lengthBytesLength + 1);
        }

        public static string Repeat(string stuff, int n)
        {
            return string.Concat(Enumerable.Repeat(stuff, n));
        }

        private static bool IsLongNotation(byte berByte, out int lengthBytesLength)
        {
            bool isLongNotation = berByte >> 7 == 1;
            lengthBytesLength = isLongNotation? berByte & 127 : 0;

            return isLongNotation;
        }

        private static int GetLongLength(byte[] bytes)
        {
            Array.Reverse(bytes);
            Array.Resize(ref bytes, 4);   // this will of course explode if length is larger than a 32 bit integer
            return BitConverter.ToInt32(bytes);
        }
    }
}
