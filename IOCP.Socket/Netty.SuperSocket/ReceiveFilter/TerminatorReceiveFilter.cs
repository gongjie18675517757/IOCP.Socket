using DotNetty.Buffers;
using DotNetty.Common.Utilities;
using Netty.SuperSocket.RequestInfo;
using System;
using System.Collections.Generic;
using System.Text;

namespace Netty.SuperSocket.ReceiveFilter
{
    /// <summary>
    /// 结束符协议
    /// </summary>
    public class TerminatorReceiveFilter : IReceiveFilter<StringRequestInfo>
    {
        private readonly Encoding encoding;
        private readonly string terminator;
        private readonly IRequestInfoParser<StringRequestInfo, string> requestInfoParser;


        public TerminatorReceiveFilter(Encoding encoding, string terminator) : this(encoding, terminator, new BasicRequestInfoParser(":", ","))
        {

        }

        public TerminatorReceiveFilter(Encoding encoding, string terminator, IRequestInfoParser<StringRequestInfo, string> requestInfoParser)
        {
            if (string.IsNullOrWhiteSpace(terminator))
            {
                throw new ArgumentException("分割符为空", nameof(terminator));
            }

            this.encoding = encoding;
            this.terminator = terminator;
            this.requestInfoParser = requestInfoParser;
        }

        public StringRequestInfo Decode(IByteBuffer buffer)
        {
            var eol = FindEndOfLine(buffer);
            if (eol > 0)
            {
                int length = eol - buffer.ReaderIndex;
                int delimLength = terminator.Length;
                var source = buffer.ToString(0, length, encoding);
                buffer.SkipBytes(length + delimLength);
                return requestInfoParser.Decode(source);
            }

            return null;
        }

        private int FindEndOfLine(IByteBuffer byteBuffer)
        {
            var chars = terminator.ToCharArray();
            var byteChar = (byte)chars[0];

            var index = byteBuffer.ForEachByte(new ByteProcessor(b => b != byteChar));
            if (index != -1)
            {
                for (int i = 1; i < chars.Length; i++)
                {
                    var chatByte = (byte)chars[i];
                    if (index > byteBuffer.ReadableBytes)
                    {
                        index = -1;
                        break;
                    }
                    var byteItem = byteBuffer.GetByte(index + 1);
                    if (chatByte == byteItem)
                        index += 1;
                    else
                    {
                        index = -1;
                        break;
                    }
                }

            }
            if (index != -1)
                index -= chars.Length - 1;
            return index;
        }

        public void Encode(IByteBuffer byteBuffer, StringRequestInfo requestInfo)
        {
            var str = requestInfoParser.Encode(requestInfo);
            byteBuffer.WriteString(str, encoding);
            byteBuffer.WriteString(terminator, encoding);
        }
    }
}
