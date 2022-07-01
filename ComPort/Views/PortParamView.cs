using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows;
using ComPort.Properties;

namespace ComPort
{
    public partial class MainWindow
    {
        /// <summary>
        /// Представление настроек портов на главном окне программы
        /// </summary>
        public class PortParamView : IPortListener, IWMListener
        {
            /// <summary>
            /// Ссылка на единственный экземпляр класса
            /// </summary>
            private static PortParamView instance = null;

            /// <summary>
            /// Модель портов
            /// </summary>
            private PortModel portModel;

            /// <summary>
            /// Главное окно программы
            /// </summary>
            private MainWindow wnd;

            /// <summary>
            /// Список портов в системе
            /// </summary>
            private List<string> portList;

            /// <summary>
            /// Метод-фабрика предназначенный для создания единственного экземпляра класса PortParamView
            /// </summary>
            /// <param name="wnd">Ссылка на главное окно программы</param>
            /// <param name="portModel">Ссылка на объект - модель порта</param>
            /// <returns>Возвращает ссылку на объект PortParamView</returns>
            public static PortParamView GetInstance(MainWindow wnd, PortModel portModel)
            {
                if (instance == null) instance = new PortParamView(wnd, portModel);
                return instance;
            }

            /// <summary>
            /// Конструктор
            /// </summary>
            /// <param name="wnd">Ссылка на главное окно программы</param>
            /// <param name="portModel">Ссылка на объект - модель порта</param>
            private PortParamView(MainWindow wnd, PortModel portModel)
            {
                if (wnd == null) throw new Exception("Параметр wnd равен null");
                if (portModel == null) throw new Exception("Параметр portModel равен null");

                this.wnd = wnd;
                this.portModel = portModel;

                wnd.lbPort2.Visibility = Visibility.Hidden;
                wnd.cbPort2.Visibility = Visibility.Hidden;

                portList = Win32ComPort.GetPortNames();
                if ((portList == null) || (portList.Count == 0))
                {
                    MessageBox.Show(wnd, "В системе не обнаружено ни одного последовательного порта", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
                    Environment.Exit(0);
                }
                //wnd.portList.Sort(PortListComparer.instance);
                wnd.cbPort1.Items.Clear();
                foreach (string s in portList)
                {
                    wnd.cbPort1.Items.Add(s);
                    wnd.cbPort2.Items.Add(s);
                }
                wnd.cbPort1.SelectedIndex = 0;
                wnd.cbPort2.SelectedIndex = 0;

                wnd.cbSpeed.Items.Clear();
                foreach (string s in new string[] { "110", "300", "600", "1200", "2400", "4800", "9600", "14400", "19200", "38400", "56000", "57600", "115200", "128000", "256000", "460800" })
                    wnd.cbSpeed.Items.Add(s);
                wnd.cbSpeed.SelectedIndex = wnd.cbSpeed.Items.Count - 1 - 3;

                wnd.cbDataBits.Items.Clear();
                foreach (string s in new string[] { "5", "6", "7", "8" })
                    wnd.cbDataBits.Items.Add(s);
                wnd.cbDataBits.SelectedIndex = wnd.cbDataBits.Items.Count - 1;

                wnd.cbParity.Items.Clear();
                foreach (string s in new string[] { "Нет", "Нечет", "Чет", "Маркер", "Пробел" })
                    wnd.cbParity.Items.Add(s);
                wnd.cbParity.SelectedIndex = 0;

                wnd.cbStopBits.Items.Clear();
                foreach (string s in new string[] { "1", "1,5", "2" })
                    wnd.cbStopBits.Items.Add(s);
                wnd.cbStopBits.SelectedIndex = 0;

                wnd.cbFlowControl.Items.Clear();
                foreach (string s in new string[] { "Нет", "XON/XOFF", "Аппаратное" })
                    wnd.cbFlowControl.Items.Add(s);
                wnd.cbFlowControl.SelectedIndex = 0;

                wnd.chbTwoPorts.Click += (sender, e) => RefreshPortsCount();

                // Загружаем настройки прграммы
                if ((Settings.Default.port1 != null) && (portList.Contains(Settings.Default.port1)))
                    wnd.cbPort1.SelectedIndex = portList.IndexOf(Settings.Default.port1);
                if ((Settings.Default.port2 != null) && (portList.Contains(Settings.Default.port2)))
                    wnd.cbPort2.SelectedIndex = portList.IndexOf(Settings.Default.port2);
                try
                {
                    if (Settings.Default.two_ports)
                    {
                        wnd.chbTwoPorts.IsChecked = true;
                        RefreshPortsCount();
                    }
                }
                catch (Exception) { }
                try
                {
                    if (Settings.Default.baud_rate > 0) wnd.cbSpeed.Text = Settings.Default.baud_rate.ToString();
                }
                catch (Exception) { }

                // При закрытии главного окна программы сохраняем настройки
                wnd.Closed += (sender, e) =>
                {
                    Settings.Default.port1 = wnd.cbPort1.Text;
                    Settings.Default.port2 = wnd.cbPort2.Text;
                    Settings.Default.two_ports = wnd.chbTwoPorts.IsChecked ?? false;
                    Settings.Default.baud_rate = Int32.Parse(wnd.cbSpeed.Text);
                    Settings.Default.Save();
                };

                wnd.bnOpenClosePort.Click += OpenClosePortClick;
            }

            /// <summary>
            /// Обработчик нажатия кнопки "Открыть"/"Закрыть"
            /// </summary>
            /// <param name="sender">Отправитель</param>
            /// <param name="e">Список аргументов</param>
            void OpenClosePortClick(object sender, RoutedEventArgs e)
            {
                if (!portModel.IsOpened)
                {
                    PortParams param = new PortParams();

                    // Cкорость обмена
                    try
                    {
                        int baudRate = Int32.Parse(wnd.cbSpeed.Text);
                        param.baudRate = baudRate;
                    }
                    catch (Exception)
                    {
                        MessageBox.Show(wnd, "Неверно задана скорость обмена", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
                        return;
                    }

                    try
                    {
                        // Имена портов
                        param.portName1 = wnd.cbPort1.Text;
                        param.portName2 = wnd.cbPort2.Text;

                        // Размеры буферов чтения и записи
                        param.readBufferSize = 10000;
                        param.writeBufferSize = 10000;

                        // Количество бит данных
                        int dataBits = Int32.Parse(wnd.cbDataBits.Text);
                        param.dataBits = dataBits;

                        // Режим проверки четности
                        Win32.ParityFlags parity = (Win32.ParityFlags)wnd.cbParity.SelectedIndex;
                        param.parity = parity;

                        // Устанавливаем количество стоповых бит
                        Win32.StopBitsFlags stopBits;
                        switch (wnd.cbStopBits.SelectedIndex)
                        {
                            case 0: stopBits = Win32.StopBitsFlags.One;
                                break;
                            case 1: stopBits = Win32.StopBitsFlags.OnePointFive;
                                break;
                            case 2: stopBits = Win32.StopBitsFlags.Two;
                                break;
                            default: throw new Exception("Ошибка состояния элемента " + wnd.cbStopBits.Name);
                        }
                        param.stopBits = stopBits;

                        // Количество используемых портов
                        bool? state = wnd.chbTwoPorts.IsChecked;
                        if (!state.HasValue) throw new Exception("Ошибка состояния элемента " + wnd.chbTwoPorts.Name);
                        if (state.Value && (wnd.cbPort1.SelectedIndex != wnd.cbPort2.SelectedIndex))
                            param.twoPorts = true;
                        else
                            param.twoPorts = false;

                        // Открываем порты
                        portModel.Open(param);
                    }
                    catch (Exception ex)
                    {
                        MessageBox.Show(wnd, ex.Message, "Ошибка открытия порта", MessageBoxButton.OK, MessageBoxImage.Error);
                        return;
                    }
                }
                else portModel.Close();
            }

            /// <summary>
            /// Метод, вызываемый после открытия портов
            /// </summary>
            /// <param name="param">Параметры портов</param>
            public void PortOpened(PortParams param)
            {
                wnd.cbSpeed.Text = param.baudRate.ToString();
                wnd.cbPort1.Text = param.portName1;
                wnd.cbPort2.Text = param.portName2;
                wnd.cbDataBits.Text = param.dataBits.ToString();
                wnd.cbParity.SelectedIndex = (int)param.parity;
                switch (param.stopBits)
                {
                    case Win32.StopBitsFlags.One:
                        wnd.cbStopBits.SelectedIndex = 0;
                        break;
                    case Win32.StopBitsFlags.OnePointFive:
                        wnd.cbStopBits.SelectedIndex = 1;
                        break;
                    case Win32.StopBitsFlags.Two:
                        wnd.cbStopBits.SelectedIndex = 2;
                        break;
                    default: throw new Exception("Ошибка состояния элемента " + wnd.cbStopBits.Name);
                }
                bool? state = wnd.chbTwoPorts.IsChecked;
                if (!state.HasValue) throw new Exception("Ошибка состояния элемента " + wnd.chbTwoPorts.Name);
                if (!state.Value && param.twoPorts)
                {
                    wnd.chbTwoPorts.IsChecked = true;
                    RefreshPortsCount();
                }

                wnd.bnOpenClosePort.Content = "Закрыть";
                wnd.cbPort1.IsEnabled = false;
                wnd.cbPort2.IsEnabled = false;
                wnd.cbSpeed.IsEnabled = false;
                wnd.cbDataBits.IsEnabled = false;
                wnd.cbParity.IsEnabled = false;
                wnd.cbStopBits.IsEnabled = false;
                wnd.cbFlowControl.IsEnabled = false;
                wnd.chbTwoPorts.IsEnabled = false;
            }

            /// <summary>
            /// Метод, вызываемый после закрытия портов
            /// </summary>
            public void PortClosed()
            {
                wnd.Dispatcher.BeginInvoke(new Action(() =>
                {
                    wnd.bnOpenClosePort.Content = "Открыть";
                    wnd.cbPort1.IsEnabled = true;
                    wnd.cbPort2.IsEnabled = true;
                    wnd.cbSpeed.IsEnabled = true;
                    wnd.cbDataBits.IsEnabled = true;
                    wnd.cbParity.IsEnabled = true;
                    wnd.cbStopBits.IsEnabled = true;
                    //wnd.cbFlowControl.IsEnabled = true;
                    wnd.chbTwoPorts.IsEnabled = true;
                }));
            }

            /// <summary>
            /// Метод, вызываемый после изменения количества задействованных портов. Выполняет отбражение изменений на форме
            /// </summary>
            private void RefreshPortsCount()
            {
                bool? state = wnd.chbTwoPorts.IsChecked;
                if (!state.HasValue) { MessageBox.Show(wnd, "Ошибка состояния элемента " + wnd.chbTwoPorts.Name, "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error); }
                if (state.Value)
                {
                    wnd.lbPort1.Content = "Порт передачи";
                    wnd.lbPort2.Visibility = Visibility.Visible;
                    wnd.cbPort2.Visibility = Visibility.Visible;
                }
                else
                {
                    wnd.lbPort1.Content = "Порт";
                    wnd.lbPort2.Visibility = Visibility.Hidden;
                    wnd.cbPort2.Visibility = Visibility.Hidden;
                }
            }

            /// <summary>
            /// Метод, вызываемый при получении оконного сообщения WM_CLOSE_PORT
            /// </summary>
            public void ClosePorts()
            {
                if (portModel.IsOpened) portModel.Close();
            }

            /// <summary>
            /// Метод, вызываемый про обновлении списка портов в системе
            /// </summary>
            public void RefreshPortList()
            {
                // Получаем новый список портов
                List<string> newPortList = Win32ComPort.GetPortNames();
                //newPortList.Sort(PortListComparer.instance);

                if ((newPortList == null) || (newPortList.Count == 0))
                {
                    MessageBox.Show(wnd, "В системе не обнаружено ни одного последовательного порта", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
                    Environment.Exit(0);
                }

                // Находим позиции выбранных портов в новом списке
                int pos1 = newPortList.IndexOf(portList[wnd.cbPort1.SelectedIndex]);
                int pos2 = newPortList.IndexOf(portList[wnd.cbPort2.SelectedIndex]);

                // Заносим новый список портов в cbPort1 и cbPort2
                wnd.cbPort1.Items.Clear();
                wnd.cbPort2.Items.Clear();
                foreach (string s in newPortList)
                {
                    wnd.cbPort1.Items.Add(s);
                    wnd.cbPort2.Items.Add(s);
                }

                // Если есть открытые порты и один из них был удален, то закрываем порты
                if (portModel.IsOpened && ((pos1 == -1) || (wnd.chbTwoPorts.IsChecked.HasValue && wnd.chbTwoPorts.IsChecked.Value && (pos2 == -1))))
                    portModel.Close();

                // Если выбранный в cbPort1 порт удален, то устанавливаем первый элемент из нового списка
                if (pos1 == -1) wnd.cbPort1.SelectedIndex = 0;
                else wnd.cbPort1.SelectedIndex = pos1;

                // Если выбранный в cbPort2 порт удален, то устанавливаем первый элемент из нового списка
                if (pos2 == -1) wnd.cbPort2.SelectedIndex = 0;
                else wnd.cbPort2.SelectedIndex = pos2;

                // Сохраняем новый список портов
                portList = newPortList;
            }
        }
    }
}
