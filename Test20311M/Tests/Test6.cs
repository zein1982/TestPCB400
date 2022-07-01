using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using Microsoft.Win32;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using System.ComponentModel;
using System.Data;
using System.IO.Ports;
using System.Windows.Controls.Primitives;
using System.Windows.Documents;
using System.Windows.Media;
using System.Windows.Threading;

namespace Test20311M
{
    namespace Test6
    {

    }

    public partial class MainWindow
    {
        class Test6 : Test, IReadDisplay
        {
            /// <summary>
            /// Экземпляр класса
            /// </summary>
            private static Test6 instance = null;
            private readonly OpenFileDialog chooseOpenFileDialog = new OpenFileDialog();
            private readonly SaveFileDialog chooseSaveFileDialog = new SaveFileDialog();
            private string chosenFile;
            private byte[] message;
            private static byte[] line  = new byte[512000];
            private static byte[] line_e = new byte[512000];
            private static byte[] line2 = new byte[512000];
            /*
             * //добавим таймер для задержек хотя лучше Thread.Sleep(100); в скобках пауза в миллисекундах
             * static System.Windows.Forms.Timer Timer1 = new System.Windows.Forms.Timer();
             * static bool exitFlag = false;
             * //exitFlag = false; //флаг окончания счета
             * //Timer1.Start();   //пуск таймера
             * //Timer1.Stop();
             * //ожидаем окончания счета
             * //while (exitFlag == false) { };
             */
            public uint mode;
            private bool _checked = true, _unchecked = false, debug = false, start = true, datacol = false;
            private int filebytecount;
            private const int period = 100;
            private int chfile = 0x55;
            //флаг чтения фуйла эталона
            private bool chtef = false;

            string str1, str2, str3, str4, str5;

            /// <summary>
            /// Метод-фабрика, предназначенный для создания единственного экземпляра класса
            /// </summary>
            /// <param name="wnd">Ссылка на главное окно программы</param>
            /// <param name="portModel">Ссылка на контроллер порта</param>
            /// <returns>Возвращает ссылку на объект Test6</returns>
            public static Test6 GetInstance(MainWindow wnd, PortModel portModel)
            {
                if (instance == null) instance = new Test6(wnd, portModel);
                return instance;
            }

            /// <summary>
            /// Конструктор
            /// </summary>
            /// <param name="wnd">Ссылка на главное окно программы</param>
            /// <param name="portModel">Ссылка на контроллер порта</param>
            /// <param name="programmer">Программатор 1986</param>
            private Test6(MainWindow wnd, PortModel portModel)
                : base(wnd, portModel)
            {
            }

            //метод имитации нажатия на кнопку "Запуск теста"
            public void Button2_Click()
            {
                //очистить окно сообщений
                wnd.ListBox1_Test6.Items.Clear();

                try
                {
                    //если в массив посылки не записано хотя бы 10 байт, файл поверки не читали
                    if (message.Length < 10)
                    {
                        Xceed.Wpf.Toolkit.MessageBox.Show("Не считан файл проверки", "Warning");
                        return;
                    }

                    /*
                    if (chtef == false)
                    {
                        Xceed.Wpf.Toolkit.MessageBox.Show("Не считан эталонный файл проверки", "Warning");
                        return;
                    }
                    */

                    //первый байт код команды пердать всю таблицу
                    message[0] = 0x5;
                    //записываем и читаем через 1с
                    portModel.WriteAndRead(message, 1000, this, 1000);
                }
                catch
                {
                }

                DataCompare();

                wnd.ListBox1_Test6.Items.Clear();
            }

            /// <summary>
            /// Рассчитать контрольную сумму (сумма по модулю 256 с прибавлением единицы при переносе в 9 разряд)
            /// </summary>
            /// <param name="buf">Массив данных</param>
            /// <param name="offset">Сдвиг (индекс первого элемента)</param>
            /// <param name="length">Длина массива</param>
            /// <returns>Возвращает контрольную сумму</returns>
            public static byte Calculate(byte[] buf, int offset, int length)
            {
                UInt32 CS = 0;
                for (int i = offset; i < (offset + length); ++i)
                    CS += buf[i];

                do
                {
                    UInt16 tmp = (UInt16)((CS & 0xFFFF0000) >> 16);
                    CS &= 0xFFFF;
                    CS += tmp;
                }
                while ((CS & 0xFFFF0000) != 0);

                do
                {
                    byte tmp = (byte)((CS & 0xFF00) >> 8);
                    CS &= 0xFF;
                    CS += tmp;
                }
                while ((CS & 0xFF00) != 0);

                return (byte)CS;
            }

           public Int32 raschet_cs(byte[] buf, int dlina) //расчет кс общий метод
            {
                Int32 a, b, c, i=0;
                for (int j = 0; j < dlina; j++)
                {
                    i += buf[j];
                }
                a = i;
                b = i;
                a &= (int)~0xFFFFFF00;//осталась кс
                b &= ~0xFF;
                b >>= 8;//остались переносы
                c = a + b;
                //кс
                if (c > 0xff)
                {
                    a = c & 0xff;
                    b = (int)((c & 0xffffff00) >> 8);
                    c = a + b;
                }
                return c;
            }


            //метод сравнения принятого массива с эталонным по 28 байт из 6272
            public void DataCompare()
            {
                string outLogerr, outBytes;
                string path = @"c:\TestPCB";

                DirectoryInfo di = Directory.CreateDirectory(path);

                outLogerr = @"c:\TestPCB\log_err.txt";
                outBytes = @"c:\TestPCB\outBytes.bin";

                FileStream outStream = new FileStream(outLogerr, FileMode.Create, FileAccess.ReadWrite, FileShare.ReadWrite);
                StreamWriter sw = new StreamWriter(outStream);

                BinaryWriter wb = new BinaryWriter(File.Open(outBytes, FileMode.Create));

                int a=0, a_1 = 0, a_2 = 0, a_3 = 8, compare = 28, compare_2 = 0, n_cont = 0, c_1 = 0, c_2 = 0, c_3 = 0, c_4 = 0, c_5 = 0, c_6 = 0, c_7 = 0, c_8 = 0;
                
                if(filebytecount > 6272)
                {
                    Xceed.Wpf.Toolkit.MessageBox.Show("Неправильный формат файла", "Warning");
                    return;
                }
                
                //цикл на количество байт в файле + 28 байт
                for (int i = 0; i < filebytecount + 28; i++)
                {
                    //писать в файл в дозволенных границах на чтение
                    if (i < 6272)
                    {
                        wb.Write((byte)line2[i]);
                    }
                    //если общий индекс больше верхней границы
                    if (i > compare)
                    {
                        //нижняя граница 28 байт из 6272
                        compare_2 = compare - 28;

                        //флаг отладочной информации
                        if (debug == true)
                        {
                            wnd.ListBox1_Test6.Items.Add("-------------------------Номер заданного контакта №" + Convert.ToString(n_cont + 1, toBase: 10) +"----------------------------");
                        }
                        //запись в файл в директории проекта
                        sw.WriteLine("-------------------------Номер заданного контакта №" + Convert.ToString(n_cont + 1, toBase: 10) + "----------------------------");
                        //сравнение 28 байт
                        for (int j = compare_2; j < compare; j++)
                        {
                            //сравнение с массивом эталоном массива принятых с УСМА байт
                            if (line2[j] != line_e[j])
                            {
                                //флаг наличия хотя бы одной ошибки для вывода сообщения "Не годен"
                                a = 1;
                                //флаг отладочной информации
                                if (debug == true)
                                {
                                    wnd.ListBox1_Test6.Items.Add("Номер байта №" + Convert.ToString(j + 1, toBase: 10) + ", эталонный байт: 0x" + Convert.ToString(line_e[j], toBase: 2).PadLeft(8,'0') + ", принятый байт: 0x" + Convert.ToString(line2[j], toBase: 2).PadLeft(8,'0'));
                                }
                                //запись в файл в директории проекта
                                sw.WriteLine("Номер байта №" + Convert.ToString(j + 1, toBase: 10) + ", эталонный байт: 0x" + Convert.ToString(line_e[j], toBase: 2).PadLeft(8, '0') + ", принятый байт: 0x" + Convert.ToString(line2[j], toBase: 2).PadLeft(8, '0'));
                                
                                //если конец строки в 28 байт, обнулисть счетчики
                                if (a_2 >= 224)
                                {
                                    a_2 = 0;
                                    a_3 = 8;
                                    c_1 = 0; c_2 = 0; c_3 = 0; c_4 = 0; c_5 = 0; c_6 = 0; c_7 = 0; c_8 = 0;
                                }

                                //добавка к номерам контактов в строке
                                if (a_2 >= a_3)
                                {
                                    a_3 = a_2;
                                    c_1 += a_3;
                                    c_2 += a_3;
                                    c_3 += a_3;
                                    c_4 += a_3;
                                    c_5 += a_3;
                                    c_6 += a_3;
                                    c_7 += a_3;
                                    c_8 += a_3;
                                }

                                //переключение на следующий байт в строке
                                a_2 += 8;

                                //поиск несовпавших битов в текущем байте
                                a_1 = line2[j] ^ line_e[j];

                                //побитовый поиск единиц после операции ^
                                //ошибка в битах с единицей
                                //первый контакт
                                if ((a_1 & 0x1) == 0x1)
                                {
                                    c_1 += 1;
                                    if (debug == true)
                                    {
                                        wnd.ListBox1_Test6.Items.Add("Неисправна связь №" + Convert.ToString(c_1, toBase: 10));
                                    }
                                    sw.WriteLine("Неисправна связь №" + Convert.ToString(c_1, toBase: 10));
                                    c_1 = 0;
                                }
                                //в условие может не зайти, а обнулить надо, чтобы не суммировалось
                                else { c_1 = 0; }
                                //второй контакт и далее по порядку
                                if ((a_1 & 0x2) == 0x2)
                                {
                                    c_2 += 2;
                                    if (debug == true)
                                    {
                                        wnd.ListBox1_Test6.Items.Add("Неисправна связь №" + Convert.ToString(c_2, toBase: 10));
                                    }
                                    sw.WriteLine("Неисправна связь №" + Convert.ToString(c_2, toBase: 10));
                                    c_2 = 0;
                                }
                                else { c_2 = 0; }
                                if ((a_1 & 0x4) == 0x4)
                                {
                                    c_3 += 3;
                                    if (debug == true)
                                    {
                                        wnd.ListBox1_Test6.Items.Add("Неисправна связь №" + Convert.ToString(c_3, toBase: 10));
                                    }
                                    sw.WriteLine("Неисправна связь №" + Convert.ToString(c_3, toBase: 10));
                                    c_3 = 0;
                                }
                                else { c_3 = 0; }
                                if ((a_1 & 0x8) == 0x8)
                                {
                                    c_4 += 4;
                                    if (debug == true)
                                    {
                                        wnd.ListBox1_Test6.Items.Add("Неисправна связь №" + Convert.ToString(c_4, toBase: 10));
                                    }
                                    sw.WriteLine("Неисправна связь №" + Convert.ToString(c_4, toBase: 10));
                                    c_4 = 0;
                                }
                                else { c_4 = 0; }
                                if ((a_1 & 0x10) == 0x10)
                                {
                                    c_5 += 5;
                                    if (debug == true)
                                    {
                                        wnd.ListBox1_Test6.Items.Add("Неисправна связь №" + Convert.ToString(c_5, toBase: 10));
                                    }
                                    sw.WriteLine("Неисправна связь №" + Convert.ToString(c_5, toBase: 10));
                                    c_5 = 0;
                                }
                                else { c_5 = 0; }
                                if ((a_1 & 0x20) == 0x20)
                                {
                                    c_6 += 6;
                                    if (debug == true)
                                    {
                                        wnd.ListBox1_Test6.Items.Add("Неисправна связь №" + Convert.ToString(c_6, toBase: 10));
                                    }
                                    sw.WriteLine("Неисправна связь №" + Convert.ToString(c_6, toBase: 10));
                                    c_6 = 0;
                                }
                                else { c_6 = 0; }
                                if ((a_1 & 0x40) == 0x40)
                                {
                                    c_7 += 7;
                                    if (debug == true)
                                    {
                                        wnd.ListBox1_Test6.Items.Add("Неисправна связь №" + Convert.ToString(c_7, toBase: 10));
                                    }
                                    sw.WriteLine("Неисправна связь №" + Convert.ToString(c_7, toBase: 10));
                                    c_7 = 0;
                                }
                                else { c_7 = 0; }
                                if ((a_1 & 0x80) == 0x80)
                                {
                                    c_8 += 8;
                                    if (debug == true)
                                    {
                                        wnd.ListBox1_Test6.Items.Add("Неисправна связь №" + Convert.ToString(c_8, toBase: 10));
                                    }
                                    sw.WriteLine("Неисправна связь №" + Convert.ToString(c_8, toBase: 10));
                                    c_8 = 0;
                                }
                                else { c_8 = 0; }
                            }
                        }
                        //переключатель на следующую строку
                        compare += 28;
                        //переключатель заданного контакта
                        n_cont++;
                    }
                }
                //закрыть поток записи
                sw.Close();
                wb.Close();

                if (a == 1)
                {
                    wnd.ListBox1_Test6.Items.Add("Не годен!");

                }
                else if ((filebytecount != 0)&&(datacol == true))
                {
                    wnd.ListBox1_Test6.Items.Add("Годен!");
                    //wnd.Image1_Test6.IsEnabled = false;
                    //wnd.ListBox1_Test6.Items.

                }
                if (filebytecount == 0)
                {
                    Xceed.Wpf.Toolkit.MessageBox.Show("Нет данных для сравнения", "Warning");
                }
            }

            /// <summary>
            /// Обработчик приема данных 
            /// </summary>
            /// <param name="progData">Массив данных</param>
            /// <param name="count">Размер массива</param>
            public void DataReceived(byte[] data, int count)
            {
                wnd.Dispatcher.BeginInvoke(DispatcherPriority.Send, new Action(() =>
                    {
                if (count == 0)
                {
                    datacol = false;
                    wnd.TextBox1_Test6.Text = "Нет ответа";
                }
                if (count > 0)
                {
                    datacol = true;
                    switch (mode)
                    {
                        //первый запуск холостой
                        case 0x1:

                             for (int i = 0; i < count; i++)
                             {
                                 line2[i] = data[i];
                             }
                            mode = 0;

                            break;
                        //прием ответа "Вывод КС"
                        case 0x6:

                            try
                            {
                                if (_checked == true)
                                {
                                    int a,b,c = 0;
                                    a = (data[1]<<8);
                                    b = data[2];
                                    c |= a | b;
                                    wnd.ListBox1_Test6.Items.Add("Номер версии : " + Convert.ToString(c, toBase: 10) + ", КС : 0х" + Convert.ToString(data[3], toBase: 16) + ", Регистр ошибок : " + Convert.ToString(data[4], toBase: 2));
                                }
                            }
                            catch
                            {
                                if (count > 5)
                                    Xceed.Wpf.Toolkit.MessageBox.Show("Неверный ответ от УСМА", "Warning");
                            }

                            try
                            {
                                if (_unchecked == true)
                                {
                                    int a, b, c = 0;
                                    a = (data[0] << 8);
                                    b = data[1];
                                    c |= a | b;
                                    wnd.ListBox1_Test6.Items.Add("Номер версии : " + Convert.ToString(c, toBase: 10) + ", КС : 0х" + Convert.ToString(data[2], toBase: 16) + ", Регистр ошибок : " + Convert.ToString(data[3], toBase: 2));
                                }
                            }
                            catch
                            {
                                if (count > 4)
                                    Xceed.Wpf.Toolkit.MessageBox.Show("Неверный ответ от УСМА", "Warning");
                            }

                            mode = 0;

                                break;

                        default:
                             datacol = true;
                             wnd.TextBox1_Test6.Text = "Есть данные";
                             for (int i = 0; i < count; i++)
                                {
                                    //проверка первого байта с Саньки на 0.5, должно быть 0.5
                                    if (data[0] != 0x5)
                                    {
                                        Xceed.Wpf.Toolkit.MessageBox.Show("Неверный ответ от УСМА", "Warning");
                                        break;
                                    }
                                    //собственно чтения Санькиного массива с УСМА
                                    line2[i] = data[i];
                                    if (debug == true)
                                    {
                                        wnd.ListBox1_Test6.Items.Add(Convert.ToString(count, toBase: 10) + "{№" + Convert.ToString(i, toBase: 10) + ", 0x" + Convert.ToString(line2[i], toBase: 2).PadLeft(8, '0') + "},");
                                    }
                                }
                            break;
                    }                  
                }
                    }));
            }

            //собственно метод который обрабатывает нажатия на кнопки
            protected override void Init()
            {
                //-----------------------------------------------------------------------------------------------------------------------------------------------------------------
                //кнопка сохранить файл
                wnd.Button6_Test6.Click += (sender, e) =>
                   {
                       chooseSaveFileDialog.FileName = "";
                       chooseSaveFileDialog.ShowDialog();
                       chosenFile = chooseSaveFileDialog.FileName;

                       try
                       {
                           FileStream outStream = new FileStream(chosenFile, FileMode.Create, FileAccess.ReadWrite, FileShare.ReadWrite);
                           StreamWriter sw = new StreamWriter(outStream);
                           int a_1 = 0, a_2 = 0, a_3 = 8, compare = 28, compare_2 = 0, n_cont = 0, c_1 = 0, c_2 = 0, c_3 = 0, c_4 = 0, c_5 = 0, c_6 = 0, c_7 = 0, c_8 = 0;

                           for (int i = 0; i < filebytecount + 28; i++)
                           {
                               if (i > compare)
                               {

                                   compare_2 = compare - 28;

                                   if (debug == true)
                                   {
                                       wnd.ListBox1_Test6.Items.Add("-------------------------Номер заданного контакта №" + Convert.ToString(n_cont + 1, toBase: 10) + "----------------------------");
                                   }
                                   sw.WriteLine("-------------------------Номер заданного контакта №" + Convert.ToString(n_cont + 1, toBase: 10) + "----------------------------");
                                   for (int j = compare_2; j < compare; j++)
                                   {
                                       //сравнение с эталоном
                                       if (line2[j] != line_e[j])
                                       {
                                           if (debug == true)
                                           {
                                               wnd.ListBox1_Test6.Items.Add("Номер байта №" + Convert.ToString(j + 1, toBase: 10) + ", эталонный байт: 0x" + Convert.ToString(line_e[j], toBase: 2).PadLeft(8, '0') + ", принятый байт: 0x" + Convert.ToString(line2[j], toBase: 2).PadLeft(8, '0'));
                                           }
                                           sw.WriteLine("Номер байта №" + Convert.ToString(j + 1, toBase: 10) + ", эталонный байт: 0x" + Convert.ToString(line_e[j], toBase: 2).PadLeft(8, '0') + ", принятый байт: 0x" + Convert.ToString(line2[j], toBase: 2).PadLeft(8, '0'));
                                           
                                           if (a_2 >= 224)
                                           {
                                               a_2 = 0;
                                               a_3 = 8;
                                               c_1 = 0; c_2 = 0; c_3 = 0; c_4 = 0; c_5 = 0; c_6 = 0; c_7 = 0; c_8 = 0;
                                           }

                                           if (a_2 >= a_3)
                                           {
                                               a_3 = a_2;
                                               c_1 += a_3;
                                               c_2 += a_3;
                                               c_3 += a_3;
                                               c_4 += a_3;
                                               c_5 += a_3;
                                               c_6 += a_3;
                                               c_7 += a_3;
                                               c_8 += a_3;
                                           }

                                           a_2 += 8;

                                           a_1 = line2[j] ^ line_e[j];

                                           if ((a_1 & 0x1) == 0x1)
                                           {
                                               c_1 += 1;
                                               if (debug == true)
                                               {
                                                   wnd.ListBox1_Test6.Items.Add("Неисправна связь №" + Convert.ToString(c_1, toBase: 10));
                                               }
                                               sw.WriteLine("Неисправна связь №" + Convert.ToString(c_1, toBase: 10));
                                               c_1 = 0;
                                           }
                                           else { c_1 = 0; }
                                           if ((a_1 & 0x2) == 0x2)
                                           {
                                               c_2 += 2;
                                               if (debug == true)
                                               {
                                                   wnd.ListBox1_Test6.Items.Add("Неисправна связь №" + Convert.ToString(c_2, toBase: 10));
                                               }
                                               sw.WriteLine("Неисправна связь №" + Convert.ToString(c_2, toBase: 10));
                                               c_2 = 0;
                                           }
                                           else { c_2 = 0; }
                                           if ((a_1 & 0x4) == 0x4)
                                           {
                                               c_3 += 3;
                                               if (debug == true)
                                               {
                                                   wnd.ListBox1_Test6.Items.Add("Неисправна связь №" + Convert.ToString(c_3, toBase: 10));
                                               }
                                               sw.WriteLine("Неисправна связь №" + Convert.ToString(c_3, toBase: 10));
                                               c_3 = 0;
                                           }
                                           else { c_3 = 0; }
                                           if ((a_1 & 0x8) == 0x8)
                                           {
                                               c_4 += 4;
                                               if (debug == true)
                                               {
                                                   wnd.ListBox1_Test6.Items.Add("Неисправна связь №" + Convert.ToString(c_4, toBase: 10));
                                               }
                                               sw.WriteLine("Неисправна связь №" + Convert.ToString(c_4, toBase: 10));
                                               c_4 = 0;
                                           }
                                           else { c_4 = 0; }
                                           if ((a_1 & 0x10) == 0x10)
                                           {
                                               c_5 += 5;
                                               if (debug == true)
                                               {
                                                   wnd.ListBox1_Test6.Items.Add("Неисправна связь №" + Convert.ToString(c_5, toBase: 10));
                                               }
                                               sw.WriteLine("Неисправна связь №" + Convert.ToString(c_5, toBase: 10));
                                               c_5 = 0;
                                           }
                                           else { c_5 = 0; }
                                           if ((a_1 & 0x20) == 0x20)
                                           {
                                               c_6 += 6;
                                               if (debug == true)
                                               {
                                                   wnd.ListBox1_Test6.Items.Add("Неисправна связь №" + Convert.ToString(c_6, toBase: 10));
                                               }
                                               sw.WriteLine("Неисправна связь №" + Convert.ToString(c_6, toBase: 10));
                                               c_6 = 0;
                                           }
                                           else { c_6 = 0; }
                                           if ((a_1 & 0x40) == 0x40)
                                           {
                                               c_7 += 7;
                                               if (debug == true)
                                               {
                                                   wnd.ListBox1_Test6.Items.Add("Неисправна связь №" + Convert.ToString(c_7, toBase: 10));
                                               }
                                               sw.WriteLine("Неисправна связь №" + Convert.ToString(c_7, toBase: 10));
                                               c_7 = 0;
                                           }
                                           else { c_7 = 0; }
                                           if ((a_1 & 0x80) == 0x80)
                                           {
                                               c_8 += 8;
                                               if (debug == true)
                                               {
                                                   wnd.ListBox1_Test6.Items.Add("Неисправна связь №" + Convert.ToString(c_8, toBase: 10));
                                               }
                                               sw.WriteLine("Неисправна связь №" + Convert.ToString(c_8, toBase: 10));
                                               c_8 = 0;
                                           }
                                           else { c_8 = 0; }
                                       }
                                   }
                                   compare += 28;

                                   n_cont++;
                               }
                           }
                           sw.Close();
                       }
                       catch 
                       {
                           Xceed.Wpf.Toolkit.MessageBox.Show("Пустое имя пути не допускается", "Warning");
                       }
                   };

                //---------------------------------------------------------------------------------------------------------------------------------------------------------------
                //кнопка загрузка теста
                wnd.Button1_Test6.Click += (sender, e) =>
                   {
                       wnd.ListBox1_Test6.Items.Clear();
                       chooseOpenFileDialog.FileName = "";
                       chooseOpenFileDialog.ShowDialog();
                       chosenFile = chooseOpenFileDialog.FileName;
                       wnd.TextBox2_Test6.Text = chosenFile;

                       try
                       {
                           FileStream inStream = new FileStream(chosenFile, FileMode.Open, FileAccess.Read);
                           BinaryReader fs = new BinaryReader(inStream);
                           
                           filebytecount = (int)fs.BaseStream.Length;

                           
                           if (filebytecount > 6272)
                           {
                               Xceed.Wpf.Toolkit.MessageBox.Show("Неправильный формат файла", "Warning");
                               return;
                           }
                           
                            
                           //по флагу читаем открытый файл в соотв-й массив, line - сам тест, line_e - массив эталона
                           if (chfile == 0x55)
                           {
                               fs.Read(line, 0, filebytecount);
                           }
                           if (chfile == 0xAA) 
                           {
                               fs.Read(line_e, 0, filebytecount);
                               chtef = true;
                           }

                           message = new byte[(int)fs.BaseStream.Length+1];

                           for (int i = 0; i < (int)fs.BaseStream.Length; i++)
                           {
                               if (chfile == 0x55)
                               {
                                   if ((debug == true)&&(start==false))
                                   {
                                       wnd.ListBox1_Test6.Items.Add("{№" + Convert.ToString(i + 1, toBase: 10) + ", 0x" + Convert.ToString(line[i], toBase: 2).PadLeft(8, '0') + "},");
                                   }
                               }
                               
                               if(chfile == 0xAA)
                               {
                                   if ((debug == true)&&(start==false))
                                   {
                                       wnd.ListBox1_Test6.Items.Add("{№" + Convert.ToString(i + 1, toBase: 10) + ", 0x" + Convert.ToString(line_e[i], toBase: 2).PadLeft(8, '0') + "},");
                                   }
                               }
                              
                               wnd.TextBox1_Test6.Text = "Файл проверки успешно считан!";
                           }

                           if (chfile == 0x55)
                           {
                               str1 = Convert.ToString(raschet_cs(line,filebytecount), toBase: 16);
                               str2 = str1.ToUpper();
                               wnd.TextBox2_Test6.Text = chosenFile + " KC 0x" + str2;
                           }
                           if (chfile == 0xAA)
                           {
                               str1 = Convert.ToString(raschet_cs(line_e, filebytecount), toBase: 16);
                               str2 = str1.ToUpper();
                               wnd.TextBox2_Test6.Text = chosenFile + " КС 0x" + str2;
                               //wnd.TextBox2_Test6.Text = Convert.ToString(Calculate(line_e, 0, filebytecount));
                           }
                               //записываем прочитанный массив в массив сообщения на пердачу
                               for (int i = 0; i < fs.BaseStream.Length; i++)
                               {
                                   message[i + 1] = line[i];
                               }

                           fs.Close();
                       }
                       catch 
                       {
                           if (filebytecount < 6272)
                           {
                               Xceed.Wpf.Toolkit.MessageBox.Show("Пустое имя пути не допускается", "Warning");
                           }
                       }

                       //пропуск записи перезаписи массивов по старту
                       if (start == true)
                       {
                           Button2_Click();
                           mode = 0x1;
                           start = false;
                       }
                   };

                //-------------------------------------------------------------------------------------------------------------------------------------------------
                //кнопка запуск теста
                wnd.Button2_Test6.Click += (sender, e) =>
                   {
                       wnd.ListBox1_Test6.Items.Clear();

                      try
                       {
                           if (message.Length < 10)
                           {
                               Xceed.Wpf.Toolkit.MessageBox.Show("Не считан файл проверки", "Warning");
                               return;
                           }

                           if (chtef==false)
                           {
                               Xceed.Wpf.Toolkit.MessageBox.Show("Не считан эталонный файл проверки", "Warning");
                               return;
                           }
                           //команда передачи всей таблицы
                           message[0] = 0x5;
                           
                           //записываем и читаем через 1с
                           portModel.WriteAndRead(message, 1000, this, 1000);
                           
                       }
                       catch 
                       {
                          Xceed.Wpf.Toolkit.MessageBox.Show("Не считан файл проверки", "Warning");
                       }
                      //метод вычисления неисправных контактов
                      DataCompare();

                      //обнуление массива входящего пакета
                      for (int i = 0; i < 7000; i++)
                      {
                          line2[i] = 0x0;
                      }
                   };
               
                //----------------------------------------------------------------------------------------------------------------------------
                //возврат команды по флагу 
                //флаг установлен
                wnd.CheckBox1_Test6.Checked += (sender, e) =>
                    {
                        _checked = true;
                        _unchecked = false;
                        try
                        {
                            portModel.PortClear();
                        }
                        catch { }

                        message = new byte[2];

                        message[0] = 0x01;
                        message[1] = 0x01;

                        portModel.Write(message);
                    };

                //флаг снят
                wnd.CheckBox1_Test6.Unchecked += (sender, e) =>
                    {
                        _unchecked = true;
                        _checked = false;
                        try
                        {
                            portModel.PortClear();
                        }
                        catch { }
                        message = new byte[2];

                        message[0] = 0x01;
                        message[1] = 0x00;

                        portModel.Write(message);
                    };

                //----------------------------------------------------------------------------------------------------------------------------
                //выбор массива эталона для фала элатона для чтения
                //флаг установлен
                wnd.CheckBox2_Test6.Checked += (sender, e) =>
                {
                    chfile = 0xAA;
                };

                //флаг снят
                wnd.CheckBox2_Test6.Unchecked += (sender, e) =>
                {
                    chfile = 0x55;
                };

                //------------------------------------------------------------------------------------------------------------------------------
                //----------------------------------------------------------------------------------------------------------------------------
                //птичка вывода информации для разработчика
                //флаг установлен
                wnd.CheckBox3_Test6.Checked += (sender, e) =>
                {
                    debug = true;
                    Xceed.Wpf.Toolkit.MessageBox.Show("Выбран режим с отладочной информацией", "Warning");
                };

                //флаг снят
                wnd.CheckBox3_Test6.Unchecked += (sender, e) =>
                {
                    debug = false;
                };

                //------------------------------------------------------------------------------------------------------------------------------
                //------------------------------------------------------------------------------------------------------------------------------
                //кнопка вывод кс
                wnd.Button5_Test6.Click += (sender, e) =>
                    {
                        //очистить окно сообщений
                        wnd.ListBox1_Test6.Items.Clear();

                        //номер режима для разборки входящих пакетов
                        mode = 0x6;

                        //создаем массив на код команды усма
                        message = new byte[1];

                        //код команды вывода кс 0х06
                        message[0] = 0x06;
          
                        //записываем и читаем через 100мс
                        portModel.WriteAndRead(message, 100, this);
                        //----------------------------------------------------------------------------------------------------------------------
                    };
            }
        }
    }
}

























































































//byte[] line = new byte[fs.BaseStream.Length];
//long m = fs.BaseStream.Length;
//long g,a,b,c,d = 0;
//fs.Read(line_e, 0, filebytecount);
//wnd.ListBox1_Test6.Items.Add("{№" + Convert.ToString(i + 1, toBase: 10) + ", 0x" + Convert.ToString(line_e[i], toBase: 2) + "},"); 
//установка флага записи в массив эталона или нет
//if (chosenFile == "test.txt")
//{
//    chfile = 0x55;
//}
//else if (chosenFile == "test_e.txt") { chfile = 0xAA; }
//else { chfile = 0x55; }
//var t1 = new Thread(ThreadMain);
//t1.Start();
//while (b < 6000)
//{
//   b++;
//portModel.RunCyclicWriteAndRead(message, 100, this);
//}
//portModel.WriteAndRead(message, 1000, this, 1000);

//wnd.ListBox1_Test6.Items.Add(Convert.ToString(c, toBase: 10) + "{№" + Convert.ToString(1, toBase: 10) + ", 0x" + Convert.ToString(2, toBase: 2) + "},");
//portModel.WriteAndRead(message, 100, this, 100);
//exitFlag = false; //флаг окончания счета
//Timer1.Start();   //пуск таймера

//DisplayData(MessageType.Normal, "\n" + "Проверка выходных синхросигналов для контейнера Н6М:" + "\n");

//ожидаем окончания счета
//while (exitFlag == false) { };
//Thread.Sleep(1500);
/*
            public bool OpenProgDialog()
            {
                if (openDialog == null)
                {
                    openDialog = new OpenFileDialog();
                    openDialog.Filter = "Все файлы (*.*)|*.*";
                    openDialog.CheckFileExists = true;
                    openDialog.Multiselect = false;
                }

                if (openDialog.ShowDialog() == true)
                {
                    return true;
                }
                else return false;
            }
             */
//private PortModel portModel;
//private OpenFileDialog openDialog = null;
//private static Programmer instance = null;
//private int chfile;
/// Приемник - объект, выполняющий операции чтения с порта
/// </summary>
//private Receiver receiver;
/// <summary>
/// Объект через который осуществляется вывод обменной информации
/// </summary>
//private IPortDisplay display;

/// <summary>
/// Делегат, вызываемый для отправки сообщения об освобождении портов другим программам
/// </summary>
//private Action closePortsAction;

/// <summary>
/// Делегат, вызываемый для обработки ошибок в операциях потокового чтения/записи с портом
/// </summary>
//private Action<string> errorHandler;
//Receiver rvr = new Receiver(this,0);
//string[] line;
//Int32[] line2;
/*
FileStream outStream = new FileStream(chosenFile, FileMode.Create, FileAccess.ReadWrite, FileShare.ReadWrite);
                           StreamWriter sw = new StreamWriter(outStream);
                           int a, c_1 = 0, c_2 = 0, c_3 = 0, c_4 = 0, c_5 = 0, c_6 = 0, c_7 = 0, c_8 = 0, a_1 = 0, a_2 = 0, a_3 = 0;
                           for (int i = 0; i < filebytecount; i++)
                           {
                               if (i >= 1)
                               {
                                   a_2 += 8;
                               }

                               if (line2[i] != line[i])
                               {

                                   wnd.ListBox1_Test6.Items.Add("Несовпавшие номера{№" + Convert.ToString(i+1, toBase: 10) + ", Считанный байт: 0x" + Convert.ToString(line[i], toBase: 2) + "}" + ",Принятый байт: 0x" + Convert.ToString(line2[i], toBase: 2) + "},");

                                   if (a_2 > a_3)
                                   {
                                       a_3 = a_2;
                                       c_1 += a_3;
                                       c_2 += a_3;
                                       c_3 += a_3;
                                       c_4 += a_3;
                                       c_5 += a_3;
                                       c_6 += a_3;
                                       c_7 += a_3;
                                       c_8 += a_3;
                                   }

                                   a = line2[i] ^ line[i];
                                   //for (int j = 0; j < 8; j++)
                                   //{
                                   if ((a_1 & 0x1) == 0x1)
                                   {
                                       c_1 += 1;
                                       wnd.ListBox1_Test6.Items.Add("Неисправен контакт №" + Convert.ToString(c_1, toBase: 10));
                                       c_1 = 0;
                                   }
                                   if ((a_1 & 0x2) == 0x2)
                                   {
                                       c_2 += 2;
                                       wnd.ListBox1_Test6.Items.Add("Неисправен контакт №" + Convert.ToString(c_2, toBase: 10));
                                       c_2 = 0;
                                   }
                                   if ((a_1 & 0x4) == 0x4)
                                   {
                                       c_3 += 3;
                                       wnd.ListBox1_Test6.Items.Add("Неисправен контакт №" + Convert.ToString(c_3, toBase: 10));
                                       c_3 = 0;
                                   }
                                   if ((a_1 & 0x8) == 0x8)
                                   {
                                       c_4 += 4;
                                       wnd.ListBox1_Test6.Items.Add("Неисправен контакт №" + Convert.ToString(c_4, toBase: 10));
                                       c_4 = 0;
                                   }
                                   if ((a_1 & 0x10) == 0x10)
                                   {
                                       c_5 += 5;
                                       wnd.ListBox1_Test6.Items.Add("Неисправен контакт №" + Convert.ToString(c_5, toBase: 10));
                                       c_5 = 0;
                                   }
                                   if ((a_1 & 0x20) == 0x20)
                                   {
                                       c_6 += 6;
                                       wnd.ListBox1_Test6.Items.Add("Неисправен контакт №" + Convert.ToString(c_6, toBase: 10));
                                       c_6 = 0;
                                   }
                                   if ((a_1 & 0x40) == 0x40)
                                   {
                                       c_7 += 7;
                                       wnd.ListBox1_Test6.Items.Add("Неисправен контакт №" + Convert.ToString(c_7, toBase: 10));
                                       c_7 = 0;
                                   }
                                   if ((a_1 & 0x80) == 0x80)
                                   {
                                       c_8 += 8;
                                       wnd.ListBox1_Test6.Items.Add("Неисправен контакт №" + Convert.ToString(c_8, toBase: 10));
                                       c_8 = 0;
                                   }
                                   
                                       sw.WriteLine("Несовпавшие номера{№" + Convert.ToString(i, toBase: 10) + ", Считанный байт: 0x" + Convert.ToString(line[i], toBase: 2) + "}" + ",Принятый байт: 0x" + Convert.ToString(line2[i], toBase: 2) + "},");
                               }
                           }
                           sw.Close();
 */
/*
                         if (i >= 1)
                         {
                             a_2 += 8;
                         }

                         if (chfile == 0x55)
                         {

                             if (line2[i] != line[i])
                             {
                                 wnd.ListBox1_Test6.Items.Add("Несовпавшие номера{№" + Convert.ToString(i + 1, toBase: 10) + ", Считанный байт: 0x" + Convert.ToString(line[i], toBase: 2) + "}" + ",Принятый байт: 0x" + Convert.ToString(line2[i], toBase: 2) + "},");

                                 if (a_2 > a_3)
                                 {
                                     a_3 = a_2;
                                     c_1 += a_3;
                                     c_2 += a_3;
                                     c_3 += a_3;
                                     c_4 += a_3;
                                     c_5 += a_3;
                                     c_6 += a_3;
                                     c_7 += a_3;
                                     c_8 += a_3;
                                 }

                                 a_1 = line2[i] ^ line[i];

                                 if ((a_1 & 0x1) == 0x1)
                                 {
                                     c_1 += 1;
                                     wnd.ListBox1_Test6.Items.Add("Неисправен контакт №" + Convert.ToString(c_1, toBase: 10));
                                     c_1 = 0;
                                 }
                                 if ((a_1 & 0x2) == 0x2)
                                 {
                                     c_2 += 2;
                                     wnd.ListBox1_Test6.Items.Add("Неисправен контакт №" + Convert.ToString(c_2, toBase: 10));
                                     c_2 = 0;
                                 }
                                 if ((a_1 & 0x4) == 0x4)
                                 {
                                     c_3 += 3;
                                     wnd.ListBox1_Test6.Items.Add("Неисправен контакт №" + Convert.ToString(c_3, toBase: 10));
                                     c_3 = 0;
                                 }
                                 if ((a_1 & 0x8) == 0x8)
                                 {
                                     c_4 += 4;
                                     wnd.ListBox1_Test6.Items.Add("Неисправен контакт №" + Convert.ToString(c_4, toBase: 10));
                                     c_4 = 0;
                                 }
                                 if ((a_1 & 0x10) == 0x10)
                                 {
                                     c_5 += 5;
                                     wnd.ListBox1_Test6.Items.Add("Неисправен контакт №" + Convert.ToString(c_5, toBase: 10));
                                     c_5 = 0;
                                 }
                                 if ((a_1 & 0x20) == 0x20)
                                 {
                                     c_6 += 6;
                                     wnd.ListBox1_Test6.Items.Add("Неисправен контакт №" + Convert.ToString(c_6, toBase: 10));
                                     c_6 = 0;
                                 }
                                 if ((a_1 & 0x40) == 0x40)
                                 {
                                     c_7 += 7;
                                     wnd.ListBox1_Test6.Items.Add("Неисправен контакт №" + Convert.ToString(c_7, toBase: 10));
                                     c_7 = 0;
                                 }
                                 if ((a_1 & 0x80) == 0x80)
                                 {
                                     c_8 += 8;
                                     wnd.ListBox1_Test6.Items.Add("Неисправен контакт №" + Convert.ToString(c_8, toBase: 10));
                                     c_8 = 0;
                                 }

                                 sw.WriteLine("Несовпавшие номера{№" + Convert.ToString(i + 1, toBase: 10) + ", Считанный байт: 0x" + Convert.ToString(line[i], toBase: 2) + "}" + ",Принятый байт: 0x" + Convert.ToString(line2[i], toBase: 2) + "},");
                                 a = 1;

                             }
                         }
                         else
                         {
                             if (line2[i] != line_e[i])
                             {
                                 wnd.ListBox1_Test6.Items.Add("Несовпавшие номера{№" + Convert.ToString(i + 1, toBase: 10) + ", Считанный байт: 0x" + Convert.ToString(line_e[i], toBase: 2) + "}" + ",Принятый байт: 0x" + Convert.ToString(line2[i], toBase: 2) + "},");

                                 if (a_2 > a_3)
                                 {
                                     a_3 = a_2;
                                     c_1 += a_3;
                                     c_2 += a_3;
                                     c_3 += a_3;
                                     c_4 += a_3;
                                     c_5 += a_3;
                                     c_6 += a_3;
                                     c_7 += a_3;
                                     c_8 += a_3;
                                 }

                                 a_1 = line2[i] ^ line_e[i];

                                 if ((a_1 & 0x1) == 0x1)
                                 {
                                     c_1 += 1;
                                     wnd.ListBox1_Test6.Items.Add("Неисправен контакт №" + Convert.ToString(c_1, toBase: 10));
                                     c_1 = 0;
                                 }
                                 if ((a_1 & 0x2) == 0x2)
                                 {
                                     c_2 += 2;
                                     wnd.ListBox1_Test6.Items.Add("Неисправен контакт №" + Convert.ToString(c_2, toBase: 10));
                                     c_2 = 0;
                                 }
                                 if ((a_1 & 0x4) == 0x4)
                                 {
                                     c_3 += 3;
                                     wnd.ListBox1_Test6.Items.Add("Неисправен контакт №" + Convert.ToString(c_3, toBase: 10));
                                     c_3 = 0;
                                 }
                                 if ((a_1 & 0x8) == 0x8)
                                 {
                                     c_4 += 4;
                                     wnd.ListBox1_Test6.Items.Add("Неисправен контакт №" + Convert.ToString(c_4, toBase: 10));
                                     c_4 = 0;
                                 }
                                 if ((a_1 & 0x10) == 0x10)
                                 {
                                     c_5 += 5;
                                     wnd.ListBox1_Test6.Items.Add("Неисправен контакт №" + Convert.ToString(c_5, toBase: 10));
                                     c_5 = 0;
                                 }
                                 if ((a_1 & 0x20) == 0x20)
                                 {
                                     c_6 += 6;
                                     wnd.ListBox1_Test6.Items.Add("Неисправен контакт №" + Convert.ToString(c_6, toBase: 10));
                                     c_6 = 0;
                                 }
                                 if ((a_1 & 0x40) == 0x40)
                                 {
                                     c_7 += 7;
                                     wnd.ListBox1_Test6.Items.Add("Неисправен контакт №" + Convert.ToString(c_7, toBase: 10));
                                     c_7 = 0;
                                 }
                                 if ((a_1 & 0x80) == 0x80)
                                 {
                                     c_8 += 8;
                                     wnd.ListBox1_Test6.Items.Add("Неисправен контакт №" + Convert.ToString(c_8, toBase: 10));
                                     c_8 = 0;
                                 }

                                 sw.WriteLine("Несовпавшие номера{№" + Convert.ToString(i + 1, toBase: 10) + ", Считанный байт: 0x" + Convert.ToString(line_e[i], toBase: 2) + "}" + ",Принятый байт: 0x" + Convert.ToString(line2[i], toBase: 2) + "},");
                                 a = 1;

                             }

                         }
                      
                          */
/*
            /// <summary>
            /// Обработчик ошибок, возникающих при выполнении потоковых операций записи/чтения с портом
            /// </summary>
            /// <param name="message"></param>
            private void ErrorHandler(string message)
            {
                //Close();
                errorHandler(message);
            }
            */
/*
                       message[0] = 0x5B;
                       message[1] = 0x2A;
                       message[2] = 0x33;
                       g = Convert.ToInt32(wnd.TextBox3_Test6.Text);
                       a = (g & 0xFF000000 >> 24);
                       b = (g & 0x00FF0000 >> 16);
                       c = (g & 0x0000FF00 >> 8);
                       d =  g & 0x000000FF;
                       message[3] = Convert.ToByte(a);
                       message[4] = Convert.ToByte(b);
                       message[2] = Convert.ToByte(c);
                       //message[3] = Convert.ToByte(wnd.TextBox3_Test6.Text);
                       //message[4] = Convert.ToByte(wnd.TextBox3_Test6.Text);
                       //message[4] |= ((message[4])<<32);
                       */
/*
               wnd.Button3_Test6.Click += (sender, e) =>
                   {
                       int a=0;// b=0, c=0, d=0;
                       //wnd.ListBox1_Test6.Items.Clear();

                       string outStruct;

                       //if (tbOut.Text == "")
                       outStruct = "log_err.txt";


                       FileStream outStream = new FileStream(outStruct, FileMode.Create, FileAccess.ReadWrite, FileShare.ReadWrite);
                       StreamWriter sw = new StreamWriter(outStream);

                       for (int i = 0; i < filebytecount; i++)
                       {
                           if (line2[i] != line[i])
                           {
                                
                                
                               sw.WriteLine("Несовпавшие номера{№" + Convert.ToString(i, toBase: 10) + ", Считанный байт: 0x" + Convert.ToString(line[i], toBase: 2) + "}" + ",Принятый байт: 0x" + Convert.ToString(line2[i], toBase: 2) + "},");
                               a = 1;
                               wnd.ListBox1_Test6.Items.Add("Несовпавшие номера{№" + Convert.ToString(i, toBase: 10) + ", Считанный байт: 0x" + Convert.ToString(line[i], toBase: 2) + "}" + ",Принятый байт: 0x" + Convert.ToString(line2[i], toBase: 2) + "},");
                           }

                           //else
                           //{
                             //  wnd.ListBox1_Test6.Items.Clear();
                              // wnd.ListBox1_Test6.Items.Add("Годен!");
 
                           //}

                       }
                       sw.Close();  
                       if (a == 1)
                       {
                           wnd.ListBox1_Test6.Items.Add("Не годен!");

                       }
                       else if(filebytecount!=0)
                       {
                           wnd.ListBox1_Test6.Items.Add("Годен!");
 
                       }
                       if (filebytecount == 0)
                       {
                           Xceed.Wpf.Toolkit.MessageBox.Show("Нет данных для сравнения", "Warning");
                       }
                        
                   };
                */
/*
wnd.Button4_Test6.Click += (sender, e) =>
    {
        int b, c, d=0;
        wnd.ListBox1_Test6.Items.Clear();
        message = new byte[4];
                        
        message[0] = 0x02;
        try
        {
            d = Convert.ToByte(wnd.TextBox4_Test6.Text);
        }
        catch 
        {
            if ((d < 1)||(d>7))
                Xceed.Wpf.Toolkit.MessageBox.Show("Введен не коректный номер регистра", "Warning");
        }
        if ((d < 1) || (d > 7))
        {
            d = 7;
            message[1] = 0x07;
            Xceed.Wpf.Toolkit.MessageBox.Show("Диапазон номера регистра от 1 до 7", "Warning");
        }
        else { message[1] = (byte)d; }

        d = d * 4;
        b = ((((int)filebytecount / d) & 0xFF00) >> 8);
        c = (((int)filebytecount / d) & 0x00FF);
        message[2] = Convert.ToByte(b);
        message[3] = Convert.ToByte(c);
                       
        if (filebytecount == 0)
            Xceed.Wpf.Toolkit.MessageBox.Show("Не считан фуйл проверки", "Warning");
                        
                        
        portModel.Write(message);
    };
*/

//portModel.RunCyclicWriteAndRead(line2, 50000, );
//rc.Start();
//DataReceived(line2, 100);
//portModel.RunCyclicWriteAndRead(line2, 5000);
//else
//{
//  wnd.ListBox1_Test6.Items.Clear();
// wnd.ListBox1_Test6.Items.Add("Годен!");

//}
//for (int j = 0; j < 8; j++)
//{