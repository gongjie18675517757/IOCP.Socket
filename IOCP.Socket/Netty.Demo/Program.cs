using DotNetty.Buffers;
using DotNetty.Codecs;
using DotNetty.Handlers.Logging;
using DotNetty.Transport.Bootstrapping;
using DotNetty.Transport.Channels;
using DotNetty.Transport.Channels.Sockets;
using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Netty.Demo
{

    class Server
    {
        private IChannel bootstrapChannel;

        public async Task Run()
        {
            var bossGroup = new MultithreadEventLoopGroup(1);
            var workerGroup = new MultithreadEventLoopGroup(5);
            var bootstrap = new ServerBootstrap();
            bootstrap
                .Group(bossGroup, workerGroup)
                .Channel<TcpServerSocketChannel>()
                .Option(ChannelOption.SoBacklog, 100)
                .ChildHandler(new ActionChannelInitializer<ISocketChannel>(channel =>
                {
                    IChannelPipeline pipeline = channel.Pipeline;
                    pipeline.AddLast(new DeCoder());
                    pipeline.AddLast(new EnCoder());
                    pipeline.AddLast(new EnCoder2());
                    pipeline.AddLast(new EchoServerHandler());
                }));
            bootstrapChannel = await bootstrap.BindAsync(2012);
        }

        public async Task Close()
        {
            await bootstrapChannel.CloseAsync();
        }
    }

    class Program
    {
        static async Task Main(string[] args)
        {
            var bossGroup = new MultithreadEventLoopGroup(1);
            var workerGroup = new MultithreadEventLoopGroup(5);
            var bootstrap = new ServerBootstrap();
            bootstrap
                .Group(bossGroup, workerGroup)
                .Channel<TcpServerSocketChannel>()
                .Option(ChannelOption.SoBacklog, 100)
                .Handler(new LoggingHandler("LSTN"))
                .ChildHandler(new ActionChannelInitializer<ISocketChannel>(channel =>
                {
                    IChannelPipeline pipeline = channel.Pipeline;
                    pipeline.AddLast(new LoggingHandler("CONN"));
                    pipeline.AddLast(new DeCoder());
                    pipeline.AddLast(new EnCoder());
                    pipeline.AddLast(new EnCoder2());
                    pipeline.AddLast(new EnCoder3());
                    pipeline.AddLast(new EchoServerHandler());
                }));
            IChannel bootstrapChannel = await bootstrap.BindAsync(2012);
            Console.ReadLine();
            await bootstrapChannel.CloseAsync();
        }
    }

    public class EnCoder : MessageToByteEncoder<string>
    {
        protected override void Encode(IChannelHandlerContext context, string message, IByteBuffer output)
        {
            var vs = Encoding.UTF8.GetBytes(message);
            output.WriteBytes(vs);
        }
    }

    public class EnCoder2 : MessageToByteEncoder<int>
    {
        protected override void Encode(IChannelHandlerContext context, int message, IByteBuffer output)
        {
            var vs = BitConverter.GetBytes(message);
            output.WriteBytes(vs);
        }
    }

    public class EnCoder3 : MessageToByteEncoder<byte[]>
    {
        protected override void Encode(IChannelHandlerContext context, byte[] message, IByteBuffer output)
        {
            output.WriteBytes(message);
        }
    }

    public class DeCoder : ByteToMessageDecoder
    {
        protected override void Decode(IChannelHandlerContext context, IByteBuffer input, List<object> output)
        {
            var readIndex = input.ReaderIndex;
            if (input.IsReadable(2))
            {
                var length = input.ReadUnsignedShort();
                if (input.IsReadable(length))
                {
                    //var body = new byte[length];
                    //input.ReadBytes(body);
                    //Console.WriteLine(BitConverter.ToString(body));
                    var body = input.ReadBytes(length);
                    output.Add(body);
                    Console.WriteLine($"读:{input.ReaderIndex}\t写:{input.WriterIndex}");
                }
                else
                {
                    input.SetReaderIndex(readIndex);
                }
            }
        }
    }

    public class EchoServerHandler : ChannelHandlerAdapter
    {
        public override void ChannelRegistered(IChannelHandlerContext context)
        {
            base.ChannelRegistered(context);
            Console.WriteLine(nameof(ChannelRegistered));

        }

        public override void ChannelActive(IChannelHandlerContext context)
        {
            base.ChannelActive(context);


            Console.WriteLine(nameof(ChannelActive));

            var channelId = context.Channel.Id.AsShortText();
            Console.WriteLine($"{channelId}\t{context.Channel.RemoteAddress}");
        }

        public override void ChannelInactive(IChannelHandlerContext context)
        {
            base.ChannelInactive(context);
            Console.WriteLine(nameof(ChannelInactive));

            var channelId = context.Channel.Id.AsShortText();
            Console.WriteLine($"{channelId}\t{context.Channel.RemoteAddress}");
        }

        public override void ChannelUnregistered(IChannelHandlerContext context)
        {
            base.ChannelUnregistered(context);
            Console.WriteLine(nameof(ChannelUnregistered));
        }
        public override void ChannelWritabilityChanged(IChannelHandlerContext context)
        {
            base.ChannelWritabilityChanged(context);
            Console.WriteLine(nameof(ChannelWritabilityChanged));
        }

        public override void ChannelRead(IChannelHandlerContext context, object message)
        {
            var buffer = message as IByteBuffer;
            if (buffer != null)
            {
                var str = buffer.ToString(Encoding.UTF8);
                context.WriteAsync(str);
                //Console.WriteLine("Received from client: " + buffer.ToString(Encoding.UTF8));
            }
            context.WriteAsync(10);
            context.WriteAsync(new byte[] { 0x01, 0x02 });

            Task.Run(async () =>
            {
                await Task.Delay(1000);
                await context.Channel.WriteAndFlushAsync(new byte[] { 0x01, 0x02, 0x05 });

                await context.Channel.CloseAsync();
            });
        }

        public override void ChannelReadComplete(IChannelHandlerContext context) => context.Flush();

        public override void ExceptionCaught(IChannelHandlerContext context, Exception exception)
        {
            Console.WriteLine("Exception: " + exception);
            context.CloseAsync();
        }
    }

}
