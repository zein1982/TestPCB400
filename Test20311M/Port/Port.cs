using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Test20311M
{
    public abstract class Port
    {
        public abstract bool IsOpen { get; protected set; }
        public abstract string PortName { get; set; }
        public abstract int BaudRate { get; set; }
        public abstract int DataBits { get; set; }
        public abstract Win32.ParityFlags Parity { get; set; }
        public abstract Win32.StopBitsFlags StopBits { get; set; }
        public abstract int ReadBufferSize { get; set; }
        public abstract int WriteBufferSize { get; set; }
        public abstract void Open();
        public abstract void Close();
        public abstract void Write(byte[] buffer, int offset, int count);
        public abstract int Read(byte[] buffer, int offset, int count);
        public abstract void DiscardInBuffer();
        public abstract void DiscardOutBuffer();
        public abstract int BytesToRead { get; protected set; }
        public abstract int BytesToWrite { get; protected set; }
        public static List<string> GetPortNames() { return null; }
        public static List<string> GetExtPortNames() { return null; }
    }
}
