using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls.Primitives;
using System.Windows.Threading;
using Test20311M.Test4;

namespace Test20311M
{
    namespace Test4
    {
        /// <summary>
        /// Класс, выполняющий обработку ответного сообщения при переводе УСМА в режим программирования
        /// </summary>
        public class ProgUsmaHandler : IReadDisplay
        {
            private static ProgUsmaHandler instance = null;
            private MainWindow wnd;
            private Programmer programmer;

            public static ProgUsmaHandler GetInstance(MainWindow wnd, Programmer programmer)
            {
                if (instance == null) { instance = new ProgUsmaHandler(wnd, programmer); }
                else
                {
                    instance.wnd = wnd;
                    instance.programmer = programmer;
                }
                return instance;
            }

            private ProgUsmaHandler(MainWindow wnd, Programmer programmer)
            {
                this.wnd = wnd;
                this.programmer = programmer;
            }

            public void DataReceived(byte[] data, int count)
            {
                wnd.Dispatcher.BeginInvoke(DispatcherPriority.Send, new Action(() =>
                {
                    if ((count == 2) && (data[0] == (byte)'O') && (data[1] == (byte)'K'))
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

                    data[32] = 0x34;
                    data[33] = 0;
                    data[34] = 0x00;
                    data[35] = 0xFF;
                    data[36] = 0x00;
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
                    data[47] = 0;

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

                    data[32] = 0x34;
                    data[33] = 0;
                    data[34] = 0x00;
                    data[35] = 0xFF;
                    data[36] = 0x00;
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
                    data[47] = 0;

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
                    if (stage == 0)
                    {
                        bool err = false;
                        if ((count == 0) || (data == null) || (data.Length == 0))
                        {
                            MessageBox.Show(wnd, "Нет ответа от СЭВМ", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
                            err = true;
                        }
                        else if (count != 3)
                        {
                            MessageBox.Show(wnd, "Принято " + count + " байт(а) (должно быть 3)", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
                            err = true;
                        }
                        else if (((data[0] & 0xF0) != 0) || ((data[1] & 0xF0) != 0) || ((data[2] & 0xF0) != 0))
                        {
                            MessageBox.Show(wnd, "Коды ошибок: " + ((data[0] >> 4) & 0xF).ToString() + ", " + ((data[1] >> 4) & 0xF).ToString() + ", " + ((data[2] >> 4) & 0xF).ToString(), "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
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
    }

    public partial class MainWindow
    {
        class Test4 : Test, ICycleWriteAndReadSubject
        {
            /// <summary>
            /// Экземпляр класса
            /// </summary>
            private static Test4 instance = null;

            private Programmer programmer;

            private LinkedList<ToggleButton> buttonList;

            private bool running, extControl;
            private ToggleButton activeButton;
            private const int period = 100;

            /// <summary>
            /// Метод-фабрика, предназначенный для создания единственного экземпляра класса
            /// </summary>
            /// <param name="wnd">Ссылка на главное окно программы</param>
            /// <param name="portModel">Ссылка на контроллер порта</param>
            /// <param name="programmer">Программатор 1986</param>
            /// <returns>Возвращает ссылку на объект Test3</returns>
            public static Test4 GetInstance(MainWindow wnd, PortModel portModel, Programmer programmer)
            {
                if (instance == null) instance = new Test4(wnd, portModel, programmer);
                return instance;
            }

            /// <summary>
            /// Конструктор
            /// </summary>
            /// <param name="wnd">Ссылка на главное окно программы</param>
            /// <param name="portModel">Ссылка на контроллер порта</param>
            /// <param name="programmer">Программатор 1986</param>
            private Test4(MainWindow wnd, PortModel portModel, Programmer programmer) : base(wnd, portModel)
            {
                this.programmer = programmer;
            }

            protected override void Init()
            {
                running = false;
                extControl = false;
                activeButton = null;

                buttonList = new LinkedList<ToggleButton>();
                buttonList.Clear();
                buttonList.AddLast(wnd.bnTestUsma_test4);
                buttonList.AddLast(wnd.bnCant90_test4);
                buttonList.AddLast(wnd.bnCant15_test4);
                buttonList.AddLast(wnd.bnCant30_test4);
                buttonList.AddLast(wnd.bnLock_test4);
                buttonList.AddLast(wnd.bnUnLock_test4);
                buttonList.AddLast(wnd.bnUnDeploy_test4);

                RoutedEventHandler buttonHandler = (sender, e) =>
                {
                    if (!running || (sender != activeButton))
                    {
                        foreach (ToggleButton button in buttonList)
                            if (button != sender) button.IsChecked = false;

                        SendMessage((ToggleButton)sender);
                    }
                    else
                    {
                        portModel.StopCyclicWrite();
                    }
                };

                foreach (ToggleButton button in buttonList)
                    button.Click += buttonHandler;

                // Перевод УСМА в режим программирования
                wnd.bnProgUsma_test4.Click += (sender, e) =>
                {
                    Action sendProgCommandAction = () =>
                    {
                        byte[] data = new byte[] { (byte)'P', (byte)'R', 0 };
                        try
                        {
                            portModel.WriteAndRead(data, 100, ProgUsmaHandler.GetInstance(wnd, programmer));
                        }
                        catch(Exception ex)
                        {
                            System.Windows.MessageBox.Show(wnd, ex.Message, "Ошибка записи данных на порт", MessageBoxButton.OK, MessageBoxImage.Error);
                        }
                    };

                    if (running)
                    {
                        portModel.StopCyclicWrite();
                        Task.Factory.StartNew(() =>
                        {
                            while (running) { Thread.Sleep(10); }
                            sendProgCommandAction();
                        });
                    }
                    else { sendProgCommandAction(); }
                };

                // Перевод контейнера Н6М в процедуру Имит Л60
                wnd.bnL60_test4.Click += (sender, e) =>
                {
                    if (running)
                    {
                        portModel.StopCyclicWrite();
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

                //wnd.chbDebugMode_test4.Click += (sender, e) =>
                //{
                //    debugMode = wnd.chbDebugMode_test4.IsChecked ?? false;
                //    if (running) { SendMessage((ToggleButton)activeButton); }
                //};

                wnd.chbExtControl_test4.Click += (sender, e) =>
                {
                    extControl = wnd.chbExtControl_test4.IsChecked ?? false;
                    wnd.bnProgUsma_test4.IsEnabled = !extControl;
                    wnd.bnL60_test4.IsEnabled = extControl;
                    if (running) { SendMessage(activeButton); }
                };
            }

            public void SendMessage(ToggleButton sender)
            {
                byte[] data = null;

                if (sender == wnd.bnCant90_test4) { data = extControl ? new byte[] { 0x34, 1, 0x30, 0xCF, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0 } : new byte[] { 0x30, 0xCF, 0 }; }
                else if (sender == wnd.bnCant15_test4) { data = extControl ? new byte[] { 0x34, 1, 0x44, 0xBB, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0 } : new byte[] { 0x44, 0xBB, 0 }; }
                else if (sender == wnd.bnCant30_test4) { data = extControl ? new byte[] { 0x34, 1, 0x55, 0xAA, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0 } : new byte[] { 0x55, 0xAA, 0 }; }
                else if (sender == wnd.bnLock_test4) { data = extControl ? new byte[] { 0x34, 1, 0x11, 0xEE, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0 } : new byte[] { 0x11, 0xEE, 0 }; }
                else if (sender == wnd.bnUnLock_test4) { data = extControl ? new byte[] { 0x34, 1, 0x22, 0xDD, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0 } : new byte[] { 0x22, 0xDD, 0 }; }
                else if (sender == wnd.bnUnDeploy_test4) { data = extControl ? new byte[] { 0x34, 1, 0x48, 0xB7, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0 } : new byte[] { 0x48, 0xB7, 0 }; }
                else if (sender == wnd.bnTestUsma_test4) { data = extControl ? new byte[] { 0x34, 1, 0, 0xFF, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0 } : new byte[] { 0, 0xFF, 0 }; }
                else return;

                activeButton = (ToggleButton)sender;

                if (!running)
                {
                    try
                    {
                        portModel.RunCyclicWriteAndRead(data, period, this);
                        running = true;
                    }
                    catch (Exception ex)
                    {
                        activeButton.IsChecked = false;
                        MessageBox.Show(wnd, ex.Message, "Ошибка записи данных на порт", MessageBoxButton.OK, MessageBoxImage.Error);
                        return;
                    }
                }
                else portModel.RenewWritingData(data, period);
            }

            public void DataReceived(byte[] data, int count)
            {
                wnd.Dispatcher.BeginInvoke(DispatcherPriority.Send, new Action(() =>
                {
                    if(extControl)
                    {
                        if (count == 0)
                        {
                            wnd.lbDebug_test4.Content = "Нет ответа от СЭВМ";
                            return;
                        }
                        else if((count != 16) || (data[0] != 4))
                        {
                            wnd.lbDebug_test4.Content = "Неверный ответа от СЭВМ";
                            return;
                        }
                        else
                        {
                            count = data[1];
                            for(int i = 0; (i < count) && (i < 14); ++i)
                            {
                                data[i] = data[i + 2];
                            }
                        }
                    }

                    if (count == 0)
                    {
                        wnd.lbDebug_test4.Content = "Нет ответа";
                        return;
                    }

                    if (!extControl)
                    {
                        if(count != 10)
                        {
                            wnd.lbDebug_test4.Content = "Неверный размер посылки";
                            return;
                        }
                    }
                    else
                    {
                        if(count != 6)
                        {
                            wnd.lbDebug_test4.Content = "Неверный размер посылки";
                            return;
                        }
                    }

                    string s = "  Сбой\tСбой\tН6 \t Н6 \tПоход\tНаклон\tНаклон\tНаклон\n"
                    + "  пак.\tинф.\tсверн\tразв\t Н6  \t  30  \t  15  \t  0\n"
                    + (((data[0] & 0x80) != 0) ? "    1\t" : "    0\t")
                    + (((data[0] & 0x40) != 0) ? "    1\t" : "    0\t")
                    + (((data[0] & 0x20) != 0) ? "    1\t" : "    0\t")
                    + (((data[0] & 0x10) != 0) ? "    1\t" : "    0\t")
                    + (((data[0] & 0x08) != 0) ? "    1\t" : "    0\t")
                    + (((data[0] & 0x04) != 0) ? "    1\t" : "    0\t")
                    + (((data[0] & 0x02) != 0) ? "    1\t" : "    0\t")
                    + (((data[0] & 0x01) != 0) ? "    1\n" : "    0\n")
                    + "  НР\t  МЕСТ\t Разреш\t Запрещ\n"
                    + "  12\t режим\t  вращ \t  вращ\n"
                    + (((data[1] & 0x08) != 0) ? "    1\t" : "    0\t")
                    + (((data[1] & 0x04) != 0) ? "    1\t" : "    0\t")
                    + (((data[1] & 0x02) != 0) ? "    1\t" : "    0\t")
                    + (((data[1] & 0x01) != 0) ? "    1\n" : "    0\n")
                    + "Разв\t Разв\t Разв\t Разв \t Рассто-\n"
                    + " НЦ \t  ВЦ \t закр\t 06.68\t порено\n"
                    + (((data[2] & 0x80) != 0) ? "    1\t" : "    0\t")
                    + (((data[2] & 0x40) != 0) ? "    1\t" : "    0\t")
                    + (((data[2] & 0x20) != 0) ? "    1\t" : "    0\t")
                    + (((data[2] & 0x10) != 0) ? "    1\t" : "    0\t")
                    + (((data[2] & 0x04) != 0) ? "    1\n" : "    0\n")
                    + "Сверн\t Сверн\t Сверн\t Сверн\t Засто-\n"
                    + " НЦ  \t  ВЦ  \t закр \t 06.68\t порено\n"
                    + (((data[3] & 0x80) != 0) ? "    1\t" : "    0\t")
                    + (((data[3] & 0x40) != 0) ? "    1\t" : "    0\t")
                    + (((data[3] & 0x20) != 0) ? "    1\t" : "    0\t")
                    + (((data[3] & 0x10) != 0) ? "    1\t" : "    0\t")
                    + (((data[3] & 0x04) != 0) ? "    1\n" : "    0\n")
                    + "Отказ\t  РУ-\t  Пере-\n"
                    + "бака \t РУЧН\t грузка\n"
                    + (((data[4] & 0x40) != 0) ? "    1\t" : "    0\t")
                    + (((data[4] & 0x20) != 0) ? "    1\t" : "    0\t")
                    + (((data[4] & 0x10) != 0) ? "    1\n" : "    0\n"
                    + "Тест 12\t\t= " + Convert.ToString(data[1] >> 4, 2).PadRight(4, '0')
                    + "\nКвит. режима 12\t= " + Convert.ToString(data[5], 2).PadRight(8, '0'));

                    wnd.lbDataDisplay_test4.Content = s;

                    if (!extControl)
                    {
                        string str = "Данные портов:\n"
                        + "\n[9]    | " + ((data[5] >> 0) & 1).ToString() + " |  1-Наклон не 0, 0-Наклон 0 "
                        + "\n[10]  | " + ((data[5] >> 1) & 1).ToString() + " |  Наклонено 15 (0-да, 1-нет)"
                        + "\n[11]  | " + ((data[5] >> 2) & 1).ToString() + " |  Наклонено 30 (0-да, 1-нет)"
                        + "\n[12]  | " + ((data[5] >> 3) & 1).ToString() + " |  Отказ бака (0-отказ, 1-НР)"
                        + "\n[13]  | " + ((data[5] >> 4) & 1).ToString() + " |  Походное Н6 можно Сверт (1-да, 0-нет)"
                        + "\n[14]  | " + ((data[5] >> 5) & 1).ToString() + " |  РУ-РУЧН (0-отказ, 1-НР)"
                        + "\n[15]  | " + ((data[5] >> 6) & 1).ToString() + " |  Местный режим (0-да, 1-нет)"
                        + "\n[16]  | " + ((data[5] >> 7) & 1).ToString() + " |  Развернуть 01 (0-Развертывание 1-Не развертывание)\n"

                        + "\n[18]  | " + ((data[6] >> 0) & 1).ToString() + " |  Развернут Н6.06.68 (0-да, 1-нет)"
                        + "\n[19]  | " + ((data[6] >> 1) & 1).ToString() + " |  Развернуты Закрылки (1-да, 0-нет)"
                        + "\n[20]  | " + ((data[6] >> 2) & 1).ToString() + " |  Свернут Н6.06.68 (0-да, 1-нет)"
                        + "\n[21]  | " + ((data[6] >> 3) & 1).ToString() + " |  Свернуты Закрылки (1-да, 0-нет)"
                        + "\n[22]  | " + ((data[6] >> 4) & 1).ToString() + " |  Свернут Походное (1-да, 0-нет)"
                        + "\n[23]  | " + ((data[6] >> 5) & 1).ToString() + " |  Расстопорено (0-да, 1-нет)"
                        + "\n[24]  | " + ((data[6] >> 6) & 1).ToString() + " |  Застопорено (0-да, 1-нет)"
                        + "\n[25]  | " + ((data[6] >> 7) & 1).ToString() + " |  Развернуты НЦ (0-да, 1-нет)\n"

                        + "\n[63]  | " + ((data[7] >> 0) & 1).ToString() + " |  Развернуты ВЦ (1-да, 0-нет)"
                        + "\n[64]  | " + ((data[7] >> 1) & 1).ToString() + " |  Мест Наклонить 15 (выполняем когда 0)"
                        + "\n[65]  | " + ((data[7] >> 2) & 1).ToString() + " |  Мест Наклонить 30 (выполняем когда 0)"
                        + "\n[66]  | " + ((data[7] >> 3) & 1).ToString() + " |  Мест Свернуть (выполняем когда 0)"
                        + "\n[67]  | " + ((data[7] >> 4) & 1).ToString() + " |  Мест Развернуть (выполняем когда 0)"
                        + "\n[68]  | " + ((data[7] >> 5) & 1).ToString() + " |  Мест Расстопорить (выполняем когда 0)"
                        + "\n[69]  | " + ((data[7] >> 6) & 1).ToString() + " |  Мест Застопорить (выполняем когда 0)"
                        + "\n[70]  | " + ((data[7] >> 7) & 1).ToString() + " |  Перегрузка (0-отказ, 1-НР)\n"

                        + "\n[27]  | " + ((data[8] >> 0) & 1).ToString() + " |  Свернуть НЦ (0-да, 1-нет)"
                        + "\n[28]  | " + ((data[8] >> 1) & 1).ToString() + " |  Свернуть ВЦ (0-да, 1-нет)"
                        + "\n[29]  | " + ((data[8] >> 2) & 1).ToString() + " |  Развернуть Закрылки (0-да, 1-нет)"
                        + "\n[30]  | " + ((data[8] >> 3) & 1).ToString() + " |  Развернуть Н6.06.68 (0-да, 1-нет)"
                        + "\n[31]  | " + ((data[8] >> 4) & 1).ToString() + " |  РазвернутьРасстопорить (0-раз, 1-све)"
                        + "\n[32]  | " + ((data[8] >> 5) & 1).ToString() + " |  НР 12 (0-Отказ, 1-НР)"
                        + "\n[33]  | " + ((data[8] >> 6) & 1).ToString() + " |  Свернуть Закрылки (0-да, 1-нет)"
                        + "\n[34]  | " + ((data[8] >> 7) & 1).ToString() + " |  Свернуть Н6.06.68 (0-да, 1-нет)\n"

                        + "\n[45]  | " + ((data[9] >> 0) & 1).ToString() + " |  Развернуть НЦ (0-да, 1-нет)"
                        + "\n[46]  | " + ((data[9] >> 1) & 1).ToString() + " |  Развернуть ВЦ (0-да, 1-нет)"
                        + "\n[47]  | " + ((data[9] >> 2) & 1).ToString() + " |  01 - Развернуто (1 если Н606 раз)"
                        + "\n[48]  | " + ((data[9] >> 3) & 1).ToString() + " |  Наклонить 15 (1-да, 0-нет)"
                        + "\n[49]  | " + ((data[9] >> 4) & 1).ToString() + " |  Наклонить 30 (1-да, 0-нет)"
                        + "\n[50]  | " + ((data[9] >> 5) & 1).ToString() + " |  Вертикально (1 если Н606 раз)"
                        + "\n[51]  | " + ((data[9] >> 6) & 1).ToString() + " |  Расстопорить (0-да, 1-нет)"
                        + "\n[52]  | " + ((data[9] >> 7) & 1).ToString() + " |  Застопорить (0-да, 1-нет)";

                        wnd.lbDebug_test4.Content = str;
                    }
                    else
                    {
                        wnd.lbDebug_test4.Content = String.Format("Команда режим 12 = {0:X2}", data[5]);
                    }
                }));
            }

            public void CyclicWriteCompleted()
            {
                wnd.Dispatcher.BeginInvoke(new Action(() =>
                {
                    running = false;
                    foreach (ToggleButton button in buttonList)
                        button.IsChecked = false;
                }));
            }
        }
    }
}
