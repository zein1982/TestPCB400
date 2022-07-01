using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows.Documents;
using System.Windows.Media;
using System.Windows.Threading;

namespace Test20311M
{
    public partial class MainWindow
    {
        /// <summary>
        /// Контроллер вывода на главное окно программы обменной информации
        /// </summary>
        class PortDisplay : IPortDisplay
        {
            /// <summary>
            /// Экземпляр класса
            /// </summary>
            private static PortDisplay instance = null;

            /// <summary>
            /// Главное окно программы
            /// </summary>
            private MainWindow wnd;

            /// <summary>
            /// Счетчик количества строк данных на экране
            /// </summary>
            private int Nlines;

            /// <summary>
            /// Максимальное количество строк, выводимое на экран
            /// </summary>
            private const int N_LINE_MAX = 10000;

            /// <summary>
            /// Признак приостановки вывода данных
            /// </summary>
            private bool suspended;

            /// <summary>
            /// Признак отображения записаваемый на порт
            /// </summary>
            private bool showWrite;

            /// <summary>
            /// Признак отображения читаемой с порта
            /// </summary>
            private bool showRead;

            /// <summary>
            /// Метод-фабрика, предназначенный для создания единственного экземпляра класса
            /// </summary>
            /// <param name="wnd">Ссылка на главное окно программы</param>
            /// <returns>Возвращает ссылку на объект PortDisplay</returns>
            public static PortDisplay GetInstance(MainWindow wnd)
            {
                if (instance == null) instance = new PortDisplay(wnd);
                return instance;
            }

            /// <summary>
            /// Конструктор
            /// </summary>
            /// <param name="wnd">Ссылка на главное окно программы</param>
            private PortDisplay(MainWindow wnd)
            {
                if (wnd == null) throw new Exception("Параметр wnd равен null");
                this.wnd = wnd;
                Nlines = 0;
                suspended = false;
                showWrite = true;
                showRead = true;

                // Кнопка очистки
                wnd.bnClear.Click += (sender, e) =>
                {
                    Nlines = 0;
                    wnd.rtLog.Document.Blocks.Clear();
                    wnd.rtLog.Document.Blocks.Add(new System.Windows.Documents.Paragraph());
                };

                // Разрешить/запретить вывод записываемой на порт информации
                wnd.chbShowWrite.Click += (sender, e) =>
                {
                    showWrite = wnd.chbShowWrite.IsChecked ?? false;
                };

                // Разрешить/запретить вывод считываемой с порта информации
                wnd.chbShowRead.Click += (sender, e) =>
                {
                    showRead = wnd.chbShowRead.IsChecked ?? false;
                };
            }

            /// <summary>
            /// Метод, вызываемый для отображения считанной с порта информации
            /// </summary>
            /// <param name="progData">Массив данных для отображения</param>
            /// <param name="count">Количество элементов в массиве</param>
            public void DataReceived(byte[] data, int count) { if(showRead) AddData(data, count, false); }

            /// <summary>
            /// Метод, вызываемый для отображения записанной на порт информации
            /// </summary>
            /// <param name="progData">Массив данных для отображения</param>
            /// <param name="count">Количество элементов в массиве</param>
            /// <param name="index">Номер посылки</param>
            /// <param name="overlap">Признак "перекрываемого" отображения данных</param>
            public void DataWritten(byte[] data, int count, uint index, bool overlap) { if(showWrite) AddData(data, count, true, overlap); }

            /// <summary>
            /// Общий метод для отображения обменной информации
            /// </summary>
            /// <param name="progData">Массив данных для отображения</param>
            /// <param name="count">Количество элементов в массиве</param>
            /// <param name="redColor">Признак выделения текста красным цветом</param>
            /// <param name="overlap">Признак "перекрываемого" отображения данных</param>
            private void AddData(byte[] data, int count, bool redColor, bool overlap = true)
            {
                if((!suspended) && (count > 0))
                {
                    Action action = () =>
                    {
                        if (!wnd.expander.IsExpanded) return;

                        int radix, width, itemsPerLine;
                        char filler = '0';
                        if (wnd.rbBin1.IsChecked.HasValue && wnd.rbBin1.IsChecked.Value) { radix = 2; width = 8; itemsPerLine = 5; }
                        else if (wnd.rbOct1.IsChecked.HasValue && wnd.rbOct1.IsChecked.Value) { radix = 8; width = 3; itemsPerLine = 12; }
                        else if (wnd.rbDec1.IsChecked.HasValue && wnd.rbDec1.IsChecked.Value) { radix = 10; width = 3; itemsPerLine = 12; filler = ' '; }
                        else if (wnd.rbHex1.IsChecked.HasValue && wnd.rbHex1.IsChecked.Value) { radix = 16; width = 2; itemsPerLine = 16; }
                        else throw new Exception("Не выбран формат преобразования");

                        StringBuilder s = new StringBuilder((count / itemsPerLine + 1) * 71);
                        for (int i = 0; i < count; i += itemsPerLine)
                        {
                            s.Append(" ");
                            // Выводим данные в выбранном формате
                            List<string> s1 = new List<string>(itemsPerLine);
                            int j;
                            for (j = i; ((j - i) < itemsPerLine) && (j < count); ++j)
                                s1.Add(Convert.ToString(data[j], radix).PadLeft(width, filler));
                            s.Append(String.Join(" ", s1).ToUpper().PadRight(50));

                            // Разделитель
                            s.Append("\t\t\t");

                            // Выводим данные в виде ASCII-строки
                            for (j = 0; (j < itemsPerLine) && (i + j < count); ++j)
                                s.Append(wnd.convertingChars[data[i + j]]);

                            s.Append('\n');
                            ++Nlines;
                        }

                        if (redColor)
                        {
                            // Выделяем введенный текст красным цветом
                            TextRange tr = new TextRange(wnd.rtLog.Document.ContentEnd, wnd.rtLog.Document.ContentEnd);
                            tr.Text = s.ToString();
                            tr.ApplyPropertyValue(TextElement.ForegroundProperty, Brushes.Red);
                        }
                        else wnd.rtLog.AppendText(s.ToString());

                        while (Nlines > N_LINE_MAX) { wnd.rtLog.Document.Blocks.Remove(wnd.rtLog.Document.Blocks.FirstBlock); --Nlines; }
                        wnd.rtLog.ScrollToEnd();
                    };
                    if (overlap) { wnd.Dispatcher.BeginInvoke(DispatcherPriority.Send, action); }
                    else { wnd.Dispatcher.Invoke(DispatcherPriority.Send, action); }
                }
            }

            /// <summary>
            /// Метод, вызываемой для отображения сообщения о приостановке отображения входных данных с порта
            /// </summary>
            public void SuspendDisplaying()
            {
                suspended = true;
                wnd.Dispatcher.BeginInvoke(DispatcherPriority.Send, new Action(() =>
                {
                    wnd.brProgressPanel.Visibility = System.Windows.Visibility.Visible;
                    System.Windows.Media.Effects.BlurEffect objBlur = new System.Windows.Media.Effects.BlurEffect();
                    objBlur.Radius = 4;
                    wnd.rtLog.Effect = objBlur;
                    wnd.rtLog.IsEnabled = false;
                }));
            }

            /// <summary>
            /// Метод, вызываемый для скрытия мообщения о приостановке вывода
            /// </summary>
            public void ResumeDisplaying()
            {
                suspended = false;
                wnd.Dispatcher.BeginInvoke(new Action(() =>
                {
                    wnd.brProgressPanel.Visibility = System.Windows.Visibility.Hidden;
                    wnd.rtLog.Effect = null;
                    wnd.rtLog.IsEnabled = true;
                }));
            }
        }
    }
}