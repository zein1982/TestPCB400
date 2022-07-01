using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;

namespace ComPort
{
    public interface IPortListener
    {
        /// <summary>
        /// Метод, уведомляющий об открытии портов
        /// </summary>
        /// <param name="param"></param>
        void PortOpened(PortParams param);
        /// <summary>
        /// Метод, уведомляющий о закрытии портов (ВНИМАНИЕ! метод вызывается НЕ в оконном потоке)
        /// </summary>
        void PortClosed();
    }

    public interface IReadDisplay
    {
        /// <summary>
        /// Отображение данных, считанных с порта
        /// </summary>
        /// <param name="data">Массив данных</param>
        /// <param name="count">Количество элементов в массиве</param>
        void DataReceived(byte[] data, int count);
    }

    public interface IWriteDisplay
    {
        /// <summary>
        /// Отображение данных, записываемых на порт
        /// </summary>
        /// <param name="data">Массив данных</param>
        /// <param name="count">Количество элементов в массиве</param>
        /// <param name="index">Номер посылки</param>
        void DataWritten(byte[] data, int count, uint index);
    }

    public interface IPortDisplay : IReadDisplay, IWriteDisplay
    {
        /// <summary>
        /// Отображение сообщения, информирующего о приостановке вывода данных, считываемых с порта
        /// </summary>
        void SuspendDisplaying();

        /// <summary>
        /// Скрытие сообщения о приостановке вывода данных
        /// </summary>
        void ResumeDisplaying();
    }

    public interface ICycleWriteSubject
    {
        /// <summary>
        /// Уведомление об остановке потока записи в порт (ВНИМАНИЕ! метод может быть вызван Не в оконном потоке)
        /// </summary>
        void CyclicWriteCompleted();
    }

    public interface ICycleWriteAndReadSubject : ICycleWriteSubject, IReadDisplay {}

    /// <summary>
    /// Параметры порта
    /// </summary>
    public struct PortParams
    {
        /// <summary>
        /// Признак работы с двумя портами: первый - для передачи, второй - для приема
        /// </summary>
        public bool twoPorts;

        /// <summary>
        /// Имя первого порта
        /// </summary>
        public string portName1;

        /// <summary>
        /// Имя второго порта
        /// </summary>
        public string portName2;

        /// <summary>
        /// Скорость обмена
        /// </summary>
        public int baudRate;

        /// <summary>
        /// Размер буфера чтения
        /// </summary>
        public int readBufferSize;

        /// <summary>
        /// Размер буфера записи
        /// </summary>
        public int writeBufferSize;

        /// <summary>
        /// Число бит данных
        /// </summary>
        public int dataBits;

        /// <summary>
        /// Контроль четности
        /// </summary>
        public Win32.ParityFlags parity;

        /// <summary>
        /// Число стоповых бит
        /// </summary>
        public Win32.StopBitsFlags stopBits;
    }

    /// <summary>
    /// Управление портами
    /// </summary>
    public class PortModel
    {
        /// <summary>
        /// Объект через который осуществляется вывод обменной информации
        /// </summary>
        private IPortDisplay display;

        /// <summary>
        /// Делегат, вызываемый для отправки сообщения об освобождении портов другим программам
        /// </summary>
        private Action closePortsAction;

        /// <summary>
        /// Делегат, вызываемый для обработки ошибок в операциях потокового чтения/записи с портом
        /// </summary>
        private Action<string> errorHandler;

        /// <summary>
        /// Порты
        /// </summary>
        private Win32ComPort port1, port2;

        /// <summary>
        /// Параметры портов
        /// </summary>
        private PortParams param;

        /// <summary>
        /// Список наблюдателей за состоянием портов
        /// </summary>
        private LinkedList<IPortListener> portListeners;

        /// <summary>
        /// Признак работы потока по закрытию портов
        /// </summary>
        private volatile bool IsClosing;

        /// <summary>
        /// Приемник - объект, выполняющий операции чтения с порта
        /// </summary>
        private Receiver receiver;

        /// <summary>
        /// Передатчик - объект, выполняющий операции записи в порт
        /// </summary>
        private Transmitter transmitter;

        /// <summary>
        /// Состояние порта (открыт/не открыт)
        /// </summary>
        public bool IsOpened { get; private set; }

        /// <summary>
        /// Конструктор
        /// </summary>
        /// <param name="display">Объект через который осуществляется вывод обменной информации</param>
        /// <param name="closePortsAction">Делегат, вызываемый для отправки сообщения об освобождении портов другим программам</param>
        /// <param name="errorHandler">Делегат, вызываемый для обработки ошибок в операциях потокового чтения/записи с портом</param>
        public PortModel(IPortDisplay display, Action closePortsAction, Action<string> errorHandler)
        {
            // Инициализируем поля, создаваемого объекта
            this.display = display;
            this.closePortsAction = closePortsAction;
            this.errorHandler = errorHandler;
            IsOpened = false;
            IsClosing = false;
            port1 = new Win32ComPort();
            port2 = new Win32ComPort();
            receiver = new Receiver(display, ErrorHandler);
            transmitter = new Transmitter(display, ErrorHandler);
            portListeners = new LinkedList<IPortListener>();

            // Устанавливаем параметры портов по умолчанию
            param = new PortParams();
            param.twoPorts = false;
            param.portName1 = "COM1";
            param.portName2 = "COM1";
            param.baudRate = 9600;
            param.readBufferSize = 10000;
            param.writeBufferSize = 10000;
            param.dataBits = 8;
            param.parity = Win32.ParityFlags.None;
            param.stopBits = Win32.StopBitsFlags.One;
        }

        /// <summary>
        /// Открыть порт(ы)
        /// </summary>
        /// <param name="param">Список параметров</param>
        public void Open(PortParams param)
        {
            // Устанавливаем скорость обмена
            port1.BaudRate = param.baudRate;
            port2.BaudRate = param.baudRate;

            // Устанавливаем имена портов
            port1.PortName = param.portName1;
            port2.PortName = param.portName2;

            // Устанавливаем размеры буферов чтения и записи
            port1.ReadBufferSize = param.readBufferSize;
            port1.WriteBufferSize = param.writeBufferSize;
            port2.ReadBufferSize = param.readBufferSize;
            port2.WriteBufferSize = param.writeBufferSize;

            // Устанавливаем количество бит данных
            port1.DataBits = param.dataBits;
            port2.DataBits = param.dataBits;

            // Устанавливаем режим проверки четности
            port1.Parity = param.parity;
            port2.Parity = param.parity;

            // Устанавливаем количество стоповых бит
            port1.StopBits = param.stopBits;
            port2.StopBits = param.stopBits;

            // Открытие первого порта
            try { port1.Open(); }
            catch (Exception)
            {
                if (closePortsAction != null) { closePortsAction(); }
                Thread.Sleep(300);
                try { port1.Open(); }
                catch (Exception) { throw; }
            }

            // Открытие второго порта (если выбрана соответствующая опция)
            if (param.twoPorts)
            {
                try { port2.Open(); }
                catch (Exception)
                {
                    closePortsAction();
                    Thread.Sleep(300);
                    try { port2.Open(); }
                    catch (Exception) { if (port1.IsOpen) port1.Close(); throw; }
                }

                // Очистка буферов чтения и записи
                port2.DiscardInBuffer();
                port2.DiscardOutBuffer();
            }

            // Очистка буферов чтения и записи
            port1.DiscardInBuffer();
            port1.DiscardOutBuffer();

            // Запускаем поток непрерывного чтения данных с порта
            if(param.twoPorts) receiver.Start(port2);
            else receiver.Start(port1);

            // Изменяем состояние объекта в "открыт" и уведомляем всех зарегистрированных наблюдателей
            IsOpened = true;
            foreach (IPortListener listener in portListeners)
                listener.PortOpened(param);
        }

        /// <summary>
        /// Закрыть порт(ы)
        /// </summary>
        public void Close()
        {
            // Проверяем, что поток закрытия уже не запущен
            if (!IsClosing)
            {
                // Запускаем поток зыкрытия портов
                IsClosing = true;
                System.Threading.Tasks.Task.Factory.StartNew(() =>
                {
                    // Останавливаем потоковые операции записи/чтения
                    receiver.Stop();
                    transmitter.StopCyclicWrite();

                    // Закрываем порты
                    if (port2.IsOpen) port2.Close();
                    if (port1.IsOpen) port1.Close();

                    // Изменяем состояние объекта в "закрыт" и уведомляем всех зарегистрированных наблюдателей
                    IsOpened = false;
                    foreach (IPortListener listener in portListeners)
                        listener.PortClosed();

                    IsClosing = false;
                });
            }
        }

        /// <summary>
        /// Записать данные в порт
        /// </summary>
        /// <param name="data">Массив данных для записи</param>
        public void Write(byte[] data)
        {
            if (!IsOpened) throw new Exception("Не открыт порт для передачи данных");
            if ((data == null) || (data.Length <= 0)) throw new Exception("Массив данных пуст");

            if (transmitter.writeTaskIsRunning) { transmitter.StopCyclicWrite(); receiver.Resume(); }
            transmitter.Write(port1, data);
        }

        /// <summary>
        /// Записать данные в порт и прочитать ответ
        /// </summary>
        /// <param name="writeData">Массив данных для записи</param>
        /// <param name="delay">Задержка между записью и чтением (в милисекундах)</param>
        /// <param name="readDisplay">Объек-обработчик считанных данных, может быть равен null</param>
        /// <returns></returns>
        public int WriteAndRead(byte[] writeData, int delay, IReadDisplay readDisplay)
        {
            if (!IsOpened) throw new Exception("Не открыт порт для передачи данных");
            if ((writeData == null) || (writeData.Length <= 0)) throw new Exception("Массив данных пуст");
            if (delay <= 0) throw new Exception("Некорректное значение аргумента delay");

            if (transmitter.writeTaskIsRunning) transmitter.StopCyclicWrite();
            receiver.Suspend();
            transmitter.Write(port1, writeData);
            Thread.Sleep(delay);
            receiver.Read(readDisplay);
            receiver.Resume();
            return 0;
        }

        /// <summary>
        /// Запустить циклическую запись в порт
        /// </summary>
        /// <param name="data">Массив данных для записи</param>
        /// <param name="period">Период цикла</param>
        /// <param name="cycleWriteSubject">Ссылка на объект, вызвавший операцию циклической записи</param>
        public void RunCyclicWrite(byte[] data, int period, ICycleWriteSubject cycleWriteSubject)
        {
            if (!IsOpened) throw new Exception("Не открыт порт для передачи данных");
            if ((data == null) || (data.Length <= 0)) throw new Exception("Массив данных пуст");
            if (period <= 0) throw new Exception("Некорректное значение аргумента period");
            if (cycleWriteSubject == null) throw new Exception("Аргумент cycleWriteSubject равен null");

            transmitter.WritingData = data;
            if (transmitter.writeTaskIsRunning) { transmitter.StopCyclicWrite(); receiver.Resume(); }
            transmitter.RunCyclicWrite(port1, period, cycleWriteSubject, null);
        }

        /// <summary>
        /// Запуск процесса циклической записи и чтения
        /// </summary>
        /// <param name="data">Массив данных для записи</param>
        /// <param name="period">Период цикла</param>
        /// <param name="cycleWriteAndReadSubject">Объект-обработчик принимаемых с порта данных</param>
        public void RunCyclicWriteAndRead(byte[] data, int period, ICycleWriteAndReadSubject cycleWriteAndReadSubject)
        {
            if (!IsOpened) throw new Exception("Не открыт порт для передачи данных");
            if ((data == null) || (data.Length <= 0)) throw new Exception("Массив данных пуст");
            if (period <= 0) throw new Exception("Некорректное значение аргумента period");
            if (cycleWriteAndReadSubject == null) throw new Exception("Аргумент cycleWriteAndReadSubject равен null");

            if (transmitter.writeTaskIsRunning) transmitter.StopCyclicWrite();
            receiver.Suspend();
            transmitter.WritingData = data;
            transmitter.RunCyclicWrite(port1, period, cycleWriteAndReadSubject, () => receiver.Read(cycleWriteAndReadSubject));
        }

        /// <summary>
        /// Остановить циклическую запись
        /// </summary>
        public void StopCyclicWrite()
        {
            if (!IsOpened) throw new Exception("Не открыт порт для передачи данных");
            transmitter.StopCyclicWrite();
            receiver.Resume();
        }

        /// <summary>
        /// Обновить данные для записи
        /// </summary>
        /// <param name="data"></param>
        /// <param name="period"></param>
        public void RenewWritingData(byte[] data, int period)
        {
            if ((data == null) || (data.Length <= 0)) throw new Exception("Массив данных пуст");
            transmitter.WritingData = data;
            transmitter.Period = period;
        }

        /// <summary>
        /// Зарегистрировать наблюдателя за состоянием портов
        /// </summary>
        /// <param name="listener">Ссылка на объект-наблюдатель</param>
        public void AddPortListener(IPortListener listener)
        {
            if (!portListeners.Contains(listener))
                portListeners.AddLast(listener);
        }

        /// <summary>
        /// Исключить указынный объект из списка наблюдателей за состоянием портов
        /// </summary>
        /// <param name="listener">Объект для исключения</param>
        public void RemovePortListener(IPortListener listener)
        {
            portListeners.Remove(listener);
        }

        /// <summary>
        /// Удалить всех зарегистрированных наблдателей за сотоянием портов
        /// </summary>
        public void RemoveAllPortListeners()
        {
            portListeners.Clear();
        }

        /// <summary>
        /// Обработчик ошибок, возникающих при выполнении потоковых операций записи/чтения с портом
        /// </summary>
        /// <param name="message"></param>
        private void ErrorHandler(string message)
        {
            Close();
            errorHandler(message);
        }
    }
}
