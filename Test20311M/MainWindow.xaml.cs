using Xceed.Wpf.Toolkit;
using Xceed.Wpf.DataGrid;
using System;
using System.Linq;
using System.Text;
using System.IO.Ports;
using System.Windows;
using System.Collections.Generic;
using System.Windows.Interop;
using Test20311M.Properties;
using System.Windows.Media;
using System.Text.RegularExpressions;
using System.Deployment.Application;
using System.Windows.Input;

namespace Test20311M
{
    public abstract class Test
    {
        /// <summary>
        /// Главное окно программы
        /// </summary>
        protected MainWindow wnd;

        /// <summary>
        /// Контроллер порта
        /// </summary>
        protected PortModel portModel;

        //protected Receiver rvr;

        private bool initialized = false;

        /// <summary>
        /// Конструктор
        /// </summary>
        /// <param name="wnd">Ссылка на главное окно программы</param>
        /// <param name="portModel">Ссылка на контроллер порта</param>
        protected Test(MainWindow wnd, PortModel portModel)
        {
            if (wnd == null) throw new Exception("Параметр wnd равен null");
            if (portModel == null) throw new Exception("Параметр portModel равен null");
            this.wnd = wnd;
            this.portModel = portModel;
            //this.rvr = rvr;
        }

        /// <summary>
        /// Общий метод для инициализации теста
        /// </summary>
        public void InitTest()
        {
            if (!initialized)
            {
                Init();
                initialized = true;
            }
        }

        /// <summary>
        /// Инициализация теста
        /// </summary>
        protected abstract void Init();

        /// <summary>
        /// Расчет контрольной суммы по алгоритму суммы по модулю 256 с добавлние единицы при переносе в 9 разряд
        /// </summary>
        /// <param name="buf">массив данных</param>
        /// <param name="offset">сдвиг данных в массиве (начальный индекс)</param>
        /// <param name="length">длина массива</param>
        /// <returns>Возвращает контрольную сумму</returns>
        public static byte CalcCheckSum(byte[] buf, uint offset, uint length)
        {
            int checkSum = 0;
            for(int i = 0; i < length; ++i)
                checkSum += buf[offset + i] & 0xFF;
    
            do
            {
                int tmp = (int)((checkSum & 0xFFFF0000) >> 16) & 0xFFFF;
                checkSum &= 0xFFFF;
                checkSum += tmp;
            }
            while((checkSum & 0xFFFF0000) != 0);

            do
            {
                int tmp = (checkSum & 0xFF00) >> 8;
                checkSum &= 0xFF;
                checkSum += tmp;
            }
            while((checkSum & 0xFF00) != 0);
    
            return (byte) checkSum;
        }
    }

    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        private char[] convertingChars;
        private string convertingString;

        private PortListWindow portListWnd;
        private OpenPortDialog openPortDialog;

        private Test[] testList;

        public MainWindow()
        {
            //GraphWindow_test2 wnd1 = new GraphWindow_test2((sender, e) => { });
            //wnd1.ShowDialog();
            //Close();

            var port = new TcpPort();
            port.BaudRate = 115200;
            System.Console.WriteLine(port.BaudRate.ToString());
            var tmp = TcpPort.GetPortNames();

            InitializeComponent();
            
            // Проверяем наличие обновлений, если приложение установлено через ClickOnce
            try
            {
                if (ApplicationDeployment.IsNetworkDeployed)
                {
                    ApplicationDeployment updater = ApplicationDeployment.CurrentDeployment;

                    // Пишем в заголовке текущую версию развертки
                    this.Title += " " + ApplicationDeployment.CurrentDeployment.CurrentVersion;

                    if (updater.CheckForUpdate())
                    {
                        UpdateWindow updateWindow = new UpdateWindow();
                        updateWindow.ShowDialog();
                        System.Windows.Forms.Application.Restart();
                        System.Windows.Application.Current.Shutdown();
                    }
                }
            }
            catch(Exception e)
            {
                //System.Windows.MessageBox.Show(this, e.Message, "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
                System.Console.WriteLine(e.Message);
            }
            
            // Создаем контроллер оконных сообщений
            WMController windowMessageController = new WMController();
            // Создаем контроллер вывода обменной информации на главное окно - дисплей
            PortDisplay display = PortDisplay.GetInstance(this);

            // Создаем модель порта и передаем ей указатель на дисплей и делегаты для отправки сообщений другим программам и обработки ошибок
            PortModel portModel = new PortModel(display, windowMessageController.ClosePorts, ErrorHandler);

            // Создаем представление параметров портов на главном окне, регистрируем его как наблюдателя состояния порта и слушателя оконных сообщений
            OpenPortController portParamView = new OpenPortController(this, true, portModel, true,
                    cbPort1, cbPort2, lbPort1, lbPort2, bnOpenClosePort, null,
                    cbSpeed, cbDataBits, cbParity, cbStopBits, cbFlowControl, chbTwoPorts);
            portModel.AddPortListener(portParamView);
            windowMessageController.AddWMListener(portParamView);

            // Создаем диалог выбора порта, регистрируем его как наблюдателя состояния порта и слушателя оконных сообщений
            openPortDialog = new OpenPortDialog(portModel);
            portModel.AddPortListener(openPortDialog);
            windowMessageController.AddWMListener(openPortDialog);

            // Создаем контроллер терминала для ввода отправляемых на порт данных
            WriteTerminal writeTerminal = WriteTerminal.GetInstance(this, portModel);

            // При нажатии на кнопку "Список портов" всплывает окно с соответствующей информацией
            bnPortList.Click += (sender, e) =>
            {
                // Окно регистрируется как слушатель оконных сообщений
                portListWnd = new PortListWindow();
                portListWnd.Owner = this;
                windowMessageController.AddWMListener(portListWnd);

                portListWnd.ShowDialog();
            };

            // При нажатии на расширитель (expander) на форме показывается и скрывается элементы управления портами
            expander.Expanded += (sender, e) => { expander.Header = "Скрыть (<Alt> + <E>)"; wndSeparator.Height = new GridLength(374); };
            expander.Collapsed += (sender, e) => { expander.Header = "Показать настройки портов (<Alt> + <E>)"; wndSeparator.Height = new GridLength(33); };
            expander.IsExpanded = false;

            // Создаем массив символов для перобразования данных из числового формата в строковый и обратно
            byte[] bytes = new byte[256];
            for (int i = 0; i < 256; ++i)
                bytes[i] = (byte)i;

            convertingChars = Encoding.GetEncoding(1251).GetChars(bytes);

            convertingChars[00] = '□'; convertingChars[01] = '☺'; convertingChars[02] = '☻'; convertingChars[03] = '♥';
            convertingChars[04] = '♦'; convertingChars[05] = '♣'; convertingChars[06] = '♠'; convertingChars[07] = '•';
            convertingChars[08] = '◘'; convertingChars[09] = '○'; convertingChars[10] = '◙'; convertingChars[11] = '♂';
            convertingChars[12] = '♀'; convertingChars[13] = '♪'; convertingChars[14] = '♫'; convertingChars[15] = '☼';
            convertingChars[16] = '►'; convertingChars[17] = '◄'; convertingChars[18] = '↕'; convertingChars[19] = '‼';
            convertingChars[20] = '¶'; convertingChars[21] = '§'; convertingChars[22] = '▬'; convertingChars[23] = '↨';
            convertingChars[24] = '↑'; convertingChars[25] = '↓'; convertingChars[26] = '→'; convertingChars[27] = '←';
            convertingChars[28] = '∟'; convertingChars[29] = '↔'; convertingChars[30] = '▲'; convertingChars[31] = '▼';
            convertingChars[149] = '◌';
            convertingChars[167] = '❡';
            convertingChars[182] = '⁋';

            convertingString = new string(convertingChars);


            // Устанавливаем для диалогового окна открытия портов "слушателя" оконных сообщений
            windowMessageController.SetWindow(openPortDialog);

            // Закрываем программу, если порт не открыт
            bool? ret = openPortDialog.ShowDialog();
            if (!ret.HasValue || !ret.Value) Close();

            // Устанавливаем для главного окна программы "слушателя" оконных сообщений
            windowMessageController.SetWindow(this);

            // При закрытии главного окна программы сохраняем настройки
            Closed += (sender, e) =>
            {
                if (WindowState == System.Windows.WindowState.Maximized) { Settings.Default.height = -1; }
                else { Settings.Default.height = (int)ActualHeight; }
                Settings.Default.last_test = tcMain.SelectedIndex;
                Settings.Default.Save();
            };

            // При нажатии <Alt> + <E> разворачиваем/сворачиваем панель настройки портов
            KeyDown += (sender, e) =>
            {
                if (e.SystemKey == Key.E)
                {
                    expander.IsExpanded = !expander.IsExpanded;
                    e.Handled = true;
                }
            };

            // Создаем объект-программатор микроконтроллера
            Programmer programmer = Programmer.GetInstance(this, portModel, dataGrid_test1, lbStatus_test1);

            // Составляем список тестов
            testList = new Test[6];
            testList[0] = Test1.GetInstance(this, portModel, programmer); 
            testList[1] = Test2.GetInstance(this, portModel);
            testList[2] = Test3.GetInstance(this, portModel, programmer);
            testList[3] = Test4.GetInstance(this, portModel, programmer);
            testList[4] = Test5.GetInstance(this, portModel);
            testList[5] = Test6.GetInstance(this, portModel);

            // Выполняем инициализацию тестов при переключении вкладок
            tcMain.SelectionChanged += (sender, e) => { if(testList[tcMain.SelectedIndex] != null) testList[tcMain.SelectedIndex].InitTest(); };

            // Устанавливаем активную вкладку с тестом согласно сохраненным параметрам
            int last_test;
            try { last_test = Settings.Default.last_test; }
            catch (Exception) { last_test = 5; }
            if (last_test < 0) last_test = 5;
            tcMain.SelectedIndex = last_test;
            if (testList[last_test] != null) testList[last_test].InitTest();

            // Устанавливаем высоту окна
            int height;
            try
            { height = Settings.Default.height; }
            catch (Exception) { height = 800; }
            if (height == -1) { WindowState = System.Windows.WindowState.Maximized; }
            else { Height = height; }
        }

        public void InitTest1()
        {
            testList[0].InitTest();
        }

        /// <summary>
        /// Обработчик ошибок, возникающих при выполнении потоковых операций записи/чтения с портом
        /// </summary>
        /// <param name="message"></param>
        public void ErrorHandler(string message)
        {
            this.Dispatcher.BeginInvoke(new Action(() =>
            {
                System.Windows.MessageBox.Show(this, message, "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
            }));
        }
    }
}