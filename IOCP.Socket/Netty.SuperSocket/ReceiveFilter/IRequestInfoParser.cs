using Netty.SuperSocket.RequestInfo;

namespace Netty.SuperSocket.ReceiveFilter
{
    /// <summary>
    /// 命令行解析
    /// </summary>
    /// <typeparam name="TRequestInfo"></typeparam>
    public interface IRequestInfoParser<TRequestInfo,T> where TRequestInfo : IRequestInfo
    {
        TRequestInfo Decode(string source);

        T Encode(StringRequestInfo requestInfo);
    }
}
