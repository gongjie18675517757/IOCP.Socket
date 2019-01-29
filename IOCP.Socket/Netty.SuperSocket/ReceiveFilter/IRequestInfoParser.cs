using Netty.SuperSocket.RequestInfo;

namespace Netty.SuperSocket.ReceiveFilter
{
    /// <summary>
    /// 命令行解析
    /// </summary>
    /// <typeparam name="TRequestInfo"></typeparam>
    public interface IRequestInfoParser<TRequestInfo> where TRequestInfo : IRequestInfo
    {
        TRequestInfo Decode(string source);

        void Encode(DotNetty.Buffers.IByteBuffer byteBuffer, StringRequestInfo requestInfo);
    }
}
