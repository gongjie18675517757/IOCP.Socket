using System;
using System.Collections.Concurrent;
using System.Net;
using System.Net.Sockets;
using System.Threading;

namespace IOCP.SocketCore
{
    /// <summary>
    /// TCP服务
    /// </summary>
    public class TcpServer
    {
        private Socket listen;
        private volatile int isRun;
        private readonly int bufferLength;

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
        public int Backlog { get; private set; }

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
        public event EventHandler<SocketClient> NewConnection;

        /// <summary>
        /// 客户端断开事件
        /// </summary>
        public event EventHandler<SocketClient> Disconnected;

        /// <summary>
        /// 收到数据
        /// </summary>
        public event EventHandler<ReceiveDataArges> ReceiveData;

        /// <summary>
        /// 
        /// </summary>
        /// <param name="bufferLength">每个Receive的SocketAsyncEventArgs缓冲区大小</param>
        public TcpServer(int bufferLength = 1024, int backlog = 100)
        {
            this.bufferLength = bufferLength;
            Backlog = backlog;
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

            var local = new IPEndPoint(IPAddress.Any, port);
            listen = new Socket(local.AddressFamily, SocketType.Stream, ProtocolType.Tcp)
            {
                NoDelay = true
            };
            listen.Bind(local);
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
            var connection = new SocketClient
            {
                SocketAsyncEvent = socketReceiveAsync,
                RemoteEndPoint = socket.RemoteEndPoint,
                Socket = socket,
            };
            socketReceiveAsync.UserToken = connection;
            NewConnection?.Invoke(this, connection);
            StartReceive(connection);

        }

        /// <summary>
        /// 异步发送
        /// </summary>
        /// <param name="connection"></param>
        /// <param name="bytes"></param>
        public void SendAsync(SocketClient connection, byte[] bytes)
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
        private void StartReceive(SocketClient connection)
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
                    var connection = (SocketClient)e.UserToken;
                    var bytes = new byte[e.BytesTransferred];
                    Interlocked.Add(ref receiveBytesCount, bytes.Length);
                    Buffer.BlockCopy(e.Buffer, e.Offset, bytes, 0, bytes.Length);

                    ReceiveData?.Invoke(this, new ReceiveDataArges(connection, bytes));
                    StartReceive(connection);
                }
            }
            else
            {
                OnConnectionClose((SocketClient)e.UserToken);
            }
        }

        /*连接已关闭*/
        private void OnConnectionClose(SocketClient connection)
        {
            var e = connection.SocketAsyncEvent;
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
            connection.SocketAsyncEvent = null;
            connection.Socket = null;

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

        public void Close(SocketClient socketClient)
        {
            OnConnectionClose(socketClient);
        }
    }
}
