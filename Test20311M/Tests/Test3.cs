using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Markup;
using System.Windows.Media;
using System.Windows.Threading;
using Test20311M.Test3;

namespace Test20311M
{
    namespace Test3
    {
        /// <summary>
        /// Класс для управления кнопками с двумя состояниями на форме
        /// </summary>
        public class ButtonController
        {
            /// <summary>
            /// Ссылка на контроллер теста
            /// </summary>
            protected MainWindow.Test3 test;

            /// <summary>
            /// Управляемая кнопка
            /// </summary>
            protected ToggleButton button;

            /// <summary>
            /// Данные для передачи
            /// </summary>
            protected byte[] data;

            /// <summary>
            /// Конструктор
            /// </summary>
            /// <param name="test">Ссылка на контроллер теста</param>
            /// <param name="button">Управлемая кнопка</param>
            /// <param name="progData">Массив данных для передачи</param>
            public ButtonController(MainWindow.Test3 test, ToggleButton button, byte[] data)
            {
                this.test = test;
                this.button = button;
                this.data = data;

                // При нажатии на кнопку вызываем метод SendMessage() контроллера теста
                button.Click += (sender, e) =>
                {
                    if (button.IsChecked ?? false) test.SendMessage(this);
                    else test.StopTest();
                };
            }

            /// <summary>
            /// Снять "залипание" кнопки
            /// </summary>
            public virtual void Uncheck() { button.IsChecked = false; }

            /// <summary>
            /// Установить "залипание" кнопки
            /// </summary>
            public virtual void Check() { button.IsChecked = true; }

            /// <summary>
            /// Получить данные для передачи
            /// </summary>
            /// <returns>Массив данных для передачи</returns>
            public virtual byte[] GetData() { return data; }
        }

        /// <summary>
        /// Класс для управления кнопкой установки сектора и элементов ввода сектора
        /// </summary>
        public class SectorButtonController : ButtonController
        {
            /// <summary>
            /// Текстовое поле для ввода градусов
            /// </summary>
            private TextBox tbDegrees;

            /// <summary>
            /// Текстовое поле для ввода минут
            /// </summary>
            private TextBox tbMinutes;

            /// <summary>
            /// Выпадающий список для выбора секторов
            /// </summary>
            private ComboBox cbSector;

            /// <summary>
            /// Конструктор
            /// </summary>
            /// <param name="test">Ссылка на контроллер теста</param>
            /// <param name="tbDegrees">Текстовое поле для ввода градусов</param>
            /// <param name="tbMinutes">Текстовое поле для ввода минут</param>
            /// <param name="cbSector">Элемент выбора предустановленных секторов</param>
            /// <param name="button">Кнопка установки сектора</param>
            /// <param name="progData">Массив данных для передачи</param>
            public SectorButtonController(MainWindow.Test3 test, TextBox tbDegrees, TextBox tbMinutes, ComboBox cbSector, ToggleButton button, byte[] data) : base(test, button, data)
            {
                this.tbDegrees = tbDegrees;
                this.tbMinutes = tbMinutes;
                this.cbSector = cbSector;

                // При нажатии на кнопку вызываем метод Check() или Uncheck() для блокировки/разблокировки элементов выбора сектора
                button.Click += (sender, e) =>
                {
                    if (button.IsChecked ?? false) Check();
                    else Uncheck();
                };

                // При вводе данных в поле "градусы" проверяем, чтобы введенное число было в диапазоне от 0 до 359,
                // а также вызываем метод SendMessage(), если кнопка находится в "залипшем" состоянии, чтобы контроллер теста обновил данные
                tbDegrees.TextChanged += (sender, e) =>
                {
                    try
                    {
                        if (Convert.ToInt32(tbDegrees.Text) > 359)
                            tbDegrees.Text = "359";
                    }
                    catch (FormatException)
                    {
                        tbDegrees.Text = "0";
                    }
                };

                // При вводе данных в поле "минуты" проверяем, чтобы введенное число было в диапазоне от 0 до 59,
                // а также вызываем метод SendMessage(), если кнопка находится в "залипшем" состоянии, чтобы контроллер теста обновил данные
                tbMinutes.TextChanged += (sender, e) =>
                {
                    try
                    {
                        if (Convert.ToInt32(tbMinutes.Text) > 59)
                            tbMinutes.Text = "59";
                    }
                    catch (FormatException)
                    {
                        tbMinutes.Text = "0";
                    }
                };

                // При нажатии клавиши ENTER на элементы ввода угла обновляем данные для передачи
                KeyEventHandler keyDownHandler = (sender, e) =>
                {
                    if (e.Key == Key.Return)
                    {
                        cbSector.SelectedIndex = 0;
                        RenewData();
                        test.SendMessage(this);
                    }
                };
                tbDegrees.KeyDown += keyDownHandler;
                tbMinutes.KeyDown += keyDownHandler;

                // При выборе одного из предустановленных углов загружаем его значение в элементы отображения угла на форме
                // и обновляем данные для передачи
                cbSector.SelectionChanged += (sender, r) =>
                {
                    int degrees, minutes;
                    switch(cbSector.SelectedIndex)
                    {
                        case 0: return;
                        case 1: degrees = 179;
                                minutes = 59;
                                break;
                        case 2: degrees = 0;
                                minutes = 1;
                                break;
                        case 3: degrees = 0;
                                minutes = 0;
                                break;
                        default: throw new Exception("Неизвестное состояние элемента cbSector");
                    }

                    tbDegrees.Text = degrees.ToString();
                    tbMinutes.Text = minutes.ToString();
                    RenewData();
                    test.SendMessage(this);
                };
            }

            /// <summary>
            /// Обновить данные для передачи
            /// </summary>
            public void RenewData()
            {
                // Получаем угол для установки из текстовых полей "градусы", "минуты"
                double angle = Convert.ToInt32(tbDegrees.Text);
                angle += ((double)Convert.ToInt32(tbMinutes.Text)) / 60.0 + 0.006;

                // Переводим угол в двоичный вид
                int binary_angle = 0;
                double division = 180.0;
                for (int i = 0; i < 14; ++i)
                {
                    if (angle >= division)
                    {
                        binary_angle |= (1 << (13 - i));
                        angle -= division;
                    }
                    division /= 2.0;
                }

                // Формируем сообщение для отправки
                byte[] message = new byte[5];
                message[0] = 0x41;
                message[1] = 0;
                message[2] = (byte)binary_angle;
                message[3] = (byte)(binary_angle >> 8);
                message[4] = 0x33;

                // Вычисляем признак четности полученного сообщения и при необходимости устанавливаем соответствующий бит
                int numberOfBits = 0;
                for (int i = 0; i < 8; ++i)
                {
                    if ((message[2] & (1 << i)) != 0) ++numberOfBits;
                    if ((message[3] & (1 << i)) != 0) ++numberOfBits;
                }
                if ((numberOfBits & 1) == 0) message[4] = 0xB3;

                data = message;
            }

            public override void Check()
            {
                base.Check();
                cbSector.IsEnabled = true;
                tbDegrees.IsEnabled = true;
                tbMinutes.IsEnabled = true;
            }

            public override void Uncheck()
            {
                base.Uncheck();
                cbSector.SelectedIndex = 0;
                cbSector.IsEnabled = false;
                tbDegrees.IsEnabled = false;
                tbMinutes.IsEnabled = false;
                data = new byte[] { 0x59, 0, 0, 0, 0x70 };
            }

            /// <summary>
            /// Метод для загрузки новых данных данных и запуска их передачи
            /// </summary>
            /// <param name="degrees">Градусы</param>
            /// <param name="minutes">Минуты</param>
            public void LoadDataAndRun(int degrees, int minutes)
            {
                // Сохраняем данные в текстовые поля "градусы", "минуты"
                tbDegrees.Text = degrees.ToString();
                tbMinutes.Text = minutes.ToString();

                // Вызываем метод SendMessage() контроллера теста
                //Check();
                //test.SendMessage(this);
            }
        }

        /// <summary>
        /// Класс, выполняющий отправку сообщения для перевода УСМА в режим программирования
        /// </summary>
        public class ProgUsmaHandler : IReadDisplay
        {
            private static ProgUsmaHandler instance = null;
            private MainWindow wnd;
            private PortModel port;
            private Programmer programmer;
            private byte[] data = new byte[] { (byte)'P', (byte)'R', (byte)'O', (byte)'G', 0xB3 };

            public static ProgUsmaHandler GetInstance(MainWindow wnd, PortModel port, Programmer programmer)
            {
                if (instance == null) { instance = new ProgUsmaHandler(wnd, port, programmer); }
                else
                {
                    instance.wnd = wnd;
                    instance.port = port;
                    instance.programmer = programmer;
                }
                return instance;
            }

            private ProgUsmaHandler(MainWindow wnd, PortModel port, Programmer programmer)
            {
                this.wnd = wnd;
                this.port = port;
                this.programmer = programmer;
            }

            public void Run()
            {
                try
                {
                    port.WriteAndRead(data, 100, this);
                }
                catch (Exception ex)
                {
                    System.Windows.MessageBox.Show(wnd, ex.Message, "Ошибка записи данных на порт", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }

            public void DataReceived(byte[] data, int count)
            {
                wnd.Dispatcher.BeginInvoke(DispatcherPriority.Send, new Action(() =>
                {
                    if((count == 2) && (data[0] == (byte)'O') && (data[1] == (byte)'K'))
                    {
                        if (programmer.ProgIsOpened)
                        {
                            if (System.Windows.MessageBox.Show(wnd, "УСМА переведен в режим программирования.\n\nЗаписать программу из файла\n" + programmer.FilePath + "?", "Сообщение", MessageBoxButton.YesNo, MessageBoxImage.Question) == MessageBoxResult.Yes)
                            {
                                programmer.RawProg();
                            }
                        }
                        else
                        {
                            if (System.Windows.MessageBox.Show(wnd, "УСМА переведен в режим программирования.\n\nЗаписать программу из файла?", "Сообщение", MessageBoxButton.YesNo, MessageBoxImage.Question) == MessageBoxResult.Yes)
                            {
                                if (programmer.OpenProgDialog())
                                {
                                    programmer.RawProg();
                                }
                            }
                            //MessageBox.Show(wnd, "УСМА переведен в режим программирования", "Сообщение", MessageBoxButton.OK, MessageBoxImage.Information);
                        }
                    }
                    else
                    {
                        MessageBox.Show(wnd, "Не удалось перевести УСМА в режим программирования", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
                    }
                }));
            }
        }

        /// <summary>
        /// Класс, выполняющий обработку ответного сообщения при переводе УСМА в режим программирования
        /// </summary>
        public class L60Handler : IReadDisplay
        {
            private static L60Handler instance = null;
            private MainWindow wnd;
            private PortModel port;
            private Semaphore sema;
            private volatile int stage;
            private Task task;
            private CancellationTokenSource tokenSrc;
            private volatile bool running;

            public L60Handler(MainWindow wnd, PortModel port)
            {
                this.wnd = wnd;
                this.port = port;

                running = false;
                sema = new Semaphore(0, 1);
            }

            public static L60Handler GetInstance(MainWindow wnd, PortModel port)
            {
                if (instance == null) { instance = new L60Handler(wnd, port); }
                return instance;
            }

            public void Run()
            {
                if (running) { return; }
                running = true;

                tokenSrc = new CancellationTokenSource();

                task = Task.Factory.StartNew((ct) =>
                {
                    stage = 0;
                    CancellationToken cancelTok = (CancellationToken)ct;

                    byte[] data = new byte[48];
                    data[0] = 0x21;
                    data[1] = 0;
                    data[2] = 0x10;
                    data[3] = 0;
                    data[4] = 0;
                    data[5] = 0;
                    data[6] = 0;
                    data[7] = 0;
                    data[8] = 0;
                    data[9] = 0;
                    data[10] = 0;
                    data[11] = 0x18;
                    data[12] = 0;
                    data[13] = 0;
                    data[14] = 0;
                    data[15] = 0x18 + 0x10;   // Контрольная сумма

                    data[16] = 0x22;
                    data[17] = 0;
                    data[18] = 0x10;
                    data[19] = 0;
                    data[20] = 0;
                    data[21] = 0;
                    data[22] = 0;
                    data[23] = 0;
                    data[24] = 0;
                    data[25] = 0;
                    data[26] = 0;
                    data[27] = 0;
                    data[28] = 0;
                    data[29] = 0;
                    data[30] = 0;
                    data[31] = 0x10;  // Контрольная сумма

                    data[32] = 0x33;
                    data[33] = 0;
                    data[34] = 0xEF;
                    data[35] = 0;
                    data[36] = 0;
                    data[37] = 0;
                    data[38] = 0;
                    data[39] = 0;
                    data[40] = 0;
                    data[41] = 0;
                    data[42] = 0;
                    data[43] = 0;
                    data[44] = 0;
                    data[45] = 0;
                    data[46] = 0;
                    data[47] = 0xEF;

                    try
                    {
                        port.WriteAndRead(data, 100, this);
                    }
                    catch (Exception ex)
                    {
                        wnd.Dispatcher.BeginInvoke(new Action(() =>
                        {
                            System.Windows.MessageBox.Show(wnd, ex.Message, "Ошибка записи данных на порт", MessageBoxButton.OK, MessageBoxImage.Error);
                        }));
                        running = false;
                        return;
                    }

                    sema.WaitOne();
                    if (cancelTok.IsCancellationRequested) { return; }

                    stage = 1;

                    data[0] = 0x21;
                    data[1] = 0;
                    data[2] = 0xEF;
                    data[3] = 0;
                    data[4] = 0;
                    data[5] = 0;
                    data[6] = 0;
                    data[7] = 0;
                    data[8] = 0;
                    data[9] = 0;
                    data[10] = 0;
                    data[11] = 0;
                    data[12] = 0;
                    data[13] = 0;
                    data[14] = 0;
                    data[15] = 0xEF;    // Контрольная сумма

                    data[16] = 0x22;
                    data[17] = 0;
                    data[18] = 0xEF;
                    data[19] = 0;
                    data[20] = 0;
                    data[21] = 0;
                    data[22] = 0;
                    data[23] = 0;
                    data[24] = 0;
                    data[25] = 0;
                    data[26] = 0;
                    data[27] = 0;
                    data[28] = 0;
                    data[29] = 0;
                    data[30] = 0;
                    data[31] = 0xEF;    // Контрольная сумма

                    data[32] = 0x33;
                    data[33] = 0;
                    data[34] = 0xEF;
                    data[35] = 0;
                    data[36] = 0;
                    data[37] = 0;
                    data[38] = 0;
                    data[39] = 0;
                    data[40] = 0;
                    data[41] = 0;
                    data[42] = 0;
                    data[43] = 0;
                    data[44] = 0;
                    data[45] = 0;
                    data[46] = 0;
                    data[47] = 0xEF;

                    try
                    {
                        port.WriteAndRead(data, 100, this);
                    }
                    catch (Exception ex)
                    {
                        wnd.Dispatcher.BeginInvoke(new Action(() =>
                        {
                            System.Windows.MessageBox.Show(wnd, ex.Message, "Ошибка записи данных на порт", MessageBoxButton.OK, MessageBoxImage.Error);
                            running = false;
                        }));
                        return;
                    }
                }, tokenSrc.Token, tokenSrc.Token);
            }

            public void DataReceived(byte[] data, int count)
            {
                wnd.Dispatcher.BeginInvoke(DispatcherPriority.Send, new Action(() =>
                {
                    if(stage == 0)
                    {
                        bool err = false;
                        if((count == 0) || (data == null) || (data.Length == 0))
                        {
                            MessageBox.Show(wnd, "Нет ответа от СЭВМ", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
                            err = true;
                        }
                        else if(count != 3)
                        {
                            MessageBox.Show(wnd, "Принято " + count + " байт(а) (должно быть 3)", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
                            err = true;
                        }
                        else if (((data[0] & 0xF0) != 0) || ((data[1] & 0xF0) != 0) || ((data[2] & 0xF0) != 0))
                        {
                            MessageBox.Show(wnd, "Коды ошибок: " + ((data[0] >> 4) & 0xF).ToString() + ", " + ((data[1] >> 4) & 0xF).ToString() + ", " + ((data[2] >> 4) & 0xF).ToString(), "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
                            err = true;
                        }

                        if(err)
                        {
                            tokenSrc.Cancel();
                            sema.Release();
                            task.Wait();
                            task.Dispose();
                            task = null;
                            tokenSrc.Dispose();
                            tokenSrc = null;
                            running = false;
                        }
                        else
                        {
                            sema.Release();
                        }
                    }
                    else
                    {
                        if ((count == 0) || (data == null) || (data.Length == 0))
                        {
                            MessageBox.Show(wnd, "Нет ответа от СЭВМ", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
                        }
                        else if (count != 3)
                        {
                            MessageBox.Show(wnd, "Принято " + count + " байт(а) (должно быть 3)", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
                        }
                        else if (((data[0] & 0xF0) != 0) || ((data[1] & 0xF0) != 0) || ((data[2] & 0xF0) != 0))
                        {
                            MessageBox.Show(wnd, "Коды ошибок: " + ((data[0] >> 4) & 0xF).ToString() + ", " + ((data[1] >> 4) & 0xF).ToString() + ", " + ((data[2] >> 4) & 0xF).ToString(), "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
                        }
                        else
                        {
                            MessageBox.Show(wnd, "Контейнер Н6М переведен в процедуру Имит Л60", "Успех", MessageBoxButton.OK, MessageBoxImage.Information);
                        }

                        task.Wait();
                        task.Dispose();
                        task = null;
                        tokenSrc.Dispose();
                        tokenSrc = null;
                        running = false;
                    }
                }));
            }
        }

        /// <summary>
        /// Класс, выполняющий отправку сообщения для получения информации об УСМА
        /// </summary>
        public class InfoHandler : IReadDisplay
        {
            private static InfoHandler instance = null;
            private MainWindow wnd;
            private PortModel port;
            private Semaphore sema;
            private volatile int stage;
            private byte[] data14 = new byte[14];
            private byte[] data16 = new byte[16];
            private bool extControl;
            private volatile bool running;
            private Task task;
            private CancellationTokenSource tokenSrc;

            public static InfoHandler GetInstance(MainWindow wnd, PortModel port)
            {
                if (instance == null) { instance = new InfoHandler(wnd, port); }
                return instance;
            }

            private InfoHandler(MainWindow wnd, PortModel port)
            {
                this.wnd = wnd;
                this.port = port;

                running = false;
                sema = new Semaphore(0, 1);
            }

            public void Run(bool extControl)
            {
                if (running) { return; }
                running = true;
                this.extControl = extControl;

                tokenSrc = new CancellationTokenSource();

                task = Task.Factory.StartNew((ct) =>
                {
                    CancellationToken cancelTok = (CancellationToken)ct;

                    try
                    {
                        sema.WaitOne(0);

                        stage = 0;
                        byte[] data = data14;
                        int pos = 0;
                        if(extControl)
                        {
                            data = data16;
                            data[pos++] = 0x33;
                            data[pos++] = 0;
                        }
                        data[pos++] = 0xEF;
                        data[pos++] = 0;
                        data[pos++] = 0;
                        data[pos++] = 0;
                        data[pos++] = 0;
                        data[pos++] = 0;
                        data[pos++] = 0;
                        data[pos++] = 0;
                        data[pos++] = 0;
                        data[pos++] = 0;
                        data[pos++] = 0;
                        data[pos++] = 0;
                        data[pos++] = 0;
                        data[pos++] = 0xEF;

                        port.WriteAndRead(data, 40, this);

                        sema.WaitOne();
                        if (cancelTok.IsCancellationRequested) { return; }

                        stage = 1;
                        data = data14;
                        pos = 0;
                        if (extControl)
                        {
                            data = data16;
                            data[pos++] = 0x53;
                            data[pos++] = 1;
                        }
                        data[pos++] = 0xEC;
                        data[pos++] = 22;
                        data[pos++] = 0;
                        data[pos++] = 0;
                        data[pos++] = 0;
                        data[pos++] = 0;
                        data[pos++] = 0;
                        data[pos++] = 0;
                        data[pos++] = 0;
                        data[pos++] = 0;
                        data[pos++] = 0;
                        data[pos++] = 0;
                        data[pos++] = 0;
                        data[pos++] = 0xEC + 22 - 255;

                        port.WriteAndRead(data, 40, this);
                    }
                    catch (Exception ex)
                    {
                        wnd.Dispatcher.BeginInvoke(new Action(() =>
                        {
                            System.Windows.MessageBox.Show(wnd, ex.Message, "Ошибка записи данных на порт", MessageBoxButton.OK, MessageBoxImage.Error);
                            running = false;
                        }));
                        return;
                    }


                }, tokenSrc.Token, tokenSrc.Token);
            }

            public void DataReceived(byte[] data, int count)
            {
                wnd.Dispatcher.BeginInvoke(DispatcherPriority.Send, new Action(() =>
                {
                    if (stage == 0)
                    {
                        bool err = false;
                        if(extControl)
                        {
                            if ((count == 0) || (data == null) || (data.Length == 0))
                            {
                                MessageBox.Show(wnd, "Нет ответа от СЭВМ", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
                                err = true;
                            }
                            else if (count != 1)
                            {
                                MessageBox.Show(wnd, "Принято " + count + " байт(а) от СЭВМ (должен быть 1)", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
                                err = true;
                            }
                            else if ((data[0] & 0xF0) != 0)
                            {
                                MessageBox.Show(wnd, "Код ошибки: " + ((data[0] >> 4) & 0xF).ToString(), "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
                                err = true;
                            }
                        }
                        else if(count != 0)
                        {
                            MessageBox.Show(wnd, "Принято " + count + " байт(а) (должен быть 0)", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
                            err = true;
                        }

                        if (err)
                        {
                            tokenSrc.Cancel();
                            sema.Release();
                            task.Wait();
                            task.Dispose();
                            task = null;
                            tokenSrc.Dispose();
                            tokenSrc = null;
                            running = false;
                        }
                        else
                        {
                            sema.Release();
                        }
                    }
                    else
                    {
                        if (extControl)
                        {
                            if ((count == 0) || (data == null) || (data.Length == 0))
                            {
                                MessageBox.Show(wnd, "Нет ответа от СЭВМ", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
                            }
                            else if (count != 16)
                            {
                                MessageBox.Show(wnd, "Принято " + count + " байт(а) от СЭВМ (должен быть 16)", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
                            }
                            else if ((data[0] & 0xF0) != 0)
                            {
                                MessageBox.Show(wnd, "Код ошибки: " + ((data[0] >> 4) & 0xF).ToString(), "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
                            }
                            else
                            {
                                count = data[1];
                                for (int i = 0; i < (data[1] < 14 ? data[1] : 14); ++i )
                                {
                                    data[i] = data[i + 2];
                                }
                            }
                        }

                        if (count == 0)
                        {
                            MessageBox.Show(wnd, "Нет ответа от блока", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
                        }
                        else if (count != 14)
                        {
                            MessageBox.Show(wnd, "Принято " + count + " байт(а) (должно быть 14)", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
                        }
                        else if ((data[0] != 0xEC) || (data[1] != 22) || (data[13] != Test.CalcCheckSum(data, 0, 13)))
                        {
                            MessageBox.Show(wnd, "Неверный ответ", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
                        }
                        else
                        {
                            MessageBox.Show(wnd, String.Format("Номер версии програмы:\t{0}\nКонтрольная сумма:\t{1:X2}", data[2], data[3]), "Версия ПО", MessageBoxButton.OK, MessageBoxImage.Information);
                        }

                        task.Wait();
                        task.Dispose();
                        task = null;
                        tokenSrc.Dispose();
                        tokenSrc = null;
                        running = false;
                    }
                }));
            }
        }

        /// <summary>
        /// Класс, выполняющий запрос скорости вращения антенны
        /// </summary>
        public class RotationRateHandler : ICycleWriteAndReadSubject
        {
            private static RotationRateHandler instance = null;
            private MainWindow wnd;
            private PortModel port;
            private ToggleButton bnRotationRate;
            private TextBlock tbStatus;
            private byte[] data14 = new byte[14];
            private byte[] data16 = new byte[16];
            private Semaphore sema;
            private volatile int stage;
            private bool extControl;
            private bool running;
            private Task task;
            private CancellationTokenSource tokenSrc;

            public static RotationRateHandler GetInstance(MainWindow wnd, PortModel port, ToggleButton bnRotationRate, TextBlock tbStatus)
            {
                if (instance == null) { instance = new RotationRateHandler(wnd, port, bnRotationRate, tbStatus); }
                return instance;
            }

            private RotationRateHandler(MainWindow wnd, PortModel port, ToggleButton bnRotationRate, TextBlock tbStatus)
            {
                this.wnd = wnd;
                this.port = port;
                this.bnRotationRate = bnRotationRate;
                this.tbStatus = tbStatus;
                running = false;
                sema = new Semaphore(0, 1);
            }

            public void Run(bool extControl)
            {
                if (!running)
                {
                    running = true;
                    this.extControl = extControl;

                    tokenSrc = new CancellationTokenSource();

                    task = Task.Factory.StartNew((ct) =>
                    {
                        CancellationToken cancelTok = (CancellationToken)ct;

                        try
                        {
                            sema.WaitOne(0);

                            stage = 0;
                            byte[] data = data14;
                            int pos = 0;
                            if (extControl)
                            {
                                data = data16;
                                data[pos++] = 0x33;
                                data[pos++] = 0;
                            }
                            data[pos++] = 0xEF;
                            data[pos++] = 0;
                            data[pos++] = 0;
                            data[pos++] = 0;
                            data[pos++] = 0;
                            data[pos++] = 0;
                            data[pos++] = 0;
                            data[pos++] = 0;
                            data[pos++] = 0;
                            data[pos++] = 0;
                            data[pos++] = 0;
                            data[pos++] = 0;
                            data[pos++] = 0;
                            data[pos++] = 0xEF;

                            port.WriteAndRead(data, 40, this);

                            sema.WaitOne();
                            if (cancelTok.IsCancellationRequested) { return; }

                            stage = 1;
                            data = data14;
                            pos = 0;
                            if (extControl)
                            {
                                data = data16;
                                data[pos++] = 0x53;
                                data[pos++] = 1;
                            }
                            data[pos++] = 0xEE;
                            data[pos++] = 22;
                            data[pos++] = 0;
                            data[pos++] = 0;
                            data[pos++] = 0;
                            data[pos++] = 0;
                            data[pos++] = 0;
                            data[pos++] = 0;
                            data[pos++] = 0;
                            data[pos++] = 0;
                            data[pos++] = 0;
                            data[pos++] = 0;
                            data[pos++] = 0;
                            data[pos++] = 0xEE + 22 - 255;

                            port.RunCyclicWriteAndRead(extControl ? data16 : data14, 100, this);
                        }
                        catch (Exception ex)
                        {
                            wnd.Dispatcher.BeginInvoke(new Action(() =>
                            {
                                System.Windows.MessageBox.Show(wnd, ex.Message, "Ошибка записи данных на порт", MessageBoxButton.OK, MessageBoxImage.Error);
                                running = false;
                            }));
                            return;
                        }

                    }, tokenSrc.Token, tokenSrc.Token);
                }
                else
                {
                    if (stage > 0)
                    {
                        port.StopCyclicWrite();
                    }
                    else
                    {
                        tokenSrc.Cancel();
                        sema.Release();
                        task.Wait();
                        task.Dispose();
                        task = null;
                        tokenSrc.Dispose();
                        tokenSrc = null;
                    }
                    running = false;
                }
            }

            public void DataReceived(byte[] data, int count)
            {
                wnd.Dispatcher.BeginInvoke(DispatcherPriority.Send, new Action(() =>
                {
                    if (stage == 0)
                    {
                        bool err = false;
                        if (extControl)
                        {
                            if ((count == 0) || (data == null) || (data.Length == 0))
                            {
                                MessageBox.Show(wnd, "Нет ответа от СЭВМ", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
                                err = true;
                            }
                            else if (count != 1)
                            {
                                MessageBox.Show(wnd, "Принято " + count + " байт(а) от СЭВМ (должен быть 1)", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
                                err = true;
                            }
                            else if ((data[0] & 0xF0) != 0)
                            {
                                MessageBox.Show(wnd, "Код ошибки: " + ((data[0] >> 4) & 0xF).ToString(), "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
                                err = true;
                            }
                        }
                        else if (count != 0)
                        {
                            MessageBox.Show(wnd, "Принято " + count + " байт(а) (должен быть 0)", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
                            err = true;
                        }

                        if (err)
                        {
                            tokenSrc.Cancel();
                            sema.Release();
                            task.Wait();
                            task.Dispose();
                            task = null;
                            tokenSrc.Dispose();
                            tokenSrc = null;
                            running = false;

                            bnRotationRate.IsChecked = false;
                        }
                        else
                        {
                            sema.Release();
                        }
                    }
                    else
                    {
                        if (extControl)
                        {
                            if ((count == 0) || (data == null) || (data.Length == 0))
                            {
                                wnd.Dispatcher.BeginInvoke(DispatcherPriority.Send, new Action(() =>
                                {
                                    wnd.tbStatus_test3.Inlines.Clear();
                                    wnd.tbStatus_test3.Inlines.Add("Состояние: ");
                                    wnd.tbStatus_test3.Inlines.Add(new Run("Нет ответа от СЭВМ") { Foreground = Brushes.Red, FontWeight = FontWeights.Bold });
                                }));
                                return;
                            }
                            if ((count != 16) || (data == null) || (data.Length != 16))
                            {
                                wnd.Dispatcher.BeginInvoke(DispatcherPriority.Send, new Action(() =>
                                {
                                    wnd.tbStatus_test3.Inlines.Clear();
                                    wnd.tbStatus_test3.Inlines.Add("Состояние: ");
                                    wnd.tbStatus_test3.Inlines.Add(new Run("Ошибка пакета") { Foreground = Brushes.Red, FontWeight = FontWeights.Bold });
                                }));
                                return;
                            }

                            count = data[1] <= 14 ? data[1] : 14;
                            for (int i = 0; i < count; ++i)
                            {
                                data[i] = data[2 + i];
                            }
                        }

                        if ((count == 0) || (data == null) || (data.Length == 0))
                        {
                            wnd.tbStatus_test3.Inlines.Clear();
                            wnd.tbStatus_test3.Inlines.Add("Состояние: ");
                            wnd.tbStatus_test3.Inlines.Add(new Run("Нет ответа от блока") { Foreground = Brushes.Red, FontWeight = FontWeights.Bold });
                        }
                        else if (count != 14)
                        {
                            wnd.tbStatus_test3.Inlines.Clear();
                            wnd.tbStatus_test3.Inlines.Add("Состояние: ");
                            wnd.tbStatus_test3.Inlines.Add(new Run("Ошибка обмена") { Foreground = Brushes.Red, FontWeight = FontWeights.Bold });
                        }
                        else if ((data[0] != 0xEE) || (data[1] != 22) || (data[13] != Test.CalcCheckSum(data, 0, 13)))
                        {
                            wnd.tbStatus_test3.Inlines.Clear();
                            wnd.tbStatus_test3.Inlines.Add("Состояние: ");
                            wnd.tbStatus_test3.Inlines.Add(new Run("Ошибка данных") { Foreground = Brushes.Red, FontWeight = FontWeights.Bold });
                        }
                        else
                        {
                            // Получаем угол в двоичном виде
                            int binary_angle = (data[8] & 0xFF) | ((data[9] & 0xFF) << 8);

                            // Переводим значение угла в градусы
                            double digital_angle = 0;
                            double digit_weight = 180.0;
                            for (int i = 0; i < 14; ++i)
                            {
                                if ((binary_angle & (1 << (13 - i))) != 0) digital_angle += digit_weight;
                                digit_weight /= 2.0;
                            }

                            // Разделяем угол на целую часть и дробную в виде минут
                            int deg = (int)digital_angle;
                            int min = (int)((digital_angle - (int)digital_angle) * 60 + 0.5);

                            wnd.lbDisplay_test3.Content = deg.ToString().PadLeft(3, ' ') + "°" + min.ToString().PadLeft(2, '0') + "'";

                            int T = data[2] | (data[3] << 8) | (data[4] << 16) | (data[5] << 24);
                            int delta = (short)(data[6] | (data[7] << 8));
                            double f_control = (short)((data[10] & 0xFF) | ((data[11] & 0xFF) << 8)) / 64.0;
                            //double f_control = 48000000.0 * 10.0 / T;
                            //double v = (double)T / 48000000.0 * delta / 0x4000 * 60;
                            double v = ((double)delta / (double)0x4000) / ((double)T / 48000000.0) * 60;        // delta fi / delta t * 60 sec
                            double ip = 45.0 / 17.0 * 35.0 * 164.0 / 13.0;
                            double f_actual = (double)(delta >= 0 ? delta : -delta) * 48000000.0 / (double)0x4000 / (double)T * ip * (f_control >= 400.0 ? 4 : 8);
                            double slide = (f_control == 0 ? 0.0 : (f_control - Math.Abs(f_actual)) * 100.0 / f_control);

                            wnd.tbStatus_test3.Inlines.Clear();
                            wnd.tbStatus_test3.Inlines.Add("v = ");
                            wnd.tbStatus_test3.Inlines.Add(new Run(String.Format("{0:0.000}", v)) { FontWeight = FontWeights.Bold, FontFamily = new FontFamily("Courier New") });
                            wnd.tbStatus_test3.Inlines.Add(" об/мин");
                            wnd.tbStatus_test3.Inlines.Add(Environment.NewLine + "f упр = ");
                            wnd.tbStatus_test3.Inlines.Add(new Run(String.Format("{0:0.000}", f_control)) { FontWeight = FontWeights.Bold, FontFamily = new FontFamily("Courier New") });
                            wnd.tbStatus_test3.Inlines.Add(" Гц");
                            wnd.tbStatus_test3.Inlines.Add(Environment.NewLine + "f факт = ");
                            wnd.tbStatus_test3.Inlines.Add(new Run(String.Format("{0:0.000}", f_actual)) { FontWeight = FontWeights.Bold, FontFamily = new FontFamily("Courier New") });
                            wnd.tbStatus_test3.Inlines.Add(" Гц");
                            wnd.tbStatus_test3.Inlines.Add(Environment.NewLine + "s = ");
                            wnd.tbStatus_test3.Inlines.Add(new Run(String.Format("{0:0.000}", slide)) { FontWeight = FontWeights.Bold, FontFamily = new FontFamily("Courier New") });
                            wnd.tbStatus_test3.Inlines.Add(" %");

                            //wnd.tbStatus_test3.Inlines.Add(Environment.NewLine + "T = ");
                            //wnd.tbStatus_test3.Inlines.Add(new Run(String.Format("0x{0:X8}", T)) { FontWeight = FontWeights.Bold, FontFamily = new FontFamily("Courier New") });
                            //wnd.tbStatus_test3.Inlines.Add(Environment.NewLine + "∆ = ");
                            //wnd.tbStatus_test3.Inlines.Add(new Run(String.Format("0x{0:X4}", delta)) { FontWeight = FontWeights.Bold, FontFamily = new FontFamily("Courier New") });
                        }
                    }
                }));
            }

            public void CyclicWriteCompleted()
            {
                wnd.Dispatcher.BeginInvoke(new Action(() =>
                {
                    running = false;
                    bnRotationRate.IsChecked = false;
                }));
            }
        }

        /// <summary>
        /// Класс, выполняющий запрос и запись номера функции разгона
        /// </summary>
        public class AccelerationFuncHandler : ICycleWriteAndReadSubject
        {
            private static AccelerationFuncHandler instance = null;
            private MainWindow wnd;
            private PortModel port;
            private ToggleButton bnAccelerationFunc;
            private TextBlock tbStatus;
            private ComboBox cbAccelerationFunc;
            private byte[] data14 = new byte[14];
            private byte[] data16 = new byte[16];
            private Semaphore sema;
            private volatile int stage;
            private bool extControl, running, write_flg;
            private int funcNum, newFuncNum;
            private Task task;
            private CancellationTokenSource tokenSrc;

            public static AccelerationFuncHandler GetInstance(MainWindow wnd, PortModel port, ToggleButton bnAccelerationFunc, ComboBox cbAccelerationFunc, TextBlock tbStatus)
            {
                if (instance == null) { instance = new AccelerationFuncHandler(wnd, port, bnAccelerationFunc, cbAccelerationFunc, tbStatus); }
                return instance;
            }

            private AccelerationFuncHandler(MainWindow wnd, PortModel port, ToggleButton bnAccelerationFunc, ComboBox cbAccelerationFunc, TextBlock tbStatus)
            {
                this.wnd = wnd;
                this.port = port;
                this.bnAccelerationFunc = bnAccelerationFunc;
                this.cbAccelerationFunc = cbAccelerationFunc;
                this.tbStatus = tbStatus;
                running = false;
                write_flg = false;
                sema = new Semaphore(0, 1);

                // При выборе одного из предустановленных углов загружаем его значение в элементы отображения угла на форме
                // и обновляем данные для передачи
                cbAccelerationFunc.SelectionChanged += (sender, r) =>
                {
                    if (!running || ((funcNum + 1) == cbAccelerationFunc.SelectedIndex)) return;
                    newFuncNum = cbAccelerationFunc.SelectedIndex - 1;
                    if (System.Windows.MessageBox.Show(wnd, "Изменить функцию разгона на\n\"" + ((ComboBoxItem)cbAccelerationFunc.Items[newFuncNum + 1]).Content + "\"?", "Сообщение", MessageBoxButton.YesNo, MessageBoxImage.Question) == MessageBoxResult.Yes)
                    {
                        write_flg = true;
                    }
                };
            }

            public void Run(bool extControl)
            {
                if (!running)
                {
                    running = true;
                    this.extControl = extControl;

                    tokenSrc = new CancellationTokenSource();

                    task = Task.Factory.StartNew((ct) =>
                    {
                        CancellationToken cancelTok = (CancellationToken)ct;

                        try
                        {
                            sema.WaitOne(0);

                            stage = 0;
                            byte[] data = data14;
                            int pos = 0;
                            if (extControl)
                            {
                                data = data16;
                                data[pos++] = 0x33;
                                data[pos++] = 0;
                            }
                            data[pos++] = 0xEF;
                            data[pos++] = 0;
                            data[pos++] = 0;
                            data[pos++] = 0;
                            data[pos++] = 0;
                            data[pos++] = 0;
                            data[pos++] = 0;
                            data[pos++] = 0;
                            data[pos++] = 0;
                            data[pos++] = 0;
                            data[pos++] = 0;
                            data[pos++] = 0;
                            data[pos++] = 0;
                            data[pos++] = 0xEF;

                            port.WriteAndRead(data, 40, this);

                            sema.WaitOne();
                            if (cancelTok.IsCancellationRequested) { return; }

                            stage = 1;
                            data = data14;
                            pos = 0;
                            if (extControl)
                            {
                                data = data16;
                                data[pos++] = 0x53;
                                data[pos++] = 1;
                            }
                            data[pos++] = 0xED;
                            data[pos++] = 22;
                            data[pos++] = (byte)'R';
                            data[pos++] = (byte)'E';
                            data[pos++] = (byte)'Q';
                            data[pos++] = (byte)'U';
                            data[pos++] = (byte)'E';
                            data[pos++] = (byte)'S';
                            data[pos++] = (byte)'T';
                            data[pos++] = (byte)' ';
                            data[pos++] = (byte)'F';
                            data[pos++] = (byte)'#';
                            data[pos++] = (byte)' ';
                            data[pos++] = Test.CalcCheckSum(data, extControl ? 2 : (uint)0, 13);

                            port.RunCyclicWriteAndRead(extControl ? data16 : data14, 100, this);
                        }
                        catch (Exception ex)
                        {
                            wnd.Dispatcher.BeginInvoke(new Action(() =>
                            {
                                System.Windows.MessageBox.Show(wnd, ex.Message, "Ошибка записи данных на порт", MessageBoxButton.OK, MessageBoxImage.Error);
                                running = false;
                            }));
                            return;
                        }

                    }, tokenSrc.Token, tokenSrc.Token);
                }
                else
                {
                    if (stage > 0)
                    {
                        port.StopCyclicWrite();
                    }
                    else
                    {
                        tokenSrc.Cancel();
                        sema.Release();
                        task.Wait();
                        task.Dispose();
                        task = null;
                        tokenSrc.Dispose();
                        tokenSrc = null;
                    }
                    running = false;
                }
            }

            public void DataReceived(byte[] data, int count)
            {
                if (stage == 0)
                {
                    bool err = false;
                    if (extControl)
                    {
                        if ((count == 0) || (data == null) || (data.Length == 0))
                        {
                            wnd.Dispatcher.BeginInvoke(DispatcherPriority.Send, new Action(() =>
                            {
                                MessageBox.Show(wnd, "Нет ответа от СЭВМ", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
                            }));
                            err = true;
                        }
                        else if (count != 1)
                        {
                            wnd.Dispatcher.BeginInvoke(DispatcherPriority.Send, new Action(() =>
                            {
                                MessageBox.Show(wnd, "Принято " + count + " байт(а) от СЭВМ (должен быть 1)", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
                            }));
                            err = true;
                        }
                        else if ((data[0] & 0xF0) != 0)
                        {
                            wnd.Dispatcher.BeginInvoke(DispatcherPriority.Send, new Action(() =>
                            {
                                MessageBox.Show(wnd, "Код ошибки: " + ((data[0] >> 4) & 0xF).ToString(), "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
                            }));
                            err = true;
                        }
                    }
                    else if (count != 0)
                    {
                        wnd.Dispatcher.BeginInvoke(DispatcherPriority.Send, new Action(() =>
                        {
                            MessageBox.Show(wnd, "Принято " + count + " байт(а) (должен быть 0)", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
                        }));
                        err = true;
                    }

                    if (err)
                    {
                        wnd.Dispatcher.BeginInvoke(DispatcherPriority.Send, new Action(() =>
                        {
                            tokenSrc.Cancel();
                            sema.Release();
                            task.Wait();
                            task.Dispose();
                            task = null;
                            tokenSrc.Dispose();
                            tokenSrc = null;
                            running = false;

                            bnAccelerationFunc.IsChecked = false;
                            cbAccelerationFunc.IsEnabled = false;
                            funcNum = -1;
                            cbAccelerationFunc.SelectedIndex = 0;
                        }));
                    }
                    else
                    {
                        sema.Release();
                    }
                }
                else if(stage == 1)
                {
                    if (extControl)
                    {
                        if ((count == 0) || (data == null) || (data.Length == 0))
                        {
                            funcNum = -1;
                            wnd.Dispatcher.BeginInvoke(DispatcherPriority.Send, new Action(() =>
                            {
                                wnd.tbStatus_test3.Inlines.Clear();
                                wnd.tbStatus_test3.Inlines.Add("Состояние: ");
                                wnd.tbStatus_test3.Inlines.Add(new Run("Нет ответа от СЭВМ") { Foreground = Brushes.Red, FontWeight = FontWeights.Bold });
                                cbAccelerationFunc.IsEnabled = false;
                                cbAccelerationFunc.SelectedIndex = 0;
                            }));
                            return;
                        }
                        if ((count != 16) || (data == null) || (data.Length != 16))
                        {
                            funcNum = -1;
                            wnd.Dispatcher.BeginInvoke(DispatcherPriority.Send, new Action(() =>
                            {
                                wnd.tbStatus_test3.Inlines.Clear();
                                wnd.tbStatus_test3.Inlines.Add("Состояние: ");
                                wnd.tbStatus_test3.Inlines.Add(new Run("Ошибка пакета") { Foreground = Brushes.Red, FontWeight = FontWeights.Bold });
                                cbAccelerationFunc.IsEnabled = false;
                                cbAccelerationFunc.SelectedIndex = 0;
                            }));
                            return;
                        }

                        count = data[1] <= 14 ? data[1] : 14;
                        for (int i = 0; i < count; ++i)
                        {
                            data[i] = data[2 + i];
                        }
                    }

                    if ((count == 0) || (data == null) || (data.Length == 0))
                    {
                        funcNum = -1;
                        wnd.Dispatcher.BeginInvoke(DispatcherPriority.Send, new Action(() =>
                        {
                            wnd.tbStatus_test3.Inlines.Clear();
                            wnd.tbStatus_test3.Inlines.Add("Состояние: ");
                            wnd.tbStatus_test3.Inlines.Add(new Run("Нет ответа от блока") { Foreground = Brushes.Red, FontWeight = FontWeights.Bold });
                            cbAccelerationFunc.IsEnabled = false;
                            cbAccelerationFunc.SelectedIndex = 0;
                        }));
                    }
                    else if (count != 14)
                    {
                        funcNum = -1;
                        wnd.Dispatcher.BeginInvoke(DispatcherPriority.Send, new Action(() =>
                        {
                            wnd.tbStatus_test3.Inlines.Clear();
                            wnd.tbStatus_test3.Inlines.Add("Состояние: ");
                            wnd.tbStatus_test3.Inlines.Add(new Run("Ошибка обмена") { Foreground = Brushes.Red, FontWeight = FontWeights.Bold });
                            cbAccelerationFunc.IsEnabled = false;
                            cbAccelerationFunc.SelectedIndex = 0;
                        }));
                    }
                    else if ((data[0] != 0xED) || (data[1] != 22) || (data[2] != (byte)'R') || (data[13] != Test.CalcCheckSum(data, 0, 13)))
                    {
                        funcNum = -1;
                        wnd.Dispatcher.BeginInvoke(DispatcherPriority.Send, new Action(() =>
                        {
                            wnd.tbStatus_test3.Inlines.Clear();
                            wnd.tbStatus_test3.Inlines.Add("Состояние: ");
                            wnd.tbStatus_test3.Inlines.Add(new Run("Ошибка данных") { Foreground = Brushes.Red, FontWeight = FontWeights.Bold });
                            cbAccelerationFunc.IsEnabled = false;
                            cbAccelerationFunc.SelectedIndex = 0;
                        }));
                    }
                    else
                    {
                        funcNum = data[3] & 0xFF;
                        if (funcNum >= (cbAccelerationFunc.Items.Count - 1))
                        {
                            funcNum = -1;
                        }

                        wnd.Dispatcher.BeginInvoke(DispatcherPriority.Send, new Action(() =>
                        {
                            wnd.tbStatus_test3.Inlines.Clear();
                            
                            cbAccelerationFunc.SelectedIndex = (funcNum + 1);
                            cbAccelerationFunc.IsEnabled = true;
                        }));
                    }

                    if (write_flg)
                    {
                        byte[] wrCommand = new byte[extControl ? 16 : 14];

                        int pos = 0;
                        if (extControl)
                        {
                            wrCommand[pos++] = 0x53;
                            wrCommand[pos++] = 1;
                        }
                        wrCommand[pos++] = 0xED;
                        wrCommand[pos++] = 22;
                        wrCommand[pos++] = (byte)'W';
                        wrCommand[pos++] = (byte)'R';
                        wrCommand[pos++] = (byte)'I';
                        wrCommand[pos++] = (byte)'T';
                        wrCommand[pos++] = (byte)'E';
                        wrCommand[pos++] = (byte)' ';
                        wrCommand[pos++] = (byte)'F';
                        wrCommand[pos++] = (byte)'U';
                        wrCommand[pos++] = (byte)'N';
                        wrCommand[pos++] = (byte)'C';
                        wrCommand[pos++] = (byte)newFuncNum;
                        wrCommand[pos++] = Test.CalcCheckSum(wrCommand, extControl ? 2 : (uint)0, 13);

                        stage = 2;
                        write_flg = false;
                        port.RenewWritingData(wrCommand, 500);
                    }
                }
                else// if(stage == 2)
                {
                    bool err = false;
                    if (extControl)
                    {
                        if ((count == 0) || (data == null) || (data.Length == 0))
                        {
                            err = true;
                            wnd.Dispatcher.BeginInvoke(DispatcherPriority.Send, new Action(() =>
                            {
                                wnd.tbStatus_test3.Inlines.Clear();
                                MessageBox.Show(wnd, "Не удалось изменить функцию разгона.\n\nНет ответа от СЭВМ", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
                            }));
                        }
                        else if (count != 1)
                        {
                            err = true;
                            wnd.Dispatcher.BeginInvoke(DispatcherPriority.Send, new Action(() =>
                            {
                                wnd.tbStatus_test3.Inlines.Clear();
                                MessageBox.Show(wnd, "Не удалось изменить функцию разгона.\n\nПринято " + count + " байт(а) от СЭВМ (должен быть 1)", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
                            }));
                            return;
                        }
                        else if ((data[0] & 0xF0) != 0)
                        {
                            err = true;
                            wnd.Dispatcher.BeginInvoke(DispatcherPriority.Send, new Action(() =>
                            {
                                wnd.tbStatus_test3.Inlines.Clear();
                                MessageBox.Show(wnd, "Не удалось изменить функцию разгона.\n\nКод ошибки: " + ((data[0] >> 4) & 0xF).ToString(), "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
                            }));
                            return;
                        }
                    }

                    if (!err)
                    {
                        if ((count == 0) || (data == null) || (data.Length == 0))
                        {
                            wnd.Dispatcher.BeginInvoke(DispatcherPriority.Send, new Action(() =>
                            {
                                wnd.tbStatus_test3.Inlines.Clear();
                                MessageBox.Show(wnd, "Не удалось изменить функцию разгона.\n\nНет ответа от блока", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
                            }));
                        }
                        else if (count != 14)
                        {
                            wnd.Dispatcher.BeginInvoke(DispatcherPriority.Send, new Action(() =>
                            {
                                wnd.tbStatus_test3.Inlines.Clear();
                                MessageBox.Show(wnd, "Не удалось изменить функцию разгона.\n\nОшибка обмена", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
                            }));
                        }
                        else if ((data[0] != 0xED) || (data[1] != 22) || (data[2] != (byte)'W') || (data[3] != (byte)newFuncNum) || (data[13] != Test.CalcCheckSum(data, 0, 13)))
                        {
                            wnd.Dispatcher.BeginInvoke(DispatcherPriority.Send, new Action(() =>
                            {
                                wnd.tbStatus_test3.Inlines.Clear();
                                MessageBox.Show(wnd, "Не удалось изменить функцию разгона.\n\nОшибка данных", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
                            }));
                        }
                        else
                        {
                            funcNum = newFuncNum;

                            wnd.Dispatcher.BeginInvoke(DispatcherPriority.Send, new Action(() =>
                            {
                                cbAccelerationFunc.SelectedIndex = (funcNum + 1);
                                cbAccelerationFunc.IsEnabled = true;

                                wnd.tbStatus_test3.Inlines.Clear();
                                MessageBox.Show(wnd, "Функция разгона успешно изменена", "Сообщение", MessageBoxButton.OK, MessageBoxImage.Information);
                            }));
                        }
                    }

                    stage = 1;

                    data = extControl ? data16 : data14;
                    port.RenewWritingData(data, 100);
                }
            }

            public void CyclicWriteCompleted()
            {
                wnd.Dispatcher.BeginInvoke(new Action(() =>
                {
                    running = false;
                    bnAccelerationFunc.IsChecked = false;
                    cbAccelerationFunc.IsEnabled = false;
                    funcNum = -1;
                    cbAccelerationFunc.SelectedIndex = 0;
                }));
            }
        }
    }

    public partial class MainWindow
    {
        public class Test3 : Test, ICycleWriteAndReadSubject
        {
            /// <summary>
            /// Экземпляр класса
            /// </summary>
            private static Test3 instance = null;

            /// <summary>
            /// Состояние теста (запущен/не запущен)
            /// </summary>
            private bool running;

            /// <summary>
            /// Счетчик оставшихся посылок до завершения циклической записи
            /// </summary>
            private int lastDataCounter;

            /// <summary>
            /// Последняя нажатая кнопка
            /// </summary>
            private ButtonController lastCommand;

            /// <summary>
            /// Признак режима внешнего управления обменом
            /// </summary>
            private bool extControl;

            /// <summary>
            /// Объект-программатор микроконтроллера
            /// </summary>
            private Programmer programmer;

            private byte[] data1 = new byte[14];

            /// <summary>
            /// Метод-фабрика, предназначенный для создания единственного экземпляра класса
            /// </summary>
            /// <param name="wnd">Ссылка на главное окно программы</param>
            /// <param name="portModel">Ссылка на контроллер порта</param>
            /// <param name="programmer">Программатор микроконтроллера</param>
            /// <returns>Возвращает ссылку на объект Test3</returns>
            public static Test3 GetInstance(MainWindow wnd, PortModel portModel, Programmer programmer)
            {
                if (instance == null) instance = new Test3(wnd, portModel, programmer);
                return instance;
            }

            /// <summary>
            /// Конструктор
            /// </summary>
            /// <param name="wnd">Ссылка на главное окно программы</param>
            /// <param name="portModel">Ссылка на контроллер порта</param>
            /// <param name="programmer">Программатор микроконтроллера</param>
            private Test3(MainWindow wnd, PortModel portModel, Programmer programmer) : base(wnd, portModel)
            {
                this.programmer = programmer;
            }

            /// <summary>
            /// Инициализация теста
            /// </summary>
            protected override void Init()
            {
                if (wnd == null) throw new Exception("Параметр wnd равен null");
                if (portModel == null) throw new Exception("Параметр portModel равен null");
                this.wnd = wnd;
                this.portModel = portModel;

                wnd.tbStatus_test3.Inlines.Clear();
                wnd.tbStatus_test3.Inlines.Add("Состояние: ");
                wnd.tbStatus_test3.Inlines.Add(new Run("Неизвестно") { FontWeight = FontWeights.Bold });

                // Создаем объект-контроллер для кнопок
                ButtonController bcRoll1 = new ButtonController(this, wnd.bnRoll1_test3, new byte[] { 0x57, 0, 0, 0, 0x33 });
                ButtonController bcRoll2Left = new ButtonController(this, wnd.bnRoll2Left_test3, new byte[] { 0x4C, 0, 0, 0, 0x33 });
                ButtonController bcRoll2Right = new ButtonController(this, wnd.bnRoll2Right_test3, new byte[] { 0x52, 0, 0, 0, 0x33 });
                ButtonController bcCarry = new ButtonController(this, wnd.bnCarry_test3, new byte[] { 0x5C, 0, 0, 0, 0xB3 });
                ButtonController bcStop = new ButtonController(this, wnd.bnStop_test3, new byte[] { 0x59, 0, 0, 0, 0x70 });
                ButtonController bcLock = new ButtonController(this, wnd.bnLock_test3, new byte[] { 0x74, 0, 0, 0, 0xB3 });
                ButtonController bcTest22 = new ButtonController(this, wnd.bnTest22_test3, new byte[] { 0x59, 0, 0, 0, 0x70 });
                SectorButtonController bcSetSector = new SectorButtonController(this, wnd.tbDegrees_test3, wnd.tbMinutes_test3, wnd.cbSector_test3, wnd.bnSetSector_test3, new byte[] { 0x59, 0, 0, 0, 0x70 });

                lastCommand = bcStop;
                running = false;

                wnd.SizeChanged += (sender, e) =>
                {
                    wnd.lbDisplay_test3.FontSize = (wnd.ActualWidth - 900) / 4 + 100;
                };

                // Перевод УСМА в режим программирования
                wnd.bnProgUsma_test3.Click += (sender, e) =>
                {
                    if(running)
                    {
                        StopTest();
                        Task.Factory.StartNew(() =>
                        {
                            while (running) { Thread.Sleep(10); }
                            ProgUsmaHandler.GetInstance(wnd, portModel, programmer).Run();
                        });
                    }
                    else
                    {
                        ProgUsmaHandler.GetInstance(wnd, portModel, programmer).Run();
                    }
                };

                wnd.chbExtControl_test3.Click += (sender, e) =>
                {
                    bool extControl = wnd.chbExtControl_test3.IsChecked ?? false;
                    StopTest();
                    wnd.bnL60_test3.IsEnabled = extControl;
                    wnd.bnProgUsma_test3.IsEnabled = !extControl;
                };

                // Перевод контейнера Н6М в процедуру Имит Л60
                wnd.bnL60_test3.Click += (sender, e) =>
                {
                    if (running)
                    {
                        StopTest();
                        Task tsk = Task.Factory.StartNew(() =>
                        {
                            //while (running) { Thread.Sleep(10); }
                            L60Handler.GetInstance(wnd, portModel).Run();
                        });
                    }
                    else
                    {
                        L60Handler.GetInstance(wnd, portModel).Run();
                    }
                };

                wnd.bnInfo_test3.Click += (sender, e) =>
                {
                    bool extControl = wnd.chbExtControl_test3.IsChecked ?? false;
                    if (running)
                    {
                        //StopTest();
                        Task tsk = Task.Factory.StartNew(() =>
                        {
                            //while (running) { Thread.Sleep(10); }
                            InfoHandler.GetInstance(wnd, portModel).Run(extControl);
                            Thread.Sleep(200);
                            wnd.Dispatcher.BeginInvoke(DispatcherPriority.Send, new Action(() =>
                            {
                                SendMessage(lastCommand);
                                lastCommand.Check();
                            }));
                        });
                    }
                    else
                    {
                        InfoHandler.GetInstance(wnd, portModel).Run(extControl);
                    }
                };

                wnd.bnRotationRate_test3.Click += (sender, e) =>
                {
                    bool extControl = wnd.chbExtControl_test3.IsChecked ?? false;
                    if (running)
                    {
                        //StopTest();
                        Task tsk = Task.Factory.StartNew(() =>
                        {
                            //while (running) { Thread.Sleep(10); }
                            RotationRateHandler.GetInstance(wnd, portModel, wnd.bnRotationRate_test3, wnd.tbStatus_test3).Run(extControl);
                        });
                    }
                    else
                    {
                        RotationRateHandler.GetInstance(wnd, portModel, wnd.bnRotationRate_test3, wnd.tbStatus_test3).Run(extControl);
                    }
                };

                wnd.bnAccelerationFunc_test3.Click += (sender, e) =>
                {
                    bool extControl = wnd.chbExtControl_test3.IsChecked ?? false;
                    if (running)
                    {
                        //StopTest();
                        Task tsk = Task.Factory.StartNew(() =>
                        {
                            //while (running) { Thread.Sleep(10); }
                            AccelerationFuncHandler.GetInstance(wnd, portModel, wnd.bnAccelerationFunc_test3, wnd.cbAccelerationFunc_test3, wnd.tbStatus_test3).Run(extControl);
                        });
                    }
                    else
                    {
                        AccelerationFuncHandler.GetInstance(wnd, portModel, wnd.bnAccelerationFunc_test3, wnd.cbAccelerationFunc_test3, wnd.tbStatus_test3).Run(extControl);
                    }
                };

                wnd.bnProg_test3.Click += (sender, e) =>
                {
                    if (programmer.ProgIsOpened)
                    {
                        programmer.Prog(22, wnd.chbExtControl_test3.IsChecked ?? false);
                    }
                    else
                    {
                        if (System.Windows.MessageBox.Show(wnd, "Не загружена программа для перепрограммирования.\n\nВыбрать файла с программой?", "Сообщение", MessageBoxButton.YesNo, MessageBoxImage.Question) == MessageBoxResult.Yes)
                        {
                            if (programmer.OpenProgDialog())
                            {
                                programmer.Prog(22, wnd.chbExtControl_test3.IsChecked ?? false);
                            }
                        }
                    }
                };
            }

            /// <summary>
            /// Остаовить передачу данных
            /// </summary>
            public void StopTest()
            {
                // Остановка циклической передачи данных при "отлипании" кнопки
                if (running) { lastDataCounter = 1; }
                else { portModel.StopCyclicWrite(); }
            }

            /// <summary>
            /// Передать данные на порт
            /// </summary>
            /// <param name="sender">Отправитель команды</param>
            public void SendMessage(ButtonController sender)
            {
                if (lastCommand != sender)
                {
                    lastCommand.Uncheck();
                    lastCommand = sender;
                }

                if (running)
                {
                    byte[] data = sender.GetData();
                    if(extControl)
                    {
                        byte[] extdata = new byte[16];
                        extdata[0] = 0x33;
                        extdata[1] = 1;
                        for(int i = 0; i < 5; ++i)
                        {
                            extdata[2 + i] = data[i];
                        }
                        for(int i = 7; i < 16; ++i)
                        {
                            extdata[i] = 0;
                        }
                        data = extdata;
                    }
                    portModel.RenewWritingData(data, 100);
                }
                else
                {
                    extControl = wnd.chbExtControl_test3.IsChecked ?? false;

                    // Запуск циклической передачи данных
                    try
                    {
                        byte[] data = lastCommand.GetData();
                        if (extControl)
                        {
                            byte[] extdata = new byte[16];
                            extdata[0] = 0x33;
                            extdata[1] = 1;
                            for (int i = 0; i < 5; ++i)
                            {
                                extdata[2 + i] = data[i];
                            }
                            for (int i = 7; i < 16; ++i)
                            {
                                extdata[i] = 0;
                            }
                            data = extdata;
                        }

                        portModel.RunCyclicWriteAndRead(data, 100, this);
                        running = true;
                        lastDataCounter = -1;
                    }
                    catch (Exception ex)
                    {
                        MessageBox.Show(wnd, ex.Message, "Ошибка записи данных на порт", MessageBoxButton.OK, MessageBoxImage.Error);
                        lastCommand.Uncheck();
                        return;
                    }
                }
            }

            /// <summary>
            /// Метод, вызываемый по окончанию циклической записи на порт 
            /// </summary>
            public void CyclicWriteCompleted()
            {
                wnd.Dispatcher.BeginInvoke(new Action(() =>
                {
                    lastCommand.Uncheck();
                    running = false;

                    wnd.tbStatus_test3.Inlines.Clear();
                    wnd.tbStatus_test3.Inlines.Add("Состояние: ");
                    wnd.tbStatus_test3.Inlines.Add(new Run("Неизвестно") { FontWeight = FontWeights.Bold });
                }));
            }

            /// <summary>
            /// Метод, вызываемый при получении данных при циклической операции записи-чтения
            /// </summary>
            /// <param name="progData">Массив с данными для чтения</param>
            /// <param name="count">Количество байт в массиве</param>
            public void DataReceived(byte[] data, int count)
            {
                if(extControl)
                {
                    if((count != 16) || (data == null) || (data.Length != 16))
                    {
                        count = 0;
                    }
                    else
                    {
                        count = data[1] <= 14 ? data[1] : 14;
                        for(int i = 0; i < count; ++i)
                        {
                            data1[i] = data[2 + i];
                        }
                        data = data1;
                    }
                }

                if((count == 0) || (data == null) || (data.Length == 0))
                {
                    wnd.Dispatcher.BeginInvoke(DispatcherPriority.Send, new Action(() =>
                    {
                        wnd.tbStatus_test3.Inlines.Clear();
                        wnd.tbStatus_test3.Inlines.Add("Состояние: ");
                        wnd.tbStatus_test3.Inlines.Add(new Run("Нет ответа от блока") { Foreground = Brushes.Red, FontWeight = FontWeights.Bold });
                    }));
                }
                else if (count != 5)
                {
                    wnd.Dispatcher.BeginInvoke(DispatcherPriority.Send, new Action(() =>
                    {
                        wnd.tbStatus_test3.Inlines.Clear();
                        wnd.tbStatus_test3.Inlines.Add("Состояние: ");
                        wnd.tbStatus_test3.Inlines.Add(new Run("Ошибка обмена") { Foreground = Brushes.Red, FontWeight = FontWeights.Bold });
                    }));
                }
                else// if (count == 5)
                {
                    // Получаем угол в двоичном виде
                    int binary_angle = (data[1] & 0xFF) | ((data[2] & 0xFF) << 8);

                    // Переводим значение угла в градусы
                    double digital_angle = 0;
                    double digit_weight = 180.0;
                    for (int i = 0; i < 14; ++i)
                    {
                        if ((binary_angle & (1 << (13 - i))) != 0) digital_angle += digit_weight;
                        digit_weight /= 2.0;
                    }

                    // Разделяем угол на целую часть и дробную в виде минут
                    int deg = (int)digital_angle;
                    int min = (int)((digital_angle - (int)digital_angle) * 60 + 0.5);

                    string state;
                    switch(data[0] & 0x3F)
                    {
                        case 0x01: state = "Походное положение";
                            break;
                        case 0x36: state = "Вращ 1";
                            break;
                        case 0x0C: state = "Вращ 2 влево";
                            break;
                        case 0x12: state = "Вращ 2 вправо";
                            break;
                        case 0x30: state = "Разгон";
                            break;
                        case 0x27: state = "Остановка вращения";
                            break;
                        case 0x20: state = "Вращение остановлено";
                            break;
                        default: state = String.Format("Неизв. состояние {0:X2}", data[0] & 0x3F);
                            break;
                    }

                    wnd.Dispatcher.BeginInvoke(DispatcherPriority.Send, new Action(() =>
                    {
                        wnd.lbDisplay_test3.Content = deg.ToString().PadLeft(3, ' ') + "°" + min.ToString().PadLeft(2, '0') + "'";


                        wnd.tbStatus_test3.Inlines.Clear();
                        wnd.tbStatus_test3.Inlines.Add("Состояние: ");
                        wnd.tbStatus_test3.Inlines.Add(new Run(state) { FontWeight = FontWeights.Bold });
                    }));
                }

                if (lastDataCounter >= 0)
                {
                    if (lastDataCounter > 0)
                    {
                        byte[] sendingdata = extControl ? new byte[] { 0x33, 1, 0x59, 0, 0, 0, 0x70, 0, 0, 0, 0, 0, 0, 0, 0, 0 } : new byte[] { 0x59, 0, 0, 0, 0x70 };

                        portModel.RenewWritingData(sendingdata, 100);

                        --lastDataCounter;
                    }
                    else
                    {
                        portModel.StopCyclicWrite();

                        lastDataCounter = -1;
                    }
                }
            }
        }
    }
}
