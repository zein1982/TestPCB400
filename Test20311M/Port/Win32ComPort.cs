using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Globalization;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;

namespace Test20311M
{
    public class Win32ComPort : Port
    {
        private IntPtr hPort;
        private string portName;
        private Win32.DCB dcb;
        private Win32.COMMTIMEOUTS ctmo;
        private Win32.OVERLAPPED ovrRd;
        private Win32.OVERLAPPED ovrWr;
        private Win32.COMSTAT comStat;
        private int readBufferSize;
        private int writeBufferSize;

        public override bool IsOpen { get; protected set; }
        public override string PortName
        {
            get { return portName; }
            set
            {
                if (IsOpen) throw new Exception("Нельзя изменить имя порта, если он уже открыт");
                if ((value == null) || (value.IndexOf("COM") != 0)) throw new Exception("Имя порта задано неверно");
                portName = value;
            }
        }
        public override int BaudRate
        {
            get { return (int)dcb.BaudRate; }
            set
            {
                if (value < 0) throw new Exception("Скорость обмена должна быть положительным числом");

                uint old_value = dcb.BaudRate;
                dcb.BaudRate = (uint) value;

                // Загружаем структуру dcb, если порт открыт
                if (IsOpen && !Win32.SetCommState(hPort, dcb))
                {
                    dcb.BaudRate = old_value;
                    throw new Exception("Не удалось изменить скорость обмена порта " + portName);
                }
            }
        }
        public override int DataBits
        {
            get { return dcb.ByteSize; }
            set
            {
                if ((value < 5) || (value > 8)) throw new Exception("Число бит данных должно быть между 5 и 8");

                byte old_value = dcb.ByteSize;
                dcb.ByteSize = (byte) value;

                // Загружаем структуру dcb, если порт открыт
                if (IsOpen && !Win32.SetCommState(hPort, dcb))
                {
                    dcb.ByteSize = old_value;
                    throw new Exception("Не удалось изменить число бит данных");
                }
            }
        }
        public override Win32.ParityFlags Parity
        {
            get { return dcb.Parity; }
            set
            {
                if (((int)value < 0) || ((int)value > 4)) throw new Exception("Недопустимое значение параметров контроля четности");

                Win32.ParityFlags old_value = dcb.Parity;
                dcb.Parity = value;

                // Загружаем структуру dcb в порт
                if (IsOpen && !Win32.SetCommState(hPort, dcb))
                {
                    dcb.Parity = old_value;
                    throw new Exception("Не удалось изменить параметры контроля четности");
                }
            }
        }
        public override Win32.StopBitsFlags StopBits
        {
            get { return dcb.StopBits; }
            set
            {
                if (((int)value < 0) || ((int)value > 2)) throw new Exception("Недопустимое число стоповых битов");

                Win32.StopBitsFlags old_value = dcb.StopBits;
                dcb.StopBits = value;

                // Загружаем структуру dcb в порт
                if (IsOpen && !Win32.SetCommState(hPort, dcb))
                {
                    dcb.StopBits = old_value;
                    throw new Exception("Не удалось изменить количество стоповых битов");
                }
            }
        }
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

        public Win32ComPort()
        {
            hPort = IntPtr.Zero;
            portName = "COM1";
            IsOpen = false;
            readBufferSize = 10000;
            writeBufferSize = 10000;

            dcb = new Win32.DCB();
            dcb.BaudRate = 9600;                              // скорость передачи
            dcb.fBinary = true;                                 // устанавливаем двоичный режим предачи
            dcb.fParity = false;                                // отключаем контроль четности
            dcb.fOutxCtsFlow = false;                           // выключаем режим слежения за сигналом CTS
            dcb.fOutxDsrFlow = false;                           // выключаем режим слежения за сигналом DSR
            dcb.fDtrControl = Win32.DtrControlFlags.Disable;    // отключаем использование линии DTR
            dcb.fDsrSensitivity = false;                        // отключаем восприимчивость драйвера к состоянию линии DSR
            dcb.fTXContinueOnXoff = true;                       // разрешаем продолжить передачу при переполнении входного буфера (отключаем XON/XOFF-режим)
            dcb.fOutX = false;                                  // отключаем режим XON/XOFF-передачи
            dcb.fInX = false;                                   // отключаем режим XON/XOFF-приема
            dcb.fErrorChar = false;                             // запрещаем замещение ошибочных символов
            dcb.fNull = false;                                  // запрещаем отбрасывание нулевых байтов при приеме
            dcb.fRtsControl = Win32.RtsControlFlags.Disable;    // отключаем использование линии RTS
            dcb.fAbortOnError = false;                          // отключаем остановку всех операций чтения/записи при ошибке
            dcb.wReserved = 0;                                  // не используется (должно быть установлено в 0)
            dcb.XonLim = 0;                                     // минимальное число байт в приемном буфере перед посылкой символа XON (если включен XON/XOFF-режим)
            dcb.XoffLim = 0;                                    // максимальное число байт в приемном буфере перед посылкой символа XOFF (если включен XON/XOFF-режим)
            dcb.ByteSize = 8;                                   // задаём 8 бит в байте
            dcb.Parity = Win32.ParityFlags.None;                // отключаем проверку четности
            dcb.StopBits = Win32.StopBitsFlags.One;             // задаём один стоп-бит
            dcb.XonChar = 0;							        // начальный символ для XON/XOFF-режима
            dcb.XoffChar = 0;						            // конечный символ для XON/XOFF-режима
            dcb.ErrorChar = 0;						            // символ замены при dcb.fErrorChar=true
            dcb.EofChar = -1;						            // символ конца данных
            dcb.EvtChar = 0;						            // символ для вызова события
            dcb.wReserved1 = 0;						            // не используется
            

            ctmo = new Win32.COMMTIMEOUTS();
            ctmo.ReadIntervalTimeout = 0;           // максимальное время допускаемое между приемом двух символов
            ctmo.ReadTotalTimeoutMultiplier = 0;    // множитель для вычисления общего таймаута операции чтения (время приема одного символа плюс интервал между символами)
            ctmo.ReadTotalTimeoutConstant = 0;      // константа для вычисления общего таймаута операции чтеня (время ожидания приема символа)
            ctmo.WriteTotalTimeoutMultiplier = 0;   // множитель для вычисления общего таймаута операции записи (время передачи одного символа)
            ctmo.WriteTotalTimeoutConstant = 0;		// константа для вычисления общего таймаута операции записи

            ovrRd = new Win32.OVERLAPPED();
            ovrWr = new Win32.OVERLAPPED();
            comStat = new Win32.COMSTAT();
        }

        /// <summary>
        /// Открыть порт
        /// </summary>
        public override void Open()
        {
            if (IsOpen) throw new Exception("Порт уже открыт");
            
            hPort = Win32.CreateFile(
                "\\\\.\\" + portName,       // имя порта в качестве имени файла
                0x80000000 | 0x40000000,    // GENERIC_READ | GENERIC_WRITE - доступ порту для чтения и записи
                0,                          // порт не может быть общедоступным (shared)
                IntPtr.Zero,                // дескриптор порта не наследуется, используется дескриптор безопасности по умолчанию
                3,                          // OPEN_EXISTING - порт должен открываться как уже существующий файл
                0x40000000,                 // FILE_FLAG_OVERLAPPED - этот флаг указывает на использование асинхронных операций
                IntPtr.Zero                 // указатель на файл шаблона не используется при работе с портами
            );
            
            if (hPort == new IntPtr(-1))    // INVALID_HANDLE_VALUE
            {
                hPort = IntPtr.Zero;
                throw new Exception("Не удалось открыть порт " + portName);
            }

            if (!Win32.SetCommState(hPort, dcb))
            {
                Win32.CloseHandle(hPort);
                hPort = IntPtr.Zero;
                throw new Exception("Не удалось установить параметры порта " + portName);
            }

            if (!Win32.SetCommTimeouts(hPort, ref ctmo))
            {
                Win32.CloseHandle(hPort);
                hPort = IntPtr.Zero;
                throw new Exception("Не удалось установить таймауты порта " + portName);
            }

            if (!Win32.SetupComm(hPort, readBufferSize, writeBufferSize))
            {
                Win32.CloseHandle(hPort);
                hPort = IntPtr.Zero;
                throw new Exception("Не удалось установить размеры очереди приема и передачи порта " + portName);
            }

            if (!Win32.PurgeComm(hPort, 0x0004 | 0x0008)) // PURGE_TXCLEAR | PURGE_RXCLEAR
            {
                Win32.CloseHandle(hPort);
                hPort = IntPtr.Zero;
                throw new Exception("Не удалось очистить очереди приема и передачи порта " + portName);
            }

            ovrRd.hEvent = Win32.CreateEvent(IntPtr.Zero, 0, 0, null);
            ovrWr.hEvent = Win32.CreateEvent(IntPtr.Zero, 0, 0, null);

            IsOpen = true;
        }

        /// <summary>
        /// Закрыть порт
        /// </summary>
        public override void Close()
        {
            if (!IsOpen) throw new Exception("Порт не открыт");
            bool a;
            a = Win32.CloseHandle(ovrRd.hEvent);    // закрываем объекты-события
            a = Win32.CloseHandle(ovrWr.hEvent);
            a = Win32.CloseHandle(hPort);           // закрываем порт
            hPort = IntPtr.Zero;                // обнуляем дескриптор порта
            IsOpen = false;
        }

        /// <summary>
        /// Передать данные на порт
        /// </summary>
        /// <param name="buffer">Буфер с данными для передачи</param>
        /// <param name="offset">Сдвиг - начальный индекс буфера данных для начала записи</param>
        /// <param name="count">Число данных для передачи</param>
        public override void Write(byte[] buffer, int offset, int count)
        {
            if (!IsOpen) throw new Exception("Невозможно передать данные: порт закрыт");
            if (buffer == null) throw new Exception("Невозможно передать данные: получен нулевой указатель на буфер с данными для передачи");
            if (offset < 0) throw new Exception("Невозможно передать данные: переменная offset должна быть положительным числом");
            if (count < 0) throw new Exception("Невозможно передать данные: переменная count должна быть положительным числом");
            if (offset > buffer.Length) throw new Exception("Невозможно передать данные: значение offset выходит за диапазон буфера данных");
            if ((offset + count) > buffer.Length) throw new Exception("Невозможно передать данные: значение (offset + count) выходит за диапазон буфера данных");

            if(offset > 0)
            {
                byte[] source_buffer = buffer;
                buffer = new byte[count];
                Array.Copy(source_buffer, offset, buffer, 0, count);
            }
            uint n = 0;
            Win32.WriteFile(hPort, buffer, (uint)buffer.Length, out n, ref ovrWr);

            if(Marshal.GetLastWin32Error() != 997)    // ERROR_IO_PENDING
                throw new Exception("Не удалось передать данные: ошибка запуска асинхронной операции");

            if ((Win32.WaitForSingleObject(ovrWr.hEvent, 0xFFFFFFFF) != 0) && !Win32.GetOverlappedResult(hPort, ref ovrWr, out n, true))
                throw new Exception("Не удалось передать данные: ошибка завершения асинхронной операции");

            //if (n != count) throw new Exception("Передано неверное количество байт");
        }

        /// <summary>
        /// Принять данные с порта
        /// </summary>
        /// <param name="buffer">Буфер для записи данных</param>
        /// <param name="offset">Сдвиг - начальный индекс буфера данных для чтения</param>
        /// <param name="count">Число данных для чтения</param>
        /// <returns></returns>
        public override int Read(byte[] buffer, int offset, int count)
        {
            if (!IsOpen) throw new Exception("Невозможно прочитать данные: порт закрыт");
            if (buffer == null) throw new Exception("Невозможно прочитать данные: получен нулевой указатель на буфер для чтения");
            if (offset < 0) throw new Exception("Невозможно прочитать данные: переменная offset должна быть положительным числом");
            if (count < 0) throw new Exception("Невозможно прочитать данные: переменная count должна быть положительным числом");
            if (offset > buffer.Length) throw new Exception("Невозможно прочитать данные: значение offset выходит за диапазон буфера данных");
            if ((offset + count) > buffer.Length) throw new Exception("Невозможно прочитать данные: значение (offset + count) выходит за диапазон буфера данных");

            Win32.FlushFileBuffers(hPort);

            uint n;
            if (!Win32.ClearCommError(hPort, out n, out comStat))
                throw new Exception("Не удалось получить информацию с порта " + portName);
            n = comStat.cbInQue;
            if (n == 0) throw new Exception("В порту " + portName + " нет данных для чтения");

            if (offset > 0)
            {
                byte[] source_buffer = new byte[count];
                Win32.ReadFile(hPort, source_buffer, (uint)count, out n, ref ovrRd);
                Array.Copy(source_buffer, 0, buffer, offset, n);
            }
            else Win32.ReadFile(hPort, buffer, (uint)count, out n, ref ovrRd);

            return (int)n;
        }

        /// <summary>
        /// Очистка входного буфера порта
        /// </summary>
        public override void DiscardInBuffer()
        {
            if (!IsOpen) throw new Exception("Невозможно очистить буфер: порт закрыт");

            if (!Win32.PurgeComm(hPort, 0x0008))    // PURGE_RXCLEAR
                throw new Exception("Не удалось очистить очередь приема " + portName);
        }

        /// <summary>
        /// Очистка выходного буфера порта
        /// </summary>
        public override void DiscardOutBuffer()
        {
            if (!IsOpen) throw new Exception("Невозможно очистить буфер: порт закрыт");

            if (!Win32.PurgeComm(hPort, 0x0004))    // PURGE_TXCLEAR
                throw new Exception("Не удалось очистить очередь передачи " + portName);
        }

        /// <summary>
        /// Получить количество байт, находящихся во входном буфере порта
        /// </summary>
        public override int BytesToRead
        {
            get
            {
                if (!IsOpen) throw new Exception("Невозможно получить данные: порт закрыт");

                uint n;
                if (!Win32.ClearCommError(hPort, out n, out comStat))
                    throw new Exception("Не удалось получить информацию с порта " + portName);
                return (int)comStat.cbInQue;
            }
            protected set { }
        }

        /// <summary>
        /// Получить количество байт, находящихся в выходном буфере порта
        /// </summary>
        public override int BytesToWrite
        {
            get
            {
                if (!IsOpen) throw new Exception("Невозможно получить данные: порт закрыт");

                uint n;
                if (!Win32.ClearCommError(hPort, out n, out comStat))
                    throw new Exception("Не удалось получить информацию с порта " + portName);
                return (int)comStat.cbOutQue;
            }
            protected set { }
        }

        /// <summary>
        /// Получить список портов
        /// </summary>
        /// <returns>Список портов</returns>
        public new static List<string> GetPortNames() { return EnumPorts(false); }

        /// <summary>
        /// Получить расширенный список портов
        /// </summary>
        /// <returns>Расширенный список портов</returns>
        public new static List<string> GetExtPortNames() { return EnumPorts(true); }

        /// <summary>
        /// Общий метод для получения списка портов
        /// </summary>
        /// <param name="ExtList">Вариант списка: true - расширенный; false - сокращенный</param>
        /// <returns></returns>
        private static List<string> EnumPorts(bool ExtList)
        {
            // Получаем список всех устройств
            IntPtr hDevInfo = Win32.SetupDiGetClassDevs(IntPtr.Zero, null, IntPtr.Zero, 0x00000002 | 0x00000004); // DIGCF_PRESENT | DIGCF_ALLCLASSES

            // Возвращаем пустой список, если не удалось получить список устройств
            if (hDevInfo == new IntPtr(-1))      // INVALID_HANDLE_VALUE
                return null;
                //throw new Exception("Не удалось получить список устройств");

            // Создаем структуру, содержащую информацию об устройствах
            Win32.SP_DEVINFO_DATA DeviceInfoData = new Win32.SP_DEVINFO_DATA();
            DeviceInfoData.cbSize = (uint)Marshal.SizeOf(DeviceInfoData);

            byte[] buffer = new byte[128];
            UInt32 DataT;
            UInt32 requiredSize = 0;

            List<string> portList = null;

            // Совершаем обход всех устройств системы их hDevInfo
            for(uint i = 0; Win32.SetupDiEnumDeviceInfo(hDevInfo, i, ref DeviceInfoData); ++i)
            {
                bool no_data = false;

                // Получаем свойство SPDRP_FRIENDLYNAME (понятное имя)
                while (!Win32.SetupDiGetDeviceRegistryProperty(
                        hDevInfo,
                        ref DeviceInfoData,
                        0xC,        // SPDRP_FRIENDLYNAME
                        out DataT,
                        buffer,
                        (uint)buffer.Length,
                        out requiredSize))
                {
                    // Если размер буфера недостаточен, то выделяем запрашиваемый объем памяти
                    if (Marshal.GetLastWin32Error() == 122) // ERROR_INSUFFICIENT_BUFFER
                    {
                        buffer = new byte[requiredSize];
                    }
                    // Если свойство не получено, то переходим к следующему устройству
                    else
                    {
                        no_data = true;
                        break;
                    }
                }
                if (no_data) continue;

                // Если в полученной строке присутствует строка "COM", то заносим ее в список портов
                if((buffer != null) && (buffer.Length > 0))
                {
                    string str = System.Text.Encoding.Unicode.GetString(buffer);
                    str = str.Substring(0, str.IndexOf('\0'));
                    int N = str.IndexOf("(COM");
                    if ((N != -1) && (str[N + 4] >= '0') && (str[N + 4] <= '9'))
                    {
                        if (portList == null) portList = new List<string>();

                        if (ExtList)    portList.Add(str);
                        else            portList.Add(str.Substring(N + 1, str.IndexOf(')') - N - 1));
                    }
                }
            }

            // Освобождаем память из-под объекта hDevInfo
            Win32.SetupDiDestroyDeviceInfoList(hDevInfo);
            
            // Сортируем полученный список по возрастанию номера
            if ((portList != null) && (portList.Count > 1))
            {
                if (ExtList)    portList.Sort(ExtPortListComparer.instance);
                else            portList.Sort(PortListComparer.instance);
            }

            return portList;
        }

        /// <summary>
        /// Сортировщик списка портов
        /// </summary>
        private class PortListComparer : IComparer<string>
        {
            /// <summary>
            /// Экземпляр класса
            /// </summary>
            public static PortListComparer instance = new PortListComparer();

            /// <summary>
            /// Сравнение двух портов - реализация интерфеса IComparer
            /// </summary>
            /// <param name="x">Имя первого сравниваемого порта</param>
            /// <param name="y">Имя второго сравниваемого порта</param>
            /// <returns>Результат сравнения:
            /// отрицательное число, если x меньше y;
            /// положительное число, если x больше y;
            /// ноль, если x равен y</returns>
            public int Compare(string x, string y)
            {
                int N1 = Int32.Parse(x.Substring(3), NumberStyles.Number);
                int N2 = Int32.Parse(y.Substring(3), NumberStyles.Number);

                return N1 - N2;
            }
        }

        /// <summary>
        /// Сортировщик расширенного списка портов
        /// </summary>
        private class ExtPortListComparer : IComparer<string>
        {
            public static ExtPortListComparer instance = new ExtPortListComparer();

            /// <summary>
            /// Сравнение двух портов - реализация интерфеса IComparer
            /// </summary>
            /// <param name="x">Имя первого сравниваемого порта</param>
            /// <param name="y">Имя второго сравниваемого порта</param>
            /// <returns>Результат сравнения:
            /// отрицательное число, если x меньше y;
            /// положительное число, если x больше y;
            /// ноль, если x равен y</returns>
            public int Compare(string x, string y)
            {
                int N1 = x.IndexOf("(COM");
                int N2 = y.IndexOf("(COM");
                if ((N1 == -1) || (N2 == -1)) throw new Exception("В списке портов имеется некорректная запись");

                N1 = Int32.Parse(x.Substring(N1 + 4, x.IndexOf(')') - N1 - 4), NumberStyles.Number);
                N2 = Int32.Parse(y.Substring(N2 + 4, y.IndexOf(')') - N2 - 4), NumberStyles.Number);

                return N1 - N2;
            }
        }
    }
}
