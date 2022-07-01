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
    /// Interaction logic for ProgressWindow.xaml
    /// </summary>
    public partial class ProgressWindow : Window
    {
        private static ProgressWindow instance = null;
        private MainWindow wnd;

        private ProgressWindow(string status, int? progress = null)
        {
            InitializeComponent();

            lbStatus.Content = status;
            if (progress == null) { pbProgress.IsIndeterminate = true; }
            else
            {
                pbProgress.IsIndeterminate = false;
                int prg = progress ?? 0;
                pbProgress.Value = (prg <= pbProgress.Maximum ? prg : pbProgress.Maximum);
            }
        }

        private void setStatus(string status, int? progress = null)
        {
            lbStatus.Content = status;
            if (progress == null) { pbProgress.IsIndeterminate = true; }
            else
            {
                pbProgress.IsIndeterminate = false;
                int prg = progress ?? 0;
                pbProgress.Value = (prg <= pbProgress.Maximum ? prg : pbProgress.Maximum);
            }
        }

        public static void ShowProgressWnd(MainWindow wnd, string title, string status, int? progress = null, Action closedAction = null)
        {
            wnd.Dispatcher.Invoke(new Action(() =>
            {
                instance = new ProgressWindow(status, progress);
                instance.Owner = wnd;
                instance.Title = title;
                instance.wnd = wnd;
                instance.Closed += (sender, e) => { instance = null; };
                if (closedAction != null) { instance.Closed += (sender, e) => { closedAction(); }; }
                wnd.Dispatcher.BeginInvoke(new Action(() =>
                {
                    instance.ShowDialog();
                }));
            }));
        }

        public static void CloseProgressWnd()
        {
            if (instance != null)
            {
                instance.wnd.Dispatcher.BeginInvoke(new Action(() =>
                {
                    if (instance != null)
                    {
                        instance.Close();
                        instance = null;
                    }
                }));
            }
        }

        public static void SetStatus(string status, int? progress = null)
        {
            if (instance != null)
            {
                instance.wnd.Dispatcher.BeginInvoke(new Action(() =>
                {
                    if (instance != null)
                    {
                        instance.setStatus(status, progress);
                    }
                }));
            }
        }
    }
}
