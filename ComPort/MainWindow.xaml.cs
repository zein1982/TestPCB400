using Xceed.Wpf.Toolkit;
using Xceed.Wpf.DataGrid;
using System;
using System.Linq;
using System.Text;
using System.IO.Ports;
using System.Windows;
using System.Collections.Generic;
using System.Windows.Interop;
using ComPort.Properties;
using System.Deployment.Application;

namespace ComPort
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        private char[] convertingChars;
        private string convertingString;
        
        private PortListWindow portListWnd;

        public MainWindow()
        {
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
            catch (Exception e)
            {
                //System.Windows.MessageBox.Show(this, e.Message, "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
                System.Console.WriteLine(e.Message);
            }

            // Создаем контроллер оконных сообщений
            WMController windowMessageController = new WMController(this);
            // Создаем контроллер вывода обменной информации на главное окно - дисплей
            PortDisplay display = PortDisplay.GetInstance(this);

            // Создаем модель порта и передаем ей указатель на дисплей и делегаты для отправки сообщений другим программам и обработки ошибок
            PortModel portModel = new PortModel(display, windowMessageController.ClosePorts, ErrorHandler);

            // Создаем представление параметров портов на главном окне, регистрируем его как наблюдателя состояния порта и слушателя оконных сообщений
            PortParamView portParamView = PortParamView.GetInstance(this, portModel);
            portModel.AddPortListener(portParamView);
            windowMessageController.AddWMListener(portParamView);

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

        //private void bnOpenClosePort_Click(object sender, RoutedEventArgs e)
        //{

        //}
    }
}