using DotNetty.Buffers;
using DotNetty.Codecs;
using DotNetty.Transport.Bootstrapping;
using DotNetty.Transport.Channels;
using DotNetty.Transport.Channels.Sockets;
using Microsoft.Extensions.Options;
using Netty.SuperSocket.Config;
using Netty.SuperSocket.RequestInfo;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;

namespace Netty.SuperSocket
{
    public abstract class APPServer<TSession, TRequest>
        where TRequest : IRequestInfo
        where TSession : AppSession<TSession, TRequest>, new()
    {
        private volatile int isRun;
        private readonly IOptions<ServerConfig> options;
        private volatile int connectionCount;
        private volatile int sendBytesCount;
        private volatile int receiveBytesCount;
        private DateTime startTime;
        private DateTime stopTime;
        private readonly List<IChannel> channels = new List<IChannel>();
        private readonly ConcurrentDictionary<string, TSession> allSession = new ConcurrentDictionary<string, TSession>();
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
        /// 所有会话
        /// </summary>
        public IReadOnlyDictionary<string, TSession> AllSession => allSession;

        /// <summary>
        /// 本地终结点
        /// </summary>
        public IEnumerable<EndPoint> LocalAddress => channels.Select(x => x.LocalAddress);

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
        public event EventHandler<DataEventArgs<TSession, TRequest>> ReceiveData;


        public APPServer(IOptions<ServerConfig> options)
        {
            this.options = options;
        }

        /// <summary>
        /// 开始监听
        /// </summary> 
        public async Task RunAsync(CancellationToken cancellationToken)
        {
            connectionCount = 0;
            sendBytesCount = 0;
            receiveBytesCount = 0;

            startTime = DateTime.Now;
            stopTime = default(DateTime);

            var option = options.Value;

            var bossGroup = new MultithreadEventLoopGroup(option.ParentGroupCount);
            var workerGroup = new MultithreadEventLoopGroup(option.ChildGroupCount);
            var bootstrap = new ServerBootstrap();
            bootstrap
                .Group(bossGroup, workerGroup)
                .Channel<TcpServerSocketChannel>()
                .Option(ChannelOption.SoBacklog, option.ListenBacklog)
                .ChildHandler(new ActionChannelInitializer<ISocketChannel>(channel =>
                {
                    IChannelPipeline pipeline = channel.Pipeline;
                    pipeline.AddLast(new ArrayToByteEncoder(), new ArraySegmentToByteEnCoder());
                    pipeline.AddLast(new SessionHandler(ChannelActive, ChannelInactive, ChannelRead));
                }));

            foreach (var item in option.Listeners)
            {
                var bootstrapChannel = await bootstrap.BindAsync(/*item.Ip, */item.Post);
                this.channels.Add(bootstrapChannel);
            }

            isRun = 1;
            ServerStarted?.Invoke(this, new EventArgs());
            OnServerStarted();
        }

        /// <summary>
        /// 服务已启动
        /// </summary>
        protected void OnServerStarted() { }

        /// <summary>
        /// 接收新的连接
        /// </summary> 
        private void ChannelActive(IChannelHandlerContext context)
        {
            /*连接数+1*/
            Interlocked.Increment(ref connectionCount);

            var channelId = context.Channel.Id.AsShortText();
            var session = new TSession()
            {
                SessionId = channelId,
                Channel = context.Channel
            };

            if (allSession.TryAdd(channelId, session))
            {
                NewSession?.Invoke(this, session);
                OnNewSession(session);
            }
        }

        /// <summary>
        /// 连接已关闭
        /// </summary> 
        private void ChannelInactive(IChannelHandlerContext context)
        {
            /*连接数-1*/
            Interlocked.Decrement(ref connectionCount);
            var channelId = context.Channel.Id.AsShortText();

            if (allSession.TryRemove(channelId, out var session))
            {
                session.OnClosed();
                SessionClosed?.Invoke(this, session);
                OnSessionClosed(session);
            }
        }

        /// <summary>
        /// 收到数据
        /// </summary>
        /// <param name="context"></param>
        /// <param name="message"></param>
        private void ChannelRead(IChannelHandlerContext context, object message)
        {
            var channelId = context.Channel.Id.AsShortText();

            if (!allSession.TryGetValue(channelId, out var session))
                return;

            if (message is IByteBuffer byteBuffer)
            {
                session.OnReceiveDataAsync(byteBuffer);
                ReceiveData?.Invoke(this, new DataEventArgs<TSession, TRequest>()
                {
                    ByteBuffer = byteBuffer,
                    Session = session
                });
            }

            /*解析成请求*/
            if (message is TRequest request)
            {
                session.OnReceiveRequest(request);
            }
        }


        /// <summary>
        /// 收到新的连接
        /// </summary>
        /// <param name="session"></param>
        protected virtual void OnNewSession(TSession session) { }

        /// <summary>
        /// 异步发送
        /// </summary> 
        public async Task SendAsync(TSession session, byte[] bytes, int index, int length)
        {
            await session.Channel.WriteAndFlushAsync(new ArraySegment<byte>(bytes, index, length));
        }

        /// <summary>
        /// 连接关闭
        /// </summary>
        /// <param name="session">会话</param>
        protected virtual void OnSessionClosed(TSession session) { }

        /// <summary>
        /// 停止运行
        /// </summary>
        public async Task StopAsync()
        {
            foreach (var item in channels)
                await item.CloseAsync();

            allSession.Clear();
            channels.Clear();
            stopTime = DateTime.Now;
            ServerStopped?.Invoke(this, new EventArgs());
            OnServerStopped();
        }

        /// <summary>
        /// 服务已停止
        /// </summary>
        protected void OnServerStopped() { }


        class ArrayToByteEncoder : MessageToByteEncoder<byte[]>
        {
            protected override void Encode(IChannelHandlerContext context, byte[] message, IByteBuffer output)
            {
                output.WriteBytes(message);
            }
        }

        class ArraySegmentToByteEnCoder : MessageToByteEncoder<ArraySegment<byte>>
        {
            protected override void Encode(IChannelHandlerContext context, ArraySegment<byte> message, IByteBuffer output)
            {
                output.WriteBytes(message.Array, message.Offset, message.Count);
            }
        }

        class SessionHandler : ChannelHandlerAdapter
        {
            private readonly Action<IChannelHandlerContext> channelActive;
            private readonly Action<IChannelHandlerContext> channelInactive;
            private readonly Action<IChannelHandlerContext, object> channelInaChannelReadctive;

            public SessionHandler(
                Action<IChannelHandlerContext> channelActive,
                Action<IChannelHandlerContext> channelInactive,
                Action<IChannelHandlerContext, object> channelInaChannelReadctive
                )
            {
                this.channelActive = channelActive;
                this.channelInactive = channelInactive;
                this.channelInaChannelReadctive = channelInaChannelReadctive;
            }

            public override void ChannelActive(IChannelHandlerContext context)
            {
                base.ChannelActive(context);
                channelActive(context);
            }

            public override void ChannelInactive(IChannelHandlerContext context)
            {
                base.ChannelInactive(context);
                channelInactive(context);
            }

            public override void ChannelRead(IChannelHandlerContext context, object message)
            {
                base.ChannelRead(context, message);
                channelInaChannelReadctive(context, message);
            }
        }
    }
}
