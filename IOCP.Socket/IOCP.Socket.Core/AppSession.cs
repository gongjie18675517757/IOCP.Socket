using IOCP.Socket.Core.RequestInfo;
using System;
using System.IO.Pipelines;
using System.Net;
using System.Net.Sockets;

namespace IOCP.Socket.Core
{
    /// <summary>
    /// 客户端连接
    /// </summary>
    /// <typeparam name="TRequest">请求类型</typeparam>
    public class AppSession<TRequest> where TRequest : IRequestInfo
    {
        private readonly Pipe receivePipe;
        private System.Net.Sockets.Socket _socket;

        /// <summary>
        /// 连接已关闭
        /// </summary>
        public event EventHandler Closed;

        /// <summary>
        /// 收到数据
        /// </summary>
        public event EventHandler Received;

        /// <summary>
        /// 读取管道
        /// </summary>
        public PipeReader PipeReader { get; }

        /// <summary>
        /// 写数据管道
        /// </summary>
        internal PipeWriter PipeWriter { get; }

        /// <summary>
        /// 客户端套接字
        /// </summary>
        internal System.Net.Sockets.Socket Socket
        {
            get => _socket; set
            {
                _socket = value;
                RemoteEndPoint = value.RemoteEndPoint;
            }
        }

        /// <summary>
        /// 客户端地址
        /// </summary>
        public EndPoint RemoteEndPoint { get; private set; }

        /// <summary>
        /// 异步套接字
        /// </summary>
        internal SocketAsyncEventArgs SocketAsyncEvent { get; set; }

        /// <summary>
        /// 发送数据的方法
        /// </summary>
        internal Action<byte[]> SendAction { get; set; }

        public AppSession()
        {
            receivePipe = new Pipe();
            this.PipeReader = receivePipe.Reader;
            this.PipeWriter = receivePipe.Writer;
        }

        internal void SetClose()
        {
            SocketAsyncEvent = null;
            Socket = null;
            SendAction = null;
            Closed?.Invoke(this, new EventArgs());
        }

        internal void SetReceiveData(byte[] bytes, int start, int length)
        {
            PipeWriter.WriteAsync(new ReadOnlyMemory<byte>(bytes, start, length)).AsTask().ContinueWith(t =>
            {
                Received?.Invoke(this, new EventArgs());
            });
        }

        /// <summary>
        /// 连接成功
        /// </summary>
        protected virtual void OnSessionInited() { }

        /// <summary>
        /// 收到数据
        /// </summary>
        protected virtual void OnReceive() { }

        /// <summary>
        /// 连接被关闭
        /// </summary>
        protected virtual void OnClosed() { }

        public virtual void SendAsync(byte[] bytes)
        {
            SendAction?.Invoke(bytes);
        }
    }
}
