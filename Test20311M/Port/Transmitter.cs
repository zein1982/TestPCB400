using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Test20311M
{
    /// <summary>
    /// Передатчик. Выполяет операции записи данных в порт
    /// </summary>
    class Transmitter
    {
        /// <summary>
        /// Задача циклической записи данных
        /// </summary>
        private Task tsk;

        /// <summary>
        /// Источник признака отмены задачи
        /// </summary>
        private CancellationTokenSource tokenSrc;

        /// <summary>
        /// Объект через который осуществляется отображение переданных данных
        /// </summary>
        private IWriteDisplay display;

        /// <summary>
        /// Ссылка на объект, вызвавший операцию цикличсеской записи в порт
        /// </summary>
        private ICycleWriteSubject cycleWriteSubject;

        /// <summary>
        /// Делегат, вызываемый для обработки ошибок в операции потоковой записи
        /// </summary>
        private Action<string> errorHandler;

        /// <summary>
        /// Состояние потока циклической записи в порт (запущен/не запущен)
        /// </summary>
        public bool writeTaskIsRunning { get; private set; }

        /// <summary>
        /// Данные для циклической записи в порт
        /// </summary>
        public byte[] WritingData { get; set; }

        /// <summary>
        /// Период цикла записи
        /// </summary>
        public int Period { get; set; }

        /// <summary>
        /// Конструктор
        /// </summary>
        /// <param name="display">Объект через который осуществляется отображение переданных данных</param>
        /// <param name="errorHandler">Делегат, вызываемый для обработки ошибок в операции потоковой записи</param>
        public Transmitter(IWriteDisplay display, Action<string> errorHandler)
        {
            this.display = display;
            this.errorHandler = errorHandler;
            writeTaskIsRunning = false;
        }

        /// <summary>
        /// Записать данные в порт
        /// </summary>
        /// <param name="port">Порт для записи</param>
        /// <param name="progData">Массив данных для записи</param>
        /// <param name="overlap">Признак "перекрываемого" отображения данных</param>
        public void Write(GeneralPort port, byte[] data, bool overlap = true)
        {
            if (writeTaskIsRunning) StopCyclicWrite();

            port.Write(data, 0, data.Length);
            if (display != null) { display.DataWritten(data, data.Length, 0, overlap); }
        }

        /// <summary>
        /// Запустить поток циклической записи данных в порт
        /// </summary>
        /// <param name="port">Порт для записи</param>
        /// <param name="progData">Массив данных для записи</param>
        /// <param name="period">Период цикла</param>
        /// <param name="readDisplay">Метод, вызываемый для чтения данных с порта</param>
        public void RunCyclicWrite(GeneralPort port, int period, ICycleWriteSubject cycleWriteSubject, Action readDisplay)
        {
            // Останавливаем поток записи, если он был запущен ранее
            if (writeTaskIsRunning) StopCyclicWrite();

            this.cycleWriteSubject = cycleWriteSubject;

            Period = period;

            tokenSrc = new CancellationTokenSource();
            tsk = Task.Factory.StartNew((ct) =>
            {
                CancellationToken cancelTok = (CancellationToken)ct;
                try
                {
                    uint index = 0;

                    // Выполняем запись данных пока не будет получен признак отмены задачи
                    while (!cancelTok.IsCancellationRequested)
                    {
                        // Передаем данные
                        port.Write(WritingData, 0, WritingData.Length);
                        if (Period >= 50) display.DataWritten(WritingData, WritingData.Length, index++);

                        // Приостанавливаем поток на время не более 100 мкс
                        int delay = Period;
                        while(delay > 100)
                        {
                            Thread.Sleep(100);
                            delay -= 100;
                            if (cancelTok.IsCancellationRequested) return;
                        }
                        if (delay > 0) Thread.Sleep(delay);

                        if (readDisplay != null) readDisplay();
                    }
                }
                catch(Exception ex) { errorHandler(ex.Message); }
                finally { writeTaskIsRunning = false; }
            }, tokenSrc.Token, tokenSrc.Token);

            writeTaskIsRunning = true;
        }

        /// <summary>
        /// Остановить поток циклической записи в порт
        /// </summary>
        public void StopCyclicWrite()
        {
            if (!writeTaskIsRunning) return;

            // Устанавливаем флаг остановки потока циклической записи
            tokenSrc.Cancel();

            // Функция ожидания завершения потока циклической записи
            Action WaitingForTaskStopAction = () =>
            {
                tsk.Wait();
                tsk.Dispose();
                tokenSrc.Dispose();

                // Уведомляем объект, запустивший поток записи о его остановке
                if (cycleWriteSubject != null) cycleWriteSubject.CyclicWriteCompleted();
            };

            // Если метод вызван из самого потока циклической записи, то ждем его завершения в новом потоке
            if (Task.CurrentId == tsk.Id) Task.Factory.StartNew(WaitingForTaskStopAction);
            else WaitingForTaskStopAction();
        }
    }
}
