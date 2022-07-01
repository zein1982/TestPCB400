using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace ComPort
{
    /// <summary>
    /// Состояния потока чтения с порта: "Запущен", "Не запущен", "Приостановлен"
    /// </summary>
    enum ReadTaskState { stRunning, stNotRunning, stSuspended }

    /// <summary>
    /// Приемник. Выполняет операции чтения с порта
    /// </summary>
    class Receiver
    {
        /// <summary>
        /// Используемый для чтения порт
        /// </summary>
        private Win32ComPort port;

        /// <summary>
        /// Объект-дисплей, используемый для вывода принятой информации
        /// </summary>
        private IPortDisplay display;

        /// <summary>
        /// Делегат, вызываемый для обработки ошибок в операции потокового чтения
        /// </summary>
        private Action<string> errorAction;

        /// <summary>
        /// Задача чтения данных
        /// </summary>
        private Task tsk;

        /// <summary>
        /// Источник признака отмены задачи
        /// </summary>
        private CancellationTokenSource tokenSrc;

        /// <summary>
        /// Максисальное число данных, принимаемых за 100 мс, которое выводится на экран
        /// </summary>
        private const int MAX_DATA = 100256;

        /// <summary>
        /// Состояние потока чтения данных с порта
        /// </summary>
        private volatile ReadTaskState readTaskState;

        /// <summary>
        /// Объект-синхронизатор, выполняющий приостановку потока чтения данных с порта
        /// </summary>
        private ManualResetEvent suspendObj;

        /// <summary>
        /// Конструктор
        /// </summary>
        /// <param name="display">Объект через который осуществляется отображение принимаемых данных</param>
        /// <param name="errorAction"></param>
        public Receiver(IPortDisplay display, Action<string> errorAction)
        {
            this.display = display;
            this.errorAction = errorAction;
            readTaskState = ReadTaskState.stNotRunning;
            suspendObj = new ManualResetEvent(true);
        }

        /// <summary>
        /// Прочитать данные с порта
        /// </summary>
        /// <param name="readDisplay">Объект-обработчик принимаемых данных</param>
        public void Read(IReadDisplay readDisplay)
        {
            /*bool suspended = false;
            if (readTaskState == ReadTaskState.stRunning)
            {
                Suspend();
                suspended = true;
            }*/

            byte[] buf = null;
            int n = port.BytesToRead;
            if(n > 0)
            {
                buf = new byte[n];
                n = port.Read(buf, 0, n);
            }

            if (display != null) { display.DataReceived(buf, n); }
            if(readDisplay != null) readDisplay.DataReceived(buf, n);
            //if (suspended) Resume();
        }

        /// <summary>
        /// Запуск потока чтения
        /// </summary>
        /// <param name="port">Используемый порт</param>
        public void Start(Win32ComPort port)
        {
            if (readTaskState != ReadTaskState.stNotRunning) Stop();

            this.port = port;
            tokenSrc = new CancellationTokenSource();
            suspendObj.Set();
            tsk = Task.Factory.StartNew(ReadingTask, tokenSrc.Token, tokenSrc.Token);
            readTaskState = ReadTaskState.stRunning;
        }

        /// <summary>
        /// Остановка потока чтения
        /// </summary>
        public void Stop()
        {
            if (readTaskState != ReadTaskState.stNotRunning)
            {
                suspendObj.Set();
                tokenSrc.Cancel();
                tsk.Wait();
                tsk.Dispose();
                tokenSrc.Dispose();
            }
        }

        /// <summary>
        /// Основной метод потока чтения
        /// </summary>
        /// <param name="ct">Признак отмены задачи</param>
        private void ReadingTask(object ct)
        {
            CancellationToken cancelTok = (CancellationToken)ct;

            int n;
            byte[] buf = new byte[port.ReadBufferSize / 2];

            DateTime? dt0 = null;       // Время начала интервала отсчета
            DateTime? dt1 = null;       // Текущее время отсчета
            int dataCounter = 0;        // Общее число принятых байт с начала интервала
            bool displaying = true;     // Признак разрешения вывода принятых данных на форму

            try
            {
                // Выполняем чтение данных пока не будет получен признак отмены задачи
                while (!cancelTok.IsCancellationRequested)
                {
                    // Ожидание данных
                    while (suspendObj.WaitOne() && (port.BytesToRead == 0))
                    {
                        // Выход, если получен признак завершения задачи
                        if (cancelTok.IsCancellationRequested) { display.ResumeDisplaying(); return; }

                        Thread.Sleep(10);

                        // Разрешаем вывод данных на форму, если за последние 100 мс принято не более MAX_DATA байт
                        if (!displaying)
                        {
                            dt1 = DateTime.Now;
                            if ((dt1.Value - dt0.Value).TotalMilliseconds >= 100)
                            {
                                if (dataCounter <= MAX_DATA)
                                {
                                    display.ResumeDisplaying();
                                    displaying = true;
                                    dt0 = dt1;
                                    dataCounter = 0;
                                }
                                else
                                {
                                    dt0 = dt1;
                                    dataCounter = 0;
                                }
                            }
                        }
                        else if (dt0.HasValue && ((DateTime.Now - dt0.Value).TotalMilliseconds >= 100))
                        {
                            dt0 = null;
                            dataCounter = 0;
                        }
                    }

                    // Ждем пока не будет принят весь пакет данных
                    do
                    {
                        // Выход, если получен призанак завершения задачи
                        if (cancelTok.IsCancellationRequested) { display.ResumeDisplaying(); return; }

                        n = port.BytesToRead;
                        Thread.Sleep(1);
                    }
                    while ((port.BytesToRead > n) && (n <= port.ReadBufferSize / 2));

                    // Считываем данные
                    n = port.BytesToRead;
                    n = port.Read(buf, 0, n = port.BytesToRead > port.ReadBufferSize / 2 ? port.ReadBufferSize / 2 : n);

                    // Вычисляем число байт принятых за 100 мс и приостанавливаем вывод данных на форму, если это число больше значения MAX_DATA,
                    // либо восстанавливаем вывод данных, если число принятых байт меньше MAX_DATA
                    dataCounter += n;
                    if (dt0 == null) dt0 = DateTime.Now;
                    else
                    {
                        dt1 = DateTime.Now;

                        if (displaying)
                        {
                            if ((dt1.Value - dt0.Value).TotalMilliseconds >= 20)
                            {
                                if (dataCounter > MAX_DATA)
                                {
                                    // приостанавливаем
                                    display.SuspendDisplaying();
                                    displaying = false;
                                    dt0 = dt1;
                                    dataCounter = n;
                                    continue;
                                }
                                else// if ((dt1.Value - dt0.Value).TotalMilliseconds >= 100)
                                {
                                    dt0 = dt1;
                                    dataCounter = n;
                                }
                            }
                        }
                        else
                        {
                            if ((dt1.Value - dt0.Value).TotalMilliseconds >= 100)
                            {
                                if (dataCounter <= MAX_DATA)
                                {
                                    display.ResumeDisplaying();
                                    displaying = true;
                                    dt0 = dt1;
                                    dataCounter = n;
                                }
                                else
                                {
                                    dt0 = dt1;
                                    dataCounter = n;
                                }
                            }
                        }
                    }
                    // Выводим принятые данные на форму, если вывод не приостановлен
                    if (displaying) display.DataReceived(buf, n);
                }// while (!cancelTok.IsCancellationRequested)
            }
            catch(Exception ex)
            {
                errorAction(ex.Message);
            }
            finally
            {
                if (display != null) { display.ResumeDisplaying(); }
                readTaskState = ReadTaskState.stNotRunning;
            }
        }// private void ReadingTask(object ct)

        /// <summary>
        /// Приостановить поток чтения данных с порта
        /// </summary>
        public void Suspend()
        {
            if (readTaskState == ReadTaskState.stRunning)
            {
                suspendObj.Reset();
                readTaskState = ReadTaskState.stSuspended;
            }
        }

        /// <summary>
        /// Возобновить поток чтения данных с порта
        /// </summary>
        public void Resume()
        {
            if (readTaskState == ReadTaskState.stSuspended)
            {
                suspendObj.Set();
                readTaskState = ReadTaskState.stRunning;
            }
        }
    }
}
