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

namespace Test20311M
{
    /// <summary>
    /// Interaction logic for PortListWindow.xaml
    /// </summary>
    public partial class PortListWindow : Window, IWMListener
    {
        public PortListWindow()
        {
            InitializeComponent();
            bnClose.Click += (sender, e) => Close();
        }
        
        public new bool? ShowDialog()
        {
            RefreshPortList();
            return base.ShowDialog();
        }

        public void ClosePorts() {}

        public void RefreshPortList()
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
