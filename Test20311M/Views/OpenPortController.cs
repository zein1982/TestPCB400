using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using Test20311M.Properties;

namespace Test20311M
{
    /// <summary>
    /// Контроллер элементов на форме, управляющих открытием и закрытием портов
    /// </summary>
    public class OpenPortController : IPortListener, IWMListener
    {
        // Сохраняемые параметры программы
        private static string port1;
        private static string port2;
        private static bool two_ports;
        private static int baud_rate;

        /// <summary>
        /// Окно на котором расположены элементы
        /// </summary>
        private Window wnd;

        /// <summary>
        /// Модель, управляющая портами
        /// </summary>
        private PortModel portModel;

        /// <summary>
        /// Признак разрешения операции закрытия портов
        /// </summary>
        private bool isEnabledCloseOperation;

        // Элементы управления
        private ComboBox cbPort1, cbPort2, cbSpeed, cbDataBits, cbParity, cbStopBits, cbFlowControl;
        private Label lbPort1, lbPort2;
        private Button bnOpenClosePort;
        private RichTextBox rtPortList;
        private CheckBox chbTwoPorts;

        /// <summary>
        /// Список портов в системе
        /// </summary>
        private List<string> portList;

        /// <summary>
        /// Статический конструктор класса
        /// </summary>
        static OpenPortController()
        {
            // Загружаем настройки программы
            port1 = Settings.Default.port1;
            port2 = Settings.Default.port2;
            try { two_ports = Settings.Default.two_ports; }
            catch (Exception) { two_ports = false; }
            try { baud_rate = Settings.Default.baud_rate; }
            catch (Exception) { baud_rate = 460800; }
            if (baud_rate <= 0) baud_rate = 460800;
        }

        /// <summary>
        /// Конструктор
        /// </summary>
        /// <param name="wnd">Окно, содержащее элементы</param>
        /// <param name="isMainWindow">Признак главного окна программы</param>
        /// <param name="portModel">Модель, управляющая портами</param>
        /// <param name="isEnabledCloseOperation">Признак разрешения операции закрытия портов</param>
        /// <param name="cbPort1">Элемент выбора 1 порта (порт передачи или двунаправленный порт)</param>
        /// <param name="cbPort2">Элемент выбора 2 порта (порт приема)</param>
        /// <param name="lbPort1">Подпись 1 порта</param>
        /// <param name="lbPort2">Подпись 2 порта</param>
        /// <param name="bnOpenClosePort">Кнопка открытия/закрытия портов</param>
        /// <param name="rtPortList">Текстовое поля для вывода полного списка портов</param>
        /// <param name="cbSpeed">Элемент выбора скорости обмена</param>
        /// <param name="cbDataBits">Элемент выбора количества бит данных</param>
        /// <param name="cbParity">Элемент выбора параметров контроля четности</param>
        /// <param name="cbStopBits">Элемент выбора количества стоповых бит</param>
        /// <param name="cbFlowControl">Элемент выбора параметров управления потоком</param>
        /// <param name="chbTwoPorts">Флаг выбора двухпортового режима работы</param>
        public OpenPortController(Window wnd, bool isMainWindow, PortModel portModel, bool isEnabledCloseOperation,
            ComboBox cbPort1, ComboBox cbPort2, Label lbPort1, Label lbPort2, Button bnOpenClosePort, RichTextBox rtPortList,
            ComboBox cbSpeed, ComboBox cbDataBits, ComboBox cbParity, ComboBox cbStopBits, ComboBox cbFlowControl, CheckBox chbTwoPorts)
        {
            // Проверяем и сохраняем входные данные
            this.wnd = wnd;
            if (wnd == null) throw new Exception("Параметр wnd равен null");
            this.portModel = portModel;
            if (portModel == null) throw new Exception("Параметр portModel равен null");
            this.isEnabledCloseOperation = isEnabledCloseOperation;
            this.cbPort1 = cbPort1;
            if (cbPort1 == null) throw new Exception("Параметр cbPort1 равен null");
            this.cbPort2 = cbPort2;
            if (cbPort2 == null) throw new Exception("Параметр cbPort2 равен null");
            this.lbPort1 = lbPort1;
            if (lbPort1 == null) throw new Exception("Параметр lbPort1 равен null");
            this.lbPort2 = lbPort2;
            if (lbPort2 == null) throw new Exception("Параметр lbPort2 равен null");
            this.bnOpenClosePort = bnOpenClosePort;
            if (bnOpenClosePort == null) throw new Exception("Параметр bnOpenClosePort равен null");
            this.rtPortList = rtPortList;
            this.cbSpeed = cbSpeed;
            if (cbSpeed == null) throw new Exception("Параметр cbSpeed равен null");
            this.cbDataBits = cbDataBits;
            this.cbParity = cbParity;
            this.cbStopBits = cbStopBits;
            this.cbFlowControl = cbFlowControl;
            this.chbTwoPorts = chbTwoPorts;
            if (chbTwoPorts == null) throw new Exception("Параметр chbTwoPorts равен null");

            lbPort1.Content = "Порт";
            lbPort2.Content = "Порт приема";
            lbPort2.Visibility = Visibility.Hidden;
            cbPort2.Visibility = Visibility.Hidden;

            // Получаем список портов в системе
            portList = GeneralPort.GetPortNames();
            if ((portList == null) || (portList.Count == 0))
            {
                MessageBox.Show(wnd, "В системе не обнаружено ни одного последовательного порта", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
                Environment.Exit(0);
            }

            // Заносим список портов в элементы их выбора
            cbPort1.Items.Clear();
            foreach (string s in portList)
            {
                cbPort1.Items.Add(s);
                cbPort2.Items.Add(s);
            }
            cbPort1.SelectedIndex = 0;
            cbPort2.SelectedIndex = 0;

            // Выводим подробный список портов
            if (rtPortList != null)
            {
                rtPortList.Document.Blocks.Clear();
                rtPortList.Document.Blocks.Add(new System.Windows.Documents.Paragraph());
                List<string> extPortList = GeneralPort.GetExtPortNames();
                if (extPortList != null)
                {
                    foreach (string s in extPortList)
                        rtPortList.AppendText(s + '\n');
                }
            }


            cbSpeed.Items.Clear();
            foreach (string s in new string[] { "110", "300", "600", "1200", "2400", "4800", "9600", "14400", "19200", "38400", "56000", "57600", "115200", "128000", "256000", "460800" })
                cbSpeed.Items.Add(s);
            cbSpeed.SelectedIndex = cbSpeed.Items.Count - 1 - 3;

            if (cbDataBits != null)
            {
                cbDataBits.Items.Clear();
                foreach (string s in new string[] { "5", "6", "7", "8" })
                    cbDataBits.Items.Add(s);
                cbDataBits.SelectedIndex = cbDataBits.Items.Count - 1;
            }

            if (cbParity != null)
            {
                cbParity.Items.Clear();
                foreach (string s in new string[] { "Нет", "Нечет", "Чет", "Маркер", "Пробел" })
                    cbParity.Items.Add(s);
                cbParity.SelectedIndex = 0;
            }

            if (cbStopBits != null)
            {
                cbStopBits.Items.Clear();
                foreach (string s in new string[] { "1", "1,5", "2" })
                    cbStopBits.Items.Add(s);
                cbStopBits.SelectedIndex = 0;
            }

            if (cbFlowControl != null)
            {
                cbFlowControl.Items.Clear();
                foreach (string s in new string[] { "Нет", "XON/XOFF", "Аппаратное" })
                    cbFlowControl.Items.Add(s);
                cbFlowControl.SelectedIndex = 0;
            }

            chbTwoPorts.Click += (sender, e) => RefreshPortsCount();

            bnOpenClosePort.Click += (sender, e) => OpenPortClick();

            // Загружаем настройки программы
            if ((port1 != null) && (portList.Contains(port1)))
                cbPort1.SelectedIndex = portList.IndexOf(port1);
            if ((port2 != null) && (portList.Contains(port2)))
                cbPort2.SelectedIndex = portList.IndexOf(port2);

            if (two_ports) {chbTwoPorts.IsChecked = true; RefreshPortsCount(); }

            cbSpeed.Text = baud_rate.ToString();

            // При закрытии главного окна программы сохраняем настройки
            if(isMainWindow)
            {
                wnd.Closed += (sender, e) =>
                {
                    Settings.Default.port1 = this.cbPort1.Text;
                    Settings.Default.port2 = this.cbPort2.Text;
                    Settings.Default.two_ports = this.chbTwoPorts.IsChecked ?? false;
                    Settings.Default.baud_rate = Int32.Parse(this.cbSpeed.Text);
                    Settings.Default.Save();
                };
            }
        }

        /// <summary>
        /// Метод, вызываемый после изменения количества задействованных портов. Выполняет отбражение изменений на форме
        /// </summary>
        private void RefreshPortsCount()
        {
            bool? state = chbTwoPorts.IsChecked;
            if (!state.HasValue) { MessageBox.Show(wnd, "Ошибка состояния элемента " + chbTwoPorts.Name, "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error); }
            if (state.Value)
            {
                lbPort1.Content = "Порт передачи";
                lbPort2.Visibility = Visibility.Visible;
                cbPort2.Visibility = Visibility.Visible;
            }
            else
            {
                lbPort1.Content = "Порт";
                lbPort2.Visibility = Visibility.Hidden;
                cbPort2.Visibility = Visibility.Hidden;
            }
        }

        public void OpenPortClick()
        {
            if (!portModel.IsOpened)
            {
                PortParams param = new PortParams();

                // Cкорость обмена
                try
                {
                    int baudRate = Int32.Parse(cbSpeed.Text);
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
                    param.portName1 = cbPort1.Text;
                    param.portName2 = cbPort2.Text;

                    // Размеры буферов чтения и записи
                    param.readBufferSize = 10000;
                    param.writeBufferSize = 10000;

                    // Количество бит данных
                    if (cbDataBits != null)
                    {
                        int dataBits = Int32.Parse(cbDataBits.Text);
                        param.dataBits = dataBits;
                    }
                    else param.dataBits = 8;


                    // Режим проверки четности
                    if (cbParity != null)
                    {
                        Win32.ParityFlags parity = (Win32.ParityFlags)cbParity.SelectedIndex;
                        param.parity = parity;
                    }
                    else param.parity = Win32.ParityFlags.None;

                    // Устанавливаем количество стоповых бит
                    if (cbStopBits != null)
                    {
                        Win32.StopBitsFlags stopBits;
                        switch (cbStopBits.SelectedIndex)
                        {
                            case 0: stopBits = Win32.StopBitsFlags.One;
                                break;
                            case 1: stopBits = Win32.StopBitsFlags.OnePointFive;
                                break;
                            case 2: stopBits = Win32.StopBitsFlags.Two;
                                break;
                            default: throw new Exception("Ошибка состояния элемента " + cbStopBits.Name);
                        }
                        param.stopBits = stopBits;
                    }
                    else param.stopBits = Win32.StopBitsFlags.One;

                    // Количество используемых портов
                    bool? state = chbTwoPorts.IsChecked;
                    if (!state.HasValue) throw new Exception("Ошибка состояния элемента " + chbTwoPorts.Name);
                    if (state.Value && (cbPort1.SelectedIndex != cbPort2.SelectedIndex))
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
            else if(isEnabledCloseOperation) portModel.Close();
        }

        /// <summary>
        /// Метод, вызываемый после открытия портов
        /// </summary>
        /// <param name="param">Параметры портов</param>
        public void PortOpened(PortParams param)
        {
            if (isEnabledCloseOperation) bnOpenClosePort.Content = "Закрыть";

            cbSpeed.Text = param.baudRate.ToString();
            cbSpeed.IsEnabled = false;

            cbPort1.Text = param.portName1;
            cbPort1.IsEnabled = false;

            cbPort2.Text = param.portName2;
            cbPort2.IsEnabled = false;

            if (cbDataBits != null)
            {
                cbDataBits.Text = param.dataBits.ToString();
                cbDataBits.IsEnabled = false;
            }

            if (cbParity != null)
            {
                cbParity.SelectedIndex = (int)param.parity;
                cbParity.IsEnabled = false;
            }

            if (cbStopBits != null)
            {
                switch (param.stopBits)
                {
                    case Win32.StopBitsFlags.One:
                        cbStopBits.SelectedIndex = 0;
                        break;
                    case Win32.StopBitsFlags.OnePointFive:
                        cbStopBits.SelectedIndex = 1;
                        break;
                    case Win32.StopBitsFlags.Two:
                        cbStopBits.SelectedIndex = 2;
                        break;
                    default: throw new Exception("Ошибка состояния элемента " + cbStopBits.Name);
                }
                cbStopBits.IsEnabled = false;
            }

            if (cbFlowControl != null) cbFlowControl.IsEnabled = false;

            bool? state = chbTwoPorts.IsChecked;
            if (!state.HasValue) throw new Exception("Ошибка состояния элемента " + chbTwoPorts.Name);
            if (!state.Value && param.twoPorts)
            {
                chbTwoPorts.IsChecked = true;
                RefreshPortsCount();
            }
            else if(state.Value && !param.twoPorts)
            {
                chbTwoPorts.IsChecked = false;
                RefreshPortsCount();
            }

            chbTwoPorts.IsEnabled = false;
        }

        /// <summary>
        /// Метод, вызываемый после закрытия портов
        /// </summary>
        public void PortClosed()
        {
            wnd.Dispatcher.BeginInvoke(new Action(() =>
            {
                if(isEnabledCloseOperation) bnOpenClosePort.Content = "Открыть";
                cbPort1.IsEnabled = true;
                cbPort2.IsEnabled = true;
                cbSpeed.IsEnabled = true;
                if(cbDataBits != null)cbDataBits.IsEnabled = true;
                if(cbParity != null) cbParity.IsEnabled = true;
                if(cbStopBits != null) cbStopBits.IsEnabled = true;
                //if(cbFlowControl != null) cbFlowControl.IsEnabled = true;
                chbTwoPorts.IsEnabled = true;
            }));
        }

        /// <summary>
        /// Метод, вызываемый при получении оконного сообщения WM_CLOSE_PORT
        /// </summary>
        public void ClosePorts()
        {
            if (isEnabledCloseOperation && portModel.IsOpened) portModel.Close();
        }

        /// <summary>
        /// Метод, вызываемый про обновлении списка портов в системе
        /// </summary>
        public void RefreshPortList()
        {
            // Получаем новый список портов
            List<string> newPortList = GeneralPort.GetPortNames();

            if ((newPortList == null) || (newPortList.Count == 0))
            {
                MessageBox.Show(wnd, "В системе не обнаружено ни одного порта", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
                Environment.Exit(0);
            }

            // Находим позиции выбранных портов в новом списке
            int pos1 = newPortList.IndexOf(portList[cbPort1.SelectedIndex]);
            int pos2 = newPortList.IndexOf(portList[cbPort2.SelectedIndex]);

            // Заносим новый список портов в cbPort1 и cbPort2
            cbPort1.Items.Clear();
            cbPort2.Items.Clear();
            foreach (string s in newPortList)
            {
                cbPort1.Items.Add(s);
                cbPort2.Items.Add(s);
            }

            // Если есть открытые порты и один из них был удален, то закрываем порты
            if (portModel.IsOpened && ((pos1 == -1) || (chbTwoPorts.IsChecked.HasValue && chbTwoPorts.IsChecked.Value && (pos2 == -1))))
                portModel.Close();

            // Если выбранный в cbPort1 порт удален, то устанавливаем первый элемент из нового списка
            if (pos1 == -1) cbPort1.SelectedIndex = 0;
            else cbPort1.SelectedIndex = pos1;

            // Если выбранный в cbPort2 порт удален, то устанавливаем первый элемент из нового списка
            if (pos2 == -1) cbPort2.SelectedIndex = 0;
            else cbPort2.SelectedIndex = pos2;

            // Сохраняем новый список портов
            portList = newPortList;

            if (rtPortList != null)
            {
                rtPortList.Document.Blocks.Clear();
                rtPortList.Document.Blocks.Add(new System.Windows.Documents.Paragraph());

                List<string> extPortList = GeneralPort.GetExtPortNames();
                if (extPortList != null)
                {
                    foreach (string s in extPortList)
                        rtPortList.AppendText(s + '\n');
                }
            }
        }
    }
}
