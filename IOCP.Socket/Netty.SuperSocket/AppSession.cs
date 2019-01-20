using DotNetty.Buffers;
using DotNetty.Transport.Channels;
using Netty.SuperSocket.RequestInfo;
using System;
using System.Net;
using System.Net.Sockets;
using System.Threading.Tasks;

namespace Netty.SuperSocket
{
    /// <summary>
    /// 客户端连接
    /// </summary>
    /// <typeparam name="TRequest">请求类型</typeparam>
    public abstract class AppSession<TSession, TRequest>
        where TRequest : IRequestInfo
        where TSession : AppSession<TSession, TRequest>, new()
    {

        public event EventHandler<IByteBuffer> Received;

        public event EventHandler Closed;

        /// <summary>
        /// 会话ID
        /// </summary>
        public string SessionId { get; internal set; }

        /// <summary>
        /// 会话实例
        /// </summary>
        internal IChannel Channel { get; set; }

        /// <summary>
        /// 远程终结点
        /// </summary>
        public EndPoint RemoteEndPoint => Channel.RemoteAddress;

        /// <summary>
        /// 发送数据
        /// </summary> 
        public Task Send(byte[] buffer, int offset, int length)
            => Channel.WriteAndFlushAsync(new ArraySegment<byte>(buffer, offset, length));

        /// <summary>
        /// 发送数据
        /// </summary> 
        public Task Send(IByteBuffer buffer) => Channel.WriteAndFlushAsync(buffer);

        /// <summary>
        /// 收到数据
        /// </summary> 
        public virtual Task OnReceiveDataAsync(IByteBuffer buffer)
        {
            Received?.Invoke(this, buffer);
            return Task.CompletedTask;
        }

        /// <summary>
        /// 收到请求
        /// </summary> 
        public virtual Task OnReceiveRequest(TRequest request) => Task.CompletedTask;


        public virtual Task OnClosed()
        {
            Closed?.Invoke(this, new EventArgs());
            return Task.CompletedTask;
        }

        /// <summary>
        /// 关闭连接
        /// </summary>
        /// <returns></returns>
        public virtual Task CloseAsync() => Channel.CloseAsync();
    }
}
