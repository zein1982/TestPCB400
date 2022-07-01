using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Test20311M
{
    class GeneralPort : Port
    {
        private Port port;
        private Win32ComPort com = new Win32ComPort();
        private TcpPort tcp = new TcpPort();

        public GeneralPort() { port = com; }

        public override bool IsOpen
        {
            get
            {
                return port.IsOpen;
            }
            protected set { }
        }

        public override string PortName
        {
            get
            {
                return port.PortName;
            }
            set
            {
                if (IsOpen) throw new Exception("Нельзя изменить имя порта, если он уже открыт");
                bool comName = (value.IndexOf("COM") == 0);
                bool tcpName = (value.IndexOf("TCP") == 0);
                if ((value == null) || (!comName && !tcpName)) throw new Exception("Имя порта задано неверно");
                if(comName && (port is TcpPort))
                {
                    port = com;
                }
                else if(tcpName && (port is Win32ComPort))
                {
                    port = tcp;
                }
                port.PortName = value;
            }
        }

        public override int BaudRate
        {
            get
            {
                return port.BaudRate;
            }
            set
            {
                com.BaudRate = value;
                tcp.BaudRate = value;
            }
        }

        public override int DataBits
        {
            get
            {
                return port.DataBits;
            }
            set
            {
                com.DataBits = value;
                tcp.DataBits = value;
            }
        }

        public override Win32.ParityFlags Parity
        {
            get
            {
                return port.Parity;
            }
            set
            {
                com.Parity = value;
                tcp.Parity = value;
            }
        }

        public override Win32.StopBitsFlags StopBits
        {
            get
            {
                return port.StopBits;
            }
            set
            {
                com.StopBits = value;
                tcp.StopBits = value;
            }
        }

        public override int ReadBufferSize
        {
            get { return port.ReadBufferSize; }
            set
            {
                if (IsOpen) throw new Exception("Нельзя изменить размер буфера, если порт открыт");
                if (value < 0) throw new Exception("Размер буфера должен быть положительным числом");
                com.ReadBufferSize = value;
                tcp.WriteBufferSize = value;
            }
        }

        public override int WriteBufferSize
        {
            get { return port.WriteBufferSize; }
            set
            {
                if (IsOpen) throw new Exception("Нельзя изменить размер буфера, если порт открыт");
                if (value < 0) throw new Exception("Размер буфера должен быть положительным числом");
                com.WriteBufferSize = value;
                tcp.WriteBufferSize = value;
            }
        }

        public override void Open()
        {
            port.Open();
        }

        public override void Close()
        {
            port.Close();
        }

        public override void Write(byte[] buffer, int offset, int count)
        {
            port.Write(buffer, offset, count);
        }

        public override int Read(byte[] buffer, int offset, int count)
        {
            return port.Read(buffer, offset, count);
        }

        public override void DiscardInBuffer()
        {
            port.DiscardInBuffer();
        }

        public override void DiscardOutBuffer()
        {
            port.DiscardOutBuffer();
        }

        public override int BytesToRead
        {
            get
            {
                return port.BytesToRead;
            }
            protected set { }
        }

        public override int BytesToWrite
        {
            get
            {
                return port.BytesToWrite;
            }
            protected set { }
        }

        public new static List<string> GetPortNames()
        {
            List<string> list1 = Win32ComPort.GetPortNames();
            List<string> list2 = TcpPort.GetPortNames();

            if (list1 == null) return list2;
            if (list2 == null) return list1;

            foreach (string str in list2)
                list1.Add(str);
            return list1;
        }
        public new static List<string> GetExtPortNames()
        {
            List<string> list1 = Win32ComPort.GetExtPortNames();
            List<string> list2 = TcpPort.GetExtPortNames();

            if (list1 == null) return list2;
            if (list2 == null) return list1;

            foreach (string str in list2)
                list1.Add(str);
            return list1;
        }
    }
}
