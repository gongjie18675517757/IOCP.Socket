using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;
using System.Text;

namespace IOCP.SocketCore.UdpSocketServer
{ 
    public class UdpServer : IUdpServer
    {
        private Socket listen;
        private bool isRun;
        private readonly int bufferLength;
        private ConcurrentQueue<SocketAsyncEventArgs> queue = new ConcurrentQueue<SocketAsyncEventArgs>();
        private int sendBytesCount;
        private int receiveBytesCount;
        private DateTime startTime;
        private DateTime stopTime;

        public int SendBytesCount => sendBytesCount;

        public int ReceiveBytesCount => receiveBytesCount; 

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

        public bool IsRun => isRun;

        public EndPoint LocalEndPoint => listen.LocalEndPoint;

        public UdpServer(int bufferLength) => this.bufferLength = bufferLength;


        public event EventHandler<ReceiveDataEventArgs> ReceiveData;
        public event EventHandler ServerStartRun;
        public event EventHandler ServerStopRun;

        /// <summary>
        /// 开始监听
        /// </summary>
        /// <param name="port"></param>
        public void Start(int port)
        {
            var local = new IPEndPoint(IPAddress.Any, port);
            listen = new Socket(local.AddressFamily, SocketType.Dgram, ProtocolType.Udp);
            listen.Bind(local);
            SocketAsyncEventArgs socketAsync = new SocketAsyncEventArgs();
            socketAsync.SetBuffer(new byte[bufferLength], 0, bufferLength);
            socketAsync.Completed += OnReceived;
            socketAsync.RemoteEndPoint = local;
            isRun = true;
            StartReceive(socketAsync);
            ServerStartRun?.Invoke(this, new EventArgs());
            startTime = DateTime.Now;
            sendBytesCount = 0;
            receiveBytesCount = 0;
        }

        public void Stop()
        {
            isRun = false;
            listen.Close();
            stopTime = DateTime.Now;
            ServerStopRun?.Invoke(this, new EventArgs());
        }

        private void StartReceive(SocketAsyncEventArgs socketAsync)
        {
            if (isRun)
                if (!listen.ReceiveFromAsync(socketAsync))
                    OnReceived(listen, socketAsync);
        }

        private void OnReceived(object sender, SocketAsyncEventArgs e)
        {
            if (e.BytesTransferred > 0 && e.SocketError == SocketError.Success)
            {
             
                if (e.LastOperation == SocketAsyncOperation.ReceiveFrom)
                {

                    var buffer = new byte[e.BytesTransferred];
                    System.Threading.Interlocked.Add(ref receiveBytesCount, buffer.Length);
                    Buffer.BlockCopy(e.Buffer, e.Offset, buffer, 0, buffer.Length);
                    StartReceive(e);
                    var receiveEventHandler = new ReceiveDataEventArgs()
                    {
                        Data = buffer,
                        EndPoint = e.RemoteEndPoint,
                    };
                    ReceiveData.Invoke(this, receiveEventHandler);
                    // SendAsync(e.RemoteEndPoint, buffer);
                    return;
                }
            }
            StartReceive(e);
        }


        public void SendAsync(EndPoint endPoint, byte[] bytes)
        {
            if (!isRun)
                return;

            if (!queue.TryDequeue(out var socketSendAsync))
            {
                socketSendAsync = new SocketAsyncEventArgs();
                socketSendAsync.Completed += OnSended;
            }
            socketSendAsync.SetBuffer(bytes, 0, bytes.Length);
            socketSendAsync.RemoteEndPoint = endPoint;

            if (!listen.SendToAsync(socketSendAsync))
            {
                OnSended(listen, socketSendAsync);
            }
        }


        private void OnSended(object sender, SocketAsyncEventArgs e)
        {
            queue.Enqueue(e);
            System.Threading.Interlocked.Add(ref sendBytesCount, e.Buffer.Length);
        }
    }
}
