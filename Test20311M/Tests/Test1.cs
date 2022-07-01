using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows;
using Test20311M.Test3;

namespace Test20311M
{
    namespace Test1
    {

    }

    public partial class MainWindow
    {
        class Test1 : Test
        {
            /// <summary>
            /// Экземпляр класса
            /// </summary>
            private static Test1 instance = null;

            /// <summary>
            /// Программатор 1986
            /// </summary>
            private Programmer programmer;

            private byte[] voidRow = new byte[] { 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF };

            /// <summary>
            /// Метод-фабрика, предназначенный для создания единственного экземпляра класса
            /// </summary>
            /// <param name="wnd">Ссылка на главное окно программы</param>
            /// <param name="portModel">Ссылка на контроллер порта</param>
            /// <param name="programmer">Программатор 1986</param>
            /// <returns>Возвращает ссылку на объект Test3</returns>
            public static Test1 GetInstance(MainWindow wnd, PortModel portModel, Programmer programmer)
            {
                if (instance == null) instance = new Test1(wnd, portModel, programmer);
                return instance;
            }

            /// <summary>
            /// Конструктор
            /// </summary>
            /// <param name="wnd">Ссылка на главное окно программы</param>
            /// <param name="portModel">Ссылка на контроллер порта</param>
            /// <param name="programmer">Программатор 1986</param>
            private Test1(MainWindow wnd, PortModel portModel, Programmer programmer) : base(wnd, portModel)
            {
                this.programmer = programmer;
            }

            protected override void Init()
            {
                wnd.dataGrid_test1.LoadingRow += (sender, e) =>
                {
                    e.Row.Header = (0x8000000 + (e.Row.GetIndex() << 4)).ToString("X8");
                };

                List<byte[]> progData = new List<byte[]>(0x2000);
                for (int addr = 0; addr < 0x2000; ++addr)
                    progData.Add((byte[])voidRow.Clone());
                wnd.dataGrid_test1.ItemsSource = progData;

                wnd.bnOpenHex_test1.Click += (sender, e) =>
                {
                    programmer.OpenProgDialog();
                };

                wnd.bnWrite_test1.Click += (sender, e) =>
                {
                    if (programmer.ProgIsOpened)
                    {
                        programmer.RawProg();
                    }
                    else
                    {
                        if (System.Windows.MessageBox.Show(wnd, "Не загружена программа для записи.\n\nВыбрать файла с программой?", "Сообщение", MessageBoxButton.YesNo, MessageBoxImage.Question) == MessageBoxResult.Yes)
                        {
                            if (programmer.OpenProgDialog())
                            {
                                programmer.RawProg();
                            }
                        }
                    }
                };
            }
        }
    }
}
