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
using System.Deployment.Application;
using System.Diagnostics;

namespace Test20311M
{
    /// <summary>
    /// Interaction logic for UpdateWindow.xaml
    /// </summary>
    public partial class UpdateWindow : Window
    {
        private ApplicationDeployment updater;

        public UpdateWindow()
        {
            InitializeComponent();

            updater = ApplicationDeployment.CurrentDeployment;

            // Указываем обработчики событий: изменения прогресса и завершения обновления
            updater.UpdateProgressChanged += (sender, e) =>
            {
                pbProgress.Value = e.ProgressPercentage;
            };
            updater.UpdateCompleted += (sender, e) =>
            {
                Close();
            };

            // Начать обновление
            updater.UpdateAsync();
        }
    }
}
