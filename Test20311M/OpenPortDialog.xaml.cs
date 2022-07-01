using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;
using Test20311M.Properties;

namespace Test20311M
{
    /// <summary>
    /// Interaction logic for OpenPortDialog.xaml
    /// </summary>
    public partial class OpenPortDialog : Window, IWMListener, IPortListener
    {
        /// <summary>
        /// Модель портов
        /// </summary>
        private PortModel portModel;

        /// <summary>
        /// Контроллер открытия порта
        /// </summary>
        private OpenPortController controller;

        public OpenPortDialog(PortModel portModel)
        {
            InitializeComponent();

            expander.Expanded += (sender, e) => { expander.Header = "Скрыть"; this.Height += 130; };
            expander.Collapsed += (sender, e) => { expander.Header = "Подробный список портов"; this.Height -= 130; };
            expander.IsExpanded = false;

            if (portModel == null) throw new Exception("Параметр portModel равен null");
            this.portModel = portModel;

            controller = new OpenPortController(this, false, portModel, false,
                    cbPort1, cbPort2, lbPort1, lbPort2, bnOpenPort, rtPortList,
                    cbSpeed, null, null, null, null, chbTwoPorts);
        }

        public new bool? ShowDialog()
        {
            base.ShowDialog();
            return portModel.IsOpened;
        }

        public void ClosePorts() {}

        /// <summary>
        /// Метод, вызываемый после открытия портов
        /// </summary>
        /// <param name="param">Параметры портов</param>
        public void PortOpened(PortParams param)
        {
            // Закрываем диалоговое окно в случае успешного открытия порта
            Close();
        }

        /// <summary>
        /// Метод, вызываемый после закрытия портов
        /// </summary>
        public void PortClosed() {}


        public void RefreshPortList()
        {
            controller.RefreshPortList();
        }

        private void cbSpeed_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {

        }
    }
}
