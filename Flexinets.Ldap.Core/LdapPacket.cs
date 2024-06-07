using System;
using System.Diagnostics;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace Flexinets.Ldap.Core
{
    public class LdapPacket : LdapAttribute
    {        
        public int MessageId => ChildAttributes[0].GetValue<int>();


        /// <summary>
        /// Create a new Ldap packet with message id
        /// </summary>
        /// <param name="messageId"></param>
        public LdapPacket(int messageId) : base(UniversalDataType.Sequence)
        {
            ChildAttributes.Add(new LdapAttribute(UniversalDataType.Integer, messageId));
        }


        /// <summary>
        /// Create a packet with tag
        /// </summary>
        /// <param name="tag"></param>
        private LdapPacket(Tag tag) : base(tag)
        {
        }


        /// <summary>
        /// Parse an ldap packet from a byte array. 
        /// Must be the complete packet
        /// </summary>
        /// <param name="bytes"></param>
        /// <returns></returns>
        public static LdapPacket ParsePacket(byte[] bytes)
        {
            var packet = new LdapPacket(Tag.Parse(bytes[0]));
            var contentLength = Utils.BerLengthToInt(bytes, 1, out var lengthBytesCount);
            packet.ChildAttributes.AddRange(ParseAttributes(bytes, 1 + lengthBytesCount, contentLength));
            return packet;
        }


        /// <summary>
        /// Try parsing an ldap packet from a stream
        /// </summary>      
        /// <param name="stream"></param>
        /// <param name="packet"></param>
        /// <returns>True if successful. False if parsing fails or stream is empty</returns>
        public static bool TryParsePacket(Stream stream, out LdapPacket? packet)
        {
            try
            {
                var tagByte = new byte[1];
                var i = stream.Read(tagByte, 0, 1);
                if (i != 0)
                {
                    var contentLength = Utils.BerLengthToInt(stream, out int n);
                    var contentBytes = new byte[contentLength];
                    _ = stream.Read(contentBytes, 0, contentLength);

                    packet = ParsePacket(tagByte[0], contentBytes);
                    return true;
                }
            }
            catch (Exception ex)
            {
                Trace.TraceError($"Could not parse packet from stream {ex.Message}");                
            }

            packet = null;
            return false;
        }

        /// <summary>
        /// Try parsing an ldap packet from a stream asynchronously
        /// </summary>
        /// <param name="stream">Stream</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>Task returning packet if successful</returns>
        public static async Task<LdapPacket?> ParsePacketAsync(Stream stream, CancellationToken cancellationToken = default)
        {
            try
            {
                var tagByte = new byte[1];
                var i = await stream.ReadAsync(tagByte, 0, 1, cancellationToken).ConfigureAwait(false);
                if (i != 0)
                {
                    (var contentLength, _) = await Utils.BerLengthToIntAsync(stream, cancellationToken).ConfigureAwait(false);
                    var contentBytes = new byte[contentLength];
                    _ = await stream.ReadAsync(contentBytes, 0, contentLength, cancellationToken).ConfigureAwait(false);

                    return ParsePacket(tagByte[0], contentBytes);
                }
            }
            catch (Exception ex)
            {
                Trace.TraceError($"Could not parse packet from stream {ex.Message}");
            }

            return null;
        }

        private static LdapPacket ParsePacket(byte tagByte, byte[] contentBytes)
        {
            var packet = new LdapPacket(Tag.Parse(tagByte));
            packet.ChildAttributes.AddRange(ParseAttributes(contentBytes, 0, contentBytes.Length));
            return packet;
        }
    }
}
