using System;
using System.Collections.Generic;
using System.Text;

namespace IOCP.SocketCore
{
    internal interface IServer
    {
        /// <summary>
        /// 总发出的字节数
        /// </summary>
        int SendBytesCount { get; }

        /// <summary>
        /// 总接收的字节数
        /// </summary>
        int ReceiveBytesCount { get; }

        /// <summary>
        /// 运行时间
        /// </summary>
        TimeSpan RunTime { get; }

        /// <summary>
        /// 是否正在运行
        /// </summary>
        bool IsRun { get; }

        /// <summary>
        /// 开始运行
        /// </summary>
        /// <param name="port"></param>
        void Start(int port);

        /// <summary>
        /// 停止运行
        /// </summary>
        void Stop();
    }
}
