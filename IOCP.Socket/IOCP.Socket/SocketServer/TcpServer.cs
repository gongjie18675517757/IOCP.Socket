using System;
using System.Collections.Concurrent;
using System.Net;
using System.Net.Sockets;
using System.Threading;

namespace IOCP.SocketCore.SocketServer
{
    /// <summary>
    /// TCP服务
    /// </summary>
    public class TcpServer : ITcpServer
    {
        private Socket listen;
        private int isRun;
        private readonly int bufferLength;
        private ConcurrentQueue<SocketAsyncEventArgs> queue = new ConcurrentQueue<SocketAsyncEventArgs>();
        private int connectionCount;
        private int sendBytesCount;
        private int receiveBytesCount;
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
        public bool IsRun => isRun==1;

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
        public event EventHandler<TcpConnectionedArges> NewConnection;

        /// <summary>
        /// 客户端断开事件
        /// </summary>
        public event EventHandler<TcpConnectionedArges> Disconnected;

        /// <summary>
        /// 收到数据
        /// </summary>
        public event EventHandler<TcpReceiveDataArges> ReceiveData; 

        /// <summary>
        /// 
        /// </summary>
        /// <param name="bufferLength">每个Receive的SocketAsyncEventArgs缓冲区大小</param>
        public TcpServer(int bufferLength=1024)
        {
            this.bufferLength = bufferLength;
        }

        /// <summary>
        /// 开始监听
        /// </summary>
        /// <param name="port"></param>
        public void Start(int port=0)
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
            listen.Listen(1000);

            isRun = 1;
            ServerStartRun?.Invoke(this, new EventArgs());
            startAccept();
        }

        /*开始接收套接字*/
        private void startAccept(SocketAsyncEventArgs socketAsync = null)
        {
            if (!IsRun && listen != null)
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
            TcpConnection connection = new TcpConnection(this) { Socket = e.AcceptSocket };
            startAccept(e);

            System.Threading.Interlocked.Increment(ref connectionCount);
            NewConnection?.Invoke(this, new TcpConnectionedArges(connection));

            if (!queue.TryDequeue(out var socketReceiveAsync))
            {
                socketReceiveAsync = new SocketAsyncEventArgs();
                socketReceiveAsync.Completed += SocketAsync_Receive_Completed;
                socketReceiveAsync.SetBuffer(new byte[bufferLength], 0, bufferLength);
            }
            connection.ReceiveSocketAsync = socketReceiveAsync;
            socketReceiveAsync.UserToken = connection;
            StartReceive(connection);
        }

        /// <summary>
        /// 异步发送
        /// </summary>
        /// <param name="connection"></param>
        /// <param name="bytes"></param>
        public void SendAsync(TcpConnection connection, byte[] bytes)
        {
            if (!IsRun && listen!=null && connection.Connectioned)
                return;
            SocketAsyncEventArgs socketSendAsync;
            if (!queue.TryDequeue(out  socketSendAsync))
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
                    queue.Enqueue(socketSendAsync);
            }
        }

        /*发送完成*/
        private void SocketSendAsync_Send_Completed(object sender, SocketAsyncEventArgs e)
        {
            e.UserToken = null;
            queue.Enqueue(e);
        }

        /*开始接收数据*/
        private void StartReceive(TcpConnection connection)
        {
            if (!IsRun && listen != null)
                return;

            if (!connection.Socket.ReceiveAsync(connection.ReceiveSocketAsync))
            {
                SocketAsync_Receive_Completed(listen, connection.ReceiveSocketAsync);
            }
        }

        /*接收到新的数据*/
        private void SocketAsync_Receive_Completed(object sender, SocketAsyncEventArgs e)
        {
            if (e.BytesTransferred > 0 && e.SocketError == SocketError.Success)
            {
                if (e.LastOperation == SocketAsyncOperation.Receive)
                {
                    var connection = (TcpConnection)e.UserToken;
                    var bytes = new byte[e.BytesTransferred];
                    Buffer.BlockCopy(e.Buffer, e.Offset, bytes, 0, bytes.Length);
                    StartReceive(connection);
                    ReceiveData?.Invoke(this, new TcpReceiveDataArges(connection, bytes));
                }
            }
            else
            {
                OnConnectionClose((TcpConnection)e.UserToken);
            }
        }

        /*连接已关闭*/
        private void OnConnectionClose(TcpConnection connection)
        {
            Interlocked.Decrement(ref connectionCount);

            connection.Socket.Shutdown(SocketShutdown.Both);
            connection.Socket.Close();

            connection.ReceiveSocketAsync.AcceptSocket = null;
            connection.ReceiveSocketAsync.UserToken = null;


            queue.Enqueue(connection.ReceiveSocketAsync);

            Disconnected?.Invoke(this, new TcpConnectionedArges(connection));
        }

        /// <summary>
        /// 停止运行
        /// </summary>
        public void Stop()
        {
            if(IsRun)
                Interlocked.Decrement(ref isRun);  
            listen?.Shutdown(SocketShutdown.Both);
            listen?.Close();
            listen = null;

            stopTime = DateTime.Now;
            ServerStopRun?.Invoke(this, new EventArgs());
        }
    }
}
