using IOCP.Socket.Core.RequestInfo;
using Microsoft.Extensions.Options;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;
using System.Threading;

namespace IOCP.Socket.Core
{
    /// <summary>
    /// 监听模式
    /// </summary>
    public enum ListenMode
    {
        Tcp,
        Udp
    }

    /// <summary>
    /// 监听信息
    /// </summary>
    public class Listener
    {
        /// <summary>
        /// IP
        /// </summary>
        public IPAddress Ip { get; set; } = IPAddress.Any;

        /// <summary>
        /// 端口
        /// </summary>
        public int Post { get; set; } = 0;
    }

    /// <summary>
    /// 服务配置
    /// </summary>
    public class ServerConfig
    {
        /// <summary>
        /// 监听模式
        /// </summary>
        public ListenMode Mode { get; set; } = ListenMode.Tcp;

        /// <summary>
        /// 监听信息
        /// </summary>
        public IEnumerable<Listener> Listeners { get; set; } = new Listener[] { new Listener() };

        /// <summary>
        ///  监听队列的大小
        /// </summary>
        public int ListenBacklog { get; set; } = 5;

        /// <summary>
        /// 发送超时
        /// </summary>
        public int SendTimeOut { get; internal set; } = 10;

        /// <summary>
        /// 可允许连接的最大连接数
        /// </summary>
        public int MaxConnectionNumber { get; set; } = 100;

        /// <summary>
        /// 接收缓冲区大小
        /// </summary>
        public int ReceiveBufferSize { get; set; } = 1024;

        /// <summary>
        /// 发送缓冲区大小
        /// </summary>
        public int SendBufferSize { get; set; } = 1024;

        /// <summary>
        /// 是否记录命令执行的记录
        /// </summary>
        public bool LogCommand { get; set; } = true;

        /// <summary>
        /// 是否记录session的基本活动，如连接和断开
        /// </summary>
        public bool LogBasicSessionActivity { get; set; } = true;

        /// <summary>
        /// 是否记录所有Socket异常和错误
        /// </summary>
        public bool LogAllSocketException { get; set; } = true;

        /// <summary>
        /// 是否定时清空空闲会话，默认值是 false
        /// </summary>
        public bool ClearIdleSession { get; set; } = false;

        /// <summary>
        /// 清空空闲会话的时间间隔, 默认值是120, 单位为秒
        /// </summary>
        public int ClearIdleSessionInterval { get; set; } = 120;

        /// <summary>
        /// 会话空闲超时时间; 当此会话空闲时间超过此值，同时clearIdleSession被配置成true时，此会话将会被关闭; 默认值为300，单位为秒
        /// </summary>
        public int IdleSessionTimeOut { get; set; }

        /// <summary>
        /// 最大允许的请求长度，默认值为1024
        /// </summary>
        public int MaxRequestLength { get; set; }

        /// <summary>
        /// 文本的默认编码，默认值是 ASCII
        /// </summary>
        public string TextEncoding { get; set; }
    }


    public abstract class APPServer<TSession, TRequest>
        where TRequest : IRequestInfo
        where TSession : AppSession<TRequest>, new()
    {
        private System.Net.Sockets.Socket listen;
        private volatile int isRun;
        private readonly int bufferLength;
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
        /// 最大连接队列数量
        /// </summary>
        public int Backlog => options.Value.ListenBacklog;

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
        public event EventHandler ServerStartRun;

        /// <summary>
        /// 服务停止运行
        /// </summary>
        public event EventHandler ServerStopRun;

        /// <summary>
        /// 客户端连接事件
        /// </summary>
        public event EventHandler<TSession> NewConnection;

        /// <summary>
        /// 客户端断开事件
        /// </summary>
        public event EventHandler<TSession> Disconnected;

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
        /// <param name="port"></param>
        public void Start(int port = 0)
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
                var local = new IPEndPoint(item.Ip, item.Post);
                listen.Bind(local);
            }

            listen.Listen(Backlog);


            isRun = 1;
            ServerStartRun?.Invoke(this, new EventArgs());
            startAccept();
        }

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
                socketReceiveAsync.SetBuffer(new byte[bufferLength], 0, bufferLength);
            }

            /*创建一个客户端*/
            var connection = new TSession
            {
                Socket = socket,
                SocketAsyncEvent = socketReceiveAsync,
            };
            connection.SendAction = bytes => SendAsync(connection, bytes);

            socketReceiveAsync.UserToken = connection;
            NewConnection?.Invoke(this, connection);
            StartReceive(connection);
        }

        /// <summary>
        /// 异步发送
        /// </summary> 
        public void SendAsync(TSession connection, byte[] bytes)
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
                socketSendAsync.SetBuffer(bytes, 0, bytes.Length);
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

            if (!connection.Socket.ReceiveAsync(connection.SocketAsyncEvent))
                SocketAsync_Receive_Completed(listen, connection.SocketAsyncEvent);
        }

        /*接收到新的数据*/
        private void SocketAsync_Receive_Completed(object sender, SocketAsyncEventArgs e)
        {
            if (e.BytesTransferred > 0 && e.SocketError == SocketError.Success)
            {
                if (e.LastOperation == SocketAsyncOperation.Receive)
                {
                    var connection = (TSession)e.UserToken;
                    connection.SetReceiveData(e.Buffer, e.Offset, e.BytesTransferred);
                    StartReceive(connection);
                    ReceiveData?.Invoke(this, connection);
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
            var e = connection.SocketAsyncEvent;
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
            Disconnected?.Invoke(this, connection);
        }

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
            ServerStopRun?.Invoke(this, new EventArgs());
        }

        public void Close(TSession socketClient)
        {
            OnConnectionClose(socketClient);
        }
    }
}
