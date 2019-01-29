using DotNetty.Buffers;
using Netty.SuperSocket.RequestInfo;
using System.Text;

namespace Netty.SuperSocket.ReceiveFilter
{
    /// <summary>
    /// 基本命令解析
    /// </summary>
    public class BasicRequestInfoParser : IRequestInfoParser<StringRequestInfo>
    {
        private readonly string spliter;
        private readonly string parameterSpliter;

        public BasicRequestInfoParser(string spliter, string parameterSpliter)
        {
            this.spliter = spliter;
            this.parameterSpliter = parameterSpliter;
        }
        public StringRequestInfo Decode(string source)
        {
            var arr = source.Split(spliter.ToCharArray());
            return new StringRequestInfo
            {
                Key = arr[0],
                Body = arr[1],
                Args = arr[1].Split(parameterSpliter.ToCharArray())
            };
        }

        public void Encode(IByteBuffer byteBuffer, StringRequestInfo requestInfo)
        {
            var stringBuild = new StringBuilder();
            stringBuild.Append(requestInfo.Key);
            stringBuild.Append(spliter);
            for (int i = 0; i < requestInfo.Body.Length; i++)
            {
                stringBuild.Append(requestInfo.Body[i]);
                if (i != requestInfo.Body.Length - 1)
                    stringBuild.Append(parameterSpliter);
            }
            byteBuffer.WriteString(stringBuild.ToString(), encoding); 
        }
    }
}
