using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;

namespace Test20311M
{
    class TcpPort : Port
    {
        private string portName = "TCP";
        private int baudRate = 115200;
        private Socket socket = null;
        private int readBufferSize = 10000;
        private int writeBufferSize = 10000;
        private byte[] WrBuf = null, RdBuf = null;

        public override bool IsOpen { get; protected set; }

        public override string PortName { get { return portName; } set { } }

        public override int BaudRate
        {
            get { return baudRate; }
            set
            {
                if (value < 0) throw new Exception("Скорость обмена должна быть положительным числом");
                baudRate = value;
                if (IsOpen) { SetBaudRate(); }
            }
        }

        public override int DataBits { get; set; }

        public override Win32.ParityFlags Parity { get; set; }

        public override Win32.StopBitsFlags StopBits { get; set; }

        public override int ReadBufferSize
        {
            get { return readBufferSize; }
            set
            {
                if (IsOpen) throw new Exception("Нельзя изменить размер буфера, если порт открыт");
                if (value < 0) throw new Exception("Размер буфера должен быть положительным числом");
                readBufferSize = value;
            }
        }
        public override int WriteBufferSize
        {
            get { return writeBufferSize; }
            set
            {
                if (IsOpen) throw new Exception("Нельзя изменить размер буфера, если порт открыт");
                if (value < 0) throw new Exception("Размер буфера должен быть положительным числом");
                writeBufferSize = value;
            }
        }

        public override void Open()
        {
            if (IsOpen) throw new Exception("Порт уже открыт");
            try
            {
                TcpClient client = new TcpClient();
                client.Connect("10.0.10.1", 50005);
                socket = client.Client;
                socket.ReceiveBufferSize = readBufferSize;
                socket.SendBufferSize = writeBufferSize;
                if ((RdBuf == null) || (RdBuf.Length != (readBufferSize + 1)))
                {
                    RdBuf = new byte[writeBufferSize + 1];
                }
                if ((WrBuf == null) || (WrBuf.Length != (writeBufferSize + 1)))
                {
                    WrBuf = new byte[writeBufferSize + 1];
                }

                SetBaudRate();
                IsOpen = true;

            }
            catch(Exception)
            {
                if (socket != null)
                {
                    socket.Close();
                    socket.Dispose();
                }
                socket = null;
                IsOpen = false;
                throw new Exception("Ошибка подключения к преобразователю WiFi-to-RS422");
            }
        }

        public override void Close()
        {
            if (!IsOpen) throw new Exception("Порт не открыт");

            socket.Close();
            socket.Dispose();
            socket = null;
            IsOpen = false;
        }

        public override void Write(byte[] buffer, int offset, int count)
        {
            if (!IsOpen) throw new Exception("Невозможно передать данные: порт закрыт");
            if (buffer == null) throw new Exception("Невозможно передать данные: получен нулевой указатель на буфер с данными для передачи");
            if (offset < 0) throw new Exception("Невозможно передать данные: переменная offset должна быть положительным числом");
            if (count < 0) throw new Exception("Невозможно передать данные: переменная count должна быть положительным числом");
            if (offset > buffer.Length) throw new Exception("Невозможно передать данные: значение offset выходит за диапазон буфера данных");
            if ((offset + count) > buffer.Length) throw new Exception("Невозможно передать данные: значение (offset + count) выходит за диапазон буфера данных");

            WrBuf[0] = (byte)'W';
            Array.Copy(buffer, offset, WrBuf, 1, count);
            socket.Send(WrBuf, count + 1, SocketFlags.None);
        }

        public override int Read(byte[] buffer, int offset, int count)
        {
            if (!IsOpen) throw new Exception("Невозможно прочитать данные: порт закрыт");
            if (buffer == null) throw new Exception("Невозможно прочитать данные: получен нулевой указатель на буфер для чтения");
            if (offset < 0) throw new Exception("Невозможно прочитать данные: переменная offset должна быть положительным числом");
            if (count < 0) throw new Exception("Невозможно прочитать данные: переменная count должна быть положительным числом");
            if (offset > buffer.Length) throw new Exception("Невозможно прочитать данные: значение offset выходит за диапазон буфера данных");
            if ((offset + count) > buffer.Length) throw new Exception("Невозможно прочитать данные: значение (offset + count) выходит за диапазон буфера данных");

            return socket.Receive(buffer, offset, count, SocketFlags.None);
        }

        public override void DiscardInBuffer()
        {
            if (!IsOpen) throw new Exception("Невозможно очистить буфер: порт закрыт");
        }

        public override void DiscardOutBuffer()
        {
            if (!IsOpen) throw new Exception("Невозможно очистить буфер: порт закрыт");
        }

        public override int BytesToRead
        {
            get
            {
                if (!IsOpen) throw new Exception("Невозможно получить данные: порт закрыт");
                return socket.Available;
            }
            protected set { }
        }

        public override int BytesToWrite
        {
            get
            {
                if (!IsOpen) throw new Exception("Невозможно получить данные: порт закрыт");
                return -1;
            }
            protected set { }
        }

        private void SetBaudRate()
        {
            WrBuf[0] = (byte)'B';
            WrBuf[1] = (byte)baudRate;
            WrBuf[2] = (byte)(baudRate >> 8);
            WrBuf[3] = (byte)(baudRate >> 16);
            WrBuf[4] = (byte)(baudRate >> 24);
            socket.Send(WrBuf, 5, SocketFlags.None);
            Thread.Sleep(100);
            if ((socket.Available != 5) || (socket.Receive(RdBuf) != 5)) { throw new Exception("Не удалось изменть скорость обмена"); }
            for (int i = 0; i < 5; ++i)
            {
                if (RdBuf[i] != WrBuf[i]) { throw new Exception("Не удалось изменть скорость обмена"); }
            }
        }

        public new static List<string> GetPortNames()
        {
            return new List<string>() { "TCP" };
        }
        public new static List<string> GetExtPortNames()
        {
            return new List<string>() { "Преобразователь WiFi - RS422 (TCP)" };
        }
    }
}
