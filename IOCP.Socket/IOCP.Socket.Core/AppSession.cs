using IOCP.Socket.Core.RequestInfo;
using System;
using System.Net;
using System.Net.Sockets;

namespace IOCP.Socket.Core
{ 
    /// <summary>
    /// 客户端连接
    /// </summary>
    /// <typeparam name="TRequest">请求类型</typeparam>
    public abstract class AppSession<TSession, TRequest>
        where TRequest : IRequestInfo
        where TSession : AppSession<TSession, TRequest>, new()
    {        
        private System.Net.Sockets.Socket _socket;

        /// <summary>
        /// 连接已关闭
        /// </summary>
        public event EventHandler Closed;

        /// <summary>
        /// 收到数据
        /// </summary>
        public event EventHandler<DataEventArgs> Received;

        /// <summary>
        /// 客户端套接字
        /// </summary>
        internal System.Net.Sockets.Socket Socket
        {
            get => _socket; set
            {
                _socket = value;
                if (value != null)
                    RemoteEndPoint = value.RemoteEndPoint;
            }
        }

        /// <summary>
        /// 客户端地址
        /// </summary>
        public EndPoint RemoteEndPoint { get; private set; }

        /// <summary>
        /// 接收套接字
        /// </summary>
        internal SocketAsyncEventArgs ReceiveSAE { get; set; }

        /// <summary>
        /// 服务
        /// </summary>
        public virtual APPServer<TSession, TRequest> APPServer { get; internal set; }

        internal void SetClose()
        {
            ReceiveSAE = null;
            Socket = null;
            Closed?.Invoke(this, new EventArgs());
        }

        internal void SetReceiveData(byte[] bytes, int start, int length)
        {
            Received?.Invoke(this, new DataEventArgs() { Buffer = bytes, Length = length, Index = start });
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

        public virtual void SendAsync(byte[] bytes, int index, int length)
        {
            APPServer.SendAsync((TSession)(this), bytes, index, length); 
        }
    }
}
