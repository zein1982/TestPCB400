using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Controls.Primitives;
using System.Windows.Documents;
using System.Windows.Media;
using System.Windows.Threading;
using Test20311M.Test5;

namespace Test20311M
{
    namespace Test5
    {
        enum Command { cmGetData, cmSetZero, cmProgUsma, cmSetModbus, cmGetModbus }
    }

    public partial class MainWindow
    {
        class Test5 : Test, ICycleWriteAndReadSubject
        {
            /// <summary>
            /// Экземпляр класса
            /// </summary>
            private static Test5 instance = null;

            private bool running, error;

            private Command command, next_command;

            private byte[] message, getdata_msg, setzero_msg, progusma_msg, setmodbus_msg, getmodbus_msg;

            private const int period = 100;

            /// <summary>
            /// Метод-фабрика, предназначенный для создания единственного экземпляра класса
            /// </summary>
            /// <param name="wnd">Ссылка на главное окно программы</param>
            /// <param name="portModel">Ссылка на контроллер порта</param>
            /// <returns>Возвращает ссылку на объект Test3</returns>
            public static Test5 GetInstance(MainWindow wnd, PortModel portModel)
            {
                if (instance == null) instance = new Test5(wnd, portModel);
                return instance;
            }

            /// <summary>
            /// Конструктор
            /// </summary>
            /// <param name="wnd">Ссылка на главное окно программы</param>
            /// <param name="portModel">Ссылка на контроллер порта</param>
            private Test5(MainWindow wnd, PortModel portModel) : base(wnd, portModel) { }

            /// <summary>
            /// Инициализаия теста
            /// </summary>
            protected override void Init()
            {
                running = false;
                error = false;
                command = Command.cmGetData;
                next_command = Command.cmGetData;

                getdata_msg = new byte[] { 0xE2, (byte)'R', (byte)'D' };
                setzero_msg = new byte[] { 0xE3, (byte)'S', (byte)'Z' };
                progusma_msg = new byte[] { 0xE1, (byte)'P', (byte)'R' };
                setmodbus_msg = new byte[] { 0xE4, (byte)'S', 0 };
                getmodbus_msg = new byte[] { 0xE5, (byte)'R', (byte)'M' };

                // Кнопка запроса углов запускает поток непрерывного получения данных с блока
                wnd.bnGetData_test5.Click += (sender, e) =>
                {
                    if (!running)
                    {
                        command = Command.cmGetData;
                        next_command = Command.cmGetData;

                        message = getdata_msg;

                        wnd.tbStatus_test5.Inlines.Clear();

                        if (!running)
                        {
                            try
                            {
                                portModel.RunCyclicWriteAndRead(message, period, this);
                                running = true;
                            }
                            catch (Exception ex)
                            {
                                wnd.bnGetData_test5.IsChecked = false;
                                MessageBox.Show(wnd, ex.Message, "Ошибка записи данных на порт", MessageBoxButton.OK, MessageBoxImage.Error);
                                return;
                            }
                        }
                        else portModel.RenewWritingData(message, period);
                    }
                    else
                    {
                        portModel.StopCyclicWrite();
                    }
                };
                wnd.bnSetZero_test5.Click += (sender, e) =>
                {
                    if (running) { next_command = Command.cmSetZero; }
                    else
                    {
                        command = Command.cmSetZero;
                        message = setzero_msg;
                        try
                        {
                            portModel.WriteAndRead(message, period, this);
                        }
                        catch(Exception ex)
                        {
                            System.Windows.MessageBox.Show(wnd, ex.Message, "Ошибка записи данных на порт", MessageBoxButton.OK, MessageBoxImage.Error);
                        }
                    }
                };
                wnd.bnProgUsma_test5.Click += (sender, e) =>
                {
                    if (running) { next_command = Command.cmProgUsma; }
                    else
                    {
                        command = Command.cmProgUsma;
                        message = progusma_msg;
                        try
                        {
                            portModel.WriteAndRead(message, period, this);
                        }
                        catch(Exception ex)
                        {
                            System.Windows.MessageBox.Show(wnd, ex.Message, "Ошибка записи данных на порт", MessageBoxButton.OK, MessageBoxImage.Error);
                        }
                    }
                };
                wnd.bnSetModbus_test5.Click += (sender, e) =>
                {
                    if (running) { next_command = Command.cmSetModbus; }
                    else
                    {
                        command = Command.cmSetModbus;
                        setmodbus_msg[2] = (byte)(wnd.udModbusNum_test5.Value ?? 0);
                        message = setmodbus_msg;
                        try
                        {
                            portModel.WriteAndRead(message, 500, this);
                        }
                        catch(Exception ex)
                        {
                            System.Windows.MessageBox.Show(wnd, ex.Message, "Ошибка записи данных на порт", MessageBoxButton.OK, MessageBoxImage.Error);
                        }
                    }
                };
                wnd.bnGetModbus_test5.Click += (sender, e) =>
                {
                    if (running) { next_command = Command.cmGetModbus; }
                    else
                    {
                        command = Command.cmGetModbus;
                        message = getmodbus_msg;
                        try
                        {
                            portModel.WriteAndRead(message, period, this);
                        }
                        catch(Exception ex)
                        {
                            System.Windows.MessageBox.Show(wnd, ex.Message, "Ошибка записи данных на порт", MessageBoxButton.OK, MessageBoxImage.Error);
                        }
                    }
                };
            }

            /// <summary>
            /// Обработчик приема данных с блока
            /// </summary>
            /// <param name="progData">Массив данных</param>
            /// <param name="count">Размер массива</param>
            public void DataReceived(byte[] data, int count)
            {
                if(count == 0)
                {
                    wnd.Dispatcher.BeginInvoke(DispatcherPriority.Send, new Action(() =>
                    {
                        wnd.tbStatus_test5.Inlines.Clear();
                        wnd.tbStatus_test5.Inlines.Add(new Run("Нет ответа") { Foreground = Brushes.Red, FontWeight = FontWeights.Bold });
                        error = true;
                    }));
                }
                else if (((command == Command.cmGetData) && (count != 5)) || ((command != Command.cmGetData) && (count != 3)) || (message[0] != data[0]))
                {
                    wnd.Dispatcher.BeginInvoke(DispatcherPriority.Send, new Action(() =>
                    {
                        wnd.tbStatus_test5.Inlines.Clear();
                        wnd.tbStatus_test5.Inlines.Add(new Run("Ошибка пакета") { Foreground = Brushes.Red, FontWeight = FontWeights.Bold });
                        error = true;
                    }));
                }
                else
                {
                    switch(command)
                    {
                        case Command.cmGetData:
                            wnd.Dispatcher.BeginInvoke(DispatcherPriority.Send, new Action(() =>
                            {
                                short x = (short)((data[1] << 8) | data[2]);
                                short y = (short)((data[3] << 8) | data[4]);
                                bool minusx = false, minusy = false;
                                if (data[1] > 90)
                                {
                                    x = (short)(~x + 1);
                                    minusx = true;
                                }
                                if (data[3] > 90)
                                {
                                    y = (short)(~y + 1);
                                    minusy = true;
                                }

                                if (error)
                                {
                                    wnd.tbStatus_test5.Inlines.Clear();
                                    error = false;
                                }
                                wnd.lbPsiDisplay_test5.Content = "ψ = " + ((minusx ? '-' : ' ') + (x / 256).ToString()).PadLeft(3, ' ') + '°' + (((x & 0xFF) * 60) / 256).ToString().PadLeft(2, '0') + '\'';
                                wnd.lbGammaDisplay_test5.Content = "γ = " + ((minusy ? '-' : ' ') + (y / 256).ToString()).PadLeft(3, ' ') + '°' + (((y & 0xFF) * 60) / 256).ToString().PadLeft(2, '0') + '\'';
                            }));
                            break;
                        case Command.cmSetZero:
                            wnd.Dispatcher.BeginInvoke(DispatcherPriority.Send, new Action(() =>
                            {
                                if((data[1] == 'O') && (data[2] == 'K'))
                                {
                                    wnd.tbStatus_test5.Inlines.Clear();
                                    wnd.tbStatus_test5.Inlines.Add(new Run("Ноль установлен") { Foreground = Brushes.Black, FontWeight = FontWeights.Bold });
                                    error = false;
                                }
                                else
                                {
                                    wnd.tbStatus_test5.Inlines.Clear();
                                    wnd.tbStatus_test5.Inlines.Add(new Run("Ошибка пакета") { Foreground = Brushes.Red, FontWeight = FontWeights.Bold });
                                    error = true;
                                }
                            }));
                            break;
                        case Command.cmProgUsma:
                                if ((data[1] == 'O') && (data[2] == 'K'))
                                {
                                    if(running) { portModel.StopCyclicWrite(); }
                                    wnd.Dispatcher.BeginInvoke(DispatcherPriority.Send, new Action(() =>
                                    {
                                        wnd.tbStatus_test5.Inlines.Clear();
                                        wnd.tbStatus_test5.Inlines.Add(new Run("УСМА в режиме программирования") { Foreground = Brushes.Black, FontWeight = FontWeights.Bold });
                                        error = false;
                                    }));
                                }
                                else
                                {
                                    wnd.Dispatcher.BeginInvoke(DispatcherPriority.Send, new Action(() =>
                                    {
                                        wnd.tbStatus_test5.Inlines.Clear();
                                        wnd.tbStatus_test5.Inlines.Add(new Run("Ошибка пакета") { Foreground = Brushes.Red, FontWeight = FontWeights.Bold });
                                        error = true;
                                    }));
                                }
                            break;
                        case Command.cmSetModbus:
                            wnd.Dispatcher.BeginInvoke(DispatcherPriority.Send, new Action(() =>
                            {
                                if ((data[1] == 'O') && (data[2] == 'K'))
                                {
                                    wnd.tbStatus_test5.Inlines.Clear();
                                    wnd.tbStatus_test5.Inlines.Add(new Run("Номер установлен") { Foreground = Brushes.Black, FontWeight = FontWeights.Bold });
                                    error = false;
                                }
                                else
                                {
                                    wnd.tbStatus_test5.Inlines.Clear();
                                    wnd.tbStatus_test5.Inlines.Add(new Run("Ошибка пакета") { Foreground = Brushes.Red, FontWeight = FontWeights.Bold });
                                    error = true;
                                }
                            }));
                            break;
                        case Command.cmGetModbus:
                            wnd.Dispatcher.BeginInvoke(DispatcherPriority.Send, new Action(() =>
                            {
                                if (data[2] == 'K')
                                {
                                    wnd.tbStatus_test5.Inlines.Clear();
                                    wnd.tbStatus_test5.Inlines.Add(new Run("Текущий номер MODBUS: " + data[1].ToString()) { Foreground = Brushes.Black, FontWeight = FontWeights.Bold });
                                    error = false;
                                }
                                else
                                {
                                    wnd.tbStatus_test5.Inlines.Clear();
                                    wnd.tbStatus_test5.Inlines.Add(new Run("Ошибка пакета") { Foreground = Brushes.Red, FontWeight = FontWeights.Bold });
                                    error = true;
                                }
                            }));
                            break;
                        default:
                            throw new Exception("Неизвестное значение command");
                    }// switch(command)
                }

                // Меняем сообщение, отправляемое на блок, если поступила новая команда
                if(running && (command != next_command))
                {
                    switch(next_command)
                    {
                        case Command.cmGetData:
                            message = getdata_msg;
                            break;
                        case Command.cmSetZero:
                            message = setzero_msg;
                            break;
                        case Command.cmProgUsma:
                            message = progusma_msg;
                            break;
                        case Command.cmSetModbus:
                            wnd.Dispatcher.Invoke(new Action(() =>
                            {
                                setmodbus_msg[2] = (byte)(wnd.udModbusNum_test5.Value ?? 0);
                            }));
                            message = setmodbus_msg;
                            break;
                        case Command.cmGetModbus:
                            message = getmodbus_msg;
                            break;
                        default:
                            throw new Exception("Неизвестное значение next_command");
                    }
                    portModel.RenewWritingData(message, next_command == Command.cmSetModbus ? 500 : period);
                    command = next_command;
                    next_command = Command.cmGetData;
                }
            }

            /// <summary>
            /// Метод, вызываемый после остановки циклического обмена данными с блоком
            /// </summary>
            public void CyclicWriteCompleted()
            {
                wnd.Dispatcher.BeginInvoke(new Action(() =>
                {
                    running = false;
                    wnd.bnGetData_test5.IsChecked = false;
                }));
            }
        }
    }
}
