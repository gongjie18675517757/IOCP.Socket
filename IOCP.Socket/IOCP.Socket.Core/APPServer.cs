using IOCP.Socket.Core.Config;
using IOCP.Socket.Core.RequestInfo;
using Microsoft.Extensions.Options;
using System;
using System.Collections.Concurrent;
using System.Net;
using System.Net.Sockets;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;

namespace IOCP.Socket.Core
{
    public abstract class APPServer<TSession, TRequest>
        where TRequest : IRequestInfo
        where TSession : AppSession<TSession, TRequest>, new()
    {
        private System.Net.Sockets.Socket listen;
        private volatile int isRun;
        private readonly IOptions<ServerConfig> options;

        /// <summary>
        /// 缓存异步套接字字段[接收]
        /// </summary>
        private ConcurrentQueue<SocketAsyncEventArgs> receiveAsyncEventQueue
            = new ConcurrentQueue<SocketAsyncEventArgs>();

        private ConcurrentQueue<SocketAsyncEventArgs> sendAsyncEventQueue
          = new ConcurrentQueue<SocketAsyncEventArgs>();

        private volatile int connectionCount;
        private volatile int sendBytesCount;
        private volatile int receiveBytesCount;
        private DateTime startTime;
        private DateTime stopTime;

        /// <summary>
        /// 已连接的数量
        /// </summary>
        public int ConnectionCount => connectionCount;

        /// <summary>
        /// 总发出的字节数
        /// </summary>
        public int SendBytesCount => sendBytesCount;

        /// <summary>
        /// 总接收的字节数
        /// </summary>
        public int ReceiveBytesCount => receiveBytesCount;

        /// <summary>
        /// 服务运行时间
        /// </summary>
        public TimeSpan RunTime
        {
            get
            {
                if (startTime == default(DateTime))
                    return TimeSpan.FromSeconds(0);
                if (stopTime == default(DateTime))
                    return DateTime.Now - startTime;

                return stopTime - startTime;
            }
        }

        /// <summary>
        /// 是否正在运行
        /// </summary>
        public bool IsRun => isRun == 1;

        /// <summary>
        /// 本地终结点
        /// </summary>
        public EndPoint LocalEndPoint => listen?.LocalEndPoint;

        /// <summary>
        /// 服务开始运行
        /// </summary>
        public event EventHandler ServerStarted;

        /// <summary>
        /// 服务停止运行
        /// </summary>
        public event EventHandler ServerStopped;

        /// <summary>
        /// 客户端连接事件
        /// </summary>
        public event EventHandler<TSession> NewSession;

        /// <summary>
        /// 客户端断开事件
        /// </summary>
        public event EventHandler<TSession> SessionClosed;

        /// <summary>
        /// 收到数据
        /// </summary>
        public event EventHandler<TSession> ReceiveData;


        public APPServer(IOptions<ServerConfig> options)
        {
            this.options = options;
        }

        /// <summary>
        /// 开始监听
        /// </summary> 
        public Task RunAsync(CancellationToken cancellationToken)
        {
            connectionCount = 0;
            sendBytesCount = 0;
            receiveBytesCount = 0;

            startTime = DateTime.Now;
            stopTime = default(DateTime);

            listen = new System.Net.Sockets.Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp)
            {
                NoDelay = true,
                ReceiveBufferSize = options.Value.ReceiveBufferSize,
                SendBufferSize = options.Value.SendBufferSize,
                SendTimeout = options.Value.SendTimeOut * 1000,
            };

            foreach (var item in options.Value.Listeners)
            {
                var local = new IPEndPoint(IPAddress.Parse(item.Ip), item.Post);
                listen.Bind(local);
            }

            listen.Listen(options.Value.ListenBacklog);

            isRun = 1;
            ServerStarted?.Invoke(this, new EventArgs());
            OnServerStarted();
            startAccept();

            var asyncTaskMethodBuilder = AsyncTaskMethodBuilder.Create();

            cancellationToken.Register(new Action(() =>
            {
                this.Stop();
                asyncTaskMethodBuilder.SetResult();
            }));
            return asyncTaskMethodBuilder.Task;
        }

        /// <summary>
        /// 服务启动
        /// </summary>
        protected void OnServerStarted() { }

        /*开始接收套接字*/
        private void startAccept(SocketAsyncEventArgs socketAsync = null)
        {
            if (!IsRun || listen == null)
                return;

            if (socketAsync == null)
            {
                socketAsync = new SocketAsyncEventArgs();
                socketAsync.Completed += OnAcceptNewConnection;
            }
            else
            {
                socketAsync.AcceptSocket = null;
            }

            if (!listen.AcceptAsync(socketAsync))
            {
                OnAcceptNewConnection(listen, socketAsync);
            }
        }

        /*接收新的连接*/
        private void OnAcceptNewConnection(object sender, SocketAsyncEventArgs e)
        {
            /*新连接的socket对象*/
            var socket = e.AcceptSocket;

            /*继续接收*/
            startAccept(e);

            /*连接数+1*/
            Interlocked.Increment(ref connectionCount);


            /*尝试取出一个异步完成套接字*/
            if (!receiveAsyncEventQueue.TryDequeue(out var socketReceiveAsync))
            {
                socketReceiveAsync = new SocketAsyncEventArgs();
                socketReceiveAsync.Completed += SocketAsync_Receive_Completed;
                socketReceiveAsync.SetBuffer(new byte[1024], 0, 1024);
            }

            /*创建一个客户端*/
            var session = new TSession
            {
                Socket = socket,
                ReceiveSAE = socketReceiveAsync,
                APPServer = this,
            };

            socketReceiveAsync.UserToken = session;
            NewSession?.Invoke(this, session);
            OnNewSession(session);

            StartReceive(session);
        }

        /// <summary>
        /// 收到新的连接
        /// </summary>
        /// <param name="session"></param>
        protected virtual void OnNewSession(TSession session) { }

        /// <summary>
        /// 异步发送
        /// </summary> 
        public void SendAsync(TSession connection, byte[] bytes, int index, int length)
        {
            if (!IsRun || listen == null)
            {
                return;
            }
            if (!sendAsyncEventQueue.TryDequeue(out SocketAsyncEventArgs socketSendAsync))
            {
                socketSendAsync = new SocketAsyncEventArgs();
                socketSendAsync.Completed += SocketSendAsync_Send_Completed;
            }
            try
            {
                socketSendAsync.UserToken = connection;
                socketSendAsync.SetBuffer(bytes, index, length);
                if (!connection.Socket.SendAsync(socketSendAsync))
                {
                    SocketSendAsync_Send_Completed(listen, socketSendAsync);
                }
            }
            catch (Exception)
            {
                OnConnectionClose(connection);
                if (socketSendAsync != null)
                    sendAsyncEventQueue.Enqueue(socketSendAsync);
            }
        }

        /*发送完成*/
        private void SocketSendAsync_Send_Completed(object sender, SocketAsyncEventArgs e)
        {
            e.UserToken = null;
            e.AcceptSocket = null;
            sendAsyncEventQueue.Enqueue(e);
            Interlocked.Add(ref sendBytesCount, e.Buffer.Length);
        }

        /*开始接收数据*/
        private void StartReceive(TSession connection)
        {
            if (!IsRun || listen == null)
            {
                return;
            }

            if (!connection.Socket.ReceiveAsync(connection.ReceiveSAE))
                SocketAsync_Receive_Completed(listen, connection.ReceiveSAE);
        }

        /*接收到新的数据*/
        private void SocketAsync_Receive_Completed(object sender, SocketAsyncEventArgs e)
        {
            if (e.BytesTransferred > 0 && e.SocketError == SocketError.Success)
            {
                if (e.LastOperation == SocketAsyncOperation.Receive)
                {
                    var session = (TSession)e.UserToken;
                    session.SetReceiveData(e.Buffer, e.Offset, e.BytesTransferred);                  
                    ReceiveData?.Invoke(this, session);
                    StartReceive(session);
                }
            }
            else
            {
                OnConnectionClose((TSession)e.UserToken);
            }
        }

        /*连接已关闭*/
        private void OnConnectionClose(TSession connection)
        {
            var e = connection.ReceiveSAE;
            if (e == null)
                return;
            e.AcceptSocket = null;
            e.UserToken = null;
            receiveAsyncEventQueue.Enqueue(e);

            try
            {
                connection.Socket.Shutdown(SocketShutdown.Both);
                connection.Socket.Close();
            }
            catch (Exception) { }

            Interlocked.Decrement(ref connectionCount);
            connection.SetClose();
            SessionClosed?.Invoke(this, connection);
            OnSessionClosed(connection);
        }

        /// <summary>
        /// 连接关闭
        /// </summary>
        /// <param name="session">会话</param>
        protected virtual void OnSessionClosed(TSession session) { }

        /// <summary>
        /// 停止运行
        /// </summary>
        public void Stop()
        {
            if (IsRun)
                Interlocked.Decrement(ref isRun);
            try
            {
                listen?.Close();
                listen?.Shutdown(SocketShutdown.Both);
            }
            catch (Exception)
            {
            }

            listen = null;

            stopTime = DateTime.Now;
            ServerStopped?.Invoke(this, new EventArgs());
            OnServerStopped();
        }

        /// <summary>
        /// 服务已停止
        /// </summary>
        protected void OnServerStopped() { }

        public void Close(TSession socketClient)
        {
            OnConnectionClose(socketClient);
        }
    }
}
