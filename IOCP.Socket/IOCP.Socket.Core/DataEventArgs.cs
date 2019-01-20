using System;

namespace IOCP.Socket.Core
{
    public class DataEventArgs : EventArgs
    {
        public byte[] Buffer { get; set; }

        public int Index { get; set; }

        public int Length { get; set; }
    }
}
