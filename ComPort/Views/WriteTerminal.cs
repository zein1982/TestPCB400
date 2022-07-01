using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Input;

namespace ComPort
{
    public partial class MainWindow
    {
        /// <summary>
        /// Контроллер терминала ввода данных для записи в порт
        /// </summary>
        class WriteTerminal : ICycleWriteSubject
        {
            /// <summary>
            /// Экземпляр класса
            /// </summary>
            private static WriteTerminal instance = null;

            /// <summary>
            /// Главное окно программы
            /// </summary>
            private MainWindow wnd;

            /// <summary>
            /// Контроллер порта
            /// </summary>
            private PortModel portModel;

            /// <summary>
            /// Делегат, вызываемый при нажатии на кнопку bnSend
            /// </summary>
            private Action bnSendAction;

            /// <summary>
            /// Буфер переданных сообщений
            /// </summary>
            private LinkedList<string> messageBuffer;

            /// <summary>
            /// Указатель на выбранное сообщение
            /// </summary>
            private int messagePointer;

            /// <summary>
            /// Основание системы счисления для передаваемых на порт данных
            /// </summary>
            private int radix;

            /// <summary>
            /// Метод-фабрика, предназначенный для создания единственного экземпляра класса
            /// </summary>
            /// <param name="wnd">Ссылка на главное окно программы</param>
            /// <param name="portModel">Ссылка на контроллер порта</param>
            /// <returns>Возвращает ссылку на объект WriteTerminal</returns>
            public static WriteTerminal GetInstance(MainWindow wnd, PortModel portModel)
            {
                if (instance == null) instance = new WriteTerminal(wnd, portModel);
                return instance;
            }

            /// <summary>
            /// Конструктор
            /// </summary>
            /// <param name="wnd">Ссылка на главное окно программы</param>
            /// <param name="portModel">Ссылка на контроллер порта</param>
            private WriteTerminal(MainWindow wnd, PortModel portModel)
            {
                if (wnd == null) throw new Exception("Параметр wnd равен null");
                if (portModel == null) throw new Exception("Параметр portModel равен null");

                this.wnd = wnd;
                this.portModel = portModel;
                messageBuffer = new LinkedList<string>();
                messagePointer = 0;
                radix = 16;
                bnSendAction = Write;

                wnd.udLoop.Visibility = Visibility.Hidden;
                wnd.lbLoop.Visibility = Visibility.Hidden;

                // При установке/сбросе флага "Зациклить" на главном отображаются/скрываются элементы для задания периода цикла
                wnd.chbLoop.Click += (sender, e) =>
                {
                    if (wnd.chbLoop.IsChecked.HasValue && wnd.chbLoop.IsChecked.Value)
                    {
                        bnSendAction = RunCyclicWrite;
                        wnd.chbLoop.Content = "Зациклить с интервалом";
                        wnd.udLoop.Visibility = Visibility.Visible;
                        wnd.lbLoop.Visibility = Visibility.Visible;
                        wnd.bnSend.Content = "Запустить";
                    }
                    else if (wnd.chbLoop.IsChecked.HasValue && !wnd.chbLoop.IsChecked.Value)
                    {
                        bnSendAction = Write;
                        wnd.chbLoop.Content = "Зациклить";
                        wnd.udLoop.Visibility = Visibility.Hidden;
                        wnd.lbLoop.Visibility = Visibility.Hidden;
                        wnd.bnSend.Content = "Передать";
                    }
                    else MessageBox.Show(wnd, "Ошибка состояния элемента " + wnd.chbLoop.Name, "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
                };

                // При нажатии клавиши ввода в редакторе сообщения или при нажатии на кнопку "Отправить" вызываем метод для отправки сообщения на порт
                wnd.bnSend.Click += (sender, e) => bnSendAction();
                wnd.tbMessage.KeyDown += (sender, e) => { if (e.Key == Key.Return) bnSendAction(); };

                // При нажатии клавиш "вверх" и "вниз" в редакторе данных для передачи пролистываем список ранее введенных сообщений
                wnd.tbMessage.KeyUp += (sender, e) =>
                {
                    if (e.Key == Key.Up)
                    {
                        if (messageBuffer.Count > messagePointer + 1)
                        {
                            wnd.tbMessage.Text = messageBuffer.ElementAt(++messagePointer);
                            wnd.tbMessage.SelectionStart = wnd.tbMessage.Text.Length;
                        }
                    }
                    else if (e.Key == Key.Down)
                    {
                        if (messagePointer > 0)
                        {
                            wnd.tbMessage.Text = messageBuffer.ElementAt(--messagePointer);
                            wnd.tbMessage.SelectionStart = wnd.tbMessage.Text.Length;
                        }
                    }
                };

                // Преобразовываем введенные строки при изменении формата вводимых данных
                RoutedEventHandler writeRadixChanger = (sender, e) =>
                {
                    LinkedListNode<string> node = messageBuffer.First;
                    while (node != null)
                    {
                        node.Value = NumericToStr(StrToNumeric(node.Value, radix));
                        node = node.Next;
                    }

                    wnd.tbMessage.Text = NumericToStr(StrToNumeric(wnd.tbMessage.Text, radix));

                    if (wnd.rbBin2.IsChecked.HasValue && wnd.rbBin2.IsChecked.Value) radix = 2;
                    else if (wnd.rbOct2.IsChecked.HasValue && wnd.rbOct2.IsChecked.Value) radix = 8;
                    else if (wnd.rbDec2.IsChecked.HasValue && wnd.rbDec2.IsChecked.Value) radix = 10;
                    else if (wnd.rbHex2.IsChecked.HasValue && wnd.rbHex2.IsChecked.Value) radix = 16;
                    else if (wnd.rbString2.IsChecked.HasValue && wnd.rbString2.IsChecked.Value) radix = -1;
                    else throw new Exception("Не выбран формат преобразования");
                };
                wnd.rbBin2.Checked += writeRadixChanger;
                wnd.rbOct2.Checked += writeRadixChanger;
                wnd.rbDec2.Checked += writeRadixChanger;
                wnd.rbHex2.Checked += writeRadixChanger;
                wnd.rbString2.Checked += writeRadixChanger;

                // Ограничиваем набор допустимых символов для ввода в редактор данных
                //wnd.tbMessage.PreviewTextInput += (sender, e) => { e.Handled = "0123456789abcdefABCDEF ".IndexOf(e.Text) < 0; };
            }

            /// <summary>
            /// Получение данных, введенных в редактор  tbMessage
            /// </summary>
            /// <returns>Массив введенных  данных</returns>
            private byte[] GetData()
            {
                if (!portModel.IsOpened)
                {
                    MessageBox.Show(wnd, "Не открыт порт для передачи данных", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
                    return null;
                }

                // Получаем данные из редактора
                List<byte> bData = StrToNumeric(wnd.tbMessage.Text);
                if ((bData == null) || (bData.Count == 0))
                {
                    wnd.tbMessage.Text = "";
                    // Вывести ToolTip: "Введите данные"
                    MessageBox.Show(wnd, "Нет данных для передачи", "Внимание", MessageBoxButton.OK, MessageBoxImage.Information);
                    return null;
                }

                // Возвращаем данные в редактор
                string message = NumericToStr(bData);
                wnd.tbMessage.Text = message;
                wnd.tbMessage.SelectionStart = wnd.tbMessage.Text.Length;

                // Сохраняем в буфер введенную строку
                if (messageBuffer.Count == 0) messageBuffer.AddFirst(message);
                else if (message != messageBuffer.ElementAt(0))
                {
                    if (messagePointer != 0)
                    {
                        if (message != messageBuffer.ElementAt(messagePointer))
                            messagePointer = 0;
                        else ++messagePointer;
                    }
                    messageBuffer.AddFirst(message);
                }

                // Добавляем контрольную сумму к сообщению, если выбрана соответствующая опция
                if (wnd.chbCheckSum.IsChecked.HasValue && wnd.chbCheckSum.IsChecked.Value)
                {
                    UInt32 CS = 0;
                    foreach (byte b in bData)
                        CS += b;

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

                    bData.Add((byte)CS);
                }

                // Получаем массив данных для записи
                return bData.ToArray<byte>();
            }

            /// <summary>
            /// Записать введенные данные на порт.
            /// Метод вызывается при нажатии на кнопку bnSend через делегат bnSendAction
            /// </summary>
            private void Write()
            {
                byte[] data = GetData();

                // Передаем данные
                if (data != null)
                {
                    try { portModel.Write(data); }
                    catch (Exception e)
                    {
                        MessageBox.Show(wnd, e.Message, "Ошибка записи данных на порт", MessageBoxButton.OK, MessageBoxImage.Error);
                        return;
                    }
                }
            }

            /// <summary>
            /// Начать циклическую запись введенных данных на порт.
            /// Метод вызывается при нажатии на кнопку bnSend через делегат bnSendAction
            /// </summary>
            private void RunCyclicWrite()
            {
                byte[] data = GetData();
                if(!wnd.udLoop.Value.HasValue)
                {
                    MessageBox.Show(wnd, "Недопустимое значение интервала задержки", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
                    return;
                }
                int delay = wnd.udLoop.Value.Value;

                // Передаем данные
                if (data != null)
                {
                    try { portModel.RunCyclicWrite(data, delay, this); }
                    catch (Exception e)
                    {
                        MessageBox.Show(wnd, e.Message, "Ошибка записи данных на порт", MessageBoxButton.OK, MessageBoxImage.Error);
                        return;
                    }

                    bnSendAction = StopCyclicWrite;
                    wnd.bnSend.Content = "Остановить";
                    wnd.tbMessage.IsEnabled = false;
                    wnd.chbLoop.IsEnabled = false;
                    wnd.udLoop.IsEnabled = false;
                    wnd.chbCheckSum.IsEnabled = false;
                }
            }

            /// <summary>
            /// Остановить циклическую запись на порт.
            /// Метод вызывается при нажатии на кнопку bnSend через делегат bnSendAction
            /// </summary>
            private void StopCyclicWrite()
            {
                portModel.StopCyclicWrite();
            }

            /// <summary>
            /// Метод, вызываемый по окончанию циклической записи на порт 
            /// </summary>
            public void CyclicWriteCompleted()
            {
                wnd.Dispatcher.BeginInvoke(new Action(() =>
                {
                    bnSendAction = RunCyclicWrite;
                    wnd.tbMessage.IsEnabled = true;
                    wnd.chbLoop.IsEnabled = true;
                    wnd.udLoop.IsEnabled = true;
                    wnd.chbCheckSum.IsEnabled = true;

                    if (wnd.chbLoop.IsChecked.HasValue && wnd.chbLoop.IsChecked.Value)
                        wnd.bnSend.Content = "Запустить";
                    else if (wnd.chbLoop.IsChecked.HasValue && !wnd.chbLoop.IsChecked.Value)
                        wnd.bnSend.Content = "Передать";
                    else MessageBox.Show(wnd, "Ошибка состояния элемента " + wnd.chbLoop.Name, "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
                }));
            }

            /// <summary>
            /// Преобразование строки в массив данных с учетом выбранного в программе формата
            /// </summary>
            /// <param name="str">Масив строк для преобразования</param>
            /// <param name="radix">Основание системы счисления, -1 означает строковый формат.
            /// Если параметр не указан, то основание выбирается в соответствии с выбранной опцией в окне прокраммы</param>
            /// <returns>Миссив данных</returns>
            private List<byte> StrToNumeric(string str, int radix = 0)
            {
                if (str == null) return null;

                // Определяем основание системы счисления
                if (radix == 0)
                {
                    if (wnd.rbBin2.IsChecked.HasValue && wnd.rbBin2.IsChecked.Value) radix = 2;
                    else if (wnd.rbOct2.IsChecked.HasValue && wnd.rbOct2.IsChecked.Value) radix = 8;
                    else if (wnd.rbDec2.IsChecked.HasValue && wnd.rbDec2.IsChecked.Value) radix = 10;
                    else if (wnd.rbHex2.IsChecked.HasValue && wnd.rbHex2.IsChecked.Value) radix = 16;
                    else if (wnd.rbString2.IsChecked.HasValue && wnd.rbString2.IsChecked.Value) radix = -1;
                    else throw new Exception("Не выбран формат преобразования");
                }
                else if ((radix != -1) && (radix != 2) && (radix != 8) && (radix != 10) && (radix != 16)) throw new Exception("Недопустимое основание системы счисления");

                // Преобразование из строкового типа
                if (radix == -1)
                {
                    List<byte> dataList = new List<byte>(str.Length + 1);
                    foreach (char c in str)
                    {
                        int index = wnd.convertingString.IndexOf(c);
                        if (index >= 0) dataList.Add((byte)index);
                    }
                    return dataList;
                }

                // Получаем введенные данные
                string[] sData = str.Split(new char[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
                if (sData.Length == 0) return null;

                // Преобразуем данные из строкового в числовой формат
                List<byte> bData = new List<byte>(sData.Length + 1);
                foreach (string s in sData)
                {
                    try
                    {
                        byte b = Convert.ToByte(s, radix);
                        bData.Add(b);
                    }
                    catch (Exception) { }
                }
                if (bData.Count == 0) return null;

                return bData;
            }

            /// <summary>
            /// Преобразование массива данных в строку с учетом выбранного в программе формата
            /// </summary>
            /// <param name="data">Массив данных для преобразования</param>
            /// <returns>Строка</returns>
            private string NumericToStr(List<byte> data)
            {
                if (data == null) return null;

                int radix, width;
                if (wnd.rbBin2.IsChecked.HasValue && wnd.rbBin2.IsChecked.Value) { radix = 2; width = 8; }
                else if (wnd.rbOct2.IsChecked.HasValue && wnd.rbOct2.IsChecked.Value) { radix = 8; width = 3; }
                else if (wnd.rbDec2.IsChecked.HasValue && wnd.rbDec2.IsChecked.Value) { radix = 10; width = 0; }
                else if (wnd.rbHex2.IsChecked.HasValue && wnd.rbHex2.IsChecked.Value) { radix = 16; width = 2; }
                else if (wnd.rbString2.IsChecked.HasValue && wnd.rbString2.IsChecked.Value)
                {
                    char[] cArray = new char[data.Count];
                    for (int i = 0; i < data.Count; ++i)
                        cArray[i] = wnd.convertingChars[data[i]];
                    return new string(cArray);
                }
                else throw new Exception("Не выбран формат преобразования");

                List<string> sData = new List<string>(data.Count);
                foreach (byte b in data)
                    sData.Add(Convert.ToString(b, radix).PadLeft(width, '0'));
                return String.Join(" ", sData).ToUpper();
            }
        }
    }
}
