using DotNetty.Buffers;
using DotNetty.Common.Utilities;
using Netty.SuperSocket.RequestInfo;
using System.Text;

namespace Netty.SuperSocket.ReceiveFilter
{
    /// <summary>
    /// 命令行解析
    /// </summary>
    public class CommandLineReceiveFilter : IReceiveFilter<StringRequestInfo>
    {
        private readonly Encoding encoding;
        private readonly IRequestInfoParser<StringRequestInfo> requestInfoParser;

        public CommandLineReceiveFilter(Encoding encoding, IRequestInfoParser<StringRequestInfo> requestInfoParser)
        {
            this.encoding = encoding;
            this.requestInfoParser = requestInfoParser;
        }

        public CommandLineReceiveFilter()
        {
            encoding = Encoding.UTF8;
            requestInfoParser = new BasicRequestInfoParser(":", ",");
        }

        public StringRequestInfo Decode(IByteBuffer buffer)
        {
            var readerIndex = buffer.ReaderIndex;
            var eol = FindEndOfLine(buffer);
            if (eol > 0)
            {
                int length = eol - buffer.ReaderIndex;
                int delimLength = buffer.GetByte(eol) == '\r' ? 2 : 1;
                var source = buffer.ToString(0, length, encoding);
                buffer.SkipBytes(length + delimLength);
                return requestInfoParser.Decode(source);
            }
            return null;
        }

        int FindEndOfLine(IByteBuffer buffer)
        {
            int i = buffer.ForEachByte(ByteProcessor.FindLF);
            if (i > 0 && buffer.GetByte(i - 1) == '\r')
            {
                i--;
            }

            return i;
        }

        public void Encode(IByteBuffer byteBuffer, StringRequestInfo requestInfo)
        {
            var str = requestInfoParser.Encode(requestInfo);
            byteBuffer.WriteString(str, encoding);
        }
    }
}
