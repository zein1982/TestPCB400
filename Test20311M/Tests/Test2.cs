using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Threading;
using Test20311M.Test2;
using Xceed.Wpf.Toolkit;

namespace Test20311M
{
    namespace Test2 
    {
        /// <summary>
        /// Интерфейс приемника данных
        /// </summary>
        public interface IDataReceiver
        {
            /// <summary>
            /// Уведомляет об изменении рабочих данных
            /// </summary>
            /// <param name="param">Параметр (необязательный)</param>
            void DataChanged(object param);//fjfjfjfffff
        }

        /// <summary>
        /// Интерфейс источника данных
        /// </summary>
        public interface IDateSource
        {
            /// <summary>
            /// Получить данные
            /// </summary>
            /// <returns>Возвращает массив данных</returns>
            byte[] GetData();

            /// <summary>
            /// Вывести данные на окно
            /// </summary>
            /// <param name="progData">Массив данных</param>
            /// <param name="displayArray">Вывод массива данных в указанный TextBlock</param>
            /// <param name="noDataError">Вывод сообщения об отсутствии даннных в указанный TextBlock</param>
            /// <param name="checkSumError">Вывод сообщения об ошибке контрольной суммы в указанный TextBlock</param>
            /// <param name="packetError">Вывод сообщения об ошибке пакета в указанный TextBlock</param>
            void DisplayData(byte[] data, Action<TextBlock, byte[]> displayArray, Action<TextBlock> noDataError, Action<TextBlock> checkSumError, Action<TextBlock> packetError);

            /// <summary>
            /// Изменить режим
            /// </summary>
            /// <param name="mode">Код режима</param>
            void ChangeMode(int mode);
        }

        /// <summary>
        /// Вспомогательный класс для расчета контрольной суммы
        /// </summary>
        class CheckSumCalculator
        {
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
                for (int i = offset; i < (offset + length); ++i )
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

            /// <summary>
            /// Установить бит четности в младшем разряде указанного байта данных
            /// </summary>
            /// <param name="progData">Ссылка на байт данных</param>
            public static void SetParityBit(ref byte data)
            {
                int cnt = 0;
                for (int i = 0; i < 8; ++i)
                    if (((1 << i) & data) != 0) ++cnt;

                if ((cnt & 1) == 0) data |= 1;
            }
        }

        /// <summary>
        /// Абстрактный класс - представление данных в окне
        /// </summary>
        abstract class DataView
        {
            /// <summary>
            /// Признак завершения инициализации (нужен для предотвращения попытки получения данных до окончательной инициализации всех компонентов представления)
            /// </summary>
            protected bool Initialized { get; set; }
            
            /// <summary>
            /// Массив последних данных, выведенных в окно
            /// </summary>
            protected byte[] lastDisplayedData;
            
            /// <summary>
            /// Приемник даннных
            /// </summary>
            public IDataReceiver DataController { get; set; }
            
            /// <summary>
            /// Рабочие данные
            /// </summary>
            public byte[] Data { get; protected set; }

            /// <summary>
            /// Признак отсутствия данных в представлении
            /// </summary>
            public virtual bool IsNull
            {
                get { return false; }
                private set { }
            }
            
            /// <summary>
            /// Инициализация представления
            /// </summary>
            /// <param name="testGrid">Сетка в которой необходимо поместить все элементы управления</param>
            public void InitView(Grid testGrid)
            {
                lastDisplayedData = null;
                Init(testGrid);
            }

            /// <summary>
            /// Инициализация представления
            /// </summary>
            /// <param name="testGrid">Сетка в которой необходимо поместить все элементы управления</param>
            protected abstract void Init(Grid testGrid);
            
            /// <summary>
            /// Получить введенные данные и разместить в массиве Data
            /// </summary>
            public abstract void Calculate();
            
            /// <summary>
            /// Вывести данные в окно
            /// </summary>
            /// <param name="progData">Массив данных</param>
            /// <param name="resultTextBlock">Текстовый элемент в котором необходимо вывести данные</param>
            /// <param name="displayArray">Вывод массива данных в указанный TextBlock</param>
            /// <param name="noDataError">Вывод сообщения об отсутствии даннных в указанный TextBlock</param>
            /// <param name="checkSumError">Вывод сообщения об ошибке контрольной суммы в указанный TextBlock</param>
            /// <param name="packetError">Вывод сообщения об ошибке пакета в указанный TextBlock</param>
            /// <param name="parameter">Дополнительный (необязательный) пареметр</param>
            public abstract void DisplayData(byte[] data, TextBlock resultTextBlock, Action<TextBlock, byte[]> displayArray, Action<TextBlock> noDataError, Action<TextBlock> checkSumError, Action<TextBlock> packetError, object parameter);
            
            /// <summary>
            /// Обработчик всех событий изменения дынных в элементах ввода данных
            /// </summary>
            /// <param name="sender">Элемент, вызвавший событие</param>
            /// <param name="e">Параметры</param>
            protected void DefaultHandler(object sender, EventArgs e) { if (Initialized) Calculate(); }

            /// <summary>
            /// Получить размер массива данных и позицию контрольной суммы
            /// </summary>
            /// <param name="length">Размер массиваданных</param>
            /// <param name="checkSumPos">Позиция контрольной суммы (начиная с нулевой)</param>
            public virtual void GetDataLength(out int length, out int checkSumPos)
            {
                length = 14;
                checkSumPos = 13;
            }
        }

        /// <summary>
        /// Представление элементов для выбора режима Н8
        /// </summary>
        class N8ModeView : IDateSource
        {
            /// <summary>
            /// Приемник данных
            /// </summary>
            private IDataReceiver dataReceiver;

            /// <summary>
            /// Радиокнопка для выбора режима
            /// </summary>
            private RadioButton rb200, rb400, rb800, rbSDC, rbDisabled;

            /// <summary>
            /// Массив данных (состоит из одного байта - кода частоты)
            /// </summary>
            private byte[] data;

            /// <summary>
            /// Конструктор
            /// </summary>
            /// <param name="dataReceiver">Приемник данных</param>
            /// <param name="rb200">Радиокнопка "200 Гц"</param>
            /// <param name="rb400">Радиокнопка "400 Гц"</param>
            /// <param name="rb800">Радиокнопка "800 Гц"</param>
            /// /// <param name="rbDisabled">Радиокнопка "Откл"</param>
            public N8ModeView(IDataReceiver dataReceiver, RadioButton rb200, RadioButton rb400, RadioButton rb800, RadioButton rbSDC, RadioButton rbDisabled)
            {
                data = new byte[] { 0 };

                // Сохраняем данные
                this.dataReceiver = dataReceiver;
                this.rb200 = rb200;
                this.rb400 = rb400;
                this.rb800 = rb800;
                this.rbSDC = rbSDC;
                this.rbDisabled = rbDisabled;

                // При изменении частоты сообщаем об этом приемнику данных
                RoutedEventHandler checkingHandler = (sender, e) =>
                {
                    byte newData = 0;
                    if (rb200.IsChecked ?? false) newData =  1;
                    else if (rb400.IsChecked ?? false) newData  = 2;
                    else if (rb800.IsChecked ?? false) newData = 3;
                    else if (rbSDC.IsChecked ?? false) newData = 4;
                    else if (rbDisabled.IsChecked ?? false) newData = 0;
                    else throw new Exception("Не установлена частота");
                    data[0] = newData;

                    dataReceiver.DataChanged(null);
                };
                rb200.Checked += checkingHandler;
                rb400.Checked += checkingHandler;
                rb800.Checked += checkingHandler;
                rbSDC.Checked += checkingHandler;
                rbDisabled.Checked += checkingHandler;

                // Вызываем обработчик для инициализации данных
                checkingHandler(null, null);
            }

            /// <summary>
            /// Получить данные (код режима работы Н8)
            /// </summary>
            /// <returns>Возвращает код частоты: 0 - Откл, 1 - 200 Гц, 2 - 400 Гц, 3 - 800 Гц, 4 - СДЦ</returns>
            public byte[] GetData() { return data; }

            
            public void ChangeMode(int mode) {}
            public void DisplayData(byte[] data, Action<TextBlock, byte[]> displayArray, Action<TextBlock> noDataError, Action<TextBlock> checkSumError, Action<TextBlock> packetError) {}
        }

        /// <summary>
        /// Представление элементов для выбора сигналов Н8
        /// </summary>
        class N8SygnalsView : IDateSource
        {
            /// <summary>
            /// Счетчик циклов ожидания для автоматического перехода в режим "Имит 400"
            /// </summary>
            public int ImitCounter { set; get; }

            /// <summary>
            /// Приемник данных
            /// </summary>
            private IDataReceiver dataReceiver;

            /// <summary>
            /// Массив данных (состоит из одного байта - состояния флагов выбора завершающего сигнала)
            /// </summary>
            private byte[] data;

            /// <summary>
            /// Флаги завершающих сигналов
            /// </summary>
            private CheckBox chbStrobePRD, chbSynchrN6;

            /// <summary>
            /// Список элеметов выбора режима работы
            /// </summary>
            private LinkedList<ImitComboBox> cbImitList;

            /// <summary>
            /// Конструктор
            /// </summary>
            /// <param name="dataReceiver">Приемник данных</param>
            /// <param name="chbStrobePRD">Флаг "Строб ПРД"</param>
            /// <param name="chbSynchrN6">Флаг "Синхр Н6"</param>
            public N8SygnalsView(IDataReceiver dataReceiver, CheckBox chbStrobePRD, CheckBox chbSynchrN6)
            {
                data = new byte[] { 0 };

                // Сохраняем данные
                this.dataReceiver = dataReceiver;
                this.chbStrobePRD = chbStrobePRD;
                this.chbSynchrN6 = chbSynchrN6;

                cbImitList = null;

                ImitCounter = 0;

                // При изменении флагов завершающих сигналов сообщаем об этом приемнику данных
                RoutedEventHandler checkingHandler = (sender, e) =>
                {
                    //if (((!chbStrobePRD.IsChecked ?? false) || (!chbSynchrN6.IsChecked ?? false)) && (cbImitList != null))
                    //{
                    //    foreach (ImitComboBox cb in cbImitList)
                    //    {
                    //        if (cb.SelectedIndex == 0) { cb.SelectedIndex = 2; }
                    //    }
                    //}

                    dataReceiver.DataChanged(null);
                };
                chbStrobePRD.Click += checkingHandler;
                chbSynchrN6.Click += checkingHandler;

                // Вызываем обработчик для инициализации данных
                checkingHandler(null, null);
            }

            /// <summary>
            /// Зарегистрировать элемент выбора режима работы
            /// </summary>
            /// <param name="cbImit">Регистрируемы элемент</param>
            public void addImitComboBox(ImitComboBox cbImit)
            {
                if (cbImitList == null) { cbImitList = new LinkedList<ImitComboBox>(); }
                if (!cbImitList.Contains(cbImit))
                {
                    cbImitList.AddLast(cbImit);

                    // Если выбрана внешняя синхронизация, то устанавливаем значение ImitCounter
                    cbImit.SelectionChanged += (sender, e) =>
                    {
                        if (((ImitComboBox)sender).SelectedIndex == 0)
                        {
                            ImitCounter = 4;
                        }
                    };

                    // Если выбрана внешняя синхронизация, то устанавливаем флаги "Строб ПРД" и "Синхр Н6"
                    //cbImit.SelectionChanged += (sender, e) =>
                    //{
                    //    if (((ImitComboBox)sender).SelectedIndex == 0)
                    //    {
                    //        chbStrobePRD.IsChecked = true;
                    //        chbSynchrN6.IsChecked = true;
                    //    }
                    //};
                }
            }

            /// <summary>
            /// Переход в режим "Имит 400"
            /// </summary>
            public void goImit()
            {
                bool dataChanged = false;
                foreach (ImitComboBox cb in cbImitList)
                {
                    if (cb.SelectedIndex == 0)
                    {
                        cb.SelectedIndex = 2;
                        dataChanged = true;
                    }
                }
                if (dataChanged) dataReceiver.DataChanged(null);
            }

            /// <summary>
            /// Получить данные (флаги разрешения сигналов Н8)
            /// </summary>
            /// <returns>Возвращает флаги разрешения сигналов Н8</returns>
            public byte[] GetData()
            {
                data[0] = 0;
                if (chbStrobePRD.IsChecked ?? false) data[0] |= 1;
                if (chbSynchrN6.IsChecked ?? false) data[0] |= 2;
                return data;
            }

            public void ChangeMode(int mode) { }
            public void DisplayData(byte[] data, Action<TextBlock, byte[]> displayArray, Action<TextBlock> noDataError, Action<TextBlock> checkSumError, Action<TextBlock> packetError) { }
        }

        /// <summary>
        /// Представление элементов для выбора флагов ошибок
        /// </summary>
        class ErrorFlagsView : IDateSource
        {
            /// <summary>
            /// Приемник данных
            /// </summary>
            private IDataReceiver dataReceiver;

            /// <summary>
            /// Флаг ошибок
            /// </summary>
            private CheckBox chbFlag0, chbFlag1, chbFlag2, chbFlag3, chbFlag4, chbFlag5, chbFlag6, chbFlag7;

            /// <summary>
            /// Массив данных (состоит из одного байта - состояния флагов ошибок)
            /// </summary>
            private byte[] data;

            /// <summary>
            /// Конструктор
            /// </summary>
            /// <param name="dataReceiver">Приемник данных</param>
            /// <param name="chbFlag0">Флаг 0</param>
            /// <param name="chbFlag1">Флаг 1</param>
            /// <param name="chbFlag2">Флаг 2</param>
            /// <param name="chbFlag3">Флаг 3</param>
            /// <param name="chbFlag4">Флаг 4</param>
            /// <param name="chbFlag5">Флаг 5</param>
            /// <param name="chbFlag6">Флаг 6</param>
            /// <param name="chbFlag7">Флаг 7</param>
            public ErrorFlagsView(IDataReceiver dataReceiver, CheckBox chbFlag0, CheckBox chbFlag1, CheckBox chbFlag2, CheckBox chbFlag3, CheckBox chbFlag4, CheckBox chbFlag5, CheckBox chbFlag6, CheckBox chbFlag7)
            {
                data = new byte[] { 0 };

                // Сохраняем данные
                this.dataReceiver = dataReceiver;
                this.chbFlag0 = chbFlag0;
                this.chbFlag1 = chbFlag1;
                this.chbFlag2 = chbFlag2;
                this.chbFlag3 = chbFlag3;
                this.chbFlag4 = chbFlag4;
                this.chbFlag5 = chbFlag5;
                this.chbFlag6 = chbFlag6;
                this.chbFlag7 = chbFlag7;

                // При изменении флагов ошибок сообщаем об этом приемнику данных
                RoutedEventHandler checkingHandler = (sender, e) =>
                {
                    byte newData = 0;
                    if (chbFlag0.IsChecked ?? false) newData |= 1;
                    if (chbFlag1.IsChecked ?? false) newData |= 2;
                    if (chbFlag2.IsChecked ?? false) newData |= 4;
                    if (chbFlag3.IsChecked ?? false) newData |= 8;
                    if (chbFlag4.IsChecked ?? false) newData |= 16;
                    if (chbFlag5.IsChecked ?? false) newData |= 32;
                    if (chbFlag6.IsChecked ?? false) newData |= 64;
                    if (chbFlag7.IsChecked ?? false) newData |= 128;
                    data[0] = newData;

                    dataReceiver.DataChanged(null);
                };
                chbFlag0.Click += checkingHandler;
                chbFlag1.Click += checkingHandler;
                chbFlag2.Click += checkingHandler;
                chbFlag3.Click += checkingHandler;
                chbFlag4.Click += checkingHandler;
                chbFlag5.Click += checkingHandler;
                chbFlag6.Click += checkingHandler;
                chbFlag7.Click += checkingHandler;

                // Вызываем обработчик для инициализации данных
                checkingHandler(null, null);
            }

            /// <summary>
            /// Получить данные (байт состояния флагов ошибок)
            /// </summary>
            /// <returns>Возвращает байт состояния флагов ошибок</returns>
            public byte[] GetData() { return data; }

            public void ChangeMode(int mode) { }
            public void DisplayData(byte[] data, Action<TextBlock, byte[]> displayArray, Action<TextBlock> noDataError, Action<TextBlock> checkSumError, Action<TextBlock> packetError) { }
        }

        /// <summary>
        /// Представление элементов для выбора контроллера ШД21
        /// </summary>
        class SD21ControlView : IDateSource
        {
            /// <summary>
            /// Приемник данных
            /// </summary>
            private IDataReceiver dataReceiver;

            /// <summary>
            /// Радиокнопка для выбора контроллера ШД21
            /// </summary>
            private RadioButton rbStandControlSD21, rb1001ControlSD21;

            /// <summary>
            /// Массив данных (состоит из одного байта - кода частоты)
            /// </summary>
            private byte[] data;

            /// <summary>
            /// Конструктор
            /// </summary>
            /// <param name="dataReceiver">Приемник данных</param>
            /// <param name="rbStandControlSD21">Радиокнопка "Стенд"</param>
            /// <param name="rb1001ControlSD21">Радиокнопка "Н6.21.10.01"</param>
            public SD21ControlView(IDataReceiver dataReceiver, RadioButton rbStandControlSD21, RadioButton rb1001ControlSD21)
            {
                data = new byte[] { 0 };

                // Сохраняем данные
                this.dataReceiver = dataReceiver;
                this.rbStandControlSD21 = rbStandControlSD21;
                this.rb1001ControlSD21 = rb1001ControlSD21;

                // При изменении частоты сообщаем об этом приемнику данных
                RoutedEventHandler checkingHandler = (sender, e) =>
                {
                    byte newData;
                    if (rbStandControlSD21.IsChecked ?? false) newData = 1;
                    else if (rb1001ControlSD21.IsChecked ?? false) newData = 0;
                    else throw new Exception("Не выбран контроллер ШД21");
                    data[0] = newData;

                    dataReceiver.DataChanged(null);
                };
                rbStandControlSD21.Checked += checkingHandler;
                rb1001ControlSD21.Checked += checkingHandler;

                // Вызываем обработчик для инициализации данных
                checkingHandler(null, null);
            }

            /// <summary>
            /// Получить данные (код контроллера)
            /// </summary>
            /// <returns>Возвращает код контроллера: 0 - Стенд, 1 - Н6.21.10.01</returns>
            public byte[] GetData() { return data; }


            public void ChangeMode(int mode) { }
            public void DisplayData(byte[] data, Action<TextBlock, byte[]> displayArray, Action<TextBlock> noDataError, Action<TextBlock> checkSumError, Action<TextBlock> packetError) { }
        }

        /// <summary>
        /// Контроллер данных линий ЛС21, ЛС31
        /// </summary>
        class LS321DataController : IDateSource, IDataReceiver
        {
            /// <summary>
            /// Приемник данных
            /// </summary>
            private IDataReceiver dataReceiver;

            /// <summary>
            /// Список представлений данных для всех режимов работы
            /// </summary>
            private DataView[] dataViewList;

            /// <summary>
            /// Текущее представление данных
            /// </summary>
            private DataView currentView;

            /// <summary>
            /// Сетка для размещения элементовв ввода
            /// </summary>
            private Grid testGrid;

            /// <summary>
            /// Редактор для ручного ввода массива данных
            /// </summary>
            private CommandEditor commandEditor;

            /// <summary>
            /// Текстовые поля для вывода результатов обработки принятых данных и состояния Н6.21.10.02
            /// </summary>
            private TextBlock resultTextBlock, statusTextBlock;

            /// <summary>
            /// Радиокнопка для выбора между ручным и упрощенным способом ввода данных
            /// </summary>
            private RadioButton rbSelectData, rbInputData;

            /// <summary>
            /// Получить данные
            /// </summary>
            /// <returns>Возвращает массив данных для передачи по ЛС21 или ЛС31</returns>
            public byte[] GetData()
            {
                // Если выбран упрощенный режим ввода данных, то получаем данные с объекта-представления
                if (rbSelectData.IsChecked ?? false) return (currentView == null ? null : currentView.Data);
                // Если выбран ручной ввод данных, то получаем данные с редактора команд
                else return commandEditor.GetData();
            }

            /// <summary>
            /// Конструктор
            /// </summary>
            /// <param name="dataReceiver">Приемник данных</param>
            /// <param name="dataViewList">Список представлений данных</param>
            /// <param name="testGrid">Сетка для размещения элементовв ввода</param>
            /// <param name="commandEditor">Редактор для ручного ввода массива данных</param>
            /// <param name="rbSelectData">Радиокнопка для выбора упрощенного режима ввода данных</param>
            /// <param name="rbInputData">РАдиокнопка для выбора ручного режима ввода данных</param>
            /// <param name="resultTextBlock">Текстовое поле для вывода результатов обработки принятых данных</param>
            /// <param name="statusTextBlock">Текстовое поле для вывода состояния Н6.21.10.02</param>
            public LS321DataController(IDataReceiver dataReceiver, DataView[] dataViewList, Grid testGrid, MaskedTextBox commandEditor, RadioButton rbSelectData, RadioButton rbInputData, TextBlock resultTextBlock, TextBlock statusTextBlock)
            {
                currentView = null;

                // Сохраняем данные
                this.dataReceiver = dataReceiver;
                this.dataViewList = dataViewList;
                this.testGrid = testGrid;
                this.commandEditor = new CommandEditor(commandEditor, 14, 13);
                this.rbSelectData = rbSelectData;
                this.rbInputData = rbInputData;
                this.resultTextBlock = resultTextBlock;
                this.statusTextBlock = statusTextBlock;

                // Для редактора команд и представлений данных устанавливаем приемник данных
                this.commandEditor.DataController = this;
                foreach (DataView dv in dataViewList)
                    if (dv != null) dv.DataController = this;

                // При изменении режима ввода блокируем неиспользуемые элементы в окне
                RoutedEventHandler inputModeLS21 = (sender, e) =>
                {
                    if (sender == rbSelectData)
                    {
                        testGrid.IsEnabled = true;
                        this.commandEditor.IsEnabled = false;
                        if(currentView != null) currentView.Calculate();
                    }
                    else
                    {
                        testGrid.IsEnabled = false;
                        this.commandEditor.IsEnabled = true;
                    }
                };
                rbSelectData.Checked += inputModeLS21;
                rbInputData.Checked += inputModeLS21;
            }

            /// <summary>
            /// Изменить режим работы
            /// </summary>
            /// <param name="mode">Код режима</param>
            public void ChangeMode(int mode)
            {
                // Выбираем представление, соответствующее новому режиму
                currentView = dataViewList[mode];

                // Убираем элементы текущего представления с окна
                testGrid.Children.Clear();
                resultTextBlock.Text = "";
                statusTextBlock.Text = "";

                // Инициализируем новое представление
                if(currentView != null)
                {
                    currentView.InitView(testGrid);
                    currentView.Calculate();
                }
            }

            /// <summary>
            /// Информирование об изменении даных в представлении или редакторе команд
            /// </summary>
            /// <param name="param">Новые данные в текстовом виде (если метод вызван представлением)</param>
            public void DataChanged(object param)
            {
                // Если получены данные в текстовом виде, то загружаем их в редактор команд
                if(param != null) commandEditor.Text = (string) param;
                // Сообщаем приемнику данных об их изменении
                dataReceiver.DataChanged(null);
            }

            /// <summary>
            /// Вывести данные на окно
            /// </summary>
            /// <param name="progData">Массив данных</param>
            /// <param name="displayArray">Вывод массива данных в указанный TextBlock</param>
            /// <param name="noDataError">Вывод сообщения об отсутствии даннных в указанный TextBlock</param>
            /// <param name="checkSumError">Вывод сообщения об ошибке контрольной суммы в указанный TextBlock</param>
            /// <param name="packetError">Вывод сообщения об ошибке пакета в указанный TextBlock</param>
            public void DisplayData(byte[] data, Action<TextBlock, byte[]> displayArray, Action<TextBlock> noDataError, Action<TextBlock> checkSumError, Action<TextBlock> packetError)
            {
                // Делегируем исполнение метода объекту-представлению
                currentView.DisplayData(data, resultTextBlock, displayArray, noDataError, checkSumError, packetError, statusTextBlock);
            }
        }

        /// <summary>
        /// Контроллер данных линии 2ЛС
        /// </summary>
        class TwoLSDataController : DataView, IDateSource, IDataReceiver
        {
            /// <summary>
            /// Сетка для размещения элементовв ввода
            /// </summary>
            private Grid testGrid;

            /// <summary>
            /// Редактор для ручного ввода массива данных
            /// </summary>
            private CommandEditor commandEditor;

            /// <summary>
            /// Радиокнопка для выбора между ручным и упрощенным способом ввода данных
            /// </summary>
            private RadioButton rbSelectData, rbInputData;

            private Label lb1, lb2, lb3, lb4, lb5, lb6, lb7, lb8;
            private TwoDigitUpDown udNfc, udNfw, udNf1, udNf2, udNf3, udNf4;
            private BinaryMaskedTextBox tbPFt, tbVaru;
            private SimpleCheckBox chbPsdc;

            /// <summary>
            /// Получить данные
            /// </summary>
            /// <returns>Возвращает массив данных для передачи по 2ЛС</returns>
            public byte[] GetData()
            {
                // Если выбран упрощенный режим ввода данных, то возвращаем массив Data
                if (rbSelectData.IsChecked ?? false) return Data;
                // Если выбран ручной ввод данных, то получаем данные с редактора команд
                else return commandEditor.GetData();
            }

            /// <summary>
            /// Конструктор
            /// </summary>
            /// <param name="dataReceiver">Приемник данных</param>
            /// <param name="testGrid">Сетка для размещения элементовв ввода</param>
            /// <param name="commandEditor">Редактор для ручного ввода массива данных</param>
            /// <param name="rbSelectData">Радиокнопка для выбора упрощенного режима ввода данных</param>
            /// <param name="rbInputData">РАдиокнопка для выбора ручного режима ввода данных</param>
            public TwoLSDataController(IDataReceiver dataReceiver, Grid testGrid, MaskedTextBox commandEditor, RadioButton rbSelectData, RadioButton rbInputData)
            {
                Data = new byte[8];

                // Сохраняем данные
                DataController = dataReceiver;
                this.testGrid = testGrid;
                this.commandEditor = new CommandEditor(commandEditor, 8, -1);
                this.rbSelectData = rbSelectData;
                this.rbInputData = rbInputData;

                // Устанавливаем приемник данных для редактора команд
                this.commandEditor.DataController = this;

                // При изменении режима ввода блокируем неиспользуемые элементы в окне
                RoutedEventHandler inputModeLS21 = (sender, e) =>
                {
                    if (sender == rbSelectData)
                    {
                        testGrid.IsEnabled = true;
                        this.commandEditor.IsEnabled = false;
                        Calculate();
                    }
                    else
                    {
                        testGrid.IsEnabled = false;
                        this.commandEditor.IsEnabled = true;
                    }
                };
                rbSelectData.Checked += inputModeLS21;
                rbInputData.Checked += inputModeLS21;

                // Инициализируем элементы ввода данных

                chbPsdc = new SimpleCheckBox("Псдц");
                chbPsdc.Margin = new Thickness(10 + 5, 10 + 5, 0, 0);

                lb1 = new Label();
                lb1.Content = "NFц";
                lb1.Margin = new Thickness(190, 10, 0, 0);
                udNfc = new TwoDigitUpDown(0, 15, 0);
                udNfc.Margin = new Thickness(300, 10, 0, 0);

                lb2 = new Label();
                lb2.Content = "NF1";
                lb2.Margin = new Thickness(10, 36, 0, 0);
                udNf1 = new TwoDigitUpDown(0, 15, 0);
                udNf1.Margin = new Thickness(120, 36, 0, 0);

                lb3 = new Label();
                lb3.Content = "NF2";
                lb3.Margin = new Thickness(190, 36, 0, 0);
                udNf2 = new TwoDigitUpDown(0, 15, 0);
                udNf2.Margin = new Thickness(300, 36, 0, 0);

                lb4 = new Label();
                lb4.Content = "NF3";
                lb4.Margin = new Thickness(10, 62, 0, 0);
                udNf3 = new TwoDigitUpDown(0, 15, 0);
                udNf3.Margin = new Thickness(120, 62, 0, 0);

                lb5 = new Label();
                lb5.Content = "NF4";
                lb5.Margin = new Thickness(190, 62, 0, 0);
                udNf4 = new TwoDigitUpDown(0, 15, 0);
                udNf4.Margin = new Thickness(300, 62, 0, 0);

                lb6 = new Label();
                lb6.Content = "NFр";
                lb6.Margin = new Thickness(10, 88, 0, 0);
                udNfw = new TwoDigitUpDown(0, 15, 0);
                udNfw.Margin = new Thickness(120, 88, 0, 0);

                lb7 = new Label();
                lb7.Content = "П Fт";
                lb7.Margin = new Thickness(10, 114, 0, 0);
                tbPFt = new BinaryMaskedTextBox(5);
                tbPFt.Margin = new Thickness(120, 114, 0, 0);

                lb8 = new Label();
                lb8.Content = "Код ВАРУ";
                lb8.Margin = new Thickness(10, 140, 0, 0);
                tbVaru = new BinaryMaskedTextBox(6);
                tbVaru.Margin = new Thickness(120, 140, 0, 0);

                // Регистрируем обработчик событий по-умолчанию для всех элементов
                chbPsdc.Click += DefaultHandler;
                udNfc.ValueChanged += DefaultHandler;
                udNfw.ValueChanged += DefaultHandler;
                udNf1.ValueChanged += DefaultHandler;
                udNf2.ValueChanged += DefaultHandler;
                udNf3.ValueChanged += DefaultHandler;
                udNf4.ValueChanged += DefaultHandler;
                tbPFt.TextChanged += DefaultHandler;
                tbVaru.TextChanged += DefaultHandler;
            }

            /// <summary>
            /// Инициализация представления
            /// </summary>
            /// <param name="testGrid">Сетка для размещения элементов ввода</param>
            protected override void Init(Grid testGrid)
            {
                Initialized = false;
                testGrid.Children.Add(lb1);
                testGrid.Children.Add(lb2);
                testGrid.Children.Add(lb3);
                testGrid.Children.Add(lb4);
                testGrid.Children.Add(lb5);
                testGrid.Children.Add(lb6);
                testGrid.Children.Add(lb7);
                testGrid.Children.Add(lb8);
                testGrid.Children.Add(chbPsdc);
                testGrid.Children.Add(udNfc);
                testGrid.Children.Add(udNf1);
                testGrid.Children.Add(udNf2);
                testGrid.Children.Add(udNf3);
                testGrid.Children.Add(udNf4);
                testGrid.Children.Add(udNfw);
                testGrid.Children.Add(tbPFt);
                testGrid.Children.Add(tbVaru);
                Initialized = true;
            }

            /// <summary>
            /// Получить введенные данные и разместить в массиве Data
            /// </summary>
            public override void Calculate()
            {
                byte[] data = new byte[8];
                for (int i = 0; i < 8; ++i)
                    data[i] = 0;

                data[0] = (byte)((udNfc.Val & 0xF) << 1);
                if (chbPsdc.Val) data[0] |= 0x20;
                data[1] = (byte)((udNf1.Val & 0xF) << 1);
                data[2] = (byte)((udNf2.Val & 0xF) << 1);
                data[3] = (byte)((udNf3.Val & 0xF) << 1);
                data[4] = (byte)((udNf4.Val & 0xF) << 1);
                data[5] = (byte)((udNfw.Val & 0xF) << 1);
                data[6] = (byte)((tbPFt.Val & 0x1F) << 1);
                data[7] = (byte)((tbVaru.Val & 0x3F) << 1);

                for (int i = 0; i < 8; ++i )
                    CheckSumCalculator.SetParityBit(ref data[i]);

                Data = data;

                // Загружаем данные в редактор команд в текстовом виде
                string s = "";
                for (int i = 0; i < 8; ++i)
                    s += String.Format("{0:X2} ", Data[i]);
                commandEditor.Text = s.Substring(0, s.Length - 1);

                // Сообщаем приемнику данных об их изменении
                DataController.DataChanged(null);
            }

            /// <summary>
            /// Информирование об изменении даных в представлении или редакторе команд
            /// </summary>
            /// <param name="param">Не используется</param>
            public void DataChanged(object param)
            {
                // Сообщаем приемнику данных об их изменении
                DataController.DataChanged(null);
            }

            /// <summary>
            /// Изменить режим
            /// </summary>
            /// <param name="mode">Код режима</param>
            public void ChangeMode(int mode)
            {
                // Убираем элементы текущего представления с окна
                testGrid.Children.Clear();

                // Инициализируем новое представление
                Init(testGrid);
                Calculate();
            }

            public override void DisplayData(byte[] data, TextBlock resultTextBlock, Action<TextBlock, byte[]> displayArray, Action<TextBlock> noDataError, Action<TextBlock> checkSumError, Action<TextBlock> packetError, object parameter) { }
            public void DisplayData(byte[] data, Action<TextBlock, byte[]> displayArray, Action<TextBlock> noDataError, Action<TextBlock> checkSumError, Action<TextBlock> packetError) { }
        }

        /// <summary>
        /// Контроллер данных ШД21
        /// </summary>
        class SD21DataController : IDateSource, IDataReceiver
        {
            /// <summary>
            /// Приемник данных
            /// </summary>
            private IDataReceiver dataReceiver;

            /// <summary>
            /// Список представлений данных для всех режимов работы
            /// </summary>
            private DataView[] dataViewList;

            /// <summary>
            /// Текущее представление данных
            /// </summary>
            private DataView currentView;

            /// <summary>
            /// Сетка для размещения элементовв ввода
            /// </summary>
            private Grid testGrid;

            /// <summary>
            /// Редактор для ручного ввода массива данных
            /// </summary>
            private CommandEditor commandEditor;

            /// <summary>
            /// Радиокнопка для выбора между ручным и упрощенным способом ввода данных
            /// </summary>
            private RadioButton rbSelectData, rbInputData;

            /// <summary>
            /// Получить данные
            /// </summary>
            /// <returns>Возвращает массив данных для передачи по ШД21</returns>
            public byte[] GetData()
            {
                // Если выбран упрощенный режим ввода данных, то получаем данные с объекта-представления
                if (rbSelectData.IsChecked ?? false) return (currentView == null ? null : currentView.Data);
                // Если выбран ручной ввод данных, то получаем данные с редактора команд
                else return commandEditor.GetData();
            }

            /// <summary>
            /// Конструктор
            /// </summary>
            /// <param name="dataReceiver">Приемник данных</param>
            /// <param name="dataViewList">Список представлений данных</param>
            /// <param name="testGrid">Сетка для размещения элементовв ввода</param>
            /// <param name="commandEditor">Редактор для ручного ввода массива данных</param>
            /// <param name="rbSelectData">Радиокнопка для выбора упрощенного режима ввода данных</param>
            /// <param name="rbInputData">РАдиокнопка для выбора ручного режима ввода данных</param>
            public SD21DataController(IDataReceiver dataReceiver, DataView[] dataViewList, Grid testGrid, MaskedTextBox commandEditor, RadioButton rbSelectData, RadioButton rbInputData)
            {
                currentView = null;

                // Сохраняем данные
                this.dataReceiver = dataReceiver;
                this.dataViewList = dataViewList;
                this.testGrid = testGrid;
                this.commandEditor = new CommandEditor(commandEditor, 20, 18);
                this.rbSelectData = rbSelectData;
                this.rbInputData = rbInputData;

                // Для редактора команд и представлений данных устанавливаем приемник данных
                this.commandEditor.DataController = this;
                foreach (DataView dv in dataViewList)
                    if (dv != null) dv.DataController = this;

                // При изменении режима ввода блокируем неиспользуемые элементы в окне
                RoutedEventHandler inputModeLS21 = (sender, e) =>
                {
                    if (sender == rbSelectData)
                    {
                        testGrid.IsEnabled = true;
                        this.commandEditor.IsEnabled = false;
                        if (currentView != null) currentView.Calculate();
                    }
                    else
                    {
                        testGrid.IsEnabled = false;
                        this.commandEditor.IsEnabled = true;
                    }
                };
                rbSelectData.Checked += inputModeLS21;
                rbInputData.Checked += inputModeLS21;
            }

            /// <summary>
            /// Изменить режим работы
            /// </summary>
            /// <param name="mode">Код режима</param>
            public void ChangeMode(int mode)
            {
                // Выбираем представление, соответствующее новому режиму
                currentView = dataViewList[mode];

                // Устанавливаем размер массива данных
                int dataLength, checkSumPos;
                currentView.GetDataLength(out dataLength, out checkSumPos);
                commandEditor.SetLength(dataLength, checkSumPos);

                // Убираем элементы текущего представления с окна
                testGrid.Children.Clear();

                // Инициализируем новое представление
                if (currentView != null)
                {
                    currentView.InitView(testGrid);
                    currentView.Calculate();

                    if (currentView.IsNull)
                    {
                        rbSelectData.IsEnabled = false;
                        rbInputData.IsEnabled = false;
                        commandEditor.IsEnabled = false;
                    }
                    else
                    {
                        rbSelectData.IsEnabled = true;
                        rbInputData.IsEnabled = true;
                        commandEditor.IsEnabled = !rbSelectData.IsChecked ?? false;
                    }
                }
            }

            /// <summary>
            /// Информирование об изменении даных в представлении или редакторе команд
            /// </summary>
            /// <param name="param">Новые данные в тектовом виде (если метод вызван представлением)</param>
            public void DataChanged(object param)
            {
                // Если получены данные в текстовом виде, то загружаем их в редактор команд
                if (param != null) commandEditor.Text = (string)param;
                // Сообщаем приемнику данных об их изменении
                dataReceiver.DataChanged(null);
            }

            public void DisplayData(byte[] data, Action<TextBlock, byte[]> displayArray, Action<TextBlock> noDataError, Action<TextBlock> checkSumError, Action<TextBlock> packetError) { }
        }

        /// <summary>
        /// Представление данных при их отсутствии
        /// </summary>
        class NULL_DataView : DataView
        {
            private Label lb1;

            /// <summary>
            /// Признак отсутствия данных в представлении
            /// </summary>
            public override bool IsNull { get { return true; } }

            /// <summary>
            /// Конструктор
            /// </summary>
            public NULL_DataView()
            {
                Data = new byte[0];
                lastDisplayedData = null;

                lb1 = new Label();
                lb1.Content = "<НЕТ ДАННЫХ>";
                lb1.Margin = new Thickness(10, 10, 0, 0);
            }

            /// <summary>
            /// Инициализация представления
            /// </summary>
            /// <param name="testGrid">Сетка в которой необходимо поместить все элементы управления</param>
            protected override void Init(Grid testGrid)
            {
                Initialized = false;
                testGrid.Children.Add(lb1);
                Initialized = true;
            }

            /// <summary>
            /// Получить введенные данные и разместить в массиве Data
            /// </summary>
            public override void Calculate() {}

            /// <summary>
            /// Вывести данные в окно
            /// </summary>
            /// <param name="progData">Массив данных</param>
            /// <param name="resultTextBlock">Текстовый элемент в котором необходимо вывести данные</param>
            /// <param name="displayArray">Вывод массива данных в указанный TextBlock</param>
            /// <param name="noDataError">Вывод сообщения об отсутствии даннных в указанный TextBlock</param>
            /// <param name="checkSumError">Вывод сообщения об ошибке контрольной суммы в указанный TextBlock</param>
            /// <param name="packetError">Вывод сообщения об ошибке пакета в указанный TextBlock</param>
            public override void DisplayData(byte[] data, TextBlock resultTextBlock, Action<TextBlock, byte[]> displayArray, Action<TextBlock> noDataError, Action<TextBlock> checkSumError, Action<TextBlock> packetError, object parameter)
            {
                // Выход, если получены данные, которые уже были выведены
                if ((lastDisplayedData != null) && (data != null) && (lastDisplayedData.Length == data.Length))
                {
                    bool equal = true;
                    for (int i = 0; i < data.Length; ++i)
                        if (lastDisplayedData[i] != data[i])
                        {
                            equal = false;
                            break;
                        }
                    if (equal) return;
                }
                lastDisplayedData = data;

                // Проверяем наличие данных
                if ((data == null) || (data.Length <= 0))
                {
                    resultTextBlock.Inlines.Clear();
                    resultTextBlock.Inlines.Add(new Run("Нет данных") { FontWeight = FontWeights.Bold });
                    resultTextBlock.Inlines.Add(Environment.NewLine + Environment.NewLine + "Получение данных не предусмотрено");
                }
                // Проверяем длину пакета
                else
                {
                    packetError(resultTextBlock);
                    resultTextBlock.Inlines.Add(Environment.NewLine);
                    displayArray(resultTextBlock, data);
                    resultTextBlock.Inlines.Add(Environment.NewLine + Environment.NewLine + "Получение данных не предусмотрено");
                }
            }

            /// <summary>
            /// Получить размер массива данных и позицию контрольной суммы
            /// </summary>
            /// <param name="length">Размер массиваданных</param>
            /// <param name="checkSumPos">Позиция контрольной суммы (начиная с нулевой)</param>
            public override void GetDataLength(out int length, out int checkSumPos)
            {
                length = 0;
                checkSumPos = -1;
            }
        }

        class LS21_ZAPRANG_DataView : DataView
        {
            private Label lb1;

            /// <summary>
            /// Признак отсутствия данных в представлении
            /// </summary>
            public override bool IsNull { get { return true; } }

            /// <summary>
            /// Конструктор
            /// </summary>
            public LS21_ZAPRANG_DataView()
            {
                Data = new byte[0];
                lastDisplayedData = null;

                lb1 = new Label();
                lb1.Content = "<НЕТ ДАННЫХ>";
                lb1.Margin = new Thickness(10, 10, 0, 0);
            }

            /// <summary>
            /// Инициализация представления
            /// </summary>
            /// <param name="testGrid">Сетка в которой необходимо поместить все элементы управления</param>
            protected override void Init(Grid testGrid)
            {
                Initialized = false;
                testGrid.Children.Add(lb1);
                Initialized = true;
            }
            /// <summary>
            /// Получить введенные данные и разместить в массиве Data
            /// </summary>
            public override void Calculate()
            {
                byte[] data = new byte[14];
                for (int i = 0; i < 14; ++i)
                    data[i] = 0;

                data[0] = 0x47;
                data[1] = 0;
                data[2] = 0;
                data[3] = 0;
                data[4] = 0;
                data[5] = 0;
                data[7] = 0;
                data[8] = 0;
                data[9] = 0;
                data[10] = 0;
                data[11] = 0;
                data[12] = 0;
                data[13] = CheckSumCalculator.Calculate(data, 0, data.Length - 1);

                Data = data;

                if (DataController != null)
                {
                    string s = "";
                    for (int i = 0; i < 13; ++i)
                        s += String.Format("{0:X2} ", Data[i]);
                    s += "КС";

                    DataController.DataChanged(s);
                }
            }

            /// <summary>
            /// Вывести данные в окно
            /// </summary>
            /// <param name="progData">Массив данных</param>
            /// <param name="resultTextBlock">Текстовый элемент в котором необходимо вывести данные</param>
            /// <param name="displayArray">Вывод массива данных в указанный TextBlock</param>
            /// <param name="noDataError">Вывод сообщения об отсутствии даннных в указанный TextBlock</param>
            /// <param name="checkSumError">Вывод сообщения об ошибке контрольной суммы в указанный TextBlock</param>
            /// <param name="packetError">Вывод сообщения об ошибке пакета в указанный TextBlock</param>
            public override void DisplayData(byte[] data, TextBlock resultTextBlock, Action<TextBlock, byte[]> displayArray, Action<TextBlock> noDataError, Action<TextBlock> checkSumError, Action<TextBlock> packetError, object parameter)
            {
                // Выход, если получены данные, которые уже были выведены
                if ((lastDisplayedData != null) && (data != null) && (lastDisplayedData.Length == data.Length))
                {
                    bool equal = true;
                    for (int i = 0; i < data.Length; ++i)
                        if (lastDisplayedData[i] != data[i])
                        {
                            equal = false;
                            break;
                        }
                    if (equal) return;
                }
                lastDisplayedData = data;

                // Проверяем наличие данных
                if ((data == null) || (data.Length <= 0))
                {
                    resultTextBlock.Inlines.Clear();
                    resultTextBlock.Inlines.Add(new Run("Нет данных") { FontWeight = FontWeights.Bold });
                    resultTextBlock.Inlines.Add(Environment.NewLine + Environment.NewLine + "Получение данных не предусмотрено");
                }
                // Проверяем длину пакета
                else
                {
                    packetError(resultTextBlock);
                    resultTextBlock.Inlines.Add(Environment.NewLine);
                    displayArray(resultTextBlock, data);
                    resultTextBlock.Inlines.Add(Environment.NewLine + Environment.NewLine + "Получение данных не предусмотрено");
                }
            }

            /// <summary>
            /// Получить размер массива данных и позицию контрольной суммы
            /// </summary>
            /// <param name="length">Размер массиваданных</param>
            /// <param name="checkSumPos">Позиция контрольной суммы (начиная с нулевой)</param>
            public override void GetDataLength(out int length, out int checkSumPos)
            {
                length = 0;
                checkSumPos = -1;
            }

        }

        class LS21_READRANG_DataView : DataView
        {
            private Label lb1;

            /// <summary>
            /// Признак отсутствия данных в представлении
            /// </summary>
            public override bool IsNull { get { return true; } }

            /// <summary>
            /// Конструктор
            /// </summary>
            public LS21_READRANG_DataView()
            {
                Data = new byte[0];
                lastDisplayedData = null;

                lb1 = new Label();
                lb1.Content = "<НЕТ ДАННЫХ>";
                lb1.Margin = new Thickness(10, 10, 0, 0);
            }

            /// <summary>
            /// Инициализация представления
            /// </summary>
            /// <param name="testGrid">Сетка в которой необходимо поместить все элементы управления</param>
            protected override void Init(Grid testGrid)
            {
                Initialized = false;
                testGrid.Children.Add(lb1);
                Initialized = true;
            }
            /// <summary>
            /// Получить введенные данные и разместить в массиве Data
            /// </summary>
            public override void Calculate()
            {
                byte[] data = new byte[14];
                for (int i = 0; i < 14; ++i)
                    data[i] = 0;

                data[0] = 0x48;
                data[1] = 0;
                data[2] = 0;
                data[3] = 0;
                data[4] = 0;
                data[5] = 0;
                data[7] = 0;
                data[8] = 0;
                data[9] = 0;
                data[10] = 0;
                data[11] = 0;
                data[12] = 0;
                data[13] = CheckSumCalculator.Calculate(data, 0, data.Length - 1);

                Data = data;

                if (DataController != null)
                {
                    string s = "";
                    for (int i = 0; i < 13; ++i)
                        s += String.Format("{0:X2} ", Data[i]);
                    s += "КС";

                    DataController.DataChanged(s);
                }
            }

            /// <summary>
            /// Вывести данные в окно
            /// </summary>
            /// <param name="progData">Массив данных</param>
            /// <param name="resultTextBlock">Текстовый элемент в котором необходимо вывести данные</param>
            /// <param name="displayArray">Вывод массива данных в указанный TextBlock</param>
            /// <param name="noDataError">Вывод сообщения об отсутствии даннных в указанный TextBlock</param>
            /// <param name="checkSumError">Вывод сообщения об ошибке контрольной суммы в указанный TextBlock</param>
            /// <param name="packetError">Вывод сообщения об ошибке пакета в указанный TextBlock</param>
            public override void DisplayData(byte[] data, TextBlock resultTextBlock, Action<TextBlock, byte[]> displayArray, Action<TextBlock> noDataError, Action<TextBlock> checkSumError, Action<TextBlock> packetError, object parameter)
            {
                // Выход, если получены данные, которые уже были выведены
                if ((lastDisplayedData != null) && (data != null) && (lastDisplayedData.Length == data.Length))
                {
                    bool equal = true;
                    for (int i = 0; i < data.Length; ++i)
                        if (lastDisplayedData[i] != data[i])
                        {
                            equal = false;
                            break;
                        }
                    if (equal) return;
                }
                lastDisplayedData = data;

                // Проверяем наличие данных
                if ((data == null) || (data.Length <= 0))
                {
                    resultTextBlock.Inlines.Clear();
                    resultTextBlock.Inlines.Add(new Run("Нет данных") { FontWeight = FontWeights.Bold });
                    resultTextBlock.Inlines.Add(Environment.NewLine + Environment.NewLine + "Получение данных не предусмотрено");
                }
                // Проверяем длину пакета
                else
                {
                    packetError(resultTextBlock);
                    resultTextBlock.Inlines.Add(Environment.NewLine);
                    displayArray(resultTextBlock, data);
                    resultTextBlock.Inlines.Add(Environment.NewLine + Environment.NewLine + "Получение данных не предусмотрено");
                }
            }

            /// <summary>
            /// Получить размер массива данных и позицию контрольной суммы
            /// </summary>
            /// <param name="length">Размер массиваданных</param>
            /// <param name="checkSumPos">Позиция контрольной суммы (начиная с нулевой)</param>
            public override void GetDataLength(out int length, out int checkSumPos)
            {
                length = 0;
                checkSumPos = -1;
            }

        }

        /// <summary>
        /// Представление данных ЛС21 в режиме БР 
        /// </summary>
        class LS21_BR_DataView : DataView
        {
            private Label lb1, lb2, lb3, lb4, lb5, lb6, lb7, lb8, lb9, lb10, lb11, lb12, lb13, lb14, lb15;
            private TwoDigitUpDown udImsr, udJmsr, udIcorr, udJcorr, udNf1, udNf2, udNf3, udNf4, udNFcorr;
            private BinaryMaskedTextBox tbFcorr, tbAcorr, tbCosX, tbCosY, tbFmsr;
            private SimpleCheckBox chbPobr, chbPobrCorr, chbExc02, chbLoc17, chbPF;
            private ImitComboBox cbImit;
            private N8SygnalsView n8sygnals;

            /// <summary>
            /// Конструктор
            /// </summary>
            /// <param name="n8sygnals">Представление сигналов Н8</param>
            public LS21_BR_DataView(N8SygnalsView n8sygnals)
            {
                this.n8sygnals = n8sygnals;

                Data = new byte[14];
                lastDisplayedData = null;

                lb1 = new Label();
                lb1.Content = "Столбец изм.";
                lb1.Margin = new Thickness(10, 10, 0, 0);
                udImsr = new TwoDigitUpDown(1, 88, 1);
                udImsr.Margin = new Thickness(120, 10, 0, 0);

                lb2 = new Label();
                lb2.Content = "Строка изм.";
                lb2.Margin = new Thickness(190, 10, 0, 0);
                udJmsr = new TwoDigitUpDown(1, 44, 1);
                udJmsr.Margin = new Thickness(300, 10, 0, 0);

                lb3 = new Label();
                lb3.Content = "Столбец попр.";
                lb3.Margin = new Thickness(10, 36, 0, 0);
                udIcorr = new TwoDigitUpDown(1, 88, 1);
                udIcorr.Margin = new Thickness(120, 36, 0, 0);

                lb4 = new Label();
                lb4.Content = "Строка попр.";
                lb4.Margin = new Thickness(190, 36, 0, 0);
                udJcorr = new TwoDigitUpDown(1, 44, 1);
                udJcorr.Margin = new Thickness(300, 36, 0, 0);

                lb5 = new Label();
                lb5.Content = "Попр. фазы УИ";
                lb5.Margin = new Thickness(10, 62, 0, 0);
                tbFcorr = new BinaryMaskedTextBox(5);
                tbFcorr.Margin = new Thickness(120, 62, 0, 0);

                lb6 = new Label();
                lb6.Content = "Попр. ампл. УИ";
                lb6.Margin = new Thickness(190, 62, 0, 0);
                tbAcorr = new BinaryMaskedTextBox(3);
                tbAcorr.Margin = new Thickness(300, 62, 0, 0);

                lb7 = new Label();
                lb7.Content = "NF1";
                lb7.Margin = new Thickness(10, 88, 0, 0);
                udNf1 = new TwoDigitUpDown(0, 15, 0);
                udNf1.Margin = new Thickness(120, 88, 0, 0);

                lb8 = new Label();
                lb8.Content = "NF2";
                lb8.Margin = new Thickness(190, 88, 0, 0);
                udNf2 = new TwoDigitUpDown(0, 15, 0);
                udNf2.Margin = new Thickness(300, 88, 0, 0);

                lb9 = new Label();
                lb9.Content = "NF3";
                lb9.Margin = new Thickness(10, 114, 0, 0);
                udNf3 = new TwoDigitUpDown(0, 15, 0);
                udNf3.Margin = new Thickness(120, 114, 0, 0);

                lb10 = new Label();
                lb10.Content = "NF4";
                lb10.Margin = new Thickness(190, 114, 0, 0);
                udNf4 = new TwoDigitUpDown(0, 15, 0);
                udNf4.Margin = new Thickness(300, 114, 0, 0);

                lb11 = new Label();
                lb11.Content = "Cos X";
                lb11.Margin = new Thickness(10, 140, 0, 0);
                tbCosX = new BinaryMaskedTextBox(13);
                tbCosX.Margin = new Thickness(120, 140, 0, 0);

                lb12 = new Label();
                lb12.Content = "Cos Y";
                lb12.Margin = new Thickness(10, 166, 0, 0);
                tbCosY = new BinaryMaskedTextBox(13);
                tbCosY.Margin = new Thickness(120, 166, 0, 0);

                chbPobr = new SimpleCheckBox("Побр");
                chbPobr.Margin = new Thickness(260, 140 + 5, 0, 0);

                chbPobrCorr = new SimpleCheckBox("Побр попр");
                chbPobrCorr.Margin = new Thickness(260, 166 + 5, 0, 0);

                chbExc02 = new SimpleCheckBox("Снять возб 02");
                chbExc02.Margin = new Thickness(10 + 5, 192 + 5, 0, 0);

                chbLoc17 = new SimpleCheckBox("Мест 17");
                chbLoc17.Margin = new Thickness(120, 192 + 5, 0, 0);

                chbPF = new SimpleCheckBox("ПF");
                chbPF.Margin = new Thickness(260, 192 + 5, 0, 0);

                lb13 = new Label();
                lb13.Content = "Имит";
                lb13.Margin = new Thickness(10, 218, 0, 0);
                cbImit = new ImitComboBox();
                cbImit.Margin = new Thickness(120, 218, 0, 0);

                lb14 = new Label();
                lb14.Content = "Фаза УИ изм О";
                lb14.Margin = new Thickness(10, 244, 0, 0);
                tbFmsr = new BinaryMaskedTextBox(4);
                tbFmsr.Margin = new Thickness(120, 244, 0, 0);

                lb15 = new Label();
                lb15.Content = "NF поправки";
                lb15.Margin = new Thickness(190, 244, 0, 0);
                udNFcorr = new TwoDigitUpDown(0, 15, 0);
                udNFcorr.Margin = new Thickness(300, 244, 0, 0);

                n8sygnals.addImitComboBox(cbImit);

                udImsr.ValueChanged += DefaultHandler;
                udJmsr.ValueChanged += DefaultHandler;
                udIcorr.ValueChanged += DefaultHandler;
                udJcorr.ValueChanged += DefaultHandler;
                udNf1.ValueChanged += DefaultHandler;
                udNf2.ValueChanged += DefaultHandler;
                udNf3.ValueChanged += DefaultHandler;
                udNf4.ValueChanged += DefaultHandler;
                udNFcorr.ValueChanged += DefaultHandler;
                tbFcorr.TextChanged += DefaultHandler;
                tbAcorr.TextChanged += DefaultHandler;
                tbCosX.TextChanged += DefaultHandler;
                tbCosY.TextChanged += DefaultHandler;
                tbFmsr.TextChanged += DefaultHandler;
                chbPobr.Click += DefaultHandler;
                chbPobrCorr.Click += DefaultHandler;
                chbExc02.Click += DefaultHandler;
                chbLoc17.Click += DefaultHandler;
                chbPF.Click += DefaultHandler;
                cbImit.SelectionChanged += DefaultHandler;
            }

            /// <summary>
            /// Инициализация представления
            /// </summary>
            /// <param name="testGrid">Сетка в которой необходимо поместить все элементы управления</param>
            protected override void Init(Grid testGrid)
            {
                Initialized = false;
                testGrid.Children.Add(lb1);
                testGrid.Children.Add(lb2);
                testGrid.Children.Add(lb3);
                testGrid.Children.Add(lb4);
                testGrid.Children.Add(lb5);
                testGrid.Children.Add(lb6);
                testGrid.Children.Add(lb7);
                testGrid.Children.Add(lb8);
                testGrid.Children.Add(lb9);
                testGrid.Children.Add(lb10);
                testGrid.Children.Add(lb11);
                testGrid.Children.Add(lb12);
                testGrid.Children.Add(lb13);
                testGrid.Children.Add(lb14);
                testGrid.Children.Add(lb15);
                testGrid.Children.Add(udImsr);
                testGrid.Children.Add(udJmsr);
                testGrid.Children.Add(udIcorr);
                testGrid.Children.Add(udJcorr);
                testGrid.Children.Add(tbFcorr);
                testGrid.Children.Add(tbAcorr);
                testGrid.Children.Add(udNf1);
                testGrid.Children.Add(udNf2);
                testGrid.Children.Add(udNf3);
                testGrid.Children.Add(udNf4);
                testGrid.Children.Add(tbCosX);
                testGrid.Children.Add(tbCosY);
                testGrid.Children.Add(chbPobr);
                testGrid.Children.Add(chbPobrCorr);
                testGrid.Children.Add(chbExc02);
                testGrid.Children.Add(chbLoc17);
                testGrid.Children.Add(chbPF);
                testGrid.Children.Add(cbImit);
                testGrid.Children.Add(tbFmsr);
                testGrid.Children.Add(udNFcorr);
                Initialized = true;
            }

            /// <summary>
            /// Получить введенные данные и разместить в массиве Data
            /// </summary>
            public override void Calculate()
            {
                byte[] data = new byte[14];
                for (int i = 0; i < 14; ++i)
                    data[i] = 0;

                data[0] = 0x10;
                data[0] |= (byte)(udJmsr.Val & 0xF);
                data[1] = (byte)(udImsr.Val & 0x7F);
                data[2] = (byte)((udJcorr.Val & 0x3F) | ((udJmsr.Val << 2) & 0xC0));
                data[3] = (byte)(udIcorr.Val & 0x7F);
                data[4] = (byte)((tbAcorr.Val & 0x7) | ((tbFcorr.Val & 0x1F) << 3));
                data[5] = (byte)((udNf2.Val & 0xF) | ((udNf1.Val & 0xF) << 4));
                data[6] = (byte)((udNf4.Val & 0xF) | ((udNf3.Val & 0xF) << 4));
                data[7] = (byte)((tbCosX.Val & 0x1F) << 3);
                if (chbPobrCorr.Val) data[7] |= 1;
                data[8] = (byte)(tbCosX.Val >> 5);
                data[9] = (byte)((cbImit.Val & 7) << 2);
                if (chbPF.Val) data[9] |= 1;
                if (chbLoc17.Val) data[9] |= 2;
                if (chbExc02.Val) data[9] |= 0x20;
                data[10] = (byte)((tbCosY.Val & 0x1F) << 3);
                if (chbPobr.Val) data[10] |= 1;
                data[11] = (byte)(tbCosY.Val >> 5);
                data[12] = (byte)((udNFcorr.Val & 0xF) | ((tbFmsr.Val & 0xF) << 4));
                data[13] = CheckSumCalculator.Calculate(data, 0, data.Length - 1);

                Data = data;

                if (DataController != null)
                {
                    string s = "";
                    for (int i = 0; i < 13; ++i)
                        s += String.Format("{0:X2} ", Data[i]);
                    s += "КС";

                    DataController.DataChanged(s);
                }
            }

            /// <summary>
            /// Вывести данные в окно
            /// </summary>
            /// <param name="progData">Массив данных</param>
            /// <param name="resultTextBlock">Текстовый элемент в котором необходимо вывести данные</param>
            /// <param name="displayArray">Вывод массива данных в указанный TextBlock</param>
            /// <param name="noDataError">Вывод сообщения об отсутствии даннных в указанный TextBlock</param>
            /// <param name="checkSumError">Вывод сообщения об ошибке контрольной суммы в указанный TextBlock</param>
            /// <param name="packetError">Вывод сообщения об ошибке пакета в указанный TextBlock</param>
            public override void DisplayData(byte[] data, TextBlock resultTextBlock, Action<TextBlock, byte[]> displayArray, Action<TextBlock> noDataError, Action<TextBlock> checkSumError, Action<TextBlock> packetError, object parameter)
            {
                if (n8sygnals.ImitCounter != 0) { --n8sygnals.ImitCounter; }

                // Выход, если получены данные, которые уже были выведены
                if ((lastDisplayedData != null) && (data != null) && (lastDisplayedData.Length == data.Length))
                {
                    bool equal = true;
                    for (int i = 0; i < data.Length; ++i)
                        if (lastDisplayedData[i] != data[i])
                        {
                            equal = false;
                            break;
                        }
                    if (equal) return;
                }
                lastDisplayedData = data;

                // Проверяем наличие данных
                if ((data == null) || (data.Length <= 0))
                {
                    noDataError(resultTextBlock);
                    if (parameter != null) ((TextBlock)parameter).Text = "";
                }
                // Проверяем длину пакета
                else if (data.Length != 14)
                {
                    packetError(resultTextBlock);
                    resultTextBlock.Inlines.Add(Environment.NewLine);
                    displayArray(resultTextBlock, data);
                    resultTextBlock.Inlines.Add(Environment.NewLine + Environment.NewLine + "Ожидаемая длина пакета - 14 байт");
                    if (parameter != null) ((TextBlock)parameter).Text = "";
                }
                else
                {
                    if (parameter != null)
                    {
                        ((TextBlock)parameter).Text = "НР21.10.02ОК: ";
                        ((TextBlock)parameter).Inlines.Add(new Run(Convert.ToString(data[0], 2).PadLeft(8, '0') + 'b') { FontFamily = new FontFamily("Courier New"), FontWeight = FontWeights.Bold });

                        //((TextBlock)parameter).Text = "Алексанрд завис в разах: ";
                        //((TextBlock)parameter).Inlines.Add(new Run(Convert.ToString(data[0], 2).PadLeft(8, '0') + 'b') { FontFamily = new FontFamily("Courier New"), FontWeight = FontWeights.Bold });
                    }

                    resultTextBlock.Inlines.Clear();
                    displayArray(resultTextBlock, data);

                    resultTextBlock.Inlines.Add(Environment.NewLine);
                    resultTextBlock.Inlines.Add(Environment.NewLine + "Имит вкл.\t\t= ");
                    resultTextBlock.Inlines.Add(new Run(((data[0] >> 7) & 1).ToString()) { FontFamily = new FontFamily("Courier New"), FontWeight = FontWeights.Bold } );
                    resultTextBlock.Inlines.Add(Environment.NewLine + "Контроль Н6.21\t\t= ");
                    resultTextBlock.Inlines.Add(new Run(Convert.ToString(data[0] & 0x7F, 2).PadLeft(7, '0') + 'b') { FontFamily = new FontFamily("Courier New"), FontWeight = FontWeights.Bold });
                    resultTextBlock.Inlines.Add(Environment.NewLine + "Пилот сигнал О X\t= ");
                    resultTextBlock.Inlines.Add(new Run(String.Format("{0:X4}h", data[1] | (data[2] << 8))) { FontFamily = new FontFamily("Courier New"), FontWeight = FontWeights.Bold });
                    resultTextBlock.Inlines.Add(Environment.NewLine + "Пилот сигнал О Y\t\t= ");
                    resultTextBlock.Inlines.Add(new Run(String.Format("{0:X4}h", data[3] | (data[4] << 8))) { FontFamily = new FontFamily("Courier New"), FontWeight = FontWeights.Bold });
                    resultTextBlock.Inlines.Add(Environment.NewLine + "Пел напр\t\t= ");
                    resultTextBlock.Inlines.Add(new Run(Convert.ToString(data[5] & 0x3F, 2).PadLeft(6, '0') + 'b') { FontFamily = new FontFamily("Courier New"), FontWeight = FontWeights.Bold });
                    resultTextBlock.Inlines.Add(Environment.NewLine + "NFц\t\t\t= ");
                    resultTextBlock.Inlines.Add(new Run((data[6] & 0x3F).ToString()) { FontFamily = new FontFamily("Courier New"), FontWeight = FontWeights.Bold });
                    resultTextBlock.Inlines.Add(Environment.NewLine + "КС инф. Н6.21.10\t\t= ");
                    resultTextBlock.Inlines.Add(new Run(String.Format("{0:X2}h", data[7])) { FontFamily = new FontFamily("Courier New"), FontWeight = FontWeights.Bold });
                    //кс режима бр, сумма без НФц с 2лс, 1902 договорились что частота в кс не участвует тк и так защищена контрольным разрядом
                    if(data[7] == CheckSumCalculator.Calculate(data, 0, 6))
                    resultTextBlock.Inlines.Add(" (верно)");
                    else resultTextBlock.Inlines.Add(new Run(" (не верно)") { Foreground = Brushes.Red });
                    resultTextBlock.Inlines.Add(Environment.NewLine + "Слово сост. ППМ изм\t= ");
                    resultTextBlock.Inlines.Add(new Run(Convert.ToString(data[12], 2).PadLeft(8, '0') + 'b') { FontFamily = new FontFamily("Courier New"), FontWeight = FontWeights.Bold });
                    resultTextBlock.Inlines.Add(Environment.NewLine + "КС ППМ изм\t\t= ");
                    resultTextBlock.Inlines.Add(new Run(String.Format("{0:X2}h", data[13])) { FontFamily = new FontFamily("Courier New"), FontWeight = FontWeights.Bold });
                    if (data[12] == data[13])
                        resultTextBlock.Inlines.Add(" (верно)");
                    else resultTextBlock.Inlines.Add(new Run(" (не верно)") { Foreground = Brushes.Red });

                    // Переход в режим "Имит 400" при получении флага "Имит вкл."
                    if(((data[0] & 0x80) != 0) && (cbImit.SelectedIndex == 0) && (n8sygnals.ImitCounter == 0))
                    {
                        n8sygnals.goImit();
                    }
                }
            }
        }

        /// <summary>
        /// Представление данных ЛС21 в режиме ФК
        /// </summary>
        class LS21_FC_DataView : DataView
        {
            private Label lb1, lb2, lb3, lb4, lb5, lb6, lb7, lb8, lb9, lb10, lb11;
            private TwoDigitUpDown udImsr, udJmsr, udIcorr, udJcorr, udNChMsr, udNFcorr;
            private BinaryMaskedTextBox tbFcorr, tbAcorr, tbFmsr, tbTest;
            private SimpleCheckBox chbTRMsr, chbPobrMsr, chbTRCorr, chbPobrCorr, chbExc02, chbLoc17, chbPF;
            private ImitComboBox cbImit;
            private N8SygnalsView n8sygnals;

            /// <summary>
            /// Конструктор
            /// </summary>
            /// <param name="n8sygnals">Представление сигналов Н8</param>
            public LS21_FC_DataView(N8SygnalsView n8sygnals)
            {
                this.n8sygnals = n8sygnals;

                Data = new byte[14];
                lastDisplayedData = null;

                lb1 = new Label();
                lb1.Content = "Столбец изм.";
                lb1.Margin = new Thickness(10, 10, 0, 0);
                udImsr = new TwoDigitUpDown(1, 88, 1);
                udImsr.Margin = new Thickness(120, 10, 0, 0);

                lb2 = new Label();
                lb2.Content = "Строка изм.";
                lb2.Margin = new Thickness(190, 10, 0, 0);
                udJmsr = new TwoDigitUpDown(1, 44, 1);
                udJmsr.Margin = new Thickness(300, 10, 0, 0);

                lb3 = new Label();
                lb3.Content = "Столбец попр.";
                lb3.Margin = new Thickness(10, 36, 0, 0);
                udIcorr = new TwoDigitUpDown(1, 88, 1);
                udIcorr.Margin = new Thickness(120, 36, 0, 0);

                lb4 = new Label();
                lb4.Content = "Строка попр.";
                lb4.Margin = new Thickness(190, 36, 0, 0);
                udJcorr = new TwoDigitUpDown(1, 44, 1);
                udJcorr.Margin = new Thickness(300, 36, 0, 0);

                lb5 = new Label();
                lb5.Content = "Попр. фазы УИ";
                lb5.Margin = new Thickness(10, 62, 0, 0);
                tbFcorr = new BinaryMaskedTextBox(5);
                tbFcorr.Margin = new Thickness(120, 62, 0, 0);

                lb6 = new Label();
                lb6.Content = "Попр. ампл. УИ";
                lb6.Margin = new Thickness(190, 62, 0, 0);
                tbAcorr = new BinaryMaskedTextBox(3);
                tbAcorr.Margin = new Thickness(300, 62, 0, 0);

                chbTRMsr = new SimpleCheckBox("Прм-прд изм");
                chbTRMsr.Margin = new Thickness(10 + 5, 88 + 5, 0, 0);

                chbPobrMsr = new SimpleCheckBox("Побр изм");
                chbPobrMsr.Margin = new Thickness(120 + 5, 88 + 5, 0, 0);

                lb7 = new Label();
                lb7.Content = "№ канала изм";
                lb7.Margin = new Thickness(210, 88, 0, 0);
                udNChMsr = new TwoDigitUpDown(0, 4, 0);
                udNChMsr.Margin = new Thickness(300, 88, 0, 0);

                chbTRCorr = new SimpleCheckBox("Прм-прд попр");
                chbTRCorr.Margin = new Thickness(10 + 5, 114 + 5, 0, 0);

                chbPobrCorr = new SimpleCheckBox("Побр попр");
                chbPobrCorr.Margin = new Thickness(120 + 5, 114 + 5, 0, 0);

                lb8 = new Label();
                lb8.Content = "Тест обмена";
                lb8.Margin = new Thickness(10, 140, 0, 0);
                tbTest = new BinaryMaskedTextBox(8);
                tbTest.Margin = new Thickness(120, 140, 0, 0);

                chbExc02 = new SimpleCheckBox("Снять возб 02");
                chbExc02.Margin = new Thickness(10 + 5, 166 + 5, 0, 0);

                chbLoc17 = new SimpleCheckBox("Мест 17");
                chbLoc17.Margin = new Thickness(120, 166 + 5, 0, 0);

                chbPF = new SimpleCheckBox("ПF");
                chbPF.Margin = new Thickness(230, 166 + 5, 0, 0);

                lb9 = new Label();
                lb9.Content = "Имит";
                lb9.Margin = new Thickness(10, 192, 0, 0);
                cbImit = new ImitComboBox();
                cbImit.Margin = new Thickness(120, 192, 0, 0);

                lb10 = new Label();
                lb10.Content = "Фаза УИ изм О";
                lb10.Margin = new Thickness(10, 218, 0, 0);
                tbFmsr = new BinaryMaskedTextBox(4);
                tbFmsr.Margin = new Thickness(120, 218, 0, 0);

                lb11 = new Label();
                lb11.Content = "NF поправки";
                lb11.Margin = new Thickness(190, 218, 0, 0);
                udNFcorr = new TwoDigitUpDown(0, 15, 0);
                udNFcorr.Margin = new Thickness(300, 218, 0, 0);

                n8sygnals.addImitComboBox(cbImit);

                udImsr.ValueChanged += DefaultHandler;
                udJmsr.ValueChanged += DefaultHandler;
                udIcorr.ValueChanged += DefaultHandler;
                udJcorr.ValueChanged += DefaultHandler;
                udNChMsr.ValueChanged += DefaultHandler;
                udNFcorr.ValueChanged += DefaultHandler;
                tbFcorr.TextChanged += DefaultHandler;
                tbAcorr.TextChanged += DefaultHandler;
                tbFmsr.TextChanged += DefaultHandler;
                tbTest.TextChanged += DefaultHandler;
                chbTRMsr.Click += DefaultHandler;
                chbPobrMsr.Click += DefaultHandler;
                chbTRCorr.Click += DefaultHandler;
                chbPobrCorr.Click += DefaultHandler;
                chbExc02.Click += DefaultHandler;
                chbLoc17.Click += DefaultHandler;
                chbPF.Click += DefaultHandler;
                cbImit.SelectionChanged += DefaultHandler;
            }

            /// <summary>
            /// Инициализация представления
            /// </summary>
            /// <param name="testGrid">Сетка в которой необходимо поместить все элементы управления</param>
            protected override void Init(Grid testGrid)
            {
                Initialized = false;
                testGrid.Children.Add(lb1);
                testGrid.Children.Add(lb2);
                testGrid.Children.Add(lb3);
                testGrid.Children.Add(lb4);
                testGrid.Children.Add(lb5);
                testGrid.Children.Add(lb6);
                testGrid.Children.Add(lb7);
                testGrid.Children.Add(lb8);
                testGrid.Children.Add(lb9);
                testGrid.Children.Add(lb10);
                testGrid.Children.Add(lb11);
                testGrid.Children.Add(udImsr);
                testGrid.Children.Add(udJmsr);
                testGrid.Children.Add(udIcorr);
                testGrid.Children.Add(udJcorr);
                testGrid.Children.Add(udNChMsr);
                testGrid.Children.Add(tbFcorr);
                testGrid.Children.Add(tbAcorr);
                testGrid.Children.Add(chbTRMsr);
                testGrid.Children.Add(chbPobrMsr);
                testGrid.Children.Add(chbTRCorr);
                testGrid.Children.Add(chbPobrCorr);
                testGrid.Children.Add(tbTest);
                testGrid.Children.Add(chbExc02);
                testGrid.Children.Add(chbLoc17);
                testGrid.Children.Add(chbPF);
                testGrid.Children.Add(cbImit);
                testGrid.Children.Add(tbFmsr);
                testGrid.Children.Add(udNFcorr);
                Initialized = true;
            }

            /// <summary>
            /// Получить введенные данные и разместить в массиве Data
            /// </summary>
            public override void Calculate()
            {
                byte[] data = new byte[14];
                for (int i = 0; i < 14; ++i)
                    data[i] = 0;

                data[0] = 0x20;
                data[0] |= (byte)(udJmsr.Val & 0xF);
                data[1] = (byte)(udImsr.Val & 0x7F);
                data[2] = (byte)((udJcorr.Val & 0x3F) | ((udJmsr.Val << 2) & 0xC0));
                data[3] = (byte)(udIcorr.Val & 0x7F);
                data[4] = (byte)((tbAcorr.Val & 0x7) | ((tbFcorr.Val & 0x1F) << 3));
                data[5] = (byte)((udNChMsr.Val & 0x7) | (1 << 3));
                data[7] = 0;
                if (chbTRMsr.Val) data[7] |= 8;
                if (chbPobrMsr.Val) data[7] |= 4;
                if (chbTRCorr.Val) data[7] |= 2;
                if (chbPobrCorr.Val) data[7] |= 1;
                data[8] = (byte)(tbTest.Val & 0xFF);
                data[9] = (byte)((cbImit.Val & 7) << 2);
                if (chbPF.Val) data[9] |= 1;
                if (chbLoc17.Val) data[9] |= 2;
                if (chbExc02.Val) data[9] |= 0x20;
                data[12] = (byte)((udNFcorr.Val & 0xF) | ((tbFmsr.Val & 0xF) << 4));
                data[13] = CheckSumCalculator.Calculate(data, 0, data.Length - 1);

                Data = data;

                if (DataController != null)
                {
                    string s = "";
                    for (int i = 0; i < 13; ++i)
                        s += String.Format("{0:X2} ", Data[i]);
                    s += "КС";

                    DataController.DataChanged(s);
                }
            }

            /// <summary>
            /// Вывести данные в окно
            /// </summary>
            /// <param name="progData">Массив данных</param>
            /// <param name="resultTextBlock">Текстовый элемент в котором необходимо вывести данные</param>
            /// <param name="displayArray">Вывод массива данных в указанный TextBlock</param>
            /// <param name="noDataError">Вывод сообщения об отсутствии даннных в указанный TextBlock</param>
            /// <param name="checkSumError">Вывод сообщения об ошибке контрольной суммы в указанный TextBlock</param>
            /// <param name="packetError">Вывод сообщения об ошибке пакета в указанный TextBlock</param>
            public override void DisplayData(byte[] data, TextBlock resultTextBlock, Action<TextBlock, byte[]> displayArray, Action<TextBlock> noDataError, Action<TextBlock> checkSumError, Action<TextBlock> packetError, object parameter)
            {
                if (n8sygnals.ImitCounter != 0) { --n8sygnals.ImitCounter; }

                // Выход, если получены данные, которые уже были выведены
                if ((lastDisplayedData != null) && (data != null) && (lastDisplayedData.Length == data.Length))
                {
                    bool equal = true;
                    for (int i = 0; i < data.Length; ++i)
                        if (lastDisplayedData[i] != data[i])
                        {
                            equal = false;
                            break;
                        }
                    if (equal) return;
                }
                lastDisplayedData = data;

                // Проверяем наличие данных
                if ((data == null) || (data.Length <= 0))
                {
                    noDataError(resultTextBlock);
                    if (parameter != null) ((TextBlock)parameter).Text = "";
                }
                // Проверяем длину пакета
                else if (data.Length != 14)
                {
                    packetError(resultTextBlock);
                    resultTextBlock.Inlines.Add(Environment.NewLine);
                    displayArray(resultTextBlock, data);
                    resultTextBlock.Inlines.Add(Environment.NewLine + Environment.NewLine + "Ожидаемая длина пакета - 14 байт");
                    if (parameter != null) ((TextBlock)parameter).Text = "";
                }
                else
                {
                    if (parameter != null)
                    {
                        ((TextBlock)parameter).Text = "НР21.10.02ОК: ";
                        ((TextBlock)parameter).Inlines.Add(new Run(Convert.ToString(data[0], 2).PadLeft(8, '0') + 'b') { FontFamily = new FontFamily("Courier New"), FontWeight = FontWeights.Bold });
                    }

                    resultTextBlock.Inlines.Clear();
                    displayArray(resultTextBlock, data);

                    resultTextBlock.Inlines.Add(Environment.NewLine);
                    resultTextBlock.Inlines.Add(Environment.NewLine + "Имит вкл.\t\t= ");
                    resultTextBlock.Inlines.Add(new Run(((data[0] >> 7) & 1).ToString()) { FontFamily = new FontFamily("Courier New"), FontWeight = FontWeights.Bold });
                    resultTextBlock.Inlines.Add(Environment.NewLine + "Контроль Н6.21\t\t= ");
                    resultTextBlock.Inlines.Add(new Run(Convert.ToString(data[0] & 0x7F, 2).PadLeft(7, '0') + 'b') { FontFamily = new FontFamily("Courier New"), FontWeight = FontWeights.Bold });
                    resultTextBlock.Inlines.Add(Environment.NewLine + "NFц\t\t\t= ");
                    resultTextBlock.Inlines.Add(new Run((data[5] & 0x3F).ToString()) { FontFamily = new FontFamily("Courier New"), FontWeight = FontWeights.Bold });
                    resultTextBlock.Inlines.Add(Environment.NewLine + "КС инф. Н6.21.10\t\t= ");
                    resultTextBlock.Inlines.Add(new Run(String.Format("{0:X2}h", data[6])) { FontFamily = new FontFamily("Courier New"), FontWeight = FontWeights.Bold });
                    if (data[6] == CheckSumCalculator.Calculate(data, 0, 6))
                        resultTextBlock.Inlines.Add(" (верно)");
                    else resultTextBlock.Inlines.Add(new Run(" (не верно)") { Foreground = Brushes.Red });
                    resultTextBlock.Inlines.Add(Environment.NewLine + "Столбец изм.\t\t= ");
                    resultTextBlock.Inlines.Add(new Run(data[7].ToString()) { FontFamily = new FontFamily("Courier New"), FontWeight = FontWeights.Bold });
                    resultTextBlock.Inlines.Add(Environment.NewLine + "Строка изм.\t\t= ");
                    resultTextBlock.Inlines.Add(new Run(data[8].ToString()) { FontFamily = new FontFamily("Courier New"), FontWeight = FontWeights.Bold });
                    resultTextBlock.Inlines.Add(Environment.NewLine + "Слово сост. ППМ изм\t= ");
                    resultTextBlock.Inlines.Add(new Run(Convert.ToString(data[9], 2).PadLeft(8, '0') + 'b') { FontFamily = new FontFamily("Courier New"), FontWeight = FontWeights.Bold });
                    resultTextBlock.Inlines.Add(Environment.NewLine + "Версия прогр. ППМ изм.\t= ");
                    resultTextBlock.Inlines.Add(new Run(data[10].ToString()) { FontFamily = new FontFamily("Courier New"), FontWeight = FontWeights.Bold });
                    resultTextBlock.Inlines.Add(Environment.NewLine + "КС программы ППМ изм.\t= ");
                    resultTextBlock.Inlines.Add(new Run(String.Format("{0:X2}h", data[11])) { FontFamily = new FontFamily("Courier New"), FontWeight = FontWeights.Bold });
                    resultTextBlock.Inlines.Add(Environment.NewLine + "Данные вх. слова 9.\t= ");
                    resultTextBlock.Inlines.Add(new Run(Convert.ToString(data[12] & 0xFF, 2).PadLeft(8, '0') + 'b') { FontFamily = new FontFamily("Courier New"), FontWeight = FontWeights.Bold });
                    resultTextBlock.Inlines.Add(Environment.NewLine + "КС ППМ изм\t\t= ");
                    resultTextBlock.Inlines.Add(new Run(String.Format("{0:X2}h", data[13])) { FontFamily = new FontFamily("Courier New"), FontWeight = FontWeights.Bold });
                    if (data[13] == CheckSumCalculator.Calculate(data, 7, 6))
                        resultTextBlock.Inlines.Add(" (верно)");
                    else resultTextBlock.Inlines.Add(new Run(" (не верно)") { Foreground = Brushes.Red });

                    // Переход в режим "Имит 400" при получении флага "Имит вкл."
                    if (((data[0] & 0x80) != 0) && (cbImit.SelectedIndex == 0) && (n8sygnals.ImitCounter == 0))
                    {
                        n8sygnals.goImit();
                    }
                }
            }
        }

        /// <summary>
        /// Представление данных ЛС21 в режиме ИД, подрежим - Сдвиг ИД
        /// </summary>
        class LS21_ID_Shift_DataView : DataView
        {
            private Label lb1, lb2, lb3, lb4;
            private TwoDigitUpDown udI, udJ, udIID, udJID;

            /// <summary>
            /// Конструктор
            /// </summary>
            public LS21_ID_Shift_DataView()
            {
                Data = new byte[14];
                lastDisplayedData = null;

                lb1 = new Label();
                lb1.Content = "Столбец";
                lb1.Margin = new Thickness(10, 10, 0, 0);
                udI = new TwoDigitUpDown(1, 88, 1);
                udI.Margin = new Thickness(120, 10, 0, 0);

                lb2 = new Label();
                lb2.Content = "Строка";
                lb2.Margin = new Thickness(10, 36, 0, 0);
                udJ = new TwoDigitUpDown(0, 44, 0);
                udJ.Margin = new Thickness(120, 36, 0, 0);

                lb3 = new Label();
                lb3.Content = "Столбец ИД";
                lb3.Margin = new Thickness(10, 62, 0, 0);
                udIID = new TwoDigitUpDown(1, 88, 1);
                udIID.Margin = new Thickness(120, 62, 0, 0);

                lb4 = new Label();
                lb4.Content = "Строка ИД";
                lb4.Margin = new Thickness(10, 88, 0, 0);
                udJID = new TwoDigitUpDown(1, 44, 1);
                udJID.Margin = new Thickness(120, 88, 0, 0);

                udI.ValueChanged += DefaultHandler;
                udJ.ValueChanged += DefaultHandler;
                udIID.ValueChanged += DefaultHandler;
                udJID.ValueChanged += DefaultHandler;
            }

            /// <summary>
            /// Инициализация представления
            /// </summary>
            /// <param name="testGrid">Сетка в которой необходимо поместить все элементы управления</param>
            protected override void Init(Grid testGrid)
            {
                Initialized = false;
                testGrid.Children.Add(lb1);
                testGrid.Children.Add(lb2);
                testGrid.Children.Add(lb3);
                testGrid.Children.Add(lb4);
                testGrid.Children.Add(udI);
                testGrid.Children.Add(udJ);
                testGrid.Children.Add(udIID);
                testGrid.Children.Add(udJID);
                Initialized = true;
            }

            /// <summary>
            /// Получить введенные данные и разместить в массиве Data
            /// </summary>
            public override void Calculate()
            {
                byte[] data = new byte[14];
                for (int i = 0; i < 14; ++i)
                    data[i] = 0;

                data[0] = 0x3E;
                data[1] = (byte)udI.Val;
                data[2] = (byte)udJ.Val;
                data[3] = (byte)udIID.Val;
                data[4] = (byte)udJID.Val;
                data[13] = CheckSumCalculator.Calculate(data, 0, data.Length - 1);

                Data = data;

                if (DataController != null)
                {
                    string s = "";
                    for (int i = 0; i < 13; ++i)
                        s += String.Format("{0:X2} ", Data[i]);
                    s += "КС";

                    DataController.DataChanged(s);
                }
            }

            /// <summary>
            /// Вывести данные в окно
            /// </summary>
            /// <param name="progData">Массив данных</param>
            /// <param name="resultTextBlock">Текстовый элемент в котором необходимо вывести данные</param>
            /// <param name="displayArray">Вывод массива данных в указанный TextBlock</param>
            /// <param name="noDataError">Вывод сообщения об отсутствии даннных в указанный TextBlock</param>
            /// <param name="checkSumError">Вывод сообщения об ошибке контрольной суммы в указанный TextBlock</param>
            /// <param name="packetError">Вывод сообщения об ошибке пакета в указанный TextBlock</param>
            public override void DisplayData(byte[] data, TextBlock resultTextBlock, Action<TextBlock, byte[]> displayArray, Action<TextBlock> noDataError, Action<TextBlock> checkSumError, Action<TextBlock> packetError, object parameter)
            {
                // Выход, если получены данные, которые уже были выведены
                if ((lastDisplayedData != null) && (data != null) && (lastDisplayedData.Length == data.Length))
                {
                    bool equal = true;
                    for (int i = 0; i < data.Length; ++i)
                        if (lastDisplayedData[i] != data[i])
                        {
                            equal = false;
                            break;
                        }
                    if (equal) return;
                }
                lastDisplayedData = data;

                // Проверяем наличие данных
                if ((data == null) || (data.Length <= 0))
                {
                    noDataError(resultTextBlock);
                }
                // Проверяем длину пакета
                else if (data.Length != 8)
                {
                    packetError(resultTextBlock);
                    resultTextBlock.Inlines.Add(Environment.NewLine);
                    displayArray(resultTextBlock, data);
                    resultTextBlock.Inlines.Add(Environment.NewLine + Environment.NewLine + "Ожидаемая длина пакета - 8 байт");
                }
                else
                {
                    resultTextBlock.Inlines.Clear();
                    displayArray(resultTextBlock, data);

                    resultTextBlock.Inlines.Add(Environment.NewLine);
                    resultTextBlock.Inlines.Add(Environment.NewLine + "Квитанция реж. ИД\t= ");
                    resultTextBlock.Inlines.Add(new Run(Convert.ToString(data[0] >> 4, 2).PadLeft(4, '0') + 'b') { FontFamily = new FontFamily("Courier New"), FontWeight = FontWeights.Bold });
                    if ((data[0] >> 4) == 3)
                        resultTextBlock.Inlines.Add(" (верно)");
                    else resultTextBlock.Inlines.Add(new Run(" (не верно)") { Foreground = Brushes.Red });
                    resultTextBlock.Inlines.Add(Environment.NewLine + "Квит. подр. Сдвиг ИД\t= ");
                    resultTextBlock.Inlines.Add(new Run(Convert.ToString(data[0] & 0xF, 2).PadLeft(4, '0') + 'b') { FontFamily = new FontFamily("Courier New"), FontWeight = FontWeights.Bold });
                    if ((data[0] & 0xF) == 0xE)
                        resultTextBlock.Inlines.Add(" (верно)");
                    else resultTextBlock.Inlines.Add(new Run(" (не верно)") { Foreground = Brushes.Red });
                    resultTextBlock.Inlines.Add(Environment.NewLine + "Зав. №\t\t\t= ");
                    resultTextBlock.Inlines.Add(new Run(String.Format("{0:X8}h", data[1] | (data[2] << 8) | (data[3] << 16) | (data[4] << 24))) { FontFamily = new FontFamily("Courier New"), FontWeight = FontWeights.Bold });
                    resultTextBlock.Inlines.Add(" (" + ((data[1] | (data[2] << 8) | (data[3] << 16) | (data[4] << 24)) & 0xFFFFFFFF).ToString() + ")");
                    resultTextBlock.Inlines.Add(Environment.NewLine + "Столбец ИД\t\t= ");
                    resultTextBlock.Inlines.Add(new Run(data[5].ToString()) { FontFamily = new FontFamily("Courier New"), FontWeight = FontWeights.Bold });
                    resultTextBlock.Inlines.Add(Environment.NewLine + "Строка ИД\t\t= ");
                    resultTextBlock.Inlines.Add(new Run(data[6].ToString()) { FontFamily = new FontFamily("Courier New"), FontWeight = FontWeights.Bold });
                    resultTextBlock.Inlines.Add(Environment.NewLine + "КС констант\t\t= ");
                    resultTextBlock.Inlines.Add(new Run(String.Format("{0:X2}h", data[7])) { FontFamily = new FontFamily("Courier New"), FontWeight = FontWeights.Bold });
                }
            }
        }

        /// <summary>
        /// Представление данных ЛС21 в режиме ИД, подрежим - Передача адреса по заводскому номеру
        /// </summary>
        class LS21_ID_FactNum_DataView : DataView
        {
            private Label lb1, lb2, lb3;
            private TwoDigitUpDown udIID, udJID;
            private HexadecimalMaskedTextBox tbFactNum;

            /// <summary>
            /// Конструктор
            /// </summary>
            public LS21_ID_FactNum_DataView()
            {
                Data = new byte[14];
                lastDisplayedData = null;

                lb1 = new Label();
                lb1.Content = "Зав. №";
                lb1.Margin = new Thickness(10, 10, 0, 0);
                tbFactNum = new HexadecimalMaskedTextBox(32);
                tbFactNum.Margin = new Thickness(120, 10, 0, 0);

                lb2 = new Label();
                lb2.Content = "Столбец ИД";
                lb2.Margin = new Thickness(10, 36, 0, 0);
                udIID = new TwoDigitUpDown(1, 88, 1);
                udIID.Margin = new Thickness(120, 36, 0, 0);

                lb3 = new Label();
                lb3.Content = "Строка ИД";
                lb3.Margin = new Thickness(10, 62, 0, 0);
                udJID = new TwoDigitUpDown(1, 44, 1);
                udJID.Margin = new Thickness(120, 62, 0, 0);

                tbFactNum.TextChanged += DefaultHandler;
                udIID.ValueChanged += DefaultHandler;
                udJID.ValueChanged += DefaultHandler;
            }

            /// <summary>
            /// Инициализация представления
            /// </summary>
            /// <param name="testGrid">Сетка в которой необходимо поместить все элементы управления</param>
            protected override void Init(Grid testGrid)
            {
                Initialized = false;
                testGrid.Children.Add(lb1);
                testGrid.Children.Add(lb2);
                testGrid.Children.Add(lb3);
                testGrid.Children.Add(tbFactNum);
                testGrid.Children.Add(udIID);
                testGrid.Children.Add(udJID);
                Initialized = true;
            }

            /// <summary>
            /// Получить введенные данные и разместить в массиве Data
            /// </summary>
            public override void Calculate()
            {
                byte[] data = new byte[14];
                for (int i = 0; i < 14; ++i)
                    data[i] = 0;

                data[0] = 0x3D;
                data[1] = (byte)tbFactNum.Val;
                data[2] = (byte)(tbFactNum.Val >> 8);
                data[3] = (byte)(tbFactNum.Val >> 16);
                data[4] = (byte)(tbFactNum.Val >> 24);
                data[5] = (byte)udIID.Val;
                data[6] = (byte)udJID.Val;
                data[13] = CheckSumCalculator.Calculate(data, 0, data.Length - 1);

                Data = data;

                if (DataController != null)
                {
                    string s = "";
                    for (int i = 0; i < 13; ++i)
                        s += String.Format("{0:X2} ", Data[i]);
                    s += "КС";

                    DataController.DataChanged(s);
                }
            }

            /// <summary>
            /// Вывести данные в окно
            /// </summary>
            /// <param name="progData">Массив данных</param>
            /// <param name="resultTextBlock">Текстовый элемент в котором необходимо вывести данные</param>
            /// <param name="displayArray">Вывод массива данных в указанный TextBlock</param>
            /// <param name="noDataError">Вывод сообщения об отсутствии даннных в указанный TextBlock</param>
            /// <param name="checkSumError">Вывод сообщения об ошибке контрольной суммы в указанный TextBlock</param>
            /// <param name="packetError">Вывод сообщения об ошибке пакета в указанный TextBlock</param>
            public override void DisplayData(byte[] data, TextBlock resultTextBlock, Action<TextBlock, byte[]> displayArray, Action<TextBlock> noDataError, Action<TextBlock> checkSumError, Action<TextBlock> packetError, object parameter)
            {
                // Выход, если получены данные, которые уже были выведены
                if ((lastDisplayedData != null) && (data != null) && (lastDisplayedData.Length == data.Length))
                {
                    bool equal = true;
                    for (int i = 0; i < data.Length; ++i)
                        if (lastDisplayedData[i] != data[i])
                        {
                            equal = false;
                            break;
                        }
                    if (equal) return;
                }
                lastDisplayedData = data;

                // Проверяем наличие данных
                if ((data == null) || (data.Length <= 0))
                {
                    noDataError(resultTextBlock);
                }
                // Проверяем длину пакета
                else if (data.Length != 4)
                {
                    packetError(resultTextBlock);
                    resultTextBlock.Inlines.Add(Environment.NewLine);
                    displayArray(resultTextBlock, data);
                    resultTextBlock.Inlines.Add(Environment.NewLine + Environment.NewLine + "Ожидаемая длина пакета - 4 байта");
                }
                else
                {
                    resultTextBlock.Inlines.Clear();
                    displayArray(resultTextBlock, data);

                    resultTextBlock.Inlines.Add(Environment.NewLine);
                    resultTextBlock.Inlines.Add(Environment.NewLine + "Квитанция реж. ИД\t= ");
                    resultTextBlock.Inlines.Add(new Run(Convert.ToString(data[0] >> 4, 2).PadLeft(4, '0') + 'b') { FontFamily = new FontFamily("Courier New"), FontWeight = FontWeights.Bold });
                    if ((data[0] >> 4) == 3)
                        resultTextBlock.Inlines.Add(" (верно)");
                    else resultTextBlock.Inlines.Add(new Run(" (не верно)") { Foreground = Brushes.Red });
                    resultTextBlock.Inlines.Add(Environment.NewLine + "Квит. подр. Прд. Адр.\t= ");
                    resultTextBlock.Inlines.Add(new Run(Convert.ToString(data[0] & 0xF, 2).PadLeft(4, '0') + 'b') { FontFamily = new FontFamily("Courier New"), FontWeight = FontWeights.Bold });
                    if ((data[0] & 0xF) == 0xD)
                        resultTextBlock.Inlines.Add(" (верно)");
                    else resultTextBlock.Inlines.Add(new Run(" (не верно)") { Foreground = Brushes.Red });
                    resultTextBlock.Inlines.Add(Environment.NewLine + "Столбец ИД\t\t= ");
                    resultTextBlock.Inlines.Add(new Run(data[1].ToString()) { FontFamily = new FontFamily("Courier New"), FontWeight = FontWeights.Bold });
                    resultTextBlock.Inlines.Add(Environment.NewLine + "Строка ИД\t\t= ");
                    resultTextBlock.Inlines.Add(new Run(data[2].ToString()) { FontFamily = new FontFamily("Courier New"), FontWeight = FontWeights.Bold });
                    resultTextBlock.Inlines.Add(Environment.NewLine + "КС констант\t\t= ");
                    resultTextBlock.Inlines.Add(new Run(String.Format("{0:X2}h", data[3])) { FontFamily = new FontFamily("Courier New"), FontWeight = FontWeights.Bold });
                }
            }
        }

        /// <summary>
        /// Представление данных ЛС21 в режиме ИД, подрежим - Чтение констант ППМ
        /// </summary>
        class LS21_ID_RdConst_DataView : DataView
        {
            private Label lb1, lb2, lb3;
            private TwoDigitUpDown udI, udJ, udNf;

            /// <summary>
            /// Конструктор
            /// </summary>
            public LS21_ID_RdConst_DataView()
            {
                Data = new byte[14];
                lastDisplayedData = null;

                lb1 = new Label();
                lb1.Content = "Столбец";
                lb1.Margin = new Thickness(10, 10, 0, 0);
                udI = new TwoDigitUpDown(1, 88, 1);
                udI.Margin = new Thickness(120, 10, 0, 0);

                lb2 = new Label();
                lb2.Content = "Строка";
                lb2.Margin = new Thickness(10, 36, 0, 0);
                udJ = new TwoDigitUpDown(1, 44, 1);
                udJ.Margin = new Thickness(120, 36, 0, 0);

                lb3 = new Label();
                lb3.Content = "NF";
                lb3.Margin = new Thickness(10, 62, 0, 0);
                udNf = new TwoDigitUpDown(0, 15, 0);
                udNf.Margin = new Thickness(120, 62, 0, 0);

                udI.ValueChanged += DefaultHandler;
                udJ.ValueChanged += DefaultHandler;
                udNf.ValueChanged += DefaultHandler;
            }

            /// <summary>
            /// Инициализация представления
            /// </summary>
            /// <param name="testGrid">Сетка в которой необходимо поместить все элементы управления</param>
            protected override void Init(Grid testGrid)
            {
                Initialized = false;
                testGrid.Children.Add(lb1);
                testGrid.Children.Add(lb2);
                testGrid.Children.Add(lb3);
                testGrid.Children.Add(udI);
                testGrid.Children.Add(udJ);
                testGrid.Children.Add(udNf);
                Initialized = true;
            }

            /// <summary>
            /// Получить введенные данные и разместить в массиве Data
            /// </summary>
            public override void Calculate()
            {
                byte[] data = new byte[14];
                for (int i = 0; i < 14; ++i)
                    data[i] = 0;

                data[0] = 0x3C;
                data[1] = (byte)udI.Val;
                data[2] = (byte)udJ.Val;
                data[3] = (byte)(udNf.Val & 0xF);
                data[13] = CheckSumCalculator.Calculate(data, 0, data.Length - 1);

                Data = data;

                if (DataController != null)
                {
                    string s = "";
                    for (int i = 0; i < 13; ++i)
                        s += String.Format("{0:X2} ", Data[i]);
                    s += "КС";

                    DataController.DataChanged(s);
                }
            }

            /// <summary>
            /// Вывести данные в окно
            /// </summary>
            /// <param name="progData">Массив данных</param>
            /// <param name="resultTextBlock">Текстовый элемент в котором необходимо вывести данные</param>
            /// <param name="displayArray">Вывод массива данных в указанный TextBlock</param>
            /// <param name="noDataError">Вывод сообщения об отсутствии даннных в указанный TextBlock</param>
            /// <param name="checkSumError">Вывод сообщения об ошибке контрольной суммы в указанный TextBlock</param>
            /// <param name="packetError">Вывод сообщения об ошибке пакета в указанный TextBlock</param>
            public override void DisplayData(byte[] data, TextBlock resultTextBlock, Action<TextBlock, byte[]> displayArray, Action<TextBlock> noDataError, Action<TextBlock> checkSumError, Action<TextBlock> packetError, object parameter)
            {
                // Выход, если получены данные, которые уже были выведены
                if ((lastDisplayedData != null) && (data != null) && (lastDisplayedData.Length == data.Length))
                {
                    bool equal = true;
                    for (int i = 0; i < data.Length; ++i)
                        if (lastDisplayedData[i] != data[i])
                        {
                            equal = false;
                            break;
                        }
                    if (equal) return;
                }
                lastDisplayedData = data;

                // Проверяем наличие данных
                if ((data == null) || (data.Length <= 0))
                {
                    noDataError(resultTextBlock);
                }
                // Проверяем длину пакета
                else if (data.Length != 10)
                {
                    packetError(resultTextBlock);
                    resultTextBlock.Inlines.Add(Environment.NewLine);
                    displayArray(resultTextBlock, data);
                    resultTextBlock.Inlines.Add(Environment.NewLine + Environment.NewLine + "Ожидаемая длина пакета - 10 байт");
                }
                else
                {
                    resultTextBlock.Inlines.Clear();
                    displayArray(resultTextBlock, data);

                    resultTextBlock.Inlines.Add(Environment.NewLine);
                    resultTextBlock.Inlines.Add(Environment.NewLine + "Квитанция реж. ИД\t= ");
                    resultTextBlock.Inlines.Add(new Run(Convert.ToString(data[0] >> 4, 2).PadLeft(4, '0') + 'b') { FontFamily = new FontFamily("Courier New"), FontWeight = FontWeights.Bold });
                    if ((data[0] >> 4) == 3)
                        resultTextBlock.Inlines.Add(" (верно)");
                    else resultTextBlock.Inlines.Add(new Run(" (не верно)") { Foreground = Brushes.Red });
                    resultTextBlock.Inlines.Add(Environment.NewLine + "Квит. подр. Чт. Конст.\t= ");
                    resultTextBlock.Inlines.Add(new Run(Convert.ToString(data[0] & 0xF, 2).PadLeft(4, '0') + 'b') { FontFamily = new FontFamily("Courier New"), FontWeight = FontWeights.Bold });
                    if ((data[0] & 0xF) == 0xC)
                        resultTextBlock.Inlines.Add(" (верно)");
                    else resultTextBlock.Inlines.Add(new Run(" (не верно)") { Foreground = Brushes.Red });
                    resultTextBlock.Inlines.Add(Environment.NewLine + "Kx\t\t\t= ");
                    resultTextBlock.Inlines.Add(new Run(String.Format("{0:X4}h", data[1] | (data[2] << 8))) { FontFamily = new FontFamily("Courier New"), FontWeight = FontWeights.Bold });
                    resultTextBlock.Inlines.Add(Environment.NewLine + "Ky\t\t\t= ");
                    resultTextBlock.Inlines.Add(new Run(String.Format("{0:X4}h", data[3] | (data[4] << 8))) { FontFamily = new FontFamily("Courier New"), FontWeight = FontWeights.Bold });
                    resultTextBlock.Inlines.Add(Environment.NewLine + "Попр. фазы для прд.\t= ");
                    resultTextBlock.Inlines.Add(new Run(Convert.ToString(data[5], 2).PadLeft(8, '0') + 'b') { FontFamily = new FontFamily("Courier New"), FontWeight = FontWeights.Bold });
                    resultTextBlock.Inlines.Add(Environment.NewLine + "Попр. фазы для пр. прямо= ");
                    resultTextBlock.Inlines.Add(new Run(Convert.ToString(data[6], 2).PadLeft(8, '0') + 'b') { FontFamily = new FontFamily("Courier New"), FontWeight = FontWeights.Bold });
                    resultTextBlock.Inlines.Add(Environment.NewLine + "Попр. фазы для пр. обр\t= ");
                    resultTextBlock.Inlines.Add(new Run(Convert.ToString(data[7], 2).PadLeft(8, '0') + 'b') { FontFamily = new FontFamily("Courier New"), FontWeight = FontWeights.Bold });
                    resultTextBlock.Inlines.Add(Environment.NewLine + "Аттенюатор прямо\t= ");
                    resultTextBlock.Inlines.Add(new Run(Convert.ToString(data[8] & 7, 2).PadLeft(3, '0') + 'b') { FontFamily = new FontFamily("Courier New"), FontWeight = FontWeights.Bold });
                    resultTextBlock.Inlines.Add(Environment.NewLine + "Аттенюатор обратно\t= ");
                    resultTextBlock.Inlines.Add(new Run(Convert.ToString((data[8] >> 3) & 7, 2).PadLeft(3, '0') + 'b') { FontFamily = new FontFamily("Courier New"), FontWeight = FontWeights.Bold });
                    resultTextBlock.Inlines.Add(Environment.NewLine + "КС констант\t\t= ");
                    resultTextBlock.Inlines.Add(new Run(String.Format("{0:X2}h", data[9])) { FontFamily = new FontFamily("Courier New"), FontWeight = FontWeights.Bold });
                }
            }
        }

        /// <summary>
        /// Представление данных ЛС21 в режиме ИД, подрежим - Передача констант ППМ
        /// </summary>
        class LS21_ID_WrConst_DataView : DataView
        {
            private Label lb1, lb2, lb3, lb4, lb5, lb6, lb7, lb8, lb9, lb10;
            private TwoDigitUpDown udI, udJ, udNf;
            private HexadecimalMaskedTextBox tbKx, tbKy;
            private BinaryMaskedTextBox tbFT, tbFRst, tbFRbk, tbAst, tbAbk;

            /// <summary>
            /// Конструктор
            /// </summary>
            public LS21_ID_WrConst_DataView()
            {
                Data = new byte[14];
                lastDisplayedData = null;

                lb1 = new Label();
                lb1.Content = "Столбец";
                lb1.Margin = new Thickness(10, 10, 0, 0);
                udI = new TwoDigitUpDown(1, 88, 1);
                udI.Margin = new Thickness(120, 10, 0, 0);

                lb2 = new Label();
                lb2.Content = "Строка";
                lb2.Margin = new Thickness(220, 10, 0, 0);
                udJ = new TwoDigitUpDown(1, 44, 1);
                udJ.Margin = new Thickness(300, 10, 0, 0);

                lb3 = new Label();
                lb3.Content = "NF";
                lb3.Margin = new Thickness(10, 36, 0, 0);
                udNf = new TwoDigitUpDown(0, 15, 0);
                udNf.Margin = new Thickness(120, 36, 0, 0);

                lb4 = new Label();
                lb4.Content = "Kx";
                lb4.Margin = new Thickness(10, 62, 0, 0);
                tbKx = new HexadecimalMaskedTextBox(16);
                tbKx.Margin = new Thickness(120, 62, 0, 0);

                lb5 = new Label();
                lb5.Content = "Ky";
                lb5.Margin = new Thickness(220, 62, 0, 0);
                tbKy = new HexadecimalMaskedTextBox(16);
                tbKy.Margin = new Thickness(300, 62, 0, 0);

                lb6 = new Label();
                lb6.Content = "Попр. прд.";
                lb6.Margin = new Thickness(10, 88, 0, 0);
                tbFT = new BinaryMaskedTextBox(8);
                tbFT.Margin = new Thickness(120, 88, 0, 0);

                lb7 = new Label();
                lb7.Content = "Попр. пр. прямо";
                lb7.Margin = new Thickness(10, 114, 0, 0);
                tbFRst = new BinaryMaskedTextBox(8);
                tbFRst.Margin = new Thickness(120, 114, 0, 0);

                lb8 = new Label();
                lb8.Content = "Попр. пр. обр.";
                lb8.Margin = new Thickness(10, 140, 0, 0);
                tbFRbk = new BinaryMaskedTextBox(8);
                tbFRbk.Margin = new Thickness(120, 140, 0, 0);

                lb9 = new Label();
                lb9.Content = "Атт. прямо";
                lb9.Margin = new Thickness(10, 166, 0, 0);
                tbAst = new BinaryMaskedTextBox(3);
                tbAst.Margin = new Thickness(120, 166, 0, 0);

                lb10 = new Label();
                lb10.Content = "Атт. обр.";
                lb10.Margin = new Thickness(220, 166, 0, 0);
                tbAbk = new BinaryMaskedTextBox(3);
                tbAbk.Margin = new Thickness(300, 166, 0, 0);

                udI.ValueChanged += DefaultHandler;
                udJ.ValueChanged += DefaultHandler;
                udNf.ValueChanged += DefaultHandler;
                tbKx.TextChanged += DefaultHandler;
                tbKy.TextChanged += DefaultHandler;
                tbFT.TextChanged += DefaultHandler;
                tbFRst.TextChanged += DefaultHandler;
                tbFRbk.TextChanged += DefaultHandler;
                tbAst.TextChanged += DefaultHandler;
                tbAbk.TextChanged += DefaultHandler;
            }

            /// <summary>
            /// Инициализация представления
            /// </summary>
            /// <param name="testGrid">Сетка в которой необходимо поместить все элементы управления</param>
            protected override void Init(Grid testGrid)
            {
                Initialized = false;
                testGrid.Children.Add(lb1);
                testGrid.Children.Add(lb2);
                testGrid.Children.Add(lb3);
                testGrid.Children.Add(lb4);
                testGrid.Children.Add(lb5);
                testGrid.Children.Add(lb6);
                testGrid.Children.Add(lb7);
                testGrid.Children.Add(lb8);
                testGrid.Children.Add(lb9);
                testGrid.Children.Add(lb10);
                testGrid.Children.Add(udI);
                testGrid.Children.Add(udJ);
                testGrid.Children.Add(udNf);
                testGrid.Children.Add(tbKx);
                testGrid.Children.Add(tbKy);
                testGrid.Children.Add(tbFT);
                testGrid.Children.Add(tbFRst);
                testGrid.Children.Add(tbFRbk);
                testGrid.Children.Add(tbAst);
                testGrid.Children.Add(tbAbk);
                Initialized = true;
            }

            /// <summary>
            /// Получить введенные данные и разместить в массиве Data
            /// </summary>
            public override void Calculate()
            {
                byte[] data = new byte[14];
                for (int i = 0; i < 14; ++i)
                    data[i] = 0;

                data[0] = 0x3B;
                data[1] = (byte)udI.Val;
                data[2] = (byte)udJ.Val;
                data[3] = (byte)(udNf.Val & 0xF);
                data[4] = (byte)tbKx.Val;
                data[5] = (byte)(tbKx.Val >> 8);
                data[6] = (byte)tbKy.Val;
                data[7] = (byte)(tbKy.Val >> 8);
                data[8] = (byte)tbFT.Val;
                data[9] = (byte)tbFRst.Val;
                data[10] = (byte)tbFRbk.Val;
                data[11] = (byte)((tbAst.Val & 7) | ((tbAbk.Val & 7) << 3));
                data[12] = 0;
                data[13] = CheckSumCalculator.Calculate(data, 0, data.Length - 1);

                Data = data;

                if (DataController != null)
                {
                    string s = "";
                    for (int i = 0; i < 13; ++i)
                        s += String.Format("{0:X2} ", Data[i]);
                    s += "КС";

                    DataController.DataChanged(s);
                }
            }

            /// <summary>
            /// Вывести данные в окно
            /// </summary>
            /// <param name="progData">Массив данных</param>
            /// <param name="resultTextBlock">Текстовый элемент в котором необходимо вывести данные</param>
            /// <param name="displayArray">Вывод массива данных в указанный TextBlock</param>
            /// <param name="noDataError">Вывод сообщения об отсутствии даннных в указанный TextBlock</param>
            /// <param name="checkSumError">Вывод сообщения об ошибке контрольной суммы в указанный TextBlock</param>
            /// <param name="packetError">Вывод сообщения об ошибке пакета в указанный TextBlock</param>
            public override void DisplayData(byte[] data, TextBlock resultTextBlock, Action<TextBlock, byte[]> displayArray, Action<TextBlock> noDataError, Action<TextBlock> checkSumError, Action<TextBlock> packetError, object parameter)
            {
                // Выход, если получены данные, которые уже были выведены
                if ((lastDisplayedData != null) && (data != null) && (lastDisplayedData.Length == data.Length))
                {
                    bool equal = true;
                    for (int i = 0; i < data.Length; ++i)
                        if (lastDisplayedData[i] != data[i])
                        {
                            equal = false;
                            break;
                        }
                    if (equal) return;
                }
                lastDisplayedData = data;

                // Проверяем наличие данных
                if ((data == null) || (data.Length <= 0))
                {
                    noDataError(resultTextBlock);
                }
                // Проверяем длину пакета
                else if (data.Length != 6)
                {
                    packetError(resultTextBlock);
                    resultTextBlock.Inlines.Add(Environment.NewLine);
                    displayArray(resultTextBlock, data);
                    resultTextBlock.Inlines.Add(Environment.NewLine + Environment.NewLine + "Ожидаемая длина пакета - 6 байт");
                }
                else
                {
                    resultTextBlock.Inlines.Clear();
                    displayArray(resultTextBlock, data);

                    resultTextBlock.Inlines.Add(Environment.NewLine);
                    resultTextBlock.Inlines.Add(Environment.NewLine + "Квитанция реж. ИД\t= ");
                    resultTextBlock.Inlines.Add(new Run(Convert.ToString(data[0] >> 4, 2).PadLeft(4, '0') + 'b') { FontFamily = new FontFamily("Courier New"), FontWeight = FontWeights.Bold });
                    if ((data[0] >> 4) == 3)
                        resultTextBlock.Inlines.Add(" (верно)");
                    else resultTextBlock.Inlines.Add(new Run(" (не верно)") { Foreground = Brushes.Red });
                    resultTextBlock.Inlines.Add(Environment.NewLine + "Квит. подр. Чт. Конст.\t= ");
                    resultTextBlock.Inlines.Add(new Run(Convert.ToString(data[0] & 0xF, 2).PadLeft(4, '0') + 'b') { FontFamily = new FontFamily("Courier New"), FontWeight = FontWeights.Bold });
                    if ((data[0] & 0xF) == 0xB)
                        resultTextBlock.Inlines.Add(" (верно)");
                    else resultTextBlock.Inlines.Add(new Run(" (не верно)") { Foreground = Brushes.Red });

                    resultTextBlock.Inlines.Add(Environment.NewLine + "Зав. №\t\t\t= ");
                    resultTextBlock.Inlines.Add(new Run(String.Format("{0:X8}h", data[1] | (data[2] << 8) | (data[3] << 16) | (data[4] << 24))) { FontFamily = new FontFamily("Courier New"), FontWeight = FontWeights.Bold });
                    resultTextBlock.Inlines.Add(" (" + ((data[1] | (data[2] << 8) | (data[3] << 16) | (data[4] << 24)) & 0xFFFFFFFF).ToString() + ")");
                    resultTextBlock.Inlines.Add(Environment.NewLine + "КС слов 1-12\t\t= ");
                    resultTextBlock.Inlines.Add(new Run(String.Format("{0:X2}h", data[5])) { FontFamily = new FontFamily("Courier New"), FontWeight = FontWeights.Bold });
                }
            }
        }

        /// <summary>
        /// Представление данных ЛС21 в режиме ИД, подрежим - Запись констант в ПЗУ
        /// </summary>
        class LS21_ID_Overwrite_DataView : DataView
        {
            private Label lb1, lb2;
            private TwoDigitUpDown udI, udJ;

            /// <summary>
            /// Конструктор
            /// </summary>
            public LS21_ID_Overwrite_DataView()
            {
                Data = new byte[14];
                lastDisplayedData = null;

                lb1 = new Label();
                lb1.Content = "Столбец";
                lb1.Margin = new Thickness(10, 10, 0, 0);
                udI = new TwoDigitUpDown(0, 88, 0);
                udI.Margin = new Thickness(120, 10, 0, 0);

                lb2 = new Label();
                lb2.Content = "Строка";
                lb2.Margin = new Thickness(10, 36, 0, 0);
                udJ = new TwoDigitUpDown(0, 44, 0);
                udJ.Margin = new Thickness(120, 36, 0, 0);

                udI.ValueChanged += DefaultHandler;
                udJ.ValueChanged += DefaultHandler;
            }

            /// <summary>
            /// Инициализация представления
            /// </summary>
            /// <param name="testGrid">Сетка в которой необходимо поместить все элементы управления</param>
            protected override void Init(Grid testGrid)
            {
                Initialized = false;
                testGrid.Children.Add(lb1);
                testGrid.Children.Add(lb2);
                testGrid.Children.Add(udI);
                testGrid.Children.Add(udJ);
                Initialized = true;
            }

            /// <summary>
            /// Получить введенные данные и разместить в массиве Data
            /// </summary>
            public override void Calculate()
            {
                byte[] data = new byte[14];
                for (int i = 0; i < 14; ++i)
                    data[i] = 0;

                data[0] = 0x3A;
                data[1] = (byte)udI.Val;
                data[2] = (byte)udJ.Val;
                data[3] = 0xAA;
                data[13] = CheckSumCalculator.Calculate(data, 0, data.Length - 1);

                Data = data;

                if (DataController != null)
                {
                    string s = "";
                    for (int i = 0; i < 13; ++i)
                        s += String.Format("{0:X2} ", Data[i]);
                    s += "КС";

                    DataController.DataChanged(s);
                }
            }

            /// <summary>
            /// Вывести данные в окно
            /// </summary>
            /// <param name="progData">Массив данных</param>
            /// <param name="resultTextBlock">Текстовый элемент в котором необходимо вывести данные</param>
            /// <param name="displayArray">Вывод массива данных в указанный TextBlock</param>
            /// <param name="noDataError">Вывод сообщения об отсутствии даннных в указанный TextBlock</param>
            /// <param name="checkSumError">Вывод сообщения об ошибке контрольной суммы в указанный TextBlock</param>
            /// <param name="packetError">Вывод сообщения об ошибке пакета в указанный TextBlock</param>
            public override void DisplayData(byte[] data, TextBlock resultTextBlock, Action<TextBlock, byte[]> displayArray, Action<TextBlock> noDataError, Action<TextBlock> checkSumError, Action<TextBlock> packetError, object parameter)
            {
                // Выход, если получены данные, которые уже были выведены
                if ((lastDisplayedData != null) && (data != null) && (lastDisplayedData.Length == data.Length))
                {
                    bool equal = true;
                    for (int i = 0; i < data.Length; ++i)
                        if (lastDisplayedData[i] != data[i])
                        {
                            equal = false;
                            break;
                        }
                    if (equal) return;
                }
                lastDisplayedData = data;

                // Проверяем наличие данных
                if ((data == null) || (data.Length <= 0))
                {
                    resultTextBlock.Inlines.Clear();
                    resultTextBlock.Inlines.Add(new Run("Нет данных") { FontWeight = FontWeights.Bold });
                    resultTextBlock.Inlines.Add(Environment.NewLine + Environment.NewLine + "Получение данных не предусмотрено");
                }
                // Проверяем длину пакета
                else
                {
                    packetError(resultTextBlock);
                    resultTextBlock.Inlines.Add(Environment.NewLine);
                    displayArray(resultTextBlock, data);
                    resultTextBlock.Inlines.Add(Environment.NewLine + Environment.NewLine + "Получение данных не предусмотрено");
                }
            }
        }

        /// <summary>
        /// Представление данных ЛС21 в режиме ТЕХН, подрежим - Измерение УАП
        /// </summary>
        class LS21_TC_UAP_DataView : DataView
        {
            private Label lb1, lb2, lb3, lb4;
            private TwoDigitUpDown udNf1;
            private BinaryMaskedTextBox tbCosX, tbCosY;
            private SimpleCheckBox chbPobr, chbExc02, chbLoc17, chbPF;
            private ImitComboBox cbImit;
            private N8SygnalsView n8sygnals;

            /// <summary>
            /// Конструктор
            /// </summary>
            /// <param name="n8sygnals">Представление сигналов Н8</param>
            public LS21_TC_UAP_DataView(N8SygnalsView n8sygnals)
            {
                this.n8sygnals = n8sygnals;

                Data = new byte[14];
                lastDisplayedData = null;

                lb1 = new Label();
                lb1.Content = "NF1";
                lb1.Margin = new Thickness(10, 10, 0, 0);
                udNf1 = new TwoDigitUpDown(0, 15, 0);
                udNf1.Margin = new Thickness(120, 10, 0, 0);

                lb2 = new Label();
                lb2.Content = "Cos X";
                lb2.Margin = new Thickness(10, 36, 0, 0);
                tbCosX = new BinaryMaskedTextBox(13);
                tbCosX.Margin = new Thickness(120, 36, 0, 0);

                lb3 = new Label();
                lb3.Content = "Cos Y";
                lb3.Margin = new Thickness(10, 62, 0, 0);
                tbCosY = new BinaryMaskedTextBox(13);
                tbCosY.Margin = new Thickness(120, 62, 0, 0);

                chbPobr = new SimpleCheckBox("Побр");
                chbPobr.Margin = new Thickness(10 + 5, 88 + 5, 0, 0);

                chbExc02 = new SimpleCheckBox("Снять возб 02");
                chbExc02.Margin = new Thickness(10 + 5, 114 + 5, 0, 0);

                chbLoc17 = new SimpleCheckBox("Мест 17");
                chbLoc17.Margin = new Thickness(120, 114 + 5, 0, 0);

                chbPF = new SimpleCheckBox("ПF");
                chbPF.Margin = new Thickness(230, 114 + 5, 0, 0);

                lb4 = new Label();
                lb4.Content = "Имит";
                lb4.Margin = new Thickness(10, 140, 0, 0);
                cbImit = new ImitComboBox();
                cbImit.Margin = new Thickness(120, 140, 0, 0);

                n8sygnals.addImitComboBox(cbImit);

                udNf1.ValueChanged += DefaultHandler;
                tbCosX.TextChanged += DefaultHandler;
                tbCosY.TextChanged += DefaultHandler;
                chbPobr.Click += DefaultHandler;
                chbExc02.Click += DefaultHandler;
                chbLoc17.Click += DefaultHandler;
                chbPF.Click += DefaultHandler;
                cbImit.SelectionChanged += DefaultHandler;
            }

            /// <summary>
            /// Инициализация представления
            /// </summary>
            /// <param name="testGrid">Сетка в которой необходимо поместить все элементы управления</param>
            protected override void Init(Grid testGrid)
            {
                Initialized = false;
                testGrid.Children.Add(lb1);
                testGrid.Children.Add(lb2);
                testGrid.Children.Add(lb3);
                testGrid.Children.Add(lb4);
                testGrid.Children.Add(udNf1);
                testGrid.Children.Add(tbCosX);
                testGrid.Children.Add(tbCosY);
                testGrid.Children.Add(chbPobr);
                testGrid.Children.Add(chbExc02);
                testGrid.Children.Add(chbLoc17);
                testGrid.Children.Add(chbPF);
                testGrid.Children.Add(cbImit);
                Initialized = true;
            }

            /// <summary>
            /// Получить введенные данные и разместить в массиве Data
            /// </summary>
            public override void Calculate()
            {
                byte[] data = new byte[14];
                for (int i = 0; i < 14; ++i)
                    data[i] = 0;

                data[0] = 0x41;
                data[5] = (byte)(udNf1.Val << 4);
                data[7] = (byte)((tbCosX.Val & 0x1F) << 3);
                data[8] = (byte)(tbCosX.Val >> 5);
                data[9] = (byte)((cbImit.Val & 7) << 2);
                if (chbPF.Val) data[9] |= 1;
                if (chbLoc17.Val) data[9] |= 2;
                if (chbExc02.Val) data[9] |= 0x20;
                data[10] = (byte)((tbCosY.Val & 0x1F) << 3);
                if (chbPobr.Val) data[10] |= 1;
                data[11] = (byte)(tbCosY.Val >> 5);
                data[13] = CheckSumCalculator.Calculate(data, 0, data.Length - 1);

                Data = data;

                if (DataController != null)
                {
                    string s = "";
                    for (int i = 0; i < 13; ++i)
                        s += String.Format("{0:X2} ", Data[i]);
                    s += "КС";

                    DataController.DataChanged(s);
                }
            }

            /// <summary>
            /// Вывести данные в окно
            /// </summary>
            /// <param name="progData">Массив данных</param>
            /// <param name="resultTextBlock">Текстовый элемент в котором необходимо вывести данные</param>
            /// <param name="displayArray">Вывод массива данных в указанный TextBlock</param>
            /// <param name="noDataError">Вывод сообщения об отсутствии даннных в указанный TextBlock</param>
            /// <param name="checkSumError">Вывод сообщения об ошибке контрольной суммы в указанный TextBlock</param>
            /// <param name="packetError">Вывод сообщения об ошибке пакета в указанный TextBlock</param>
            public override void DisplayData(byte[] data, TextBlock resultTextBlock, Action<TextBlock, byte[]> displayArray, Action<TextBlock> noDataError, Action<TextBlock> checkSumError, Action<TextBlock> packetError, object parameter)
            {
                if (n8sygnals.ImitCounter != 0) { --n8sygnals.ImitCounter; }

                // Выход, если получены данные, которые уже были выведены
                if ((lastDisplayedData != null) && (data != null) && (lastDisplayedData.Length == data.Length))
                {
                    bool equal = true;
                    for (int i = 0; i < data.Length; ++i)
                        if (lastDisplayedData[i] != data[i])
                        {
                            equal = false;
                            break;
                        }
                    if (equal) return;
                }
                lastDisplayedData = data;

                // Проверяем наличие данных
                if ((data == null) || (data.Length <= 0))
                {
                    noDataError(resultTextBlock);
                    if (parameter != null) ((TextBlock)parameter).Text = "";
                }
                // Проверяем длину пакета
                else if (data.Length != 14)
                {
                    packetError(resultTextBlock);
                    resultTextBlock.Inlines.Add(Environment.NewLine);
                    displayArray(resultTextBlock, data);
                    resultTextBlock.Inlines.Add(Environment.NewLine + Environment.NewLine + "Ожидаемая длина пакета - 14 байт");
                    if (parameter != null) ((TextBlock)parameter).Text = "";
                }
                else
                {
                    if (parameter != null)
                    {
                        ((TextBlock)parameter).Text = "НР21.10.02ОК: ";
                        ((TextBlock)parameter).Inlines.Add(new Run(Convert.ToString(data[0], 2).PadLeft(8, '0') + 'b') { FontFamily = new FontFamily("Courier New"), FontWeight = FontWeights.Bold });
                    }

                    resultTextBlock.Inlines.Clear();
                    displayArray(resultTextBlock, data);
                    resultTextBlock.Inlines.Add(Environment.NewLine);
                    resultTextBlock.Inlines.Add(Environment.NewLine + "Имит вкл.\t\t= ");
                    resultTextBlock.Inlines.Add(new Run(((data[0] >> 7) & 1).ToString()) { FontFamily = new FontFamily("Courier New"), FontWeight = FontWeights.Bold });
                    resultTextBlock.Inlines.Add(Environment.NewLine + "Контроль Н6.21\t\t= ");
                    resultTextBlock.Inlines.Add(new Run(Convert.ToString(data[0] & 0x7F, 2).PadLeft(7, '0') + 'b') { FontFamily = new FontFamily("Courier New"), FontWeight = FontWeights.Bold });
                    resultTextBlock.Inlines.Add(Environment.NewLine + "УАП1\t\t\t= ");
                    resultTextBlock.Inlines.Add(new Run(Convert.ToString(data[1], 2).PadLeft(8, '0') + 'b') { FontFamily = new FontFamily("Courier New"), FontWeight = FontWeights.Bold });
                    resultTextBlock.Inlines.Add(Environment.NewLine + "УАП2\t\t\t= ");
                    resultTextBlock.Inlines.Add(new Run(Convert.ToString(data[2], 2).PadLeft(8, '0') + 'b') { FontFamily = new FontFamily("Courier New"), FontWeight = FontWeights.Bold });
                    resultTextBlock.Inlines.Add(Environment.NewLine + "УАП3\t\t\t= ");
                    resultTextBlock.Inlines.Add(new Run(Convert.ToString(data[3], 2).PadLeft(8, '0') + 'b') { FontFamily = new FontFamily("Courier New"), FontWeight = FontWeights.Bold });
                    resultTextBlock.Inlines.Add(Environment.NewLine + "УАП4\t\t\t= ");
                    resultTextBlock.Inlines.Add(new Run(Convert.ToString(data[4], 2).PadLeft(8, '0') + 'b') { FontFamily = new FontFamily("Courier New"), FontWeight = FontWeights.Bold });
                    resultTextBlock.Inlines.Add(Environment.NewLine + "УАП5\t\t\t= ");
                    resultTextBlock.Inlines.Add(new Run(String.Format("{0:X8}h", data[5] | (data[6] << 8) | (data[7] << 16) | (data[8] << 24))) { FontFamily = new FontFamily("Courier New"), FontWeight = FontWeights.Bold });
                    resultTextBlock.Inlines.Add(Environment.NewLine + "УАП5H\t\t\t= ");
                    resultTextBlock.Inlines.Add(new Run(Convert.ToString(data[7] | (data[8] << 8), 2).PadLeft(16, '0') + 'b') { FontFamily = new FontFamily("Courier New"), FontWeight = FontWeights.Bold });
                    resultTextBlock.Inlines.Add(Environment.NewLine + "УАП5L\t\t\t= ");
                    resultTextBlock.Inlines.Add(new Run(Convert.ToString(data[5] | (data[6] << 8), 2).PadLeft(16, '0') + 'b') { FontFamily = new FontFamily("Courier New"), FontWeight = FontWeights.Bold });
                    resultTextBlock.Inlines.Add(Environment.NewLine + "NFц\t\t\t= ");
                    resultTextBlock.Inlines.Add(new Run((data[12] & 0x3F).ToString()) { FontFamily = new FontFamily("Courier New"), FontWeight = FontWeights.Bold });
                    resultTextBlock.Inlines.Add(Environment.NewLine + "КС\t\t\t= ");
                    resultTextBlock.Inlines.Add(new Run(String.Format("{0:X2}h", data[13])) { FontFamily = new FontFamily("Courier New"), FontWeight = FontWeights.Bold });
                    if (data[13] == CheckSumCalculator.Calculate(data, 0, 13))
                        resultTextBlock.Inlines.Add(" (верно)");
                    else resultTextBlock.Inlines.Add(new Run(" (не верно)") { Foreground = Brushes.Red });

                    // Переход в режим "Имит 400" при получении флага "Имит вкл."
                    if (((data[0] & 0x80) != 0) && (cbImit.SelectedIndex == 0) && (n8sygnals.ImitCounter == 0))
                    {
                        n8sygnals.goImit();
                    }
                }
            }
        }

        /// <summary>
        /// Представление данных ЛС21 в режиме ТЕХН, подрежим - Измерение рангов
        /// </summary>
        class LS21_TC_Rank_DataView : DataView
        {
            private Label lb1, lb2, lb3, lb4;
            private TwoDigitUpDown udNf1;
            private BinaryMaskedTextBox tbCosX, tbCosY;
            private SimpleCheckBox chbPobr, chbExc02, chbLoc17, chbPF;
            private ImitComboBox cbImit;
            private N8SygnalsView n8sygnals;

            /// <summary>
            /// Конструктор
            /// </summary>
            /// <param name="n8sygnals">Представление сигналов Н8</param>
            public LS21_TC_Rank_DataView(N8SygnalsView n8sygnals)
            {
                this.n8sygnals = n8sygnals;

                Data = new byte[14];
                lastDisplayedData = null;

                lb1 = new Label();
                lb1.Content = "NF1";
                lb1.Margin = new Thickness(10, 10, 0, 0);
                udNf1 = new TwoDigitUpDown(0, 15, 0);
                udNf1.Margin = new Thickness(120, 10, 0, 0);

                lb2 = new Label();
                lb2.Content = "Cos X";
                lb2.Margin = new Thickness(10, 36, 0, 0);
                tbCosX = new BinaryMaskedTextBox(13);
                tbCosX.Margin = new Thickness(120, 36, 0, 0);

                lb3 = new Label();
                lb3.Content = "Cos Y";
                lb3.Margin = new Thickness(10, 62, 0, 0);
                tbCosY = new BinaryMaskedTextBox(13);
                tbCosY.Margin = new Thickness(120, 62, 0, 0);

                chbPobr = new SimpleCheckBox("Побр");
                chbPobr.Margin = new Thickness(10 + 5, 88 + 5, 0, 0);

                chbExc02 = new SimpleCheckBox("Снять возб 02");
                chbExc02.Margin = new Thickness(10 + 5, 114 + 5, 0, 0);

                chbLoc17 = new SimpleCheckBox("Мест 17");
                chbLoc17.Margin = new Thickness(120, 114 + 5, 0, 0);

                chbPF = new SimpleCheckBox("ПF");
                chbPF.Margin = new Thickness(230, 114 + 5, 0, 0);

                lb4 = new Label();
                lb4.Content = "Имит";
                lb4.Margin = new Thickness(10, 140, 0, 0);
                cbImit = new ImitComboBox();
                cbImit.Margin = new Thickness(120, 140, 0, 0);

                n8sygnals.addImitComboBox(cbImit);

                udNf1.ValueChanged += DefaultHandler;
                tbCosX.TextChanged += DefaultHandler;
                tbCosY.TextChanged += DefaultHandler;
                chbPobr.Click += DefaultHandler;
                chbPobr.Click += DefaultHandler;
                chbExc02.Click += DefaultHandler;
                chbLoc17.Click += DefaultHandler;
                chbPF.Click += DefaultHandler;
                cbImit.SelectionChanged += DefaultHandler;
            }

            /// <summary>
            /// Инициализация представления
            /// </summary>
            /// <param name="testGrid">Сетка в которой необходимо поместить все элементы управления</param>
            protected override void Init(Grid testGrid)
            {
                Initialized = false;
                testGrid.Children.Add(lb1);
                testGrid.Children.Add(lb2);
                testGrid.Children.Add(lb3);
                testGrid.Children.Add(lb4);
                testGrid.Children.Add(udNf1);
                testGrid.Children.Add(tbCosX);
                testGrid.Children.Add(tbCosY);
                testGrid.Children.Add(chbPobr);
                testGrid.Children.Add(chbExc02);
                testGrid.Children.Add(chbLoc17);
                testGrid.Children.Add(chbPF);
                testGrid.Children.Add(cbImit);
                Initialized = true;
            }

            /// <summary>
            /// Получить введенные данные и разместить в массиве Data
            /// </summary>
            public override void Calculate()
            {
                byte[] data = new byte[14];
                for (int i = 0; i < 14; ++i)
                    data[i] = 0;

                data[0] = 0x42;
                data[5] = (byte)(udNf1.Val << 4);
                data[7] = (byte)((tbCosX.Val & 0x1F) << 3);
                data[8] = (byte)(tbCosX.Val >> 5);
                data[9] = (byte)((cbImit.Val & 7) << 2);
                if (chbPF.Val) data[9] |= 1;
                if (chbLoc17.Val) data[9] |= 2;
                if (chbExc02.Val) data[9] |= 0x20;
                data[10] = (byte)((tbCosY.Val & 0x1F) << 3);
                if (chbPobr.Val) data[10] |= 1;
                data[11] = (byte)(tbCosY.Val >> 5);
                data[13] = CheckSumCalculator.Calculate(data, 0, data.Length - 1);

                Data = data;

                if (DataController != null)
                {
                    string s = "";
                    for (int i = 0; i < 13; ++i)
                        s += String.Format("{0:X2} ", Data[i]);
                    s += "КС";

                    DataController.DataChanged(s);
                }
            }

            /// <summary>
            /// Вывести данные в окно
            /// </summary>
            /// <param name="progData">Массив данных</param>
            /// <param name="resultTextBlock">Текстовый элемент в котором необходимо вывести данные</param>
            /// <param name="displayArray">Вывод массива данных в указанный TextBlock</param>
            /// <param name="noDataError">Вывод сообщения об отсутствии даннных в указанный TextBlock</param>
            /// <param name="checkSumError">Вывод сообщения об ошибке контрольной суммы в указанный TextBlock</param>
            /// <param name="packetError">Вывод сообщения об ошибке пакета в указанный TextBlock</param>
            public override void DisplayData(byte[] data, TextBlock resultTextBlock, Action<TextBlock, byte[]> displayArray, Action<TextBlock> noDataError, Action<TextBlock> checkSumError, Action<TextBlock> packetError, object parameter)
            {
                if (n8sygnals.ImitCounter != 0) { --n8sygnals.ImitCounter; }

                // Выход, если получены данные, которые уже были выведены
                if ((lastDisplayedData != null) && (data != null) && (lastDisplayedData.Length == data.Length))
                {
                    bool equal = true;
                    for (int i = 0; i < data.Length; ++i)
                        if (lastDisplayedData[i] != data[i])
                        {
                            equal = false;
                            break;
                        }
                    if (equal) return;
                }
                lastDisplayedData = data;

                // Проверяем наличие данных
                if ((data == null) || (data.Length <= 0))
                {
                    noDataError(resultTextBlock);
                    if (parameter != null) ((TextBlock)parameter).Text = "";
                }
                // Проверяем длину пакета
                else if (data.Length != 14)
                {
                    packetError(resultTextBlock);
                    resultTextBlock.Inlines.Add(Environment.NewLine);
                    displayArray(resultTextBlock, data);
                    resultTextBlock.Inlines.Add(Environment.NewLine + Environment.NewLine + "Ожидаемая длина пакета - 14 байт");
                    if (parameter != null) ((TextBlock)parameter).Text = "";
                }
                else
                {
                    if (parameter != null)
                    {
                        ((TextBlock)parameter).Text = "НР21.10.02ОК: ";
                        ((TextBlock)parameter).Inlines.Add(new Run(Convert.ToString(data[0], 2).PadLeft(8, '0') + 'b') { FontFamily = new FontFamily("Courier New"), FontWeight = FontWeights.Bold });
                    }

                    resultTextBlock.Inlines.Clear();
                    displayArray(resultTextBlock, data);
                    resultTextBlock.Inlines.Add(Environment.NewLine);
                    resultTextBlock.Inlines.Add(Environment.NewLine + "Имит вкл.\t\t= ");
                    resultTextBlock.Inlines.Add(new Run(((data[0] >> 7) & 1).ToString()) { FontFamily = new FontFamily("Courier New"), FontWeight = FontWeights.Bold });
                    resultTextBlock.Inlines.Add(Environment.NewLine + "Контроль Н6.21\t\t= ");
                    resultTextBlock.Inlines.Add(new Run(Convert.ToString(data[0] & 0x7F, 2).PadLeft(7, '0') + 'b') { FontFamily = new FontFamily("Courier New"), FontWeight = FontWeights.Bold });
                    resultTextBlock.Inlines.Add(Environment.NewLine + "P0\t\t\t= ");
                    resultTextBlock.Inlines.Add(new Run(Convert.ToString(data[1], 2).PadLeft(8, '0') + 'b') { FontFamily = new FontFamily("Courier New"), FontWeight = FontWeights.Bold });
                    resultTextBlock.Inlines.Add(Environment.NewLine + "P1\t\t\t= ");
                    resultTextBlock.Inlines.Add(new Run(Convert.ToString(data[2], 2).PadLeft(8, '0') + 'b') { FontFamily = new FontFamily("Courier New"), FontWeight = FontWeights.Bold });
                    resultTextBlock.Inlines.Add(Environment.NewLine + "P2\t\t\t= ");
                    resultTextBlock.Inlines.Add(new Run(Convert.ToString(data[3], 2).PadLeft(8, '0') + 'b') { FontFamily = new FontFamily("Courier New"), FontWeight = FontWeights.Bold });
                    resultTextBlock.Inlines.Add(Environment.NewLine + "P3\t\t\t= ");
                    resultTextBlock.Inlines.Add(new Run(Convert.ToString(data[4], 2).PadLeft(8, '0') + 'b') { FontFamily = new FontFamily("Courier New"), FontWeight = FontWeights.Bold });
                    resultTextBlock.Inlines.Add(Environment.NewLine + "P4\t\t\t= ");
                    resultTextBlock.Inlines.Add(new Run(Convert.ToString(data[5], 2).PadLeft(8, '0') + 'b') { FontFamily = new FontFamily("Courier New"), FontWeight = FontWeights.Bold });
                    resultTextBlock.Inlines.Add(Environment.NewLine + "Ранг1\t\t\t= ");
                    resultTextBlock.Inlines.Add(new Run(Convert.ToString(data[6], 2).PadLeft(8, '0') + 'b') { FontFamily = new FontFamily("Courier New"), FontWeight = FontWeights.Bold });
                    resultTextBlock.Inlines.Add(Environment.NewLine + "Ранг2\t\t\t= ");
                    resultTextBlock.Inlines.Add(new Run(Convert.ToString(data[7], 2).PadLeft(8, '0') + 'b') { FontFamily = new FontFamily("Courier New"), FontWeight = FontWeights.Bold });
                    resultTextBlock.Inlines.Add(Environment.NewLine + "Ранг3\t\t\t= ");
                    resultTextBlock.Inlines.Add(new Run(Convert.ToString(data[8], 2).PadLeft(8, '0') + 'b') { FontFamily = new FontFamily("Courier New"), FontWeight = FontWeights.Bold });
                    resultTextBlock.Inlines.Add(Environment.NewLine + "Ранг4\t\t\t= ");
                    resultTextBlock.Inlines.Add(new Run(Convert.ToString(data[9], 2).PadLeft(8, '0') + 'b') { FontFamily = new FontFamily("Courier New"), FontWeight = FontWeights.Bold });
                    resultTextBlock.Inlines.Add(Environment.NewLine + "Ранг5\t\t\t= ");
                    resultTextBlock.Inlines.Add(new Run(Convert.ToString(data[10], 2).PadLeft(8, '0') + 'b') { FontFamily = new FontFamily("Courier New"), FontWeight = FontWeights.Bold });
                    resultTextBlock.Inlines.Add(Environment.NewLine + "Ранг6\t\t\t= ");
                    resultTextBlock.Inlines.Add(new Run(Convert.ToString(data[11], 2).PadLeft(8, '0') + 'b') { FontFamily = new FontFamily("Courier New"), FontWeight = FontWeights.Bold });
                    resultTextBlock.Inlines.Add(Environment.NewLine + "NFц\t\t\t= ");
                    resultTextBlock.Inlines.Add(new Run((data[12] & 0x3F).ToString()) { FontFamily = new FontFamily("Courier New"), FontWeight = FontWeights.Bold });
                    resultTextBlock.Inlines.Add(Environment.NewLine + "КС\t\t\t= ");
                    resultTextBlock.Inlines.Add(new Run(String.Format("{0:X2}h", data[13])) { FontFamily = new FontFamily("Courier New"), FontWeight = FontWeights.Bold });
                    if (data[13] == CheckSumCalculator.Calculate(data, 0, 13))
                        resultTextBlock.Inlines.Add(" (верно)");
                    else resultTextBlock.Inlines.Add(new Run(" (не верно)") { Foreground = Brushes.Red });

                    // Переход в режим "Имит 400" при получении флага "Имит вкл."
                    if (((data[0] & 0x80) != 0) && (cbImit.SelectedIndex == 0) && (n8sygnals.ImitCounter == 0))
                    {
                        n8sygnals.goImit();
                    }
                }
            }
        }

        /// <summary>
        /// Представление данных ЛС21 в режиме ТРЕН
        /// </summary>
        class LS21_TR_DataView : DataView
        {
            private Label lb1;

            /// <summary>
            /// Конструктор
            /// </summary>
            public LS21_TR_DataView()
            {
                Data = new byte[14];
                for (int i = 0; i < 14; ++i)
                    Data[i] = 0;
                Data[0] = Data[13] = 0x60;

                lastDisplayedData = null;

                lb1 = new Label();
                lb1.Content = "<НЕТ ДАННЫХ>";
                lb1.Margin = new Thickness(10, 10, 0, 0);
            }

            /// <summary>
            /// Инициализация представления
            /// </summary>
            /// <param name="testGrid">Сетка в которой необходимо поместить все элементы управления</param>
            protected override void Init(Grid testGrid)
            {
                Initialized = false;
                testGrid.Children.Add(lb1);
                Initialized = true;
            }

            /// <summary>
            /// Получить введенные данные и разместить в массиве Data
            /// </summary>
            public override void Calculate()
            {
                if (DataController != null)
                {
                    string s = "";
                    for (int i = 0; i < 13; ++i)
                        s += String.Format("{0:X2} ", Data[i]);
                    s += "КС";

                    DataController.DataChanged(s);
                }
            }

            /// <summary>
            /// Вывести данные в окно
            /// </summary>
            /// <param name="progData">Массив данных</param>
            /// <param name="resultTextBlock">Текстовый элемент в котором необходимо вывести данные</param>
            /// <param name="displayArray">Вывод массива данных в указанный TextBlock</param>
            /// <param name="noDataError">Вывод сообщения об отсутствии даннных в указанный TextBlock</param>
            /// <param name="checkSumError">Вывод сообщения об ошибке контрольной суммы в указанный TextBlock</param>
            /// <param name="packetError">Вывод сообщения об ошибке пакета в указанный TextBlock</param>
            public override void DisplayData(byte[] data, TextBlock resultTextBlock, Action<TextBlock, byte[]> displayArray, Action<TextBlock> noDataError, Action<TextBlock> checkSumError, Action<TextBlock> packetError, object parameter)
            {
                // Выход, если получены данные, которые уже были выведены
                if ((lastDisplayedData != null) && (data != null) && (lastDisplayedData.Length == data.Length))
                {
                    bool equal = true;
                    for (int i = 0; i < data.Length; ++i)
                        if (lastDisplayedData[i] != data[i])
                        {
                            equal = false;
                            break;
                        }
                    if (equal) return;
                }
                lastDisplayedData = data;

                // Проверяем наличие данных
                if ((data == null) || (data.Length <= 0))
                {
                    noDataError(resultTextBlock);
                    if (parameter != null) ((TextBlock)parameter).Text = "";
                }
                // Проверяем длину пакета
                else if (data.Length != 14)
                {
                    packetError(resultTextBlock);
                    resultTextBlock.Inlines.Add(Environment.NewLine);
                    displayArray(resultTextBlock, data);
                    resultTextBlock.Inlines.Add(Environment.NewLine + Environment.NewLine + "Ожидаемая длина пакета - 14 байт");
                    if (parameter != null) ((TextBlock)parameter).Text = "";
                }
                else
                {
                    if (parameter != null)
                    {
                        ((TextBlock)parameter).Text = "НР21.10.02ОК: ";
                        ((TextBlock)parameter).Inlines.Add(new Run(Convert.ToString(data[0], 2).PadLeft(8, '0') + 'b') { FontFamily = new FontFamily("Courier New"), FontWeight = FontWeights.Bold });
                    }

                    resultTextBlock.Inlines.Clear();
                    displayArray(resultTextBlock, data);
                    resultTextBlock.Inlines.Add(Environment.NewLine);
                    resultTextBlock.Inlines.Add(Environment.NewLine + "Имит вкл.\t\t= ");
                    resultTextBlock.Inlines.Add(new Run(((data[0] >> 7) & 1).ToString()) { FontFamily = new FontFamily("Courier New"), FontWeight = FontWeights.Bold });
                    resultTextBlock.Inlines.Add(Environment.NewLine + "Контроль Н6.21\t\t= ");
                    resultTextBlock.Inlines.Add(new Run(Convert.ToString(data[0] & 0x7F, 2).PadLeft(7, '0') + 'b') { FontFamily = new FontFamily("Courier New"), FontWeight = FontWeights.Bold });
                    resultTextBlock.Inlines.Add(Environment.NewLine + "КС\t\t\t= ");
                    resultTextBlock.Inlines.Add(new Run(String.Format("{0:X2}h", data[13])) { FontFamily = new FontFamily("Courier New"), FontWeight = FontWeights.Bold });
                    if (data[13] == CheckSumCalculator.Calculate(data, 0, 13))
                        resultTextBlock.Inlines.Add(" (верно)");
                    else resultTextBlock.Inlines.Add(new Run(" (не верно)") { Foreground = Brushes.Red });
                }
            }
        }

        /// <summary>
        /// Представление данных ЛС21 в режиме ПРОГ, подрежим - Вход в состояние программирования
        /// </summary>
        class LS21_PR_EnterProg_DataView : DataView
        {
            private Label lb1, lb2;
            private TwoDigitUpDown udI, udJ;

            /// <summary>
            /// Конструктор
            /// </summary>
            public LS21_PR_EnterProg_DataView()
            {
                Data = new byte[14];
                lastDisplayedData = null;

                lb1 = new Label();
                lb1.Content = "Столбец";
                lb1.Margin = new Thickness(10, 10, 0, 0);
                udI = new TwoDigitUpDown(0, 88, 0);
                udI.Margin = new Thickness(120, 10, 0, 0);

                lb2 = new Label();
                lb2.Content = "Строка";
                lb2.Margin = new Thickness(10, 36, 0, 0);
                udJ = new TwoDigitUpDown(0, 44, 0);
                udJ.Margin = new Thickness(120, 36, 0, 0);

                udI.ValueChanged += DefaultHandler;
                udJ.ValueChanged += DefaultHandler;
            }

            /// <summary>
            /// Инициализация представления
            /// </summary>
            /// <param name="testGrid">Сетка в которой необходимо поместить все элементы управления</param>
            protected override void Init(Grid testGrid)
            {
                Initialized = false;
                testGrid.Children.Add(lb1);
                testGrid.Children.Add(lb2);
                testGrid.Children.Add(udI);
                testGrid.Children.Add(udJ);
                Initialized = true;
            }

            /// <summary>
            /// Получить введенные данные и разместить в массиве Data
            /// </summary>
            public override void Calculate()
            {
                byte[] data = new byte[14];
                for (int i = 0; i < 14; ++i)
                    data[i] = 0;

                data[0] = 0xC1;
                data[1] = 0x0A;
                data[2] = (byte)udI.Val;
                data[3] = (byte)udJ.Val;
                data[4] = (byte)'E';
                data[5] = (byte)'N';
                data[6] = (byte)'T';
                data[7] = (byte)'E';
                data[8] = (byte)'R';
                data[9] = (byte)' ';
                data[10] = (byte)'P';
                data[11] = (byte)'R';
                data[12] = (byte)'G';
                data[13] = CheckSumCalculator.Calculate(data, 0, data.Length - 1);

                Data = data;

                if (DataController != null)
                {
                    string s = "";
                    for (int i = 0; i < 13; ++i)
                        s += String.Format("{0:X2} ", Data[i]);
                    s += "КС";

                    DataController.DataChanged(s);
                }
            }

            /// <summary>
            /// Вывести данные в окно
            /// </summary>
            /// <param name="progData">Массив данных</param>
            /// <param name="resultTextBlock">Текстовый элемент в котором необходимо вывести данные</param>
            /// <param name="displayArray">Вывод массива данных в указанный TextBlock</param>
            /// <param name="noDataError">Вывод сообщения об отсутствии даннных в указанный TextBlock</param>
            /// <param name="checkSumError">Вывод сообщения об ошибке контрольной суммы в указанный TextBlock</param>
            /// <param name="packetError">Вывод сообщения об ошибке пакета в указанный TextBlock</param>
            public override void DisplayData(byte[] data, TextBlock resultTextBlock, Action<TextBlock, byte[]> displayArray, Action<TextBlock> noDataError, Action<TextBlock> checkSumError, Action<TextBlock> packetError, object parameter)
            {
                // Выход, если получены данные, которые уже были выведены
                if ((lastDisplayedData != null) && (data != null) && (lastDisplayedData.Length == data.Length))
                {
                    bool equal = true;
                    for (int i = 0; i < data.Length; ++i)
                        if (lastDisplayedData[i] != data[i])
                        {
                            equal = false;
                            break;
                        }
                    if (equal) return;
                }
                lastDisplayedData = data;

                // Проверяем наличие данных
                if ((data == null) || (data.Length <= 0))
                {
                    resultTextBlock.Inlines.Clear();
                    resultTextBlock.Inlines.Add(new Run("Нет данных") { FontWeight = FontWeights.Bold });
                    resultTextBlock.Inlines.Add(Environment.NewLine + Environment.NewLine + "Получение данных не предусмотрено");
                }
                // Проверяем длину пакета
                else
                {
                    packetError(resultTextBlock);
                    resultTextBlock.Inlines.Add(Environment.NewLine);
                    displayArray(resultTextBlock, data);
                    resultTextBlock.Inlines.Add(Environment.NewLine + Environment.NewLine + "Получение данных не предусмотрено");
                }
            }
        }

        /// <summary>
        /// Представление данных ЛС21 в режиме ПРОГ, подрежим - Запрос состояния
        /// </summary>
        class LS21_PR_RequestState_DataView : DataView
        {
            private Label lb1, lb2;
            private TwoDigitUpDown udI, udJ;

            /// <summary>
            /// Конструктор
            /// </summary>
            public LS21_PR_RequestState_DataView()
            {
                Data = new byte[14];
                lastDisplayedData = null;

                lb1 = new Label();
                lb1.Content = "Столбец";
                lb1.Margin = new Thickness(10, 10, 0, 0);
                udI = new TwoDigitUpDown(1, 88, 1);
                udI.Margin = new Thickness(120, 10, 0, 0);

                lb2 = new Label();
                lb2.Content = "Строка";
                lb2.Margin = new Thickness(10, 36, 0, 0);
                udJ = new TwoDigitUpDown(1, 44, 1);
                udJ.Margin = new Thickness(120, 36, 0, 0);

                udI.ValueChanged += DefaultHandler;
                udJ.ValueChanged += DefaultHandler;
            }

            /// <summary>
            /// Инициализация представления
            /// </summary>
            /// <param name="testGrid">Сетка в которой необходимо поместить все элементы управления</param>
            protected override void Init(Grid testGrid)
            {
                Initialized = false;
                testGrid.Children.Add(lb1);
                testGrid.Children.Add(lb2);
                testGrid.Children.Add(udI);
                testGrid.Children.Add(udJ);
                Initialized = true;
            }

            /// <summary>
            /// Получить введенные данные и разместить в массиве Data
            /// </summary>
            public override void Calculate()
            {
                byte[] data = new byte[14];
                for (int i = 0; i < 14; ++i)
                    data[i] = 0;

                data[0] = 0xC2;
                data[1] = 0x0A;
                data[2] = (byte)udI.Val;
                data[3] = (byte)udJ.Val;
                data[4] = (byte)'S';
                data[5] = CheckSumCalculator.Calculate(data, 0, 5);
                data[6] = (byte)'R';
                data[7] = (byte)'E';
                data[8] = (byte)'Q';
                data[9] = (byte)'U';
                data[10] = (byte)'E';
                data[11] = (byte)'S';
                data[12] = (byte)'T';
                data[13] = (byte)' ';

                Data = data;

                if (DataController != null)
                {
                    string s = "";
                    for (int i = 0; i < 13; ++i)
                        s += String.Format("{0:X2} ", Data[i]);
                    s += "КС";

                    DataController.DataChanged(s);
                }
            }

            /// <summary>
            /// Вывести данные в окно
            /// </summary>
            /// <param name="progData">Массив данных</param>
            /// <param name="resultTextBlock">Текстовый элемент в котором необходимо вывести данные</param>
            /// <param name="displayArray">Вывод массива данных в указанный TextBlock</param>
            /// <param name="noDataError">Вывод сообщения об отсутствии даннных в указанный TextBlock</param>
            /// <param name="checkSumError">Вывод сообщения об ошибке контрольной суммы в указанный TextBlock</param>
            /// <param name="packetError">Вывод сообщения об ошибке пакета в указанный TextBlock</param>
            public override void DisplayData(byte[] data, TextBlock resultTextBlock, Action<TextBlock, byte[]> displayArray, Action<TextBlock> noDataError, Action<TextBlock> checkSumError, Action<TextBlock> packetError, object parameter)
            {
                // Выход, если получены данные, которые уже были выведены
                if ((lastDisplayedData != null) && (data != null) && (lastDisplayedData.Length == data.Length))
                {
                    bool equal = true;
                    for (int i = 0; i < data.Length; ++i)
                        if (lastDisplayedData[i] != data[i])
                        {
                            equal = false;
                            break;
                        }
                    if (equal) return;
                }
                lastDisplayedData = data;

                // Проверяем наличие данных
                if ((data == null) || (data.Length <= 0))
                {
                    noDataError(resultTextBlock);
                }
                // Проверяем длину пакета
                else if (data.Length != 5)
                {
                    packetError(resultTextBlock);
                    resultTextBlock.Inlines.Add(Environment.NewLine);
                    displayArray(resultTextBlock, data);
                    resultTextBlock.Inlines.Add(Environment.NewLine + Environment.NewLine + "Ожидаемая длина пакета - 5 байт");
                }
                else
                {
                    resultTextBlock.Inlines.Clear();
                    displayArray(resultTextBlock, data);

                    resultTextBlock.Inlines.Add(Environment.NewLine);
                    resultTextBlock.Inlines.Add(Environment.NewLine + "Квитанция реж. ПРОГ\t= ");
                    resultTextBlock.Inlines.Add(new Run(Convert.ToString(data[0] >> 4, 2).PadLeft(4, '0') + 'b') { FontFamily = new FontFamily("Courier New"), FontWeight = FontWeights.Bold });
                    if ((data[0] >> 4) == 0xC)
                        resultTextBlock.Inlines.Add(" (верно)");
                    else resultTextBlock.Inlines.Add(new Run(" (не верно)") { Foreground = Brushes.Red });
                    resultTextBlock.Inlines.Add(Environment.NewLine + "Квит. подр. Запр. Сост.\t= ");
                    resultTextBlock.Inlines.Add(new Run(Convert.ToString(data[0] & 0xF, 2).PadLeft(4, '0') + 'b') { FontFamily = new FontFamily("Courier New"), FontWeight = FontWeights.Bold });
                    if ((data[0] & 0xF) == 2)
                        resultTextBlock.Inlines.Add(" (верно)");
                    else resultTextBlock.Inlines.Add(new Run(" (не верно)") { Foreground = Brushes.Red });
                    resultTextBlock.Inlines.Add(Environment.NewLine + "Столбец\t\t\t= ");
                    resultTextBlock.Inlines.Add(new Run(data[1].ToString()) { FontFamily = new FontFamily("Courier New"), FontWeight = FontWeights.Bold });
                    resultTextBlock.Inlines.Add(Environment.NewLine + "Строка\t\t\t= ");
                    resultTextBlock.Inlines.Add(new Run(data[2].ToString()) { FontFamily = new FontFamily("Courier New"), FontWeight = FontWeights.Bold });
                    resultTextBlock.Inlines.Add(Environment.NewLine + "Состояние\t\t= ");
                    resultTextBlock.Inlines.Add(new Run(Convert.ToString(data[3], 2).PadLeft(8, '0') + 'b') { FontFamily = new FontFamily("Courier New"), FontWeight = FontWeights.Bold });
                    resultTextBlock.Inlines.Add(Environment.NewLine + "КС\t\t\t= ");
                    resultTextBlock.Inlines.Add(new Run(String.Format("{0:X2}h", data[4])) { FontFamily = new FontFamily("Courier New"), FontWeight = FontWeights.Bold });
                    if (data[4] == CheckSumCalculator.Calculate(data, 0, 4))
                        resultTextBlock.Inlines.Add(" (верно)");
                    else resultTextBlock.Inlines.Add(new Run(" (не верно)") { Foreground = Brushes.Red });
                }
            }
        }

        /// <summary>
        /// Представление данных ЛС21 в режиме ПРОГ, подрежим - Передача адреса
        /// </summary>
        class LS21_PR_SendAdr_DataView : DataView
        {
            private Label lb1, lb2, lb3, lb4;
            private TwoDigitUpDown udI, udJ;
            private HexadecimalMaskedTextBox tbAdr, tbSize;

            /// <summary>
            /// Конструктор
            /// </summary>
            public LS21_PR_SendAdr_DataView()
            {
                Data = new byte[14];
                lastDisplayedData = null;

                lb1 = new Label();
                lb1.Content = "Столбец";
                lb1.Margin = new Thickness(10, 10, 0, 0);
                udI = new TwoDigitUpDown(0, 88, 0);
                udI.Margin = new Thickness(120, 10, 0, 0);

                lb2 = new Label();
                lb2.Content = "Строка";
                lb2.Margin = new Thickness(10, 36, 0, 0);
                udJ = new TwoDigitUpDown(0, 44, 0);
                udJ.Margin = new Thickness(120, 36, 0, 0);

                lb3 = new Label();
                lb3.Content = "Адрес";
                lb3.Margin = new Thickness(10, 62, 0, 0);
                tbAdr = new HexadecimalMaskedTextBox(32);
                tbAdr.Margin = new Thickness(120, 62, 0, 0);

                lb4 = new Label();
                lb4.Content = "Размер массива";
                lb4.Margin = new Thickness(10, 88, 0, 0);
                tbSize = new HexadecimalMaskedTextBox(32);
                tbSize.Margin = new Thickness(120, 88, 0, 0);

                udI.ValueChanged += DefaultHandler;
                udJ.ValueChanged += DefaultHandler;
                tbAdr.TextChanged += DefaultHandler;
                tbSize.TextChanged += DefaultHandler;
            }

            /// <summary>
            /// Инициализация представления
            /// </summary>
            /// <param name="testGrid">Сетка в которой необходимо поместить все элементы управления</param>
            protected override void Init(Grid testGrid)
            {
                Initialized = false;
                testGrid.Children.Add(lb1);
                testGrid.Children.Add(lb2);
                testGrid.Children.Add(lb3);
                testGrid.Children.Add(lb4);
                testGrid.Children.Add(udI);
                testGrid.Children.Add(udJ);
                testGrid.Children.Add(tbAdr);
                testGrid.Children.Add(tbSize);
                Initialized = true;
            }

            /// <summary>
            /// Получить введенные данные и разместить в массиве Data
            /// </summary>
            public override void Calculate()
            {
                byte[] data = new byte[14];
                for (int i = 0; i < 14; ++i)
                    data[i] = 0;

                data[0] = 0xC3;
                data[1] = 0x0A;
                data[2] = (byte)udI.Val;
                data[3] = (byte)udJ.Val;
                data[4] = (byte)'L';
                data[5] = (byte)tbAdr.Val;
                data[6] = (byte)(tbAdr.Val >> 8);
                data[7] = (byte)(tbAdr.Val >> 16);
                data[8] = (byte)(tbAdr.Val >> 24);
                data[9] = (byte)tbSize.Val;
                data[10] = (byte)(tbSize.Val >> 8);
                data[11] = (byte)(tbSize.Val >> 16);
                data[12] = (byte)(tbSize.Val >> 24);
                data[13] = CheckSumCalculator.Calculate(data, 0, data.Length - 1);

                Data = data;

                if (DataController != null)
                {
                    string s = "";
                    for (int i = 0; i < 13; ++i)
                        s += String.Format("{0:X2} ", Data[i]);
                    s += "КС";

                    DataController.DataChanged(s);
                }
            }

            /// <summary>
            /// Вывести данные в окно
            /// </summary>
            /// <param name="progData">Массив данных</param>
            /// <param name="resultTextBlock">Текстовый элемент в котором необходимо вывести данные</param>
            /// <param name="displayArray">Вывод массива данных в указанный TextBlock</param>
            /// <param name="noDataError">Вывод сообщения об отсутствии даннных в указанный TextBlock</param>
            /// <param name="checkSumError">Вывод сообщения об ошибке контрольной суммы в указанный TextBlock</param>
            /// <param name="packetError">Вывод сообщения об ошибке пакета в указанный TextBlock</param>
            public override void DisplayData(byte[] data, TextBlock resultTextBlock, Action<TextBlock, byte[]> displayArray, Action<TextBlock> noDataError, Action<TextBlock> checkSumError, Action<TextBlock> packetError, object parameter)
            {
                // Выход, если получены данные, которые уже были выведены
                if ((lastDisplayedData != null) && (data != null) && (lastDisplayedData.Length == data.Length))
                {
                    bool equal = true;
                    for (int i = 0; i < data.Length; ++i)
                        if (lastDisplayedData[i] != data[i])
                        {
                            equal = false;
                            break;
                        }
                    if (equal) return;
                }
                lastDisplayedData = data;

                // Проверяем наличие данных
                if ((data == null) || (data.Length <= 0))
                {
                    resultTextBlock.Inlines.Clear();
                    resultTextBlock.Inlines.Add(new Run("Нет данных") { FontWeight = FontWeights.Bold });
                    resultTextBlock.Inlines.Add(Environment.NewLine + Environment.NewLine + "Получение данных не предусмотрено");
                }
                // Проверяем длину пакета
                else
                {
                    packetError(resultTextBlock);
                    resultTextBlock.Inlines.Add(Environment.NewLine);
                    displayArray(resultTextBlock, data);
                    resultTextBlock.Inlines.Add(Environment.NewLine + Environment.NewLine + "Получение данных не предусмотрено");
                }
            }
        }

        /// <summary>
        /// Представление данных ЛС21 в режиме ПРОГ, подрежим - Передача данных
        /// </summary>
        class LS21_PR_SendData_DataView : DataView
        {
            private Label lb1, lb2, lb3;
            private HexadecimalMaskedTextBox tbData1, tbData2, tbData3;

            /// <summary>
            /// Конструктор
            /// </summary>
            public LS21_PR_SendData_DataView()
            {
                Data = new byte[14];
                lastDisplayedData = null;

                lb1 = new Label();
                lb1.Content = "Данные слово 1";
                lb1.Margin = new Thickness(10, 10, 0, 0);
                tbData1 = new HexadecimalMaskedTextBox(32);
                tbData1.Margin = new Thickness(120, 10, 0, 0);

                lb2 = new Label();
                lb2.Content = "Данные слово 2";
                lb2.Margin = new Thickness(10, 36, 0, 0);
                tbData2 = new HexadecimalMaskedTextBox(32);
                tbData2.Margin = new Thickness(120, 36, 0, 0);

                lb3 = new Label();
                lb3.Content = "Данные слово 3";
                lb3.Margin = new Thickness(10, 62, 0, 0);
                tbData3 = new HexadecimalMaskedTextBox(32);
                tbData3.Margin = new Thickness(120, 62, 0, 0);

                tbData1.TextChanged += DefaultHandler;
                tbData2.TextChanged += DefaultHandler;
                tbData3.TextChanged += DefaultHandler;
            }

            /// <summary>
            /// Инициализация представления
            /// </summary>
            /// <param name="testGrid">Сетка в которой необходимо поместить все элементы управления</param>
            protected override void Init(Grid testGrid)
            {
                Initialized = false;
                testGrid.Children.Add(lb1);
                testGrid.Children.Add(lb2);
                testGrid.Children.Add(lb3);
                testGrid.Children.Add(tbData1);
                testGrid.Children.Add(tbData2);
                testGrid.Children.Add(tbData3);
                Initialized = true;
            }

            /// <summary>
            /// Получить введенные данные и разместить в массиве Data
            /// </summary>
            public override void Calculate()
            {
                byte[] data = new byte[14];
                for (int i = 0; i < 14; ++i)
                    data[i] = 0;

                data[0] = 0xC4;
                data[1] = (byte)tbData1.Val;
                data[2] = (byte)(tbData1.Val >> 8);
                data[3] = (byte)(tbData1.Val >> 16);
                data[4] = (byte)(tbData1.Val >> 24);
                data[5] = (byte)tbData2.Val;
                data[6] = (byte)(tbData2.Val >> 8);
                data[7] = (byte)(tbData2.Val >> 16);
                data[8] = (byte)(tbData2.Val >> 24);
                data[9] = (byte)tbData3.Val;
                data[10] = (byte)(tbData3.Val >> 8);
                data[11] = (byte)(tbData3.Val >> 16);
                data[12] = (byte)(tbData3.Val >> 24);
                data[13] = CheckSumCalculator.Calculate(data, 0, data.Length - 1);

                Data = data;

                if (DataController != null)
                {
                    string s = "";
                    for (int i = 0; i < 13; ++i)
                        s += String.Format("{0:X2} ", Data[i]);
                    s += "КС";

                    DataController.DataChanged(s);
                }
            }

            /// <summary>
            /// Вывести данные в окно
            /// </summary>
            /// <param name="progData">Массив данных</param>
            /// <param name="resultTextBlock">Текстовый элемент в котором необходимо вывести данные</param>
            /// <param name="displayArray">Вывод массива данных в указанный TextBlock</param>
            /// <param name="noDataError">Вывод сообщения об отсутствии даннных в указанный TextBlock</param>
            /// <param name="checkSumError">Вывод сообщения об ошибке контрольной суммы в указанный TextBlock</param>
            /// <param name="packetError">Вывод сообщения об ошибке пакета в указанный TextBlock</param>
            public override void DisplayData(byte[] data, TextBlock resultTextBlock, Action<TextBlock, byte[]> displayArray, Action<TextBlock> noDataError, Action<TextBlock> checkSumError, Action<TextBlock> packetError, object parameter)
            {
                // Выход, если получены данные, которые уже были выведены
                if ((lastDisplayedData != null) && (data != null) && (lastDisplayedData.Length == data.Length))
                {
                    bool equal = true;
                    for (int i = 0; i < data.Length; ++i)
                        if (lastDisplayedData[i] != data[i])
                        {
                            equal = false;
                            break;
                        }
                    if (equal) return;
                }
                lastDisplayedData = data;

                // Проверяем наличие данных
                if ((data == null) || (data.Length <= 0))
                {
                    resultTextBlock.Inlines.Clear();
                    resultTextBlock.Inlines.Add(new Run("Нет данных") { FontWeight = FontWeights.Bold });
                    resultTextBlock.Inlines.Add(Environment.NewLine + Environment.NewLine + "Получение данных не предусмотрено");
                }
                // Проверяем длину пакета
                else
                {
                    packetError(resultTextBlock);
                    resultTextBlock.Inlines.Add(Environment.NewLine);
                    displayArray(resultTextBlock, data);
                    resultTextBlock.Inlines.Add(Environment.NewLine + Environment.NewLine + "Получение данных не предусмотрено");
                }
            }
        }

        /// <summary>
        /// Представление данных ЛС21 в режиме ПРОГ, подрежим - Перезапись ПЗУ
        /// </summary>
        class LS21_PR_Overwrite_DataView : DataView
        {
            private Label lb1, lb2;
            private TwoDigitUpDown udI, udJ;

            /// <summary>
            /// Конструктор
            /// </summary>
            public LS21_PR_Overwrite_DataView()
            {
                Data = new byte[14];
                lastDisplayedData = null;

                lb1 = new Label();
                lb1.Content = "Столбец";
                lb1.Margin = new Thickness(10, 10, 0, 0);
                udI = new TwoDigitUpDown(1, 88, 1);
                udI.Margin = new Thickness(120, 10, 0, 0);

                lb2 = new Label();
                lb2.Content = "Строка";
                lb2.Margin = new Thickness(10, 36, 0, 0);
                udJ = new TwoDigitUpDown(1, 44, 1);
                udJ.Margin = new Thickness(120, 36, 0, 0);

                udI.ValueChanged += DefaultHandler;
                udJ.ValueChanged += DefaultHandler;
            }

            /// <summary>
            /// Инициализация представления
            /// </summary>
            /// <param name="testGrid">Сетка в которой необходимо поместить все элементы управления</param>
            protected override void Init(Grid testGrid)
            {
                Initialized = false;
                testGrid.Children.Add(lb1);
                testGrid.Children.Add(lb2);
                testGrid.Children.Add(udI);
                testGrid.Children.Add(udJ);
                Initialized = true;
            }

            /// <summary>
            /// Получить введенные данные и разместить в массиве Data
            /// </summary>
            public override void Calculate()
            {
                byte[] data = new byte[14];
                for (int i = 0; i < 14; ++i)
                    data[i] = 0;

                data[0] = 0xC5;
                data[1] = 0x0A;
                data[2] = (byte)udI.Val;
                data[3] = (byte)udJ.Val;
                data[4] = (byte)(~udI.Val);
                data[5] = (byte)(~udJ.Val);
                data[6] = (byte)udI.Val;
                data[7] = (byte)udJ.Val;
                data[8] = (byte)'F';
                data[9] = (byte)'I';
                data[10] = (byte)'R';
                data[11] = (byte)'E';
                data[12] = (byte)'!';
                data[13] = CheckSumCalculator.Calculate(data, 0, data.Length - 1);

                Data = data;

                if (DataController != null)
                {
                    string s = "";
                    for (int i = 0; i < 13; ++i)
                        s += String.Format("{0:X2} ", Data[i]);
                    s += "КС";

                    DataController.DataChanged(s);
                }
            }

            /// <summary>
            /// Вывести данные в окно
            /// </summary>
            /// <param name="progData">Массив данных</param>
            /// <param name="resultTextBlock">Текстовый элемент в котором необходимо вывести данные</param>
            /// <param name="displayArray">Вывод массива данных в указанный TextBlock</param>
            /// <param name="noDataError">Вывод сообщения об отсутствии даннных в указанный TextBlock</param>
            /// <param name="checkSumError">Вывод сообщения об ошибке контрольной суммы в указанный TextBlock</param>
            /// <param name="packetError">Вывод сообщения об ошибке пакета в указанный TextBlock</param>
            public override void DisplayData(byte[] data, TextBlock resultTextBlock, Action<TextBlock, byte[]> displayArray, Action<TextBlock> noDataError, Action<TextBlock> checkSumError, Action<TextBlock> packetError, object parameter)
            {
                // Выход, если получены данные, которые уже были выведены
                if ((lastDisplayedData != null) && (data != null) && (lastDisplayedData.Length == data.Length))
                {
                    bool equal = true;
                    for (int i = 0; i < data.Length; ++i)
                        if (lastDisplayedData[i] != data[i])
                        {
                            equal = false;
                            break;
                        }
                    if (equal) return;
                }
                lastDisplayedData = data;

                // Проверяем наличие данных
                if ((data == null) || (data.Length <= 0))
                {
                    resultTextBlock.Inlines.Clear();
                    resultTextBlock.Inlines.Add(new Run("Нет данных") { FontWeight = FontWeights.Bold });
                    resultTextBlock.Inlines.Add(Environment.NewLine + Environment.NewLine + "Получение данных не предусмотрено");
                }
                // Проверяем длину пакета
                else
                {
                    packetError(resultTextBlock);
                    resultTextBlock.Inlines.Add(Environment.NewLine);
                    displayArray(resultTextBlock, data);
                    resultTextBlock.Inlines.Add(Environment.NewLine + Environment.NewLine + "Получение данных не предусмотрено");
                }
            }
        }

        /// <summary>
        /// Представление данных ЛС21 в режиме ПРОГ, подрежим - Чтение данных
        /// </summary>
        class LS21_PR_ReadData_DataView : DataView
        {
            private Label lb1, lb2;
            private TwoDigitUpDown udI, udJ;

            /// <summary>
            /// Конструктор
            /// </summary>
            public LS21_PR_ReadData_DataView()
            {
                Data = new byte[14];
                lastDisplayedData = null;

                lb1 = new Label();
                lb1.Content = "Столбец";
                lb1.Margin = new Thickness(10, 10, 0, 0);
                udI = new TwoDigitUpDown(1, 88, 1);
                udI.Margin = new Thickness(120, 10, 0, 0);

                lb2 = new Label();
                lb2.Content = "Строка";
                lb2.Margin = new Thickness(10, 36, 0, 0);
                udJ = new TwoDigitUpDown(1, 44, 1);
                udJ.Margin = new Thickness(120, 36, 0, 0);

                udI.ValueChanged += DefaultHandler;
                udJ.ValueChanged += DefaultHandler;
            }

            /// <summary>
            /// Инициализация представления
            /// </summary>
            /// <param name="testGrid">Сетка в которой необходимо поместить все элементы управления</param>
            protected override void Init(Grid testGrid)
            {
                Initialized = false;
                testGrid.Children.Add(lb1);
                testGrid.Children.Add(lb2);
                testGrid.Children.Add(udI);
                testGrid.Children.Add(udJ);
                Initialized = true;
            }

            /// <summary>
            /// Получить введенные данные и разместить в массиве Data
            /// </summary>
            public override void Calculate()
            {
                byte[] data = new byte[14];
                for (int i = 0; i < 14; ++i)
                    data[i] = 0;

                data[0] = 0xC6;
                data[1] = 0x0A;
                data[2] = (byte)udI.Val;
                data[3] = (byte)udJ.Val;
                data[4] = (byte)'R';
                data[5] = (byte)'E';
                data[6] = (byte)'A';
                data[7] = (byte)'D';
                data[8] = (byte)' ';
                data[9] = (byte)'D';
                data[10] = (byte)'A';
                data[11] = (byte)'T';
                data[12] = (byte)'A';
                data[13] = CheckSumCalculator.Calculate(data, 0, data.Length - 1);

                Data = data;

                if (DataController != null)
                {
                    string s = "";
                    for (int i = 0; i < 13; ++i)
                        s += String.Format("{0:X2} ", Data[i]);
                    s += "КС";

                    DataController.DataChanged(s);
                }
            }

            /// <summary>
            /// Вывести данные в окно
            /// </summary>
            /// <param name="progData">Массив данных</param>
            /// <param name="resultTextBlock">Текстовый элемент в котором необходимо вывести данные</param>
            /// <param name="displayArray">Вывод массива данных в указанный TextBlock</param>
            /// <param name="noDataError">Вывод сообщения об отсутствии даннных в указанный TextBlock</param>
            /// <param name="checkSumError">Вывод сообщения об ошибке контрольной суммы в указанный TextBlock</param>
            /// <param name="packetError">Вывод сообщения об ошибке пакета в указанный TextBlock</param>
            public override void DisplayData(byte[] data, TextBlock resultTextBlock, Action<TextBlock, byte[]> displayArray, Action<TextBlock> noDataError, Action<TextBlock> checkSumError, Action<TextBlock> packetError, object parameter)
            {
                // Выход, если получены данные, которые уже были выведены
                if ((lastDisplayedData != null) && (data != null) && (lastDisplayedData.Length == data.Length))
                {
                    bool equal = true;
                    for (int i = 0; i < data.Length; ++i)
                        if (lastDisplayedData[i] != data[i])
                        {
                            equal = false;
                            break;
                        }
                    if (equal) return;
                }
                lastDisplayedData = data;

                // Проверяем наличие данных
                if ((data == null) || (data.Length <= 0))
                {
                    noDataError(resultTextBlock);
                }
                // Проверяем длину пакета
                else if (data.Length != 14)
                {
                    packetError(resultTextBlock);
                    resultTextBlock.Inlines.Add(Environment.NewLine);
                    displayArray(resultTextBlock, data);
                    resultTextBlock.Inlines.Add(Environment.NewLine + Environment.NewLine + "Ожидаемая длина пакета - 14 байт");
                }
                else
                {
                    resultTextBlock.Inlines.Clear();
                    displayArray(resultTextBlock, data);

                    resultTextBlock.Inlines.Add(Environment.NewLine);
                    resultTextBlock.Inlines.Add(Environment.NewLine + "Квитанция реж. ПРОГ\t= ");
                    resultTextBlock.Inlines.Add(new Run(Convert.ToString(data[0] >> 4, 2).PadLeft(4, '0') + 'b') { FontFamily = new FontFamily("Courier New"), FontWeight = FontWeights.Bold });
                    if ((data[0] >> 4) == 0xC)
                        resultTextBlock.Inlines.Add(" (верно)");
                    else resultTextBlock.Inlines.Add(new Run(" (не верно)") { Foreground = Brushes.Red });
                    resultTextBlock.Inlines.Add(Environment.NewLine + "Квит. подр. Чт. Данных\t= ");
                    resultTextBlock.Inlines.Add(new Run(Convert.ToString(data[0] & 0xF, 2).PadLeft(4, '0') + 'b') { FontFamily = new FontFamily("Courier New"), FontWeight = FontWeights.Bold });
                    if ((data[0] & 0xF) == 6)
                        resultTextBlock.Inlines.Add(" (верно)");
                    else resultTextBlock.Inlines.Add(new Run(" (не верно)") { Foreground = Brushes.Red });
                    resultTextBlock.Inlines.Add(Environment.NewLine + "Данные слово 1\t\t= ");
                    resultTextBlock.Inlines.Add(new Run(String.Format("{0:X8}h", data[1] | (data[2] << 8) | (data[3] << 16) | (data[4] << 24))) { FontFamily = new FontFamily("Courier New"), FontWeight = FontWeights.Bold });
                    resultTextBlock.Inlines.Add(Environment.NewLine + "Данные слово 2\t\t= ");
                    resultTextBlock.Inlines.Add(new Run(String.Format("{0:X8}h", data[5] | (data[6] << 8) | (data[7] << 16) | (data[8] << 24))) { FontFamily = new FontFamily("Courier New"), FontWeight = FontWeights.Bold });
                    resultTextBlock.Inlines.Add(Environment.NewLine + "Данные слово 3\t\t= ");
                    resultTextBlock.Inlines.Add(new Run(String.Format("{0:X8}h", data[9] | (data[10] << 8) | (data[11] << 16) | (data[12] << 24))) { FontFamily = new FontFamily("Courier New"), FontWeight = FontWeights.Bold });
                    resultTextBlock.Inlines.Add(Environment.NewLine + "КС\t\t\t= ");
                    resultTextBlock.Inlines.Add(new Run(String.Format("{0:X2}h", data[13])) { FontFamily = new FontFamily("Courier New"), FontWeight = FontWeights.Bold });
                    if (data[13] == CheckSumCalculator.Calculate(data, 0, 13))
                        resultTextBlock.Inlines.Add(" (верно)");
                    else resultTextBlock.Inlines.Add(new Run(" (не верно)") { Foreground = Brushes.Red });
                }
            }
        }

        /// <summary>
        /// Представление данных ЛС21 в режиме ПРОГ, подрежим - Запрос состояния
        /// </summary>
        class LS21_PR_Verification_DataView : DataView
        {
            private Label lb1, lb2, lb3;
            private TwoDigitUpDown udI, udJ, udSector;

            /// <summary>
            /// Конструктор
            /// </summary>
            public LS21_PR_Verification_DataView()
            {
                Data = new byte[14];
                lastDisplayedData = null;

                lb1 = new Label();
                lb1.Content = "Столбец";
                lb1.Margin = new Thickness(10, 10, 0, 0);
                udI = new TwoDigitUpDown(1, 88, 1);
                udI.Margin = new Thickness(120, 10, 0, 0);

                lb2 = new Label();
                lb2.Content = "Строка";
                lb2.Margin = new Thickness(10, 36, 0, 0);
                udJ = new TwoDigitUpDown(1, 44, 1);
                udJ.Margin = new Thickness(120, 36, 0, 0);

                lb3 = new Label();
                lb3.Content = "Сектор";
                lb3.Margin = new Thickness(10, 62, 0, 0);
                udSector = new TwoDigitUpDown(0, 31, 0);
                udSector.Margin = new Thickness(120, 62, 0, 0);

                udI.ValueChanged += DefaultHandler;
                udJ.ValueChanged += DefaultHandler;
                udSector.ValueChanged += DefaultHandler;
            }

            /// <summary>
            /// Инициализация представления
            /// </summary>
            /// <param name="testGrid">Сетка в которой необходимо поместить все элементы управления</param>
            protected override void Init(Grid testGrid)
            {
                Initialized = false;
                testGrid.Children.Add(lb1);
                testGrid.Children.Add(lb2);
                testGrid.Children.Add(lb3);
                testGrid.Children.Add(udI);
                testGrid.Children.Add(udJ);
                testGrid.Children.Add(udSector);
                Initialized = true;
            }

            /// <summary>
            /// Получить введенные данные и разместить в массиве Data
            /// </summary>
            public override void Calculate()
            {
                byte[] data = new byte[14];
                for (int i = 0; i < 14; ++i)
                    data[i] = 0;

                data[0] = 0xC7;
                data[1] = 0x0A;
                data[2] = (byte)udI.Val;
                data[3] = (byte)udJ.Val;
                data[4] = (byte)udSector.Val;
                data[5] = (byte)'V';
                data[6] = (byte)'E';
                data[7] = (byte)'R';
                data[8] = (byte)'I';
                data[9] = (byte)'F';
                data[10] = (byte)'Y';
                data[11] = (byte)' ';
                data[12] = (byte)' ';
                data[13] = CheckSumCalculator.Calculate(data, 0, 13);

                Data = data;

                if (DataController != null)
                {
                    string s = "";
                    for (int i = 0; i < 13; ++i)
                        s += String.Format("{0:X2} ", Data[i]);
                    s += "КС";

                    DataController.DataChanged(s);
                }
            }

            /// <summary>
            /// Вывести данные в окно
            /// </summary>
            /// <param name="progData">Массив данных</param>
            /// <param name="resultTextBlock">Текстовый элемент в котором необходимо вывести данные</param>
            /// <param name="displayArray">Вывод массива данных в указанный TextBlock</param>
            /// <param name="noDataError">Вывод сообщения об отсутствии даннных в указанный TextBlock</param>
            /// <param name="checkSumError">Вывод сообщения об ошибке контрольной суммы в указанный TextBlock</param>
            /// <param name="packetError">Вывод сообщения об ошибке пакета в указанный TextBlock</param>
            public override void DisplayData(byte[] data, TextBlock resultTextBlock, Action<TextBlock, byte[]> displayArray, Action<TextBlock> noDataError, Action<TextBlock> checkSumError, Action<TextBlock> packetError, object parameter)
            {
                // Выход, если получены данные, которые уже были выведены
                if ((lastDisplayedData != null) && (data != null) && (lastDisplayedData.Length == data.Length))
                {
                    bool equal = true;
                    for (int i = 0; i < data.Length; ++i)
                        if (lastDisplayedData[i] != data[i])
                        {
                            equal = false;
                            break;
                        }
                    if (equal) return;
                }
                lastDisplayedData = data;

                // Проверяем наличие данных
                if ((data == null) || (data.Length <= 0))
                {
                    noDataError(resultTextBlock);
                }
                // Проверяем длину пакета
                else if (data.Length != 11)
                {
                    packetError(resultTextBlock);
                    resultTextBlock.Inlines.Add(Environment.NewLine);
                    displayArray(resultTextBlock, data);
                    resultTextBlock.Inlines.Add(Environment.NewLine + Environment.NewLine + "Ожидаемая длина пакета - 11 байт");
                }
                else
                {
                    resultTextBlock.Inlines.Clear();
                    displayArray(resultTextBlock, data);

                    resultTextBlock.Inlines.Add(Environment.NewLine);
                    resultTextBlock.Inlines.Add(Environment.NewLine + "Квитанция реж. ПРОГ\t= ");
                    resultTextBlock.Inlines.Add(new Run(Convert.ToString(data[0] >> 4, 2).PadLeft(4, '0') + 'b') { FontFamily = new FontFamily("Courier New"), FontWeight = FontWeights.Bold });
                    if ((data[0] >> 4) == 0xC)
                        resultTextBlock.Inlines.Add(" (верно)");
                    else resultTextBlock.Inlines.Add(new Run(" (не верно)") { Foreground = Brushes.Red });
                    resultTextBlock.Inlines.Add(Environment.NewLine + "Квит. подр. Верификация\t= ");
                    resultTextBlock.Inlines.Add(new Run(Convert.ToString(data[0] & 0xF, 2).PadLeft(4, '0') + 'b') { FontFamily = new FontFamily("Courier New"), FontWeight = FontWeights.Bold });
                    if ((data[0] & 0xF) == 7)
                        resultTextBlock.Inlines.Add(" (верно)");
                    else resultTextBlock.Inlines.Add(new Run(" (не верно)") { Foreground = Brushes.Red });
                    resultTextBlock.Inlines.Add(Environment.NewLine + "Столбец\t\t\t= ");
                    resultTextBlock.Inlines.Add(new Run(data[1].ToString()) { FontFamily = new FontFamily("Courier New"), FontWeight = FontWeights.Bold });
                    resultTextBlock.Inlines.Add(Environment.NewLine + "Строка\t\t\t= ");
                    resultTextBlock.Inlines.Add(new Run(data[2].ToString()) { FontFamily = new FontFamily("Courier New"), FontWeight = FontWeights.Bold });
                    resultTextBlock.Inlines.Add(Environment.NewLine + "Контрольное слово\t= ");
                    resultTextBlock.Inlines.Add(new Run(String.Format("{0:X2}h", data[3])) { FontFamily = new FontFamily("Courier New"), FontWeight = FontWeights.Bold });
                    if (data[3] == (byte)'Y')
                        resultTextBlock.Inlines.Add(" (верно)");
                    else resultTextBlock.Inlines.Add(new Run(" (не верно)") { Foreground = Brushes.Red });
                    resultTextBlock.Inlines.Add(Environment.NewLine + "N сектора\t\t= ");
                    resultTextBlock.Inlines.Add(new Run(data[4].ToString()) { FontFamily = new FontFamily("Courier New"), FontWeight = FontWeights.Bold });
                    resultTextBlock.Inlines.Add(Environment.NewLine + "CRC32 сектора\t\t= ");
                    resultTextBlock.Inlines.Add(new Run(String.Format("{0:X8}h", data[5] | (data[6] << 8) | (data[7] << 8) | (data[8] << 24))) { FontFamily = new FontFamily("Courier New"), FontWeight = FontWeights.Bold });
                    resultTextBlock.Inlines.Add(Environment.NewLine + "Состояние\t\t= ");
                    resultTextBlock.Inlines.Add(new Run(Convert.ToString(data[9], 2).PadLeft(8, '0') + 'b') { FontFamily = new FontFamily("Courier New"), FontWeight = FontWeights.Bold });
                    resultTextBlock.Inlines.Add(Environment.NewLine + "КС\t\t\t= ");
                    resultTextBlock.Inlines.Add(new Run(String.Format("{0:X2}h", data[10])) { FontFamily = new FontFamily("Courier New"), FontWeight = FontWeights.Bold });
                    if (data[10] == CheckSumCalculator.Calculate(data, 0, 10))
                        resultTextBlock.Inlines.Add(" (верно)");
                    else resultTextBlock.Inlines.Add(new Run(" (не верно)") { Foreground = Brushes.Red });
                }
            }
        }

        /// <summary>
        /// Представление данных ЛС21 в режиме ПРОГ, подрежим - Отмена программирования
        /// </summary>
        class LS21_PR_Cancel_DataView : DataView
        {
            private Label lb1, lb2;
            private TwoDigitUpDown udI, udJ;

            /// <summary>
            /// Конструктор
            /// </summary>
            public LS21_PR_Cancel_DataView()
            {
                Data = new byte[14];
                lastDisplayedData = null;

                lb1 = new Label();
                lb1.Content = "Столбец";
                lb1.Margin = new Thickness(10, 10, 0, 0);
                udI = new TwoDigitUpDown(0, 88, 0);
                udI.Margin = new Thickness(120, 10, 0, 0);

                lb2 = new Label();
                lb2.Content = "Строка";
                lb2.Margin = new Thickness(10, 36, 0, 0);
                udJ = new TwoDigitUpDown(0, 44, 0);
                udJ.Margin = new Thickness(120, 36, 0, 0);

                udI.ValueChanged += DefaultHandler;
                udJ.ValueChanged += DefaultHandler;
            }

            /// <summary>
            /// Инициализация представления
            /// </summary>
            /// <param name="testGrid">Сетка в которой необходимо поместить все элементы управления</param>
            protected override void Init(Grid testGrid)
            {
                Initialized = false;
                testGrid.Children.Add(lb1);
                testGrid.Children.Add(lb2);
                testGrid.Children.Add(udI);
                testGrid.Children.Add(udJ);
                Initialized = true;
            }

            /// <summary>
            /// Получить введенные данные и разместить в массиве Data
            /// </summary>
            public override void Calculate()
            {
                byte[] data = new byte[14];
                for (int i = 0; i < 14; ++i)
                    data[i] = 0;

                data[0] = 0xC8;
                data[1] = 0x0A;
                data[2] = (byte)udI.Val;
                data[3] = (byte)udJ.Val;
                data[4] = (byte)'E';
                data[5] = (byte)'X';
                data[6] = (byte)'I';
                data[7] = (byte)'T';
                data[8] = (byte)' ';
                data[9] = (byte)'P';
                data[10] = (byte)'R';
                data[11] = (byte)'G';
                data[12] = (byte)'M';
                data[13] = CheckSumCalculator.Calculate(data, 0, data.Length - 1);

                Data = data;

                if (DataController != null)
                {
                    string s = "";
                    for (int i = 0; i < 13; ++i)
                        s += String.Format("{0:X2} ", Data[i]);
                    s += "КС";

                    DataController.DataChanged(s);
                }
            }

            /// <summary>
            /// Вывести данные в окно
            /// </summary>
            /// <param name="progData">Массив данных</param>
            /// <param name="resultTextBlock">Текстовый элемент в котором необходимо вывести данные</param>
            /// <param name="displayArray">Вывод массива данных в указанный TextBlock</param>
            /// <param name="noDataError">Вывод сообщения об отсутствии даннных в указанный TextBlock</param>
            /// <param name="checkSumError">Вывод сообщения об ошибке контрольной суммы в указанный TextBlock</param>
            /// <param name="packetError">Вывод сообщения об ошибке пакета в указанный TextBlock</param>
            public override void DisplayData(byte[] data, TextBlock resultTextBlock, Action<TextBlock, byte[]> displayArray, Action<TextBlock> noDataError, Action<TextBlock> checkSumError, Action<TextBlock> packetError, object parameter)
            {
                // Выход, если получены данные, которые уже были выведены
                if ((lastDisplayedData != null) && (data != null) && (lastDisplayedData.Length == data.Length))
                {
                    bool equal = true;
                    for (int i = 0; i < data.Length; ++i)
                        if (lastDisplayedData[i] != data[i])
                        {
                            equal = false;
                            break;
                        }
                    if (equal) return;
                }
                lastDisplayedData = data;

                // Проверяем наличие данных
                if ((data == null) || (data.Length <= 0))
                {
                    resultTextBlock.Inlines.Clear();
                    resultTextBlock.Inlines.Add(new Run("Нет данных") { FontWeight = FontWeights.Bold });
                    resultTextBlock.Inlines.Add(Environment.NewLine + Environment.NewLine + "Получение данных не предусмотрено");
                }
                // Проверяем длину пакета
                else
                {
                    packetError(resultTextBlock);
                    resultTextBlock.Inlines.Add(Environment.NewLine);
                    displayArray(resultTextBlock, data);
                    resultTextBlock.Inlines.Add(Environment.NewLine + Environment.NewLine + "Получение данных не предусмотрено");
                }
            }
        }

        /// <summary>
        /// Представление данных ЛС21 в режиме отладки, подрежим - Проверка синхросигналов
        /// </summary>
        class LS21_DB_Synchr_DataView : DataView
        {
            private Label lb1;

            /// <summary>
            /// Конструктор
            /// </summary>
            public LS21_DB_Synchr_DataView()
            {
                Data = new byte[14];
                for (int i = 0; i < 14; ++i)
                    Data[i] = 0;
                Data[0] = 0xE1;
                Data[1] = 0xA2;
                unchecked { Data[13] = (byte)(0xE1 + 0xA2 + 1); }

                lastDisplayedData = null;

                lb1 = new Label();
                lb1.Content = "<НЕТ ДАННЫХ>";
                lb1.Margin = new Thickness(10, 10, 0, 0);
            }

            /// <summary>
            /// Инициализация представления
            /// </summary>
            /// <param name="testGrid">Сетка в которой необходимо поместить все элементы управления</param>
            protected override void Init(Grid testGrid)
            {
                Initialized = false;
                testGrid.Children.Add(lb1);
                Initialized = true;
            }

            /// <summary>
            /// Получить введенные данные и разместить в массиве Data
            /// </summary>
            public override void Calculate()
            {
                if (DataController != null)
                {
                    string s = "";
                    for (int i = 0; i < 13; ++i)
                        s += String.Format("{0:X2} ", Data[i]);
                    s += "КС";

                    DataController.DataChanged(s);
                }
            }

            /// <summary>
            /// Вывести данные в окно
            /// </summary>
            /// <param name="progData">Массив данных</param>
            /// <param name="resultTextBlock">Текстовый элемент в котором необходимо вывести данные</param>
            /// <param name="displayArray">Вывод массива данных в указанный TextBlock</param>
            /// <param name="noDataError">Вывод сообщения об отсутствии даннных в указанный TextBlock</param>
            /// <param name="checkSumError">Вывод сообщения об ошибке контрольной суммы в указанный TextBlock</param>
            /// <param name="packetError">Вывод сообщения об ошибке пакета в указанный TextBlock</param>
            public override void DisplayData(byte[] data, TextBlock resultTextBlock, Action<TextBlock, byte[]> displayArray, Action<TextBlock> noDataError, Action<TextBlock> checkSumError, Action<TextBlock> packetError, object parameter)
            {
                // Выход, если получены данные, которые уже были выведены
                if ((lastDisplayedData != null) && (data != null) && (lastDisplayedData.Length == data.Length))
                {
                    bool equal = true;
                    
                    for (int i = 0; i < data.Length; ++i)
                        if (lastDisplayedData[i] != data[i])
                        {
                            equal = false;
                            break;
                        }
                    if (equal) return;
                }
                lastDisplayedData = data;

                // Проверяем наличие данных
                if ((data == null) || (data.Length <= 0))
                {
                    
                    noDataError(resultTextBlock);
                }
                // Проверяем длину пакета
                else if (data.Length != 14)
                {
                   packetError(resultTextBlock);
                    resultTextBlock.Inlines.Add(Environment.NewLine);
                    displayArray(resultTextBlock, data);
                    resultTextBlock.Inlines.Add(Environment.NewLine + Environment.NewLine + "Ожидаемая длина пакета - 14 байт");
                }
                else
                {
                    resultTextBlock.Inlines.Clear();
                    displayArray(resultTextBlock, data);

                    resultTextBlock.Inlines.Add(Environment.NewLine);
                    resultTextBlock.Inlines.Add(Environment.NewLine + "Квитанция реж. Отл.\t= ");
                    resultTextBlock.Inlines.Add(new Run(Convert.ToString(data[0] >> 4, 2).PadLeft(4, '0') + 'b') { FontFamily = new FontFamily("Courier New"), FontWeight = FontWeights.Bold });
                    if ((data[0] >> 4) == 0xE)
                        resultTextBlock.Inlines.Add(" (верно)");
                    else resultTextBlock.Inlines.Add(new Run(" (не верно)") { Foreground = Brushes.Red });
                    resultTextBlock.Inlines.Add(Environment.NewLine + "Квит. подр. Пр. СС\t= ");
                    resultTextBlock.Inlines.Add(new Run(Convert.ToString(data[0] & 0xF, 2).PadLeft(4, '0') + 'b') { FontFamily = new FontFamily("Courier New"), FontWeight = FontWeights.Bold });
                    if ((data[0] & 0xF) == 1)
                        resultTextBlock.Inlines.Add(" (верно)");
                    else resultTextBlock.Inlines.Add(new Run(" (не верно)") { Foreground = Brushes.Red });
                    for (int i = 0; i < 12; ++i)
                    {
                        resultTextBlock.Inlines.Add(Environment.NewLine + "R[" + (i + 7) + "]\t\t\t= ");
                        resultTextBlock.Inlines.Add(new Run(Convert.ToString(data[i + 1], 2).PadLeft(8, '0') + 'b') { FontFamily = new FontFamily("Courier New"), FontWeight = FontWeights.Bold });
                        if (i == 10) { resultTextBlock.Inlines.Add(new Run(String.Format(" КС пр. мк1: " + "{0:X2}", data[i+1])) { FontFamily = new FontFamily("Courier New"), FontWeight = FontWeights.Bold }); }
                        if (i == 11) { resultTextBlock.Inlines.Add(" № вер. прог. мк1: " + data[i+1]); }
                    }
                    resultTextBlock.Inlines.Add(Environment.NewLine + "КС\t\t\t= ");
                    resultTextBlock.Inlines.Add(new Run(String.Format("{0:X2}h", data[13])) { FontFamily = new FontFamily("Courier New"), FontWeight = FontWeights.Bold });
                    //for (data[12])
                    //resultTextBlock.Inlines.Add(" (верно)");
                    if (data[13] == CheckSumCalculator.Calculate(data, 0, 13))
                        resultTextBlock.Inlines.Add(" (верно)");
                    else resultTextBlock.Inlines.Add(new Run(" (не верно)") { Foreground = Brushes.Red });
                }
            }
        }

        /// <summary>
        /// Представление данных ЛС21 в режиме отладки, подрежим - Проверка амплитуды по интервалам
        /// </summary>
        class LS21_DB_Intervals_DataView : DataView
        {
            private Label lb1, lb2;
            private ChannelComboBox cbChannel;
            private ImitComboBox cbImit;

            /// <summary>
            /// Конструктор
            /// </summary>
            /// <param name="n8sygnals">Представление сигналов Н8</param>
            public LS21_DB_Intervals_DataView(N8SygnalsView n8sygnals)
            {
                Data = new byte[14];
                lastDisplayedData = null;

                lb1 = new Label();
                lb1.Content = "Канал";
                lb1.Margin = new Thickness(10, 10, 0, 0);
                cbChannel = new ChannelComboBox();
                cbChannel.Margin = new Thickness(120, 10, 0, 0);

                lb2 = new Label();
                lb2.Content = "Имит";
                lb2.Margin = new Thickness(10, 36, 0, 0);
                cbImit = new ImitComboBox();
                cbImit.Margin = new Thickness(120, 36, 0, 0);

                n8sygnals.addImitComboBox(cbImit);

                cbChannel.SelectionChanged += DefaultHandler;
                cbImit.SelectionChanged += DefaultHandler;
            }

            /// <summary>
            /// Инициализация представления
            /// </summary>
            /// <param name="testGrid">Сетка в которой необходимо поместить все элементы управления</param>
            protected override void Init(Grid testGrid)
            {
                Initialized = false;
                testGrid.Children.Add(lb1);
                testGrid.Children.Add(lb2);
                testGrid.Children.Add(cbChannel);
                testGrid.Children.Add(cbImit);
                Initialized = true;
            }

            /// <summary>
            /// Получить введенные данные и разместить в массиве Data
            /// </summary>
            public override void Calculate()
            {
                byte[] data = new byte[14];
                for (int i = 0; i < 14; ++i)
                    data[i] = 0;

                data[0] = 0xE2;
                data[1] = 0xA1;
                data[2] = (byte)(0x90 | (cbChannel.Val & 0xF));
                data[9] = (byte)(((cbImit.Val & 7) << 2) | (1 << 5));
                data[13] = CheckSumCalculator.Calculate(data, 0, data.Length - 1);

                Data = data;

                if (DataController != null)
                {
                    string s = "";
                    for (int i = 0; i < 13; ++i)
                        s += String.Format("{0:X2} ", Data[i]);
                    s += "КС";

                    DataController.DataChanged(s);
                }
            }

            /// <summary>
            /// Вывести данные в окно
            /// </summary>
            /// <param name="progData">Массив данных</param>
            /// <param name="resultTextBlock">Текстовый элемент в котором необходимо вывести данные</param>
            /// <param name="displayArray">Вывод массива данных в указанный TextBlock</param>
            /// <param name="noDataError">Вывод сообщения об отсутствии даннных в указанный TextBlock</param>
            /// <param name="checkSumError">Вывод сообщения об ошибке контрольной суммы в указанный TextBlock</param>
            /// <param name="packetError">Вывод сообщения об ошибке пакета в указанный TextBlock</param>
            public override void DisplayData(byte[] data, TextBlock resultTextBlock, Action<TextBlock, byte[]> displayArray, Action<TextBlock> noDataError, Action<TextBlock> checkSumError, Action<TextBlock> packetError, object parameter)
            {
                // Выход, если получены данные, которые уже были выведены
                if ((lastDisplayedData != null) && (data != null) && (lastDisplayedData.Length == data.Length))
                {
                    bool equal = true;
                    for (int i = 0; i < data.Length; ++i)
                        if (lastDisplayedData[i] != data[i])
                        {
                            equal = false;
                            break;
                        }
                    if (equal) return;
                }
                lastDisplayedData = data;

                // Проверяем наличие данных
                if ((data == null) || (data.Length <= 0))
                {
                    noDataError(resultTextBlock);
                    if (parameter != null) ((TextBlock)parameter).Text = "";
                }
                // Проверяем длину пакета
                else if (data.Length != 14)
                {
                    packetError(resultTextBlock);
                    resultTextBlock.Inlines.Add(Environment.NewLine);
                    displayArray(resultTextBlock, data);
                    resultTextBlock.Inlines.Add(Environment.NewLine + Environment.NewLine + "Ожидаемая длина пакета - 14 байт");
                    if (parameter != null) ((TextBlock)parameter).Text = "";
                }
                else
                {
                    resultTextBlock.Inlines.Clear();
                    displayArray(resultTextBlock, data);
                    resultTextBlock.Inlines.Add(Environment.NewLine);
                    resultTextBlock.Inlines.Add(Environment.NewLine + "№ режима\t\t= ");
                    resultTextBlock.Inlines.Add(new Run(Convert.ToString(data[0] >> 4, 2).PadLeft(4, '0') + 'b') { FontFamily = new FontFamily("Courier New"), FontWeight = FontWeights.Bold });
                    if ((data[0] >> 4) == 0xE)
                        resultTextBlock.Inlines.Add(" (верно)");
                    else resultTextBlock.Inlines.Add(new Run(" (не верно)") { Foreground = Brushes.Red });
                    resultTextBlock.Inlines.Add(Environment.NewLine + "№ канала\t\t= ");
                    resultTextBlock.Inlines.Add(new Run(Convert.ToString(data[0] & 0xF, 2).PadLeft(4, '0') + 'b') { FontFamily = new FontFamily("Courier New"), FontWeight = FontWeights.Bold });
                    if ((data[0] & 0xF) == cbChannel.Val)
                        resultTextBlock.Inlines.Add(" (верно)");
                    else resultTextBlock.Inlines.Add(new Run(" (не верно)") { Foreground = Brushes.Red });
                    resultTextBlock.Inlines.Add(Environment.NewLine + "Амплитуда ПН П4\t= ");
                    resultTextBlock.Inlines.Add(new Run(String.Format("{0:X4}h", (data[1] << 8) | data[2])) { FontFamily = new FontFamily("Courier New"), FontWeight = FontWeights.Bold });
                    resultTextBlock.Inlines.Add(Environment.NewLine + "Амплитуда ПН 20\t= ");
                    resultTextBlock.Inlines.Add(new Run(String.Format("{0:X4}h", (data[3] << 8) | data[4])) { FontFamily = new FontFamily("Courier New"), FontWeight = FontWeights.Bold });
                    resultTextBlock.Inlines.Add(Environment.NewLine + "Амплитуда ПН1\t\t= ");
                    resultTextBlock.Inlines.Add(new Run(String.Format("{0:X4}h", (data[5] << 8) | data[6])) { FontFamily = new FontFamily("Courier New"), FontWeight = FontWeights.Bold });
                    resultTextBlock.Inlines.Add(Environment.NewLine + "Амплитуда ПН2\t\t= ");
                    resultTextBlock.Inlines.Add(new Run(String.Format("{0:X4}h", (data[7] << 8) | data[8])) { FontFamily = new FontFamily("Courier New"), FontWeight = FontWeights.Bold });
                    resultTextBlock.Inlines.Add(Environment.NewLine + "Амплитуда ПН3\t\t= ");
                    resultTextBlock.Inlines.Add(new Run(String.Format("{0:X4}h", (data[9] << 8) | data[10])) { FontFamily = new FontFamily("Courier New"), FontWeight = FontWeights.Bold });
                    resultTextBlock.Inlines.Add(Environment.NewLine + "Амплитуда ПН4\t\t= ");
                    resultTextBlock.Inlines.Add(new Run(String.Format("{0:X4}h", (data[11] << 8) | data[12])) { FontFamily = new FontFamily("Courier New"), FontWeight = FontWeights.Bold });
                    resultTextBlock.Inlines.Add(Environment.NewLine + "КС\t\t\t= ");
                    resultTextBlock.Inlines.Add(new Run(String.Format("{0:X2}h", data[13])) { FontFamily = new FontFamily("Courier New"), FontWeight = FontWeights.Bold });
                    if ((data[13] == 0) || (data[13] == CheckSumCalculator.Calculate(data, 0, 13)))
                        resultTextBlock.Inlines.Add(" (верно)");
                    else resultTextBlock.Inlines.Add(new Run(" (не верно)") { Foreground = Brushes.Red });
                }
            }
        }

        /// <summary>
        /// Представление данных ЛС21 в режиме отладки, подрежим - Проверка формирования ПН1..6
        /// </summary>
        class LS21_DB_PN_DataView : DataView
        {
            private Label lb1, lb2;
            private IntervalComboBox cbInterval;
            private ImitComboBox cbImit;

            /// <summary>
            /// Конструктор
            /// </summary>
            /// <param name="n8sygnals">Представление сигналов Н8</param>
            public LS21_DB_PN_DataView(N8SygnalsView n8sygnals)
            {
                Data = new byte[14];
                lastDisplayedData = null;

                lb1 = new Label();
                lb1.Content = "Интервал";
                lb1.Margin = new Thickness(10, 10, 0, 0);
                cbInterval = new IntervalComboBox();
                cbInterval.Margin = new Thickness(120, 10, 0, 0);

                lb2 = new Label();
                lb2.Content = "Имит";
                lb2.Margin = new Thickness(10, 36, 0, 0);
                cbImit = new ImitComboBox();
                cbImit.Margin = new Thickness(120, 36, 0, 0);

                n8sygnals.addImitComboBox(cbImit);

                cbInterval.SelectionChanged += DefaultHandler;
                cbImit.SelectionChanged += DefaultHandler;
            }

            /// <summary>
            /// Инициализация представления
            /// </summary>
            /// <param name="testGrid">Сетка в которой необходимо поместить все элементы управления</param>
            protected override void Init(Grid testGrid)
            {
                Initialized = false;
                testGrid.Children.Add(lb1);
                testGrid.Children.Add(lb2);
                testGrid.Children.Add(cbInterval);
                testGrid.Children.Add(cbImit);
                Initialized = true;
            }

            /// <summary>
            /// Получить введенные данные и разместить в массиве Data
            /// </summary>
            public override void Calculate()
            {
                byte[] data = new byte[14];
                for (int i = 0; i < 14; ++i)
                    data[i] = 0;

                data[0] = 0xE3;
                data[1] = 0xA1;
                data[2] = (byte)(0xA0 | (cbInterval.Val & 0xF));
                data[9] = (byte)(((cbImit.Val & 7) << 2) | (1 << 5));
                data[13] = CheckSumCalculator.Calculate(data, 0, data.Length - 1);

                Data = data;

                if (DataController != null)
                {
                    string s = "";
                    for (int i = 0; i < 13; ++i)
                        s += String.Format("{0:X2} ", Data[i]);
                    s += "КС";

                    DataController.DataChanged(s);
                }
            }

            /// <summary>
            /// Вывести данные в окно
            /// </summary>
            /// <param name="progData">Массив данных</param>
            /// <param name="resultTextBlock">Текстовый элемент в котором необходимо вывести данные</param>
            /// <param name="displayArray">Вывод массива данных в указанный TextBlock</param>
            /// <param name="noDataError">Вывод сообщения об отсутствии даннных в указанный TextBlock</param>
            /// <param name="checkSumError">Вывод сообщения об ошибке контрольной суммы в указанный TextBlock</param>
            /// <param name="packetError">Вывод сообщения об ошибке пакета в указанный TextBlock</param>
            public override void DisplayData(byte[] data, TextBlock resultTextBlock, Action<TextBlock, byte[]> displayArray, Action<TextBlock> noDataError, Action<TextBlock> checkSumError, Action<TextBlock> packetError, object parameter)
            {
                // Выход, если получены данные, которые уже были выведены
                if ((lastDisplayedData != null) && (data != null) && (lastDisplayedData.Length == data.Length))
                {
                    bool equal = true;
                    for (int i = 0; i < data.Length; ++i)
                        if (lastDisplayedData[i] != data[i])
                        {
                            equal = false;
                            break;
                        }
                    if (equal) return;
                }
                lastDisplayedData = data;

                // Проверяем наличие данных
                if ((data == null) || (data.Length <= 0))
                {
                    noDataError(resultTextBlock);
                    if (parameter != null) ((TextBlock)parameter).Text = "";
                }
                // Проверяем длину пакета
                else if (data.Length != 14)
                {
                    packetError(resultTextBlock);
                    resultTextBlock.Inlines.Add(Environment.NewLine);
                    displayArray(resultTextBlock, data);
                    resultTextBlock.Inlines.Add(Environment.NewLine + Environment.NewLine + "Ожидаемая длина пакета - 14 байт");
                    if (parameter != null) ((TextBlock)parameter).Text = "";
                }
                else
                {
                    resultTextBlock.Inlines.Clear();
                    displayArray(resultTextBlock, data);
                    resultTextBlock.Inlines.Add(Environment.NewLine);
                    resultTextBlock.Inlines.Add(Environment.NewLine + "№ режима\t\t= ");
                    resultTextBlock.Inlines.Add(new Run(Convert.ToString(data[0] >> 4, 2).PadLeft(4, '0') + 'b') { FontFamily = new FontFamily("Courier New"), FontWeight = FontWeights.Bold });
                    if ((data[0] >> 4) == 0xE)
                        resultTextBlock.Inlines.Add(" (верно)");
                    else resultTextBlock.Inlines.Add(new Run(" (не верно)") { Foreground = Brushes.Red });
                    resultTextBlock.Inlines.Add(Environment.NewLine + "№ интервала\t\t= ");
                    resultTextBlock.Inlines.Add(new Run(Convert.ToString(data[0] & 0xF, 2).PadLeft(4, '0') + 'b') { FontFamily = new FontFamily("Courier New"), FontWeight = FontWeights.Bold });
                    if ((data[0] & 0xF) == cbInterval.Val)
                        resultTextBlock.Inlines.Add(" (верно)");
                    else resultTextBlock.Inlines.Add(new Run(" (не верно)") { Foreground = Brushes.Red });
                    resultTextBlock.Inlines.Add(Environment.NewLine + "Порог\t\t\t= ");
                    resultTextBlock.Inlines.Add(new Run(String.Format("{0:X4}h", (data[1] << 8) | data[2])) { FontFamily = new FontFamily("Courier New"), FontWeight = FontWeights.Bold });
                    resultTextBlock.Inlines.Add(Environment.NewLine + "Амплитуда ОК\t\t= ");
                    resultTextBlock.Inlines.Add(new Run(String.Format("{0:X4}h", (data[3] << 8) | data[4])) { FontFamily = new FontFamily("Courier New"), FontWeight = FontWeights.Bold });
                    resultTextBlock.Inlines.Add(Environment.NewLine + "Амплитуда П1\t\t= ");
                    resultTextBlock.Inlines.Add(new Run(String.Format("{0:X4}h", (data[5] << 8) | data[6])) { FontFamily = new FontFamily("Courier New"), FontWeight = FontWeights.Bold });
                    resultTextBlock.Inlines.Add(Environment.NewLine + "Амплитуда П2\t\t= ");
                    resultTextBlock.Inlines.Add(new Run(String.Format("{0:X4}h", (data[7] << 8) | data[8])) { FontFamily = new FontFamily("Courier New"), FontWeight = FontWeights.Bold });
                    resultTextBlock.Inlines.Add(Environment.NewLine + "Амплитуда П3\t\t= ");
                    resultTextBlock.Inlines.Add(new Run(String.Format("{0:X4}h", (data[9] << 8) | data[10])) { FontFamily = new FontFamily("Courier New"), FontWeight = FontWeights.Bold });
                    resultTextBlock.Inlines.Add(Environment.NewLine + "Амплитуда П4\t\t= ");
                    resultTextBlock.Inlines.Add(new Run(String.Format("{0:X4}h", (data[11] << 8) | data[12])) { FontFamily = new FontFamily("Courier New"), FontWeight = FontWeights.Bold });
                    resultTextBlock.Inlines.Add(Environment.NewLine + "ПН ОК\t\t\t= ");
                    resultTextBlock.Inlines.Add(new Run(((data[13] >> 7) & 1).ToString()) { FontFamily = new FontFamily("Courier New"), FontWeight = FontWeights.Bold });
                    resultTextBlock.Inlines.Add(Environment.NewLine + "П4 ОК\t\t\t= ");
                    resultTextBlock.Inlines.Add(new Run(((data[13] >> 4) & 1).ToString()) { FontFamily = new FontFamily("Courier New"), FontWeight = FontWeights.Bold });
                    resultTextBlock.Inlines.Add(Environment.NewLine + "П3 ОК\t\t\t= ");
                    resultTextBlock.Inlines.Add(new Run(((data[13] >> 3) & 1).ToString()) { FontFamily = new FontFamily("Courier New"), FontWeight = FontWeights.Bold });
                    resultTextBlock.Inlines.Add(Environment.NewLine + "П2 ОК\t\t\t= ");
                    resultTextBlock.Inlines.Add(new Run(((data[13] >> 2) & 1).ToString()) { FontFamily = new FontFamily("Courier New"), FontWeight = FontWeights.Bold });
                    resultTextBlock.Inlines.Add(Environment.NewLine + "П1 ОК\t\t\t= ");
                    resultTextBlock.Inlines.Add(new Run(((data[13] >> 1) & 1).ToString()) { FontFamily = new FontFamily("Courier New"), FontWeight = FontWeights.Bold });
                    resultTextBlock.Inlines.Add(Environment.NewLine + "Порог ОК\t\t= ");
                    resultTextBlock.Inlines.Add(new Run((data[13] & 1).ToString()) { FontFamily = new FontFamily("Courier New"), FontWeight = FontWeights.Bold });
                }
            }
        }


        /// <summary>
        /// Представление данных ЛС31 в режиме БР
        /// </summary>
        class LS31_BR_DataView : DataView
        {
            private Label lb1, lb2, lb3, lb4, lb5, lb6, lb7, lb8, lb9, lb10, lb11, lb12, lb13, lb14, lb15;
            private TwoDigitUpDown udNfCorr, udNf1, udNf2, udNf3, udNf4, udN, udI;
            private BinaryMaskedTextBox tbCosX1, tbCosX2, tbCosX3, tbCosX4, tbCosX5, tbCosX6, tbFcorr, tbAcorr;
            private SimpleCheckBox chbPdir1, chbPdir2, chbPdir3, chbPdir4, chbPdir5, chbPdir6, chbPobr, chbPobrCorr, chbMsrCorr;
            private N8SygnalsView n8sygnals;
            /// <summary>
            /// Конструктор
            /// </summary>
            /// <param name="n8sygnals">Представление сигналов Н8</param>
            public LS31_BR_DataView(N8SygnalsView n8sygnals)
            {
                this.n8sygnals = n8sygnals;

                //Int32 Nz;

                Data = new byte[14];
                lastDisplayedData = null;

                lb1 = new Label();
                lb1.Content = "Cos X1";
                lb1.Margin = new Thickness(10, 10, 0, 0);
                tbCosX1 = new BinaryMaskedTextBox(11);
                tbCosX1.Margin = new Thickness(120, 10, 0, 0);
                chbPdir1 = new SimpleCheckBox("Пнапр1");
                chbPdir1.IsChecked = true;
                chbPdir1.Margin = new Thickness(260, 10 + 5, 0, 0);

                lb2 = new Label();
                lb2.Content = "Cos X2";
                lb2.Margin = new Thickness(10, 36, 0, 0);
                tbCosX2 = new BinaryMaskedTextBox(11);
                tbCosX2.Margin = new Thickness(120, 36, 0, 0);
                chbPdir2 = new SimpleCheckBox("Пнапр2");
                chbPdir2.IsChecked = true;
                chbPdir2.Margin = new Thickness(260, 36 + 5, 0, 0);

                lb3 = new Label();
                lb3.Content = "Cos X3";
                lb3.Margin = new Thickness(10, 62, 0, 0);
                tbCosX3 = new BinaryMaskedTextBox(11);
                tbCosX3.Margin = new Thickness(120, 62, 0, 0);
                chbPdir3 = new SimpleCheckBox("Пнапр3");
                chbPdir3.IsChecked = true;
                chbPdir3.Margin = new Thickness(260, 62 + 5, 0, 0);

                lb4 = new Label();
                lb4.Content = "Cos X4";
                lb4.Margin = new Thickness(10, 88, 0, 0);
                tbCosX4 = new BinaryMaskedTextBox(11);
                tbCosX4.Margin = new Thickness(120, 88, 0, 0);
                chbPdir4 = new SimpleCheckBox("Пнапр4");
                chbPdir4.IsChecked = true;
                chbPdir4.Margin = new Thickness(260, 88 + 5, 0, 0);

                lb5 = new Label();
                lb5.Content = "Cos X5";
                lb5.Margin = new Thickness(10, 114, 0, 0);
                tbCosX5 = new BinaryMaskedTextBox(11);
                tbCosX5.Margin = new Thickness(120, 114, 0, 0);
                chbPdir5 = new SimpleCheckBox("Пнапр5");
                chbPdir5.IsChecked = true;
                chbPdir5.Margin = new Thickness(260, 114 + 5, 0, 0);

                lb6 = new Label();
                lb6.Content = "Cos X6";
                lb6.Margin = new Thickness(10, 140, 0, 0);
                tbCosX6 = new BinaryMaskedTextBox(11);
                tbCosX6.Margin = new Thickness(120, 140, 0, 0);
                chbPdir6 = new SimpleCheckBox("Пнапр6");
                chbPdir6.IsChecked = true;
                chbPdir6.Margin = new Thickness(260, 140 + 5, 0, 0);

                lb7 = new Label();
                lb7.Content = "NF попр.";
                lb7.Margin = new Thickness(10, 166, 0, 0);
                udNfCorr = new TwoDigitUpDown(0, 3, 0);
                udNfCorr.Margin = new Thickness(120, 166, 0, 0);
                chbPobr = new SimpleCheckBox("Побр");
                chbPobr.Margin = new Thickness(190 + 5, 166 + 5, 0, 0);
                chbPobrCorr = new SimpleCheckBox("Побр попр");
                chbPobrCorr.Margin = new Thickness(260, 166 + 5, 0, 0);

                lb8 = new Label();
                lb8.Content = "NF1";
                lb8.Margin = new Thickness(10, 192, 0, 0);
                udNf1 = new TwoDigitUpDown(0, 3, 0);
                udNf1.Margin = new Thickness(120, 192, 0, 0);

                lb9 = new Label();
                lb9.Content = "NF2";
                lb9.Margin = new Thickness(190, 192, 0, 0);
                udNf2 = new TwoDigitUpDown(0, 3, 0);
                udNf2.Margin = new Thickness(300, 192, 0, 0);

                lb10 = new Label();
                lb10.Content = "NF3";
                lb10.Margin = new Thickness(10, 218, 0, 0);
                udNf3 = new TwoDigitUpDown(0, 3, 0);
                udNf3.Margin = new Thickness(120, 218, 0, 0);

                lb11 = new Label();
                lb11.Content = "NF4";
                lb11.Margin = new Thickness(190, 218, 0, 0);
                udNf4 = new TwoDigitUpDown(0, 3, 0);
                udNf4.Margin = new Thickness(300, 218, 0, 0);

                lb12 = new Label();
                lb12.Content = "Линейка изм/п";
                lb12.Margin = new Thickness(10, 244, 0, 0);
                udN = new TwoDigitUpDown(0, 3, 0);
                udN.Margin = new Thickness(120, 244, 0, 0);

                lb13 = new Label();
                lb13.Content = "УИ изм/попр";
                lb13.Margin = new Thickness(190, 244, 0, 0);
                udI = new TwoDigitUpDown(1, 21, 1);
                udI.Margin = new Thickness(300, 244, 0, 0);

                chbMsrCorr = new SimpleCheckBox("Измерение/Поправка фазы УИ изм П");
                chbMsrCorr.Margin = new Thickness(10 + 5, 270 + 5, 0, 0);

                lb14 = new Label();
                lb14.Content = "Фаза/попр фазы";
                lb14.Margin = new Thickness(10, 296, 0, 0);
                tbFcorr = new BinaryMaskedTextBox(4);
                tbFcorr.Margin = new Thickness(120, 296, 0, 0);

                lb15 = new Label();
                lb15.Content = "Попр. ампл.";
                lb15.Margin = new Thickness(190, 296, 0, 0);
                tbAcorr = new BinaryMaskedTextBox(3);
                tbAcorr.Margin = new Thickness(300, 296, 0, 0);

                udNfCorr.ValueChanged += DefaultHandler;
                udNf1.ValueChanged += DefaultHandler;
                udNf2.ValueChanged += DefaultHandler;
                udNf3.ValueChanged += DefaultHandler;
                udNf4.ValueChanged += DefaultHandler;
                udN.ValueChanged += DefaultHandler;
                udI.ValueChanged += DefaultHandler;
                tbCosX1.TextChanged += DefaultHandler;
                tbCosX2.TextChanged += DefaultHandler;
                tbCosX3.TextChanged += DefaultHandler;
                tbCosX4.TextChanged += DefaultHandler;
                tbCosX5.TextChanged += DefaultHandler;
                tbCosX6.TextChanged += DefaultHandler;
                tbFcorr.TextChanged += DefaultHandler;
                tbAcorr.TextChanged += DefaultHandler;
                chbPdir1.Click += DefaultHandler;
                chbPdir2.Click += DefaultHandler;
                chbPdir3.Click += DefaultHandler;
                chbPdir4.Click += DefaultHandler;
                chbPdir5.Click += DefaultHandler;
                chbPdir6.Click += DefaultHandler;
                chbPobr.Click += DefaultHandler;
                chbPobrCorr.Click += DefaultHandler;
                chbMsrCorr.Click += DefaultHandler;
            }

            /// <summary>
            /// Инициализация представления
            /// </summary>
            /// <param name="testGrid">Сетка в которой необходимо поместить все элементы управления</param>
            protected override void Init(Grid testGrid)
            {
                Initialized = false;
                
                testGrid.Children.Add(lb1);
                testGrid.Children.Add(lb2);
                testGrid.Children.Add(lb3);
                testGrid.Children.Add(lb4);
                testGrid.Children.Add(lb5);
                testGrid.Children.Add(lb6);
                testGrid.Children.Add(lb7);
                testGrid.Children.Add(lb8);
                testGrid.Children.Add(lb9);
                testGrid.Children.Add(lb10);
                testGrid.Children.Add(lb11);
                testGrid.Children.Add(lb12);
                testGrid.Children.Add(lb13);
                testGrid.Children.Add(lb14);
                testGrid.Children.Add(lb15);
                testGrid.Children.Add(tbCosX1);
                testGrid.Children.Add(chbPdir1);
                testGrid.Children.Add(tbCosX2);
                testGrid.Children.Add(chbPdir2);
                testGrid.Children.Add(tbCosX3);
                testGrid.Children.Add(chbPdir3);
                testGrid.Children.Add(tbCosX4);
                testGrid.Children.Add(chbPdir4);
                testGrid.Children.Add(tbCosX5);
                testGrid.Children.Add(chbPdir5);
                testGrid.Children.Add(tbCosX6);
                testGrid.Children.Add(chbPdir6);
                testGrid.Children.Add(udNfCorr);
                testGrid.Children.Add(chbPobr);
                testGrid.Children.Add(chbPobrCorr);
                testGrid.Children.Add(udNf1);
                testGrid.Children.Add(udNf2);
                testGrid.Children.Add(udNf3);
                testGrid.Children.Add(udNf4);
                testGrid.Children.Add(udN);
                testGrid.Children.Add(udI);
                testGrid.Children.Add(chbMsrCorr);
                testGrid.Children.Add(tbFcorr);
                testGrid.Children.Add(tbAcorr);

                Initialized = true;
            }

            /// <summary>
            /// Получить введенные данные и разместить в массиве Data
            /// </summary>
            public override void Calculate()
            {
                byte[] data = new byte[14];
                for (int i = 0; i < 14; ++i)
                    data[i] = 0;

                data[0] = (byte)(0x10 | ((tbCosX1.Val << 1) & 0xE));
                if (chbPdir1.Val) data[0] |= 1;
                data[1] = (byte)(tbCosX1.Val >> 3);
                data[2] = (byte)(tbCosX2.Val >> 3);
                data[3] = (byte)(((tbCosX2.Val << 5) & 0xE0) | ((tbCosX3.Val << 1) & 0xE));
                if (chbPdir2.Val) data[3] |= 0x10;
                if (chbPdir3.Val) data[3] |= 1;
                data[4] = (byte)(tbCosX3.Val >> 3);
                data[5] = (byte)(tbCosX4.Val >> 3);
                data[6] = (byte)(((tbCosX4.Val << 5) & 0xE0) | ((tbCosX5.Val << 1) & 0xE));
                if (chbPdir4.Val) data[6] |= 0x10;
                if (chbPdir5.Val) data[6] |= 1;
                data[7] = (byte)(tbCosX5.Val >> 3);
                data[8] = (byte)(tbCosX6.Val >> 3);
                data[9] = (byte)(((tbCosX6.Val << 5) & 0xE0) | (udNfCorr.Val & 3));
                if (chbPdir6.Val) data[9] |= 0x10;
                if (chbPobr.Val) data[9] |= 8;
                if (chbPobrCorr.Val) data[9] |= 4;
                data[10] = (byte)((udNf1.Val & 3) | ((udNf2.Val & 3) << 2) | ((udNf3.Val & 3) << 4) | ((udNf4.Val & 3) << 6));
                data[11] = (byte)((udI.Val & 0x1F) | ((udN.Val & 3) << 5));
                if (chbMsrCorr.Val) data[11] |= 0x80;
                data[12] = (byte)((tbAcorr.Val & 7) | ((tbFcorr.Val & 0xF) << 4));
                data[13] = CheckSumCalculator.Calculate(data, 0, 13);

                Data = data;

                if (DataController != null)
                {
                    string s = "";
                    for (int i = 0; i < 13; ++i)
                        s += String.Format("{0:X2} ", Data[i]);
                    s += "КС";

                    DataController.DataChanged(s);
                }

            }

            /// <summary>
            /// Вывести данные в окно
            /// </summary>
            /// <param name="progData">Массив данных</param>
            /// <param name="resultTextBlock">Текстовый элемент в котором необходимо вывести данные</param>
            /// <param name="displayArray">Вывод массива данных в указанный TextBlock</param>
            /// <param name="noDataError">Вывод сообщения об отсутствии даннных в указанный TextBlock</param>
            /// <param name="checkSumError">Вывод сообщения об ошибке контрольной суммы в указанный TextBlock</param>
            /// <param name="packetError">Вывод сообщения об ошибке пакета в указанный TextBlock</param>
            public override void DisplayData(byte[] data, TextBlock resultTextBlock, Action<TextBlock, byte[]> displayArray, Action<TextBlock> noDataError, Action<TextBlock> checkSumError, Action<TextBlock> packetError, object parameter)
            {
                
               
                if (n8sygnals.ImitCounter != 0) { --n8sygnals.ImitCounter; }

                // Выход, если получены данные, которые уже были выведены
                if ((lastDisplayedData != null) && (data != null) && (lastDisplayedData.Length == data.Length))
                {
                    bool equal = true;
                    for (int i = 0; i < data.Length; ++i)
                        if (lastDisplayedData[i] != data[i])
                        {
                            equal = false;
                            break;
                        }
                    if (equal) return;
                }
                lastDisplayedData = data;

                // Проверяем наличие данных
                if ((data == null) || (data.Length <= 0))
                {
                    noDataError(resultTextBlock);
                    if (parameter != null) ((TextBlock)parameter).Text = "";
                }
                // Проверяем длину пакета
                else if (data.Length != 14)
                {
                    packetError(resultTextBlock);
                    resultTextBlock.Inlines.Add(Environment.NewLine);
                    displayArray(resultTextBlock, data);
                    resultTextBlock.Inlines.Add(Environment.NewLine + Environment.NewLine + "Ожидаемая длина пакета - 14 байт");
                    if (parameter != null) ((TextBlock)parameter).Text = "";
                }
                // Выводим данные
                else
                {
                    if (parameter != null)
                    {
                        if ((data[0] & 0x2) != 0x2)
                        {
                            Nz++;
                            if (Nz == 1000000) { Nz = 0; }
                        }
                        ((TextBlock)parameter).Text = "НР21.10.02КП: ";
                        ((TextBlock)parameter).Inlines.Add(new Run(Convert.ToString(data[0], 2).PadLeft(8, '0') + 'b' + '_'+ Nz) { FontFamily = new FontFamily("Courier New"), FontWeight = FontWeights.Bold });
                    }

                    resultTextBlock.Inlines.Clear();
                    displayArray(resultTextBlock, data);

                    resultTextBlock.Inlines.Add(Environment.NewLine);
                    resultTextBlock.Inlines.Add(Environment.NewLine + "Контроль Н6.21 АКП\t= ");
                    resultTextBlock.Inlines.Add(new Run(Convert.ToString(data[0], 2).PadLeft(8, '0') + 'b') { FontFamily = new FontFamily("Courier New"), FontWeight = FontWeights.Bold });
                    resultTextBlock.Inlines.Add(Environment.NewLine + "Пилот сигнал П1 X\t= ");
                    resultTextBlock.Inlines.Add(new Run(String.Format("{0:X4}h", data[1] | (data[2] << 8))) { FontFamily = new FontFamily("Courier New"), FontWeight = FontWeights.Bold });
                    resultTextBlock.Inlines.Add(Environment.NewLine + "Пилот сигнал П1 Y\t= ");
                    resultTextBlock.Inlines.Add(new Run(String.Format("{0:X4}h", data[3] | (data[4] << 8))) { FontFamily = new FontFamily("Courier New"), FontWeight = FontWeights.Bold });
                    resultTextBlock.Inlines.Add(Environment.NewLine + "Пилот сигнал П2 X\t= ");
                    resultTextBlock.Inlines.Add(new Run(String.Format("{0:X4}h", data[5] | (data[6] << 8))) { FontFamily = new FontFamily("Courier New"), FontWeight = FontWeights.Bold });
                    resultTextBlock.Inlines.Add(Environment.NewLine + "Пилот сигнал П2 Y\t= ");
                    resultTextBlock.Inlines.Add(new Run(String.Format("{0:X4}h", data[7] | (data[8] << 8))) { FontFamily = new FontFamily("Courier New"), FontWeight = FontWeights.Bold });
                    resultTextBlock.Inlines.Add(Environment.NewLine + "Пилот сигнал П3 X\t= ");
                    resultTextBlock.Inlines.Add(new Run(String.Format("{0:X4}h", data[9] | (data[10] << 8))) { FontFamily = new FontFamily("Courier New"), FontWeight = FontWeights.Bold });
                    resultTextBlock.Inlines.Add(Environment.NewLine + "Пилот сигнал П3 Y\t= ");
                    resultTextBlock.Inlines.Add(new Run(String.Format("{0:X4}h", data[11] | (data[12] << 8))) { FontFamily = new FontFamily("Courier New"), FontWeight = FontWeights.Bold });
                    resultTextBlock.Inlines.Add(Environment.NewLine + "КС\t\t\t= ");
                    resultTextBlock.Inlines.Add(new Run(String.Format("{0:X2}h", data[13])) { FontFamily = new FontFamily("Courier New"), FontWeight = FontWeights.Bold });
                    if (data[13] == CheckSumCalculator.Calculate(data, 0, 13))
                        resultTextBlock.Inlines.Add(" (верно)");
                    else resultTextBlock.Inlines.Add(new Run(" (не верно)") { Foreground = Brushes.Red });

                    // Переход в режим "Имит 400" при получении флага ошибки сигналов Н8
                    if (((data[0] & 0x80) == 0) && (n8sygnals.ImitCounter == 0))
                    {
                        n8sygnals.goImit();
                    }
                }
            }

            public int Nz { get; set; }
        }

        /// <summary>
        /// Представление данных ЛС31 в режиме ФК
        /// </summary>
        class LS31_FC_DataView : DataView
        {
            private Label lb1, lb2, lb3, lb4, lb5;
            private TwoDigitUpDown udNfCorr, udN, udI;
            private BinaryMaskedTextBox tbFcorr, tbAcorr;
            private SimpleCheckBox chbPobrCorr, chbMsrCorr;
            private N8SygnalsView n8sygnals;

            /// <summary>
            /// Конструктор
            /// </summary>
            /// <param name="n8sygnals">Представление сигналов Н8</param>
            public LS31_FC_DataView(N8SygnalsView n8sygnals)
            {
                this.n8sygnals = n8sygnals;

                Data = new byte[14];
                lastDisplayedData = null;

                lb1 = new Label();
                lb1.Content = "NF попр.";
                lb1.Margin = new Thickness(10, 10, 0, 0);
                udNfCorr = new TwoDigitUpDown(0, 3, 0);
                udNfCorr.Margin = new Thickness(120, 10, 0, 0);
                chbPobrCorr = new SimpleCheckBox("Побр попр");
                chbPobrCorr.Margin = new Thickness(190 + 5, 10 + 5, 0, 0);

                lb2 = new Label();
                lb2.Content = "Линейка изм/п";
                lb2.Margin = new Thickness(10, 36, 0, 0);
                udN = new TwoDigitUpDown(0, 3, 0);
                udN.Margin = new Thickness(120, 36, 0, 0);

                lb3 = new Label();
                lb3.Content = "УИ изм/попр";
                lb3.Margin = new Thickness(190, 36, 0, 0);
                udI = new TwoDigitUpDown(1, 21, 1);
                udI.Margin = new Thickness(300, 36, 0, 0);

                chbMsrCorr = new SimpleCheckBox("Измерение/Поправка фазы УИ изм П");
                chbMsrCorr.Margin = new Thickness(10 + 5, 62 + 5, 0, 0);

                lb4 = new Label();
                lb4.Content = "Фаза/попр фазы";
                lb4.Margin = new Thickness(10, 88, 0, 0);
                tbFcorr = new BinaryMaskedTextBox(4);
                tbFcorr.Margin = new Thickness(120, 88, 0, 0);

                lb5 = new Label();
                lb5.Content = "Попр. ампл.";
                lb5.Margin = new Thickness(190, 88, 0, 0);
                tbAcorr = new BinaryMaskedTextBox(3);
                tbAcorr.Margin = new Thickness(300, 88, 0, 0);

                udNfCorr.ValueChanged += DefaultHandler;
                udN.ValueChanged += DefaultHandler;
                udI.ValueChanged += DefaultHandler;
                tbFcorr.TextChanged += DefaultHandler;
                tbAcorr.TextChanged += DefaultHandler;
                chbPobrCorr.Click += DefaultHandler;
                chbMsrCorr.Click += DefaultHandler;
            }

            /// <summary>
            /// Инициализация представления
            /// </summary>
            /// <param name="testGrid">Сетка в которой необходимо поместить все элементы управления</param>
            protected override void Init(Grid testGrid)
            {
                Initialized = false;

                testGrid.Children.Add(lb1);
                testGrid.Children.Add(lb2);
                testGrid.Children.Add(lb3);
                testGrid.Children.Add(lb4);
                testGrid.Children.Add(lb5);
                testGrid.Children.Add(udNfCorr);
                testGrid.Children.Add(chbPobrCorr);
                testGrid.Children.Add(udN);
                testGrid.Children.Add(udI);
                testGrid.Children.Add(chbMsrCorr);
                testGrid.Children.Add(tbFcorr);
                testGrid.Children.Add(tbAcorr);

                Initialized = true;
            }

            /// <summary>
            /// Получить введенные данные и разместить в массиве Data
            /// </summary>
            public override void Calculate()
            {
                byte[] data = new byte[14];
                for (int i = 0; i < 14; ++i)
                    data[i] = 0;

                data[0] = (byte)(0x20 | (udNfCorr.Val & 3));
                if (chbPobrCorr.Val) data[0] |= 4;
                data[1] = (byte)((udI.Val & 0x1F) | ((udN.Val & 3) << 5));
                if (chbMsrCorr.Val) data[1] |= 0x80;
                data[2] = (byte)((tbAcorr.Val & 7) | ((tbFcorr.Val & 0xF) << 4));
                data[13] = CheckSumCalculator.Calculate(data, 0, 13);
                
                Data = data;

                if (DataController != null)
                {
                    string s = "";
                    for (int i = 0; i < 13; ++i)
                        s += String.Format("{0:X2} ", Data[i]);
                    s += "КС";

                    DataController.DataChanged(s);
                }
            }

            /// <summary>
            /// Вывести данные в окно
            /// </summary>
            /// <param name="progData">Массив данных</param>
            /// <param name="resultTextBlock">Текстовый элемент в котором необходимо вывести данные</param>
            /// <param name="displayArray">Вывод массива данных в указанный TextBlock</param>
            /// <param name="noDataError">Вывод сообщения об отсутствии даннных в указанный TextBlock</param>
            /// <param name="checkSumError">Вывод сообщения об ошибке контрольной суммы в указанный TextBlock</param>
            /// <param name="packetError">Вывод сообщения об ошибке пакета в указанный TextBlock</param>
            public override void DisplayData(byte[] data, TextBlock resultTextBlock, Action<TextBlock, byte[]> displayArray, Action<TextBlock> noDataError, Action<TextBlock> checkSumError, Action<TextBlock> packetError, object parameter)
            {
                if (n8sygnals.ImitCounter != 0) { --n8sygnals.ImitCounter; }

                // Выход, если получены данные, которые уже были выведены
                if ((lastDisplayedData != null) && (data != null) && (lastDisplayedData.Length == data.Length))
                {
                    bool equal = true;
                    for (int i = 0; i < data.Length; ++i)
                        if (lastDisplayedData[i] != data[i])
                        {
                            equal = false;
                            break;
                        }
                    if (equal) return;
                }
                lastDisplayedData = data;

                // Проверяем наличие данных
                if ((data == null) || (data.Length <= 0))
                {
                    noDataError(resultTextBlock);
                    if (parameter != null) ((TextBlock)parameter).Text = "";
                }
                // Проверяем длину пакета
                else if (data.Length != 14)
                {
                    packetError(resultTextBlock);
                    resultTextBlock.Inlines.Add(Environment.NewLine);
                    displayArray(resultTextBlock, data);
                    resultTextBlock.Inlines.Add(Environment.NewLine + Environment.NewLine + "Ожидаемая длина пакета - 14 байт");
                    if (parameter != null) ((TextBlock)parameter).Text = "";
                }
                // Выводим данные
                else
                {
                    if (parameter != null)
                    {
                        ((TextBlock)parameter).Text = "НР21.10.02КП: ";
                        ((TextBlock)parameter).Inlines.Add(new Run(Convert.ToString(data[0], 2).PadLeft(8, '0') + 'b') { FontFamily = new FontFamily("Courier New"), FontWeight = FontWeights.Bold });
                    }

                    resultTextBlock.Inlines.Clear();
                    displayArray(resultTextBlock, data);

                    resultTextBlock.Inlines.Add(Environment.NewLine);
                    resultTextBlock.Inlines.Add(Environment.NewLine + "Контроль Н6.21 АКП\t= ");
                    resultTextBlock.Inlines.Add(new Run(Convert.ToString(data[0], 2).PadLeft(8, '0') + 'b') { FontFamily = new FontFamily("Courier New"), FontWeight = FontWeights.Bold });
                    resultTextBlock.Inlines.Add(Environment.NewLine + "Пилот сигнал X\t\t= ");
                    resultTextBlock.Inlines.Add(new Run(String.Format("{0:X8}h", data[1] | (data[2] << 8) | (data[3] << 16) | (data[4] << 24))) { FontFamily = new FontFamily("Courier New"), FontWeight = FontWeights.Bold });
                    resultTextBlock.Inlines.Add(Environment.NewLine + "Пилот сигнал Y\t\t= ");
                    resultTextBlock.Inlines.Add(new Run(String.Format("{0:X8}h", data[5] | (data[6] << 8) | (data[7] << 16) | (data[8] << 24))) { FontFamily = new FontFamily("Courier New"), FontWeight = FontWeights.Bold });
                    resultTextBlock.Inlines.Add(Environment.NewLine + "КС инф. Н6.21.10\t\t= ");
                    resultTextBlock.Inlines.Add(new Run(String.Format("{0:X2}h", data[9])) { FontFamily = new FontFamily("Courier New"), FontWeight = FontWeights.Bold });
                    if (data[9] == CheckSumCalculator.Calculate(data, 0, 9))
                        resultTextBlock.Inlines.Add(" (верно)");
                    else resultTextBlock.Inlines.Add(new Run(" (не верно)") { Foreground = Brushes.Red });
                    resultTextBlock.Inlines.Add(Environment.NewLine + "Линейка изм.\t\t= ");
                    resultTextBlock.Inlines.Add(new Run((data[10] >> 6).ToString()) { FontFamily = new FontFamily("Courier New"), FontWeight = FontWeights.Bold });
                    resultTextBlock.Inlines.Add(Environment.NewLine + "Строка изм.\t\t= ");
                    resultTextBlock.Inlines.Add(new Run((data[10] & 0x3F).ToString()) { FontFamily = new FontFamily("Courier New"), FontWeight = FontWeights.Bold });
                    resultTextBlock.Inlines.Add(Environment.NewLine + "КС программы ПМ изм.\t= ");
                    resultTextBlock.Inlines.Add(new Run(String.Format("{0:X2}h", data[11])) { FontFamily = new FontFamily("Courier New"), FontWeight = FontWeights.Bold });
                    resultTextBlock.Inlines.Add(Environment.NewLine + "Слово сост. ПМ изм\t= ");
                    resultTextBlock.Inlines.Add(new Run(Convert.ToString(data[12], 2).PadLeft(8, '0') + 'b') { FontFamily = new FontFamily("Courier New"), FontWeight = FontWeights.Bold });
                    resultTextBlock.Inlines.Add(Environment.NewLine + "КС ПМ изм\t\t= ");
                    resultTextBlock.Inlines.Add(new Run(String.Format("{0:X2}h", data[13])) { FontFamily = new FontFamily("Courier New"), FontWeight = FontWeights.Bold });
                    if (data[13] == CheckSumCalculator.Calculate(data, 10, 3))
                        resultTextBlock.Inlines.Add(" (верно)");
                    else resultTextBlock.Inlines.Add(new Run(" (не верно)") { Foreground = Brushes.Red });

                    // Переход в режим "Имит 400" при получении флага ошибки сигналов Н8
                    if (((data[0] & 0x80) == 0) && (n8sygnals.ImitCounter == 0))
                    {
                        n8sygnals.goImit();
                    }
                }
            }
        }

        /// <summary>
        /// Представление данных ЛС31 в режиме ИД, подрежим - Сдвиг ИД
        /// </summary>
        class LS31_ID_Shift_DataView : DataView
        {
            private Label lb1, lb2, lb3, lb4;
            private TwoDigitUpDown udN, udI, udNID, udIID;

            /// <summary>
            /// Конструктор
            /// </summary>
            public LS31_ID_Shift_DataView()
            {
                Data = new byte[14];
                lastDisplayedData = null;

                lb1 = new Label();
                lb1.Content = "Линейка";
                lb1.Margin = new Thickness(10, 10, 0, 0);
                udN = new TwoDigitUpDown(0, 3, 0);
                udN.Margin = new Thickness(120, 10, 0, 0);

                lb2 = new Label();
                lb2.Content = "УИ";
                lb2.Margin = new Thickness(10, 36, 0, 0);
                udI = new TwoDigitUpDown(0, 21, 0);
                udI.Margin = new Thickness(120, 36, 0, 0);

                lb3 = new Label();
                lb3.Content = "Линейка ИД";
                lb3.Margin = new Thickness(10, 62, 0, 0);
                udNID = new TwoDigitUpDown(0, 3, 0);
                udNID.Margin = new Thickness(120, 62, 0, 0);

                lb4 = new Label();
                lb4.Content = "УИ ИД";
                lb4.Margin = new Thickness(10, 88, 0, 0);
                udIID = new TwoDigitUpDown(1, 21, 1);
                udIID.Margin = new Thickness(120, 88, 0, 0);

                udN.ValueChanged += DefaultHandler;
                udI.ValueChanged += DefaultHandler;
                udNID.ValueChanged += DefaultHandler;
                udIID.ValueChanged += DefaultHandler;
            }

            /// <summary>
            /// Инициализация представления
            /// </summary>
            /// <param name="testGrid">Сетка в которой необходимо поместить все элементы управления</param>
            protected override void Init(Grid testGrid)
            {
                Initialized = false;

                testGrid.Children.Add(lb1);
                testGrid.Children.Add(lb2);
                testGrid.Children.Add(lb3);
                testGrid.Children.Add(lb4);
                testGrid.Children.Add(udN);
                testGrid.Children.Add(udI);
                testGrid.Children.Add(udNID);
                testGrid.Children.Add(udIID);

                Initialized = true;
            }

            /// <summary>
            /// Получить введенные данные и разместить в массиве Data
            /// </summary>
            public override void Calculate()
            {
                byte[] data = new byte[14];
                for (int i = 0; i < 14; ++i)
                    data[i] = 0;

                data[0] = 0x3E;
                data[1] = (byte)((udI.Val & 0x3F) | ((udN.Val & 3) << 6));
                data[2] = (byte)((udIID.Val & 0x3F) | ((udNID.Val & 3) << 6));
                data[13] = CheckSumCalculator.Calculate(data, 0, 13);

                Data = data;

                if (DataController != null)
                {
                    string s = "";
                    for (int i = 0; i < 13; ++i)
                        s += String.Format("{0:X2} ", Data[i]);
                    s += "КС";

                    DataController.DataChanged(s);
                }

            }

            /// <summary>
            /// Вывести данные в окно
            /// </summary>
            /// <param name="progData">Массив данных</param>
            /// <param name="resultTextBlock">Текстовый элемент в котором необходимо вывести данные</param>
            /// <param name="displayArray">Вывод массива данных в указанный TextBlock</param>
            /// <param name="noDataError">Вывод сообщения об отсутствии даннных в указанный TextBlock</param>
            /// <param name="checkSumError">Вывод сообщения об ошибке контрольной суммы в указанный TextBlock</param>
            /// <param name="packetError">Вывод сообщения об ошибке пакета в указанный TextBlock</param>
            public override void DisplayData(byte[] data, TextBlock resultTextBlock, Action<TextBlock, byte[]> displayArray, Action<TextBlock> noDataError, Action<TextBlock> checkSumError, Action<TextBlock> packetError, object parameter)
            {
                // Выход, если получены данные, которые уже были выведены
                if ((lastDisplayedData != null) && (data != null) && (lastDisplayedData.Length == data.Length))
                {
                    bool equal = true;
                    for (int i = 0; i < data.Length; ++i)
                        if (lastDisplayedData[i] != data[i])
                        {
                            equal = false;
                            break;
                        }
                    if (equal) return;
                }
                lastDisplayedData = data;

                // Проверяем наличие данных
                if ((data == null) || (data.Length <= 0))
                {
                    noDataError(resultTextBlock);
                }
                // Проверяем длину пакета
                else if (data.Length != 7)
                {
                    packetError(resultTextBlock);
                    resultTextBlock.Inlines.Add(Environment.NewLine);
                    displayArray(resultTextBlock, data);
                    resultTextBlock.Inlines.Add(Environment.NewLine + Environment.NewLine + "Ожидаемая длина пакета - 7 байт");
                }
                // Выводим данные
                else
                {
                    resultTextBlock.Inlines.Clear();
                    displayArray(resultTextBlock, data);

                    resultTextBlock.Inlines.Add(Environment.NewLine);
                    resultTextBlock.Inlines.Add(Environment.NewLine + "Квитанция реж. ИД\t= ");
                    resultTextBlock.Inlines.Add(new Run(Convert.ToString(data[0] >> 4, 2).PadLeft(4, '0') + 'b') { FontFamily = new FontFamily("Courier New"), FontWeight = FontWeights.Bold });
                    if ((data[0] >> 4) == 3)
                        resultTextBlock.Inlines.Add(" (верно)");
                    else resultTextBlock.Inlines.Add(new Run(" (не верно)") { Foreground = Brushes.Red });
                    resultTextBlock.Inlines.Add(Environment.NewLine + "Квит. подр. Сдвиг ИД\t= ");
                    resultTextBlock.Inlines.Add(new Run(Convert.ToString(data[0] & 0xF, 2).PadLeft(4, '0') + 'b') { FontFamily = new FontFamily("Courier New"), FontWeight = FontWeights.Bold });
                    if ((data[0] & 0xF) == 0xE)
                        resultTextBlock.Inlines.Add(" (верно)");
                    else resultTextBlock.Inlines.Add(new Run(" (не верно)") { Foreground = Brushes.Red });
                    resultTextBlock.Inlines.Add(Environment.NewLine + "Зав. №\t\t\t= ");
                    resultTextBlock.Inlines.Add(new Run(String.Format("{0:X8}h", data[1] | (data[2] << 8) | (data[3] << 16) | (data[4] << 24))) { FontFamily = new FontFamily("Courier New"), FontWeight = FontWeights.Bold });
                    resultTextBlock.Inlines.Add(" (" + ((data[1] | (data[2] << 8) | (data[3] << 16) | (data[4] << 24)) & 0xFFFFFFFF).ToString() + ")");
                    resultTextBlock.Inlines.Add(Environment.NewLine + "Линейка ИД\t\t= ");
                    resultTextBlock.Inlines.Add(new Run((data[5] >> 6).ToString()) { FontFamily = new FontFamily("Courier New"), FontWeight = FontWeights.Bold });
                    resultTextBlock.Inlines.Add(Environment.NewLine + "УИ ИД\t\t\t= ");
                    resultTextBlock.Inlines.Add(new Run((data[5] & 0x3F).ToString()) { FontFamily = new FontFamily("Courier New"), FontWeight = FontWeights.Bold });
                    resultTextBlock.Inlines.Add(Environment.NewLine + "КС констант\t\t= ");
                    resultTextBlock.Inlines.Add(new Run(String.Format("{0:X2}h", data[6])) { FontFamily = new FontFamily("Courier New"), FontWeight = FontWeights.Bold });
                }
            }
        }

        /// <summary>
        /// Представление данных ЛС31 в режиме ИД, подрежим - Передача адреса по заводскому номеру
        /// </summary>
        class LS31_ID_FactNum_DataView : DataView
        {
            private Label lb1, lb2, lb3;
            private TwoDigitUpDown udNID, udIID;
            private HexadecimalMaskedTextBox tbFactNum;

            /// <summary>
            /// Конструктор
            /// </summary>
            public LS31_ID_FactNum_DataView()
            {
                Data = new byte[14];
                lastDisplayedData = null;

                lb1 = new Label();
                lb1.Content = "Зав. №";
                lb1.Margin = new Thickness(10, 10, 0, 0);
                tbFactNum = new HexadecimalMaskedTextBox(32);
                tbFactNum.Margin = new Thickness(120, 10, 0, 0);

                lb2 = new Label();
                lb2.Content = "Линейка ИД";
                lb2.Margin = new Thickness(10, 36, 0, 0);
                udNID = new TwoDigitUpDown(0, 3, 0);
                udNID.Margin = new Thickness(120, 36, 0, 0);

                lb3 = new Label();
                lb3.Content = "УИ ИД";
                lb3.Margin = new Thickness(10, 62, 0, 0);
                udIID = new TwoDigitUpDown(1, 21, 1);
                udIID.Margin = new Thickness(120, 62, 0, 0);

                tbFactNum.TextChanged += DefaultHandler;
                udNID.ValueChanged += DefaultHandler;
                udIID.ValueChanged += DefaultHandler;
            }

            /// <summary>
            /// Инициализация представления
            /// </summary>
            /// <param name="testGrid">Сетка в которой необходимо поместить все элементы управления</param>
            protected override void Init(Grid testGrid)
            {
                Initialized = false;

                testGrid.Children.Add(lb1);
                testGrid.Children.Add(lb2);
                testGrid.Children.Add(lb3);
                testGrid.Children.Add(tbFactNum);
                testGrid.Children.Add(udNID);
                testGrid.Children.Add(udIID);

                Initialized = true;
            }

            /// <summary>
            /// Получить введенные данные и разместить в массиве Data
            /// </summary>
            public override void Calculate()
            {
                byte[] data = new byte[14];
                for (int i = 0; i < 14; ++i)
                    data[i] = 0;

                data[0] = 0x3D;
                data[1] = (byte)tbFactNum.Val;
                data[2] = (byte)(tbFactNum.Val >> 8);
                data[3] = (byte)(tbFactNum.Val >> 16);
                data[4] = (byte)(tbFactNum.Val >> 24);
                data[5] = (byte)((udIID.Val & 0x3F) | ((udNID.Val & 3) << 6));
                data[13] = CheckSumCalculator.Calculate(data, 0, 13);

                Data = data;

                if (DataController != null)
                {
                    string s = "";
                    for (int i = 0; i < 13; ++i)
                        s += String.Format("{0:X2} ", Data[i]);
                    s += "КС";

                    DataController.DataChanged(s);
                }

            }

            /// <summary>
            /// Вывести данные в окно
            /// </summary>
            /// <param name="progData">Массив данных</param>
            /// <param name="resultTextBlock">Текстовый элемент в котором необходимо вывести данные</param>
            /// <param name="displayArray">Вывод массива данных в указанный TextBlock</param>
            /// <param name="noDataError">Вывод сообщения об отсутствии даннных в указанный TextBlock</param>
            /// <param name="checkSumError">Вывод сообщения об ошибке контрольной суммы в указанный TextBlock</param>
            /// <param name="packetError">Вывод сообщения об ошибке пакета в указанный TextBlock</param>
            public override void DisplayData(byte[] data, TextBlock resultTextBlock, Action<TextBlock, byte[]> displayArray, Action<TextBlock> noDataError, Action<TextBlock> checkSumError, Action<TextBlock> packetError, object parameter)
            {
                // Выход, если получены данные, которые уже были выведены
                if ((lastDisplayedData != null) && (data != null) && (lastDisplayedData.Length == data.Length))
                {
                    bool equal = true;
                    for (int i = 0; i < data.Length; ++i)
                        if (lastDisplayedData[i] != data[i])
                        {
                            equal = false;
                            break;
                        }
                    if (equal) return;
                }
                lastDisplayedData = data;

                // Проверяем наличие данных
                if ((data == null) || (data.Length <= 0))
                {
                    noDataError(resultTextBlock);
                }
                // Проверяем длину пакета
                else if (data.Length != 3)
                {
                    packetError(resultTextBlock);
                    resultTextBlock.Inlines.Add(Environment.NewLine);
                    displayArray(resultTextBlock, data);
                    resultTextBlock.Inlines.Add(Environment.NewLine + Environment.NewLine + "Ожидаемая длина пакета - 3 байта");
                }
                // Выводим данные
                else
                {
                    resultTextBlock.Inlines.Clear();
                    displayArray(resultTextBlock, data);

                    resultTextBlock.Inlines.Add(Environment.NewLine);
                    resultTextBlock.Inlines.Add(Environment.NewLine + "Квитанция реж. ИД\t= ");
                    resultTextBlock.Inlines.Add(new Run(Convert.ToString(data[0] >> 4, 2).PadLeft(4, '0') + 'b') { FontFamily = new FontFamily("Courier New"), FontWeight = FontWeights.Bold });
                    if ((data[0] >> 4) == 3)
                        resultTextBlock.Inlines.Add(" (верно)");
                    else resultTextBlock.Inlines.Add(new Run(" (не верно)") { Foreground = Brushes.Red });
                    resultTextBlock.Inlines.Add(Environment.NewLine + "Квит. подр. Прд. Адр.\t= ");
                    resultTextBlock.Inlines.Add(new Run(Convert.ToString(data[0] & 0xF, 2).PadLeft(4, '0') + 'b') { FontFamily = new FontFamily("Courier New"), FontWeight = FontWeights.Bold });
                    if ((data[0] & 0xF) == 0xD)
                        resultTextBlock.Inlines.Add(" (верно)");
                    else resultTextBlock.Inlines.Add(new Run(" (не верно)") { Foreground = Brushes.Red });
                    resultTextBlock.Inlines.Add(Environment.NewLine + "Линейка ИД\t\t= ");
                    resultTextBlock.Inlines.Add(new Run((data[1] >> 6).ToString()) { FontFamily = new FontFamily("Courier New"), FontWeight = FontWeights.Bold });
                    resultTextBlock.Inlines.Add(Environment.NewLine + "УИ ИД\t\t\t= ");
                    resultTextBlock.Inlines.Add(new Run((data[1] & 0x3F).ToString()) { FontFamily = new FontFamily("Courier New"), FontWeight = FontWeights.Bold });
                    resultTextBlock.Inlines.Add(Environment.NewLine + "КС констант\t\t= ");
                    resultTextBlock.Inlines.Add(new Run(String.Format("{0:X2}h", data[2])) { FontFamily = new FontFamily("Courier New"), FontWeight = FontWeights.Bold });
                }
            }
        }

        /// <summary>
        /// Представление данных ЛС31 в режиме ИД, подрежим - Чтение констант ППМ
        /// </summary>
        class LS31_ID_RdConst_DataView : DataView
        {
            private Label lb1, lb2, lb3;
            private TwoDigitUpDown udN, udI, udNf;

            /// <summary>
            /// Конструктор
            /// </summary>
            public LS31_ID_RdConst_DataView()
            {
                Data = new byte[14];
                lastDisplayedData = null;

                lb1 = new Label();
                lb1.Content = "Линейка";
                lb1.Margin = new Thickness(10, 10, 0, 0);
                udN = new TwoDigitUpDown(0, 3, 0);
                udN.Margin = new Thickness(120, 10, 0, 0);

                lb2 = new Label();
                lb2.Content = "УИ";
                lb2.Margin = new Thickness(10, 36, 0, 0);
                udI = new TwoDigitUpDown(1, 21, 1);
                udI.Margin = new Thickness(120, 36, 0, 0);

                lb3 = new Label();
                lb3.Content = "NF";
                lb3.Margin = new Thickness(10, 62, 0, 0);
                udNf = new TwoDigitUpDown(0, 3, 0);
                udNf.Margin = new Thickness(120, 62, 0, 0);

                udN.ValueChanged += DefaultHandler;
                udI.ValueChanged += DefaultHandler;
                udNf.ValueChanged += DefaultHandler;
            }

            /// <summary>
            /// Инициализация представления
            /// </summary>
            /// <param name="testGrid">Сетка в которой необходимо поместить все элементы управления</param>
            protected override void Init(Grid testGrid)
            {
                Initialized = false;

                testGrid.Children.Add(lb1);
                testGrid.Children.Add(lb2);
                testGrid.Children.Add(lb3);
                testGrid.Children.Add(udN);
                testGrid.Children.Add(udI);
                testGrid.Children.Add(udNf);

                Initialized = true;
            }

            /// <summary>
            /// Получить введенные данные и разместить в массиве Data
            /// </summary>
            public override void Calculate()
            {
                byte[] data = new byte[14];
                for (int i = 0; i < 14; ++i)
                    data[i] = 0;

                data[0] = 0x3C;
                data[1] = (byte)((udI.Val & 0x3F) | ((udN.Val & 3) << 6));
                data[2] = (byte)(udNf.Val & 3);
                data[13] = CheckSumCalculator.Calculate(data, 0, 13);

                Data = data;

                if (DataController != null)
                {
                    string s = "";
                    for (int i = 0; i < 13; ++i)
                        s += String.Format("{0:X2} ", Data[i]);
                    s += "КС";

                    DataController.DataChanged(s);
                }

            }

            /// <summary>
            /// Вывести данные в окно
            /// </summary>
            /// <param name="progData">Массив данных</param>
            /// <param name="resultTextBlock">Текстовый элемент в котором необходимо вывести данные</param>
            /// <param name="displayArray">Вывод массива данных в указанный TextBlock</param>
            /// <param name="noDataError">Вывод сообщения об отсутствии даннных в указанный TextBlock</param>
            /// <param name="checkSumError">Вывод сообщения об ошибке контрольной суммы в указанный TextBlock</param>
            /// <param name="packetError">Вывод сообщения об ошибке пакета в указанный TextBlock</param>
            public override void DisplayData(byte[] data, TextBlock resultTextBlock, Action<TextBlock, byte[]> displayArray, Action<TextBlock> noDataError, Action<TextBlock> checkSumError, Action<TextBlock> packetError, object parameter)
            {
                // Выход, если получены данные, которые уже были выведены
                if ((lastDisplayedData != null) && (data != null) && (lastDisplayedData.Length == data.Length))
                {
                    bool equal = true;
                    for (int i = 0; i < data.Length; ++i)
                        if (lastDisplayedData[i] != data[i])
                        {
                            equal = false;
                            break;
                        }
                    if (equal) return;
                }
                lastDisplayedData = data;

                // Проверяем наличие данных
                if ((data == null) || (data.Length <= 0))
                {
                    noDataError(resultTextBlock);
                }
                // Проверяем длину пакета
                else if (data.Length != 10)
                {
                    packetError(resultTextBlock);
                    resultTextBlock.Inlines.Add(Environment.NewLine);
                    displayArray(resultTextBlock, data);
                    resultTextBlock.Inlines.Add(Environment.NewLine + Environment.NewLine + "Ожидаемая длина пакета - 10 байт");
                }
                // Выводим данные
                else
                {
                    resultTextBlock.Inlines.Clear();
                    displayArray(resultTextBlock, data);

                    resultTextBlock.Inlines.Add(Environment.NewLine);
                    resultTextBlock.Inlines.Add(Environment.NewLine + "Квитанция реж. ИД\t= ");
                    resultTextBlock.Inlines.Add(new Run(Convert.ToString(data[0] >> 4, 2).PadLeft(4, '0') + 'b') { FontFamily = new FontFamily("Courier New"), FontWeight = FontWeights.Bold });
                    if ((data[0] >> 4) == 3)
                        resultTextBlock.Inlines.Add(" (верно)");
                    else resultTextBlock.Inlines.Add(new Run(" (не верно)") { Foreground = Brushes.Red });
                    resultTextBlock.Inlines.Add(Environment.NewLine + "Квит. подр. Чт. Конст.\t= ");
                    resultTextBlock.Inlines.Add(new Run(Convert.ToString(data[0] & 0xF, 2).PadLeft(4, '0') + 'b') { FontFamily = new FontFamily("Courier New"), FontWeight = FontWeights.Bold });
                    if ((data[0] & 0xF) == 0xC)
                        resultTextBlock.Inlines.Add(" (верно)");
                    else resultTextBlock.Inlines.Add(new Run(" (не верно)") { Foreground = Brushes.Red });
                    resultTextBlock.Inlines.Add(Environment.NewLine + "Kx\t\t\t= ");
                    resultTextBlock.Inlines.Add(new Run(String.Format("{0:X4}h", data[1] | (data[2] << 8))) { FontFamily = new FontFamily("Courier New"), FontWeight = FontWeights.Bold });
                    resultTextBlock.Inlines.Add(Environment.NewLine + "Попр. фазы прямо\t= ");
                    resultTextBlock.Inlines.Add(new Run(Convert.ToString(data[6], 2).PadLeft(8, '0') + 'b') { FontFamily = new FontFamily("Courier New"), FontWeight = FontWeights.Bold });
                    resultTextBlock.Inlines.Add(Environment.NewLine + "Попр. фазы обратно\t= ");
                    resultTextBlock.Inlines.Add(new Run(Convert.ToString(data[7], 2).PadLeft(8, '0') + 'b') { FontFamily = new FontFamily("Courier New"), FontWeight = FontWeights.Bold });
                    resultTextBlock.Inlines.Add(Environment.NewLine + "Аттенюатор прямо\t= ");
                    resultTextBlock.Inlines.Add(new Run(Convert.ToString(data[8] & 7, 2).PadLeft(3, '0') + 'b') { FontFamily = new FontFamily("Courier New"), FontWeight = FontWeights.Bold });
                    resultTextBlock.Inlines.Add(Environment.NewLine + "Аттенюатор обратно\t= ");
                    resultTextBlock.Inlines.Add(new Run(Convert.ToString((data[8] >> 3) & 7, 2).PadLeft(3, '0') + 'b') { FontFamily = new FontFamily("Courier New"), FontWeight = FontWeights.Bold });
                    resultTextBlock.Inlines.Add(Environment.NewLine + "КС констант\t\t= ");
                    resultTextBlock.Inlines.Add(new Run(String.Format("{0:X2}h", data[9])) { FontFamily = new FontFamily("Courier New"), FontWeight = FontWeights.Bold });
                }
            }
        }

        /// <summary>
        /// Представление данных ЛС31 в режиме ИД, подрежим - Передача констант ППМ
        /// </summary>
        class LS31_ID_WrConst_DataView : DataView
        {
            private Label lb1, lb2, lb3, lb4, lb5, lb6, lb7, lb8;
            private TwoDigitUpDown udN, udI, udNf;
            private HexadecimalMaskedTextBox tbKx;
            private BinaryMaskedTextBox tbFst, tbFbk, tbAst, tbAbk;

            /// <summary>
            /// Конструктор
            /// </summary>
            public LS31_ID_WrConst_DataView()
            {
                Data = new byte[14];
                lastDisplayedData = null;

                lb1 = new Label();
                lb1.Content = "Линейка";
                lb1.Margin = new Thickness(10, 10, 0, 0);
                udN = new TwoDigitUpDown(0, 3, 0);
                udN.Margin = new Thickness(120, 10, 0, 0);

                lb2 = new Label();
                lb2.Content = "УИ";
                lb2.Margin = new Thickness(220, 10, 0, 0);
                udI = new TwoDigitUpDown(1, 21, 1);
                udI.Margin = new Thickness(300, 10, 0, 0);

                lb3 = new Label();
                lb3.Content = "NF";
                lb3.Margin = new Thickness(10, 36, 0, 0);
                udNf = new TwoDigitUpDown(0, 3, 0);
                udNf.Margin = new Thickness(120, 36, 0, 0);

                lb4 = new Label();
                lb4.Content = "Kx";
                lb4.Margin = new Thickness(10, 62, 0, 0);
                tbKx = new HexadecimalMaskedTextBox(16);
                tbKx.Margin = new Thickness(120, 62, 0, 0);

                lb5 = new Label();
                lb5.Content = "Попр. прямо";
                lb5.Margin = new Thickness(10, 88, 0, 0);
                tbFst = new BinaryMaskedTextBox(8);
                tbFst.Margin = new Thickness(120, 88, 0, 0);

                lb6 = new Label();
                lb6.Content = "Попр. обр.";
                lb6.Margin = new Thickness(10, 114, 0, 0);
                tbFbk = new BinaryMaskedTextBox(8);
                tbFbk.Margin = new Thickness(120, 114, 0, 0);

                lb7 = new Label();
                lb7.Content = "Атт. прямо";
                lb7.Margin = new Thickness(10, 140, 0, 0);
                tbAst = new BinaryMaskedTextBox(3);
                tbAst.Margin = new Thickness(120, 140, 0, 0);

                lb8 = new Label();
                lb8.Content = "Атт. обр.";
                lb8.Margin = new Thickness(220, 140, 0, 0);
                tbAbk = new BinaryMaskedTextBox(3);
                tbAbk.Margin = new Thickness(300, 140, 0, 0);

                udN.ValueChanged += DefaultHandler;
                udI.ValueChanged += DefaultHandler;
                udNf.ValueChanged += DefaultHandler;
                tbKx.TextChanged += DefaultHandler;
                tbFst.TextChanged += DefaultHandler;
                tbFbk.TextChanged += DefaultHandler;
                tbAst.TextChanged += DefaultHandler;
                tbAbk.TextChanged += DefaultHandler;
            }

            /// <summary>
            /// Инициализация представления
            /// </summary>
            /// <param name="testGrid">Сетка в которой необходимо поместить все элементы управления</param>
            protected override void Init(Grid testGrid)
            {
                Initialized = false;
                testGrid.Children.Add(lb1);
                testGrid.Children.Add(lb2);
                testGrid.Children.Add(lb3);
                testGrid.Children.Add(lb4);
                testGrid.Children.Add(lb5);
                testGrid.Children.Add(lb6);
                testGrid.Children.Add(lb7);
                testGrid.Children.Add(lb8);
                testGrid.Children.Add(udN);
                testGrid.Children.Add(udI);
                testGrid.Children.Add(udNf);
                testGrid.Children.Add(tbKx);
                testGrid.Children.Add(tbFst);
                testGrid.Children.Add(tbFbk);
                testGrid.Children.Add(tbAst);
                testGrid.Children.Add(tbAbk);
                Initialized = true;
            }

            /// <summary>
            /// Получить введенные данные и разместить в массиве Data
            /// </summary>
            public override void Calculate()
            {
                byte[] data = new byte[14];
                for (int i = 0; i < 14; ++i)
                    data[i] = 0;

                data[0] = 0x3B;
                data[1] = (byte)((udI.Val & 0x3F) | (udN.Val << 6));
                data[3] = (byte)(udNf.Val & 3);
                data[4] = (byte)tbKx.Val;
                data[5] = (byte)(tbKx.Val >> 8);
                data[9] = (byte)tbFst.Val;
                data[10] = (byte)tbFbk.Val;
                data[11] = (byte)((tbAst.Val & 7) | ((tbAbk.Val & 7) << 3));
                data[13] = CheckSumCalculator.Calculate(data, 0, data.Length - 1);

                Data = data;

                if (DataController != null)
                {
                    string s = "";
                    for (int i = 0; i < 13; ++i)
                        s += String.Format("{0:X2} ", Data[i]);
                    s += "КС";

                    DataController.DataChanged(s);
                }
            }

            /// <summary>
            /// Вывести данные в окно
            /// </summary>
            /// <param name="progData">Массив данных</param>
            /// <param name="resultTextBlock">Текстовый элемент в котором необходимо вывести данные</param>
            /// <param name="displayArray">Вывод массива данных в указанный TextBlock</param>
            /// <param name="noDataError">Вывод сообщения об отсутствии даннных в указанный TextBlock</param>
            /// <param name="checkSumError">Вывод сообщения об ошибке контрольной суммы в указанный TextBlock</param>
            /// <param name="packetError">Вывод сообщения об ошибке пакета в указанный TextBlock</param>
            public override void DisplayData(byte[] data, TextBlock resultTextBlock, Action<TextBlock, byte[]> displayArray, Action<TextBlock> noDataError, Action<TextBlock> checkSumError, Action<TextBlock> packetError, object parameter)
            {
                // Выход, если получены данные, которые уже были выведены
                if ((lastDisplayedData != null) && (data != null) && (lastDisplayedData.Length == data.Length))
                {
                    bool equal = true;
                    for (int i = 0; i < data.Length; ++i)
                        if (lastDisplayedData[i] != data[i])
                        {
                            equal = false;
                            break;
                        }
                    if (equal) return;
                }
                lastDisplayedData = data;

                // Проверяем наличие данных
                if ((data == null) || (data.Length <= 0))
                {
                    noDataError(resultTextBlock);
                }
                // Проверяем длину пакета
                else if (data.Length != 6)
                {
                    packetError(resultTextBlock);
                    resultTextBlock.Inlines.Add(Environment.NewLine);
                    displayArray(resultTextBlock, data);
                    resultTextBlock.Inlines.Add(Environment.NewLine + Environment.NewLine + "Ожидаемая длина пакета - 6 байт");
                }
                else
                {
                    resultTextBlock.Inlines.Clear();
                    displayArray(resultTextBlock, data);

                    resultTextBlock.Inlines.Add(Environment.NewLine);
                    resultTextBlock.Inlines.Add(Environment.NewLine + "Квитанция реж. ИД\t= ");
                    resultTextBlock.Inlines.Add(new Run(Convert.ToString(data[0] >> 4, 2).PadLeft(4, '0') + 'b') { FontFamily = new FontFamily("Courier New"), FontWeight = FontWeights.Bold });
                    if ((data[0] >> 4) == 3)
                        resultTextBlock.Inlines.Add(" (верно)");
                    else resultTextBlock.Inlines.Add(new Run(" (не верно)") { Foreground = Brushes.Red });
                    resultTextBlock.Inlines.Add(Environment.NewLine + "Квит. подр. Чт. Конст.\t= ");
                    resultTextBlock.Inlines.Add(new Run(Convert.ToString(data[0] & 0xF, 2).PadLeft(4, '0') + 'b') { FontFamily = new FontFamily("Courier New"), FontWeight = FontWeights.Bold });
                    if ((data[0] & 0xF) == 0xB)
                        resultTextBlock.Inlines.Add(" (верно)");
                    else resultTextBlock.Inlines.Add(new Run(" (не верно)") { Foreground = Brushes.Red });

                    resultTextBlock.Inlines.Add(Environment.NewLine + "Зав. №\t\t\t= ");
                    resultTextBlock.Inlines.Add(new Run(String.Format("{0:X8}h", data[1] | (data[2] << 8) | (data[3] << 16) | (data[4] << 24))) { FontFamily = new FontFamily("Courier New"), FontWeight = FontWeights.Bold });
                    resultTextBlock.Inlines.Add(" (" + ((data[1] | (data[2] << 8) | (data[3] << 16) | (data[4] << 24)) & 0xFFFFFFFF).ToString() + ")");
                    resultTextBlock.Inlines.Add(Environment.NewLine + "КС слов 1-12\t\t= ");
                    resultTextBlock.Inlines.Add(new Run(String.Format("{0:X2}h", data[5])) { FontFamily = new FontFamily("Courier New"), FontWeight = FontWeights.Bold });
                }
            }
        }

        /// <summary>
        /// Представление данных ЛС31 в режиме ИД, подрежим - Запись констант в ПЗУ
        /// </summary>
        class LS31_ID_Overwrite_DataView : DataView
        {
            private Label lb1, lb2;
            private TwoDigitUpDown udN, udI;

            /// <summary>
            /// Конструктор
            /// </summary>
            public LS31_ID_Overwrite_DataView()
            {
                Data = new byte[14];
                lastDisplayedData = null;

                lb1 = new Label();
                lb1.Content = "Линейка";
                lb1.Margin = new Thickness(10, 10, 0, 0);
                udN = new TwoDigitUpDown(0, 3, 0);
                udN.Margin = new Thickness(120, 10, 0, 0);

                lb2 = new Label();
                lb2.Content = "УИ";
                lb2.Margin = new Thickness(10, 36, 0, 0);
                udI = new TwoDigitUpDown(0, 21, 0);
                udI.Margin = new Thickness(120, 36, 0, 0);

                udN.ValueChanged += DefaultHandler;
                udI.ValueChanged += DefaultHandler;
            }

            /// <summary>
            /// Инициализация представления
            /// </summary>
            /// <param name="testGrid">Сетка в которой необходимо поместить все элементы управления</param>
            protected override void Init(Grid testGrid)
            {
                Initialized = false;

                testGrid.Children.Add(lb1);
                testGrid.Children.Add(lb2);
                testGrid.Children.Add(udN);
                testGrid.Children.Add(udI);

                Initialized = true;
            }

            /// <summary>
            /// Получить введенные данные и разместить в массиве Data
            /// </summary>
            public override void Calculate()
            {
                byte[] data = new byte[14];
                for (int i = 0; i < 14; ++i)
                    data[i] = 0;

                data[0] = 0x3A;
                data[1] = (byte)((udI.Val & 0x3F) | ((udN.Val & 3) << 6));
                data[2] = 0xAA;
                data[13] = CheckSumCalculator.Calculate(data, 0, 13);

                Data = data;

                if (DataController != null)
                {
                    string s = "";
                    for (int i = 0; i < 13; ++i)
                        s += String.Format("{0:X2} ", Data[i]);
                    s += "КС";

                    DataController.DataChanged(s);
                }

            }

            /// <summary>
            /// Вывести данные в окно
            /// </summary>
            /// <param name="progData">Массив данных</param>
            /// <param name="resultTextBlock">Текстовый элемент в котором необходимо вывести данные</param>
            /// <param name="displayArray">Вывод массива данных в указанный TextBlock</param>
            /// <param name="noDataError">Вывод сообщения об отсутствии даннных в указанный TextBlock</param>
            /// <param name="checkSumError">Вывод сообщения об ошибке контрольной суммы в указанный TextBlock</param>
            /// <param name="packetError">Вывод сообщения об ошибке пакета в указанный TextBlock</param>
            public override void DisplayData(byte[] data, TextBlock resultTextBlock, Action<TextBlock, byte[]> displayArray, Action<TextBlock> noDataError, Action<TextBlock> checkSumError, Action<TextBlock> packetError, object parameter)
            {
                // Выход, если получены данные, которые уже были выведены
                if ((lastDisplayedData != null) && (data != null) && (lastDisplayedData.Length == data.Length))
                {
                    bool equal = true;
                    for (int i = 0; i < data.Length; ++i)
                        if (lastDisplayedData[i] != data[i])
                        {
                            equal = false;
                            break;
                        }
                    if (equal) return;
                }
                lastDisplayedData = data;

                // Проверяем наличие данных
                if ((data == null) || (data.Length <= 0))
                {
                    resultTextBlock.Inlines.Clear();
                    resultTextBlock.Inlines.Add(new Run("Нет данных") { FontWeight = FontWeights.Bold });
                    resultTextBlock.Inlines.Add(Environment.NewLine + Environment.NewLine + "Получение данных не предусмотрено");
                }
                // Проверяем длину пакета
                else
                {
                    packetError(resultTextBlock);
                    resultTextBlock.Inlines.Add(Environment.NewLine);
                    displayArray(resultTextBlock, data);
                    resultTextBlock.Inlines.Add(Environment.NewLine + Environment.NewLine + "Получение данных не предусмотрено");
                }
            }
        }

        /// <summary>
        /// Представление данных ЛС31 в режиме ТЕХН, подрежим - Измерение УАП
        /// </summary>
        class LS31_TC_UAP_DataView : DataView
        {
            private Label lb1;

            /// <summary>
            /// Конструктор
            /// </summary>
            public LS31_TC_UAP_DataView()
            {
                Data = new byte[14];
                for (int i = 0; i < 14; ++i)
                    Data[i] = 0;
                Data[0] = Data[13] = 0x41;

                lastDisplayedData = null;

                lb1 = new Label();
                lb1.Content = "<НЕТ ДАННЫХ>";
                lb1.Margin = new Thickness(10, 10, 0, 0);
            }

            /// <summary>
            /// Инициализация представления
            /// </summary>
            /// <param name="testGrid">Сетка в которой необходимо поместить все элементы управления</param>
            protected override void Init(Grid testGrid)
            {
                Initialized = false;
                testGrid.Children.Add(lb1);
                Initialized = true;
            }

            /// <summary>
            /// Получить введенные данные и разместить в массиве Data
            /// </summary>
            public override void Calculate()
            {
                if (DataController != null)
                {
                    string s = "";
                    for (int i = 0; i < 13; ++i)
                        s += String.Format("{0:X2} ", Data[i]);
                    s += "КС";

                    DataController.DataChanged(s);
                }
            }

            /// <summary>
            /// Вывести данные в окно
            /// </summary>
            /// <param name="progData">Массив данных</param>
            /// <param name="resultTextBlock">Текстовый элемент в котором необходимо вывести данные</param>
            /// <param name="displayArray">Вывод массива данных в указанный TextBlock</param>
            /// <param name="noDataError">Вывод сообщения об отсутствии даннных в указанный TextBlock</param>
            /// <param name="checkSumError">Вывод сообщения об ошибке контрольной суммы в указанный TextBlock</param>
            /// <param name="packetError">Вывод сообщения об ошибке пакета в указанный TextBlock</param>
            public override void DisplayData(byte[] data, TextBlock resultTextBlock, Action<TextBlock, byte[]> displayArray, Action<TextBlock> noDataError, Action<TextBlock> checkSumError, Action<TextBlock> packetError, object parameter)
            {
                // Выход, если получены данные, которые уже были выведены
                if ((lastDisplayedData != null) && (data != null) && (lastDisplayedData.Length == data.Length))
                {
                    bool equal = true;
                    for (int i = 0; i < data.Length; ++i)
                        if (lastDisplayedData[i] != data[i])
                        {
                            equal = false;
                            break;
                        }
                    if (equal) return;
                }
                lastDisplayedData = data;

                // Проверяем наличие данных
                if ((data == null) || (data.Length <= 0))
                {
                    resultTextBlock.Inlines.Clear();
                    resultTextBlock.Inlines.Add(new Run("Нет данных") { FontWeight = FontWeights.Bold });
                    resultTextBlock.Inlines.Add(Environment.NewLine + Environment.NewLine + "Получение данных не предусмотрено");
                }
                // Проверяем длину пакета
                else
                {
                    packetError(resultTextBlock);
                    resultTextBlock.Inlines.Add(Environment.NewLine);
                    displayArray(resultTextBlock, data);
                    resultTextBlock.Inlines.Add(Environment.NewLine + Environment.NewLine + "Получение данных не предусмотрено");
                }
            }
        }

        /// <summary>
        /// Представление данных ЛС31 в режиме ТЕХН, подрежим - Измерение рангов
        /// </summary>
        class LS31_TC_Rank_DataView : DataView
        {
            private Label lb1;

            /// <summary>
            /// Конструктор
            /// </summary>
            public LS31_TC_Rank_DataView()
            {
                Data = new byte[14];
                for (int i = 0; i < 14; ++i)
                    Data[i] = 0;
                Data[0] = Data[13] = 0x42;

                lastDisplayedData = null;

                lb1 = new Label();
                lb1.Content = "<НЕТ ДАННЫХ>";
                lb1.Margin = new Thickness(10, 10, 0, 0);
            }

            /// <summary>
            /// Инициализация представления
            /// </summary>
            /// <param name="testGrid">Сетка в которой необходимо поместить все элементы управления</param>
            protected override void Init(Grid testGrid)
            {
                Initialized = false;
                testGrid.Children.Add(lb1);
                Initialized = true;
            }

            /// <summary>
            /// Получить введенные данные и разместить в массиве Data
            /// </summary>
            public override void Calculate()
            {
                if (DataController != null)
                {
                    string s = "";
                    for (int i = 0; i < 13; ++i)
                        s += String.Format("{0:X2} ", Data[i]);
                    s += "КС";

                    DataController.DataChanged(s);
                }
            }

            /// <summary>
            /// Вывести данные в окно
            /// </summary>
            /// <param name="progData">Массив данных</param>
            /// <param name="resultTextBlock">Текстовый элемент в котором необходимо вывести данные</param>
            /// <param name="displayArray">Вывод массива данных в указанный TextBlock</param>
            /// <param name="noDataError">Вывод сообщения об отсутствии даннных в указанный TextBlock</param>
            /// <param name="checkSumError">Вывод сообщения об ошибке контрольной суммы в указанный TextBlock</param>
            /// <param name="packetError">Вывод сообщения об ошибке пакета в указанный TextBlock</param>
            public override void DisplayData(byte[] data, TextBlock resultTextBlock, Action<TextBlock, byte[]> displayArray, Action<TextBlock> noDataError, Action<TextBlock> checkSumError, Action<TextBlock> packetError, object parameter)
            {
                // Выход, если получены данные, которые уже были выведены
                if ((lastDisplayedData != null) && (data != null) && (lastDisplayedData.Length == data.Length))
                {
                    bool equal = true;
                    for (int i = 0; i < data.Length; ++i)
                        if (lastDisplayedData[i] != data[i])
                        {
                            equal = false;
                            break;
                        }
                    if (equal) return;
                }
                lastDisplayedData = data;

                // Проверяем наличие данных
                if ((data == null) || (data.Length <= 0))
                {
                    resultTextBlock.Inlines.Clear();
                    resultTextBlock.Inlines.Add(new Run("Нет данных") { FontWeight = FontWeights.Bold });
                    resultTextBlock.Inlines.Add(Environment.NewLine + Environment.NewLine + "Получение данных не предусмотрено");
                }
                // Проверяем длину пакета
                else
                {
                    packetError(resultTextBlock);
                    resultTextBlock.Inlines.Add(Environment.NewLine);
                    displayArray(resultTextBlock, data);
                    resultTextBlock.Inlines.Add(Environment.NewLine + Environment.NewLine + "Получение данных не предусмотрено");
                }
            }
        }

        /// <summary>
        /// Представление данных ЛС31 в режиме ТРЕН
        /// </summary>
        class LS31_TR_DataView : DataView
        {
            private Label lb1;
            private TwoDigitUpDown udTI;
            private N8SygnalsView n8sygnals;

            /// <summary>
            /// Конструктор
            /// </summary>
            /// <param name="n8sygnals">Представление сигналов Н8</param>
            public LS31_TR_DataView(N8SygnalsView n8sygnals)
            {
                this.n8sygnals = n8sygnals;

                Data = new byte[14];
                lastDisplayedData = null;

                lb1 = new Label();
                lb1.Content = "Длительность тока, мкс";
                lb1.Margin = new Thickness(10, 10, 0, 0);

                udTI = new TwoDigitUpDown(0, 255, 0);
                udTI.Margin = new Thickness(160, 10, 0, 0);

                udTI.ValueChanged += DefaultHandler;
            }

            /// <summary>
            /// Инициализация представления
            /// </summary>
            /// <param name="testGrid">Сетка в которой необходимо поместить все элементы управления</param>
            protected override void Init(Grid testGrid)
            {
                Initialized = false;

                testGrid.Children.Add(lb1);
                testGrid.Children.Add(udTI);

                Initialized = true;
            }

            /// <summary>
            /// Получить введенные данные и разместить в массиве Data
            /// </summary>
            public override void Calculate()
            {
                byte[] data = new byte[14];
                for (int i = 0; i < 14; ++i)
                    data[i] = 0;

                data[0] = 0x60;
                data[11] = (byte)udTI.Val;
                data[13] = CheckSumCalculator.Calculate(data, 0, 13);

                Data = data;

                if (DataController != null)
                {
                    string s = "";
                    for (int i = 0; i < 13; ++i)
                        s += String.Format("{0:X2} ", Data[i]);
                    s += "КС";

                    DataController.DataChanged(s);
                }

            }

            /// <summary>
            /// Вывести данные в окно
            /// </summary>
            /// <param name="progData">Массив данных</param>
            /// <param name="resultTextBlock">Текстовый элемент в котором необходимо вывести данные</param>
            /// <param name="displayArray">Вывод массива данных в указанный TextBlock</param>
            /// <param name="noDataError">Вывод сообщения об отсутствии даннных в указанный TextBlock</param>
            /// <param name="checkSumError">Вывод сообщения об ошибке контрольной суммы в указанный TextBlock</param>
            /// <param name="packetError">Вывод сообщения об ошибке пакета в указанный TextBlock</param>
            public override void DisplayData(byte[] data, TextBlock resultTextBlock, Action<TextBlock, byte[]> displayArray, Action<TextBlock> noDataError, Action<TextBlock> checkSumError, Action<TextBlock> packetError, object parameter)
            {
                if (n8sygnals.ImitCounter != 0) { --n8sygnals.ImitCounter; }

                // Выход, если получены данные, которые уже были выведены
                if ((lastDisplayedData != null) && (data != null) && (lastDisplayedData.Length == data.Length))
                {
                    bool equal = true;
                    for (int i = 0; i < data.Length; ++i)
                        if (lastDisplayedData[i] != data[i])
                        {
                            equal = false;
                            break;
                        }
                    if (equal) return;
                }
                lastDisplayedData = data;

                // Проверяем наличие данных
                if ((data == null) || (data.Length <= 0))
                {
                    resultTextBlock.Inlines.Clear();
                    resultTextBlock.Inlines.Add(new Run("Нет данных") { FontWeight = FontWeights.Bold });
                    resultTextBlock.Inlines.Add(Environment.NewLine + Environment.NewLine + "При внешней синхронизации" + Environment.NewLine + "получение данных не предусмотрено");
                    if (parameter != null) ((TextBlock)parameter).Text = "";
                }
                // Проверяем длину пакета
                else if (data.Length != 14)
                {
                    packetError(resultTextBlock);
                    resultTextBlock.Inlines.Add(Environment.NewLine);
                    displayArray(resultTextBlock, data);
                    resultTextBlock.Inlines.Add(Environment.NewLine + Environment.NewLine + "Ожидаемая длина пакета - 14 байт");
                    if (parameter != null) ((TextBlock)parameter).Text = "";
                }
                // Выводим данные
                else
                {
                    if (parameter != null)
                    {
                        ((TextBlock)parameter).Text = "НР21.10.02КП: ";
                        ((TextBlock)parameter).Inlines.Add(new Run(Convert.ToString(data[0], 2).PadLeft(8, '0') + 'b') { FontFamily = new FontFamily("Courier New"), FontWeight = FontWeights.Bold });
                    }

                    resultTextBlock.Inlines.Clear();
                    displayArray(resultTextBlock, data);

                    resultTextBlock.Inlines.Add(Environment.NewLine);
                    resultTextBlock.Inlines.Add(Environment.NewLine + "Контроль Н6.21 АКП\t= ");
                    resultTextBlock.Inlines.Add(new Run(Convert.ToString(data[0], 2).PadLeft(8, '0') + 'b') { FontFamily = new FontFamily("Courier New"), FontWeight = FontWeights.Bold });
                    resultTextBlock.Inlines.Add(Environment.NewLine + "КС\t\t\t= ");
                    resultTextBlock.Inlines.Add(new Run(String.Format("{0:X2}h", data[13])) { FontFamily = new FontFamily("Courier New"), FontWeight = FontWeights.Bold });
                    if (data[13] == CheckSumCalculator.Calculate(data, 0, 13))
                        resultTextBlock.Inlines.Add(" (верно)");
                    else resultTextBlock.Inlines.Add(new Run(" (не верно)") { Foreground = Brushes.Red });

                    // Переход в режим "Имит 400" при получении флага ошибки сигналов Н8
                    if (((data[0] & 0x80) == 0) && (n8sygnals.ImitCounter == 0))
                    {
                        n8sygnals.goImit();
                    }
                }
            }
        }

        /// <summary>
        /// Представление данных ЛС31 в режиме ПРОГ, подрежим - Вход в состояние программирования
        /// </summary>
        class LS31_PR_EnterProg_DataView : DataView
        {
            private Label lb1, lb2;
            private TwoDigitUpDown udN, udI;

            /// <summary>
            /// Конструктор
            /// </summary>
            public LS31_PR_EnterProg_DataView()
            {
                Data = new byte[14];
                lastDisplayedData = null;

                lb1 = new Label();
                lb1.Content = "Линейка";
                lb1.Margin = new Thickness(10, 10, 0, 0);
                udN = new TwoDigitUpDown(0, 3, 0);
                udN.Margin = new Thickness(120, 10, 0, 0);

                lb2 = new Label();
                lb2.Content = "УИ";
                lb2.Margin = new Thickness(10, 36, 0, 0);
                udI = new TwoDigitUpDown(0, 21, 0);
                udI.Margin = new Thickness(120, 36, 0, 0);

                udN.ValueChanged += DefaultHandler;
                udI.ValueChanged += DefaultHandler;
            }

            /// <summary>
            /// Инициализация представления
            /// </summary>
            /// <param name="testGrid">Сетка в которой необходимо поместить все элементы управления</param>
            protected override void Init(Grid testGrid)
            {
                Initialized = false;

                testGrid.Children.Add(lb1);
                testGrid.Children.Add(lb2);
                testGrid.Children.Add(udN);
                testGrid.Children.Add(udI);

                Initialized = true;
            }

            /// <summary>
            /// Получить введенные данные и разместить в массиве Data
            /// </summary>
            public override void Calculate()
            {
                byte[] data = new byte[14];
                for (int i = 0; i < 14; ++i)
                    data[i] = 0;

                data[0] = 0xC1;
                data[1] = 0x1A;
                data[2] = (byte)((udI.Val & 0x3F) | ((udN.Val & 3) << 6));
                data[3] = 0xAA;
                data[4] = (byte)'E';
                data[5] = (byte)'N';
                data[6] = (byte)'T';
                data[7] = (byte)'E';
                data[8] = (byte)'R';
                data[9] = (byte)' ';
                data[10] = (byte)'P';
                data[11] = (byte)'R';
                data[12] = (byte)'G';
                data[13] = CheckSumCalculator.Calculate(data, 0, 13);

                Data = data;

                if (DataController != null)
                {
                    string s = "";
                    for (int i = 0; i < 13; ++i)
                        s += String.Format("{0:X2} ", Data[i]);
                    s += "КС";

                    DataController.DataChanged(s);
                }

            }

            /// <summary>
            /// Вывести данные в окно
            /// </summary>
            /// <param name="progData">Массив данных</param>
            /// <param name="resultTextBlock">Текстовый элемент в котором необходимо вывести данные</param>
            /// <param name="displayArray">Вывод массива данных в указанный TextBlock</param>
            /// <param name="noDataError">Вывод сообщения об отсутствии даннных в указанный TextBlock</param>
            /// <param name="checkSumError">Вывод сообщения об ошибке контрольной суммы в указанный TextBlock</param>
            /// <param name="packetError">Вывод сообщения об ошибке пакета в указанный TextBlock</param>
            public override void DisplayData(byte[] data, TextBlock resultTextBlock, Action<TextBlock, byte[]> displayArray, Action<TextBlock> noDataError, Action<TextBlock> checkSumError, Action<TextBlock> packetError, object parameter)
            {
                // Выход, если получены данные, которые уже были выведены
                if ((lastDisplayedData != null) && (data != null) && (lastDisplayedData.Length == data.Length))
                {
                    bool equal = true;
                    for (int i = 0; i < data.Length; ++i)
                        if (lastDisplayedData[i] != data[i])
                        {
                            equal = false;
                            break;
                        }
                    if (equal) return;
                }
                lastDisplayedData = data;

                // Проверяем наличие данных
                if ((data == null) || (data.Length <= 0))
                {
                    resultTextBlock.Inlines.Clear();
                    resultTextBlock.Inlines.Add(new Run("Нет данных") { FontWeight = FontWeights.Bold });
                    resultTextBlock.Inlines.Add(Environment.NewLine + Environment.NewLine + "Получение данных не предусмотрено");
                }
                // Проверяем длину пакета
                else
                {
                    packetError(resultTextBlock);
                    resultTextBlock.Inlines.Add(Environment.NewLine);
                    displayArray(resultTextBlock, data);
                    resultTextBlock.Inlines.Add(Environment.NewLine + Environment.NewLine + "Получение данных не предусмотрено");
                }
            }
        }

        /// <summary>
        /// Представление данных ЛС31 в режиме ПРОГ, подрежим - Запрос состояния
        /// </summary>
        class LS31_PR_RequestState_DataView : DataView
        {
            private Label lb1, lb2;
            private TwoDigitUpDown udN, udI;

            /// <summary>
            /// Конструктор
            /// </summary>
            public LS31_PR_RequestState_DataView()
            {
                Data = new byte[14];
                lastDisplayedData = null;

                lb1 = new Label();
                lb1.Content = "Линейка";
                lb1.Margin = new Thickness(10, 10, 0, 0);
                udN = new TwoDigitUpDown(0, 3, 0);
                udN.Margin = new Thickness(120, 10, 0, 0);

                lb2 = new Label();
                lb2.Content = "УИ";
                lb2.Margin = new Thickness(10, 36, 0, 0);
                udI = new TwoDigitUpDown(1, 21, 1);
                udI.Margin = new Thickness(120, 36, 0, 0);

                udN.ValueChanged += DefaultHandler;
                udI.ValueChanged += DefaultHandler;
            }

            /// <summary>
            /// Инициализация представления
            /// </summary>
            /// <param name="testGrid">Сетка в которой необходимо поместить все элементы управления</param>
            protected override void Init(Grid testGrid)
            {
                Initialized = false;

                testGrid.Children.Add(lb1);
                testGrid.Children.Add(lb2);
                testGrid.Children.Add(udN);
                testGrid.Children.Add(udI);

                Initialized = true;
            }

            /// <summary>
            /// Получить введенные данные и разместить в массиве Data
            /// </summary>
            public override void Calculate()
            {
                byte[] data = new byte[14];
                for (int i = 0; i < 14; ++i)
                    data[i] = 0;

                data[0] = 0xC2;
                data[1] = 0x1A;
                data[2] = (byte)((udI.Val & 0x3F) | ((udN.Val & 3) << 6));
                data[3] = 0x55;
                data[4] = 0x3C;
                data[5] = CheckSumCalculator.Calculate(data, 0, 5);
                data[6] = (byte)'R';
                data[7] = (byte)'E';
                data[8] = (byte)'Q';
                data[9] = (byte)'U';
                data[10] = (byte)'E';
                data[11] = (byte)'S';
                data[12] = (byte)'T';
                data[13] = (byte)' ';

                Data = data;

                if (DataController != null)
                {
                    string s = "";
                    for (int i = 0; i < 13; ++i)
                        s += String.Format("{0:X2} ", Data[i]);
                    s += "КС";

                    DataController.DataChanged(s);
                }

            }

            /// <summary>
            /// Вывести данные в окно
            /// </summary>
            /// <param name="progData">Массив данных</param>
            /// <param name="resultTextBlock">Текстовый элемент в котором необходимо вывести данные</param>
            /// <param name="displayArray">Вывод массива данных в указанный TextBlock</param>
            /// <param name="noDataError">Вывод сообщения об отсутствии даннных в указанный TextBlock</param>
            /// <param name="checkSumError">Вывод сообщения об ошибке контрольной суммы в указанный TextBlock</param>
            /// <param name="packetError">Вывод сообщения об ошибке пакета в указанный TextBlock</param>
            public override void DisplayData(byte[] data, TextBlock resultTextBlock, Action<TextBlock, byte[]> displayArray, Action<TextBlock> noDataError, Action<TextBlock> checkSumError, Action<TextBlock> packetError, object parameter)
            {
                // Выход, если получены данные, которые уже были выведены
                if ((lastDisplayedData != null) && (data != null) && (lastDisplayedData.Length == data.Length))
                {
                    bool equal = true;
                    for (int i = 0; i < data.Length; ++i)
                        if (lastDisplayedData[i] != data[i])
                        {
                            equal = false;
                            break;
                        }
                    if (equal) return;
                }
                lastDisplayedData = data;

                // Проверяем наличие данных
                if ((data == null) || (data.Length <= 0))
                {
                    noDataError(resultTextBlock);
                }
                // Проверяем длину пакета
                else if (data.Length != 5)
                {
                    packetError(resultTextBlock);
                    resultTextBlock.Inlines.Add(Environment.NewLine);
                    displayArray(resultTextBlock, data);
                    resultTextBlock.Inlines.Add(Environment.NewLine + Environment.NewLine + "Ожидаемая длина пакета - 5 байт");
                }
                // Выводим данные
                else
                {
                    resultTextBlock.Inlines.Clear();
                    displayArray(resultTextBlock, data);

                    resultTextBlock.Inlines.Add(Environment.NewLine);
                    resultTextBlock.Inlines.Add(Environment.NewLine + "Квитанция реж. ПРОГ\t= ");
                    resultTextBlock.Inlines.Add(new Run(Convert.ToString(data[0] >> 4, 2).PadLeft(4, '0') + 'b') { FontFamily = new FontFamily("Courier New"), FontWeight = FontWeights.Bold });
                    if ((data[0] >> 4) == 0xC)
                        resultTextBlock.Inlines.Add(" (верно)");
                    else resultTextBlock.Inlines.Add(new Run(" (не верно)") { Foreground = Brushes.Red });
                    resultTextBlock.Inlines.Add(Environment.NewLine + "Квит. подр. Запрос Сост.\t= ");
                    resultTextBlock.Inlines.Add(new Run(Convert.ToString(data[0] & 0xF, 2).PadLeft(4, '0') + 'b') { FontFamily = new FontFamily("Courier New"), FontWeight = FontWeights.Bold });
                    if ((data[0] & 0xF) == 2)
                        resultTextBlock.Inlines.Add(" (верно)");
                    else resultTextBlock.Inlines.Add(new Run(" (не верно)") { Foreground = Brushes.Red });
                    resultTextBlock.Inlines.Add(Environment.NewLine + "Линейка\t\t\t= ");
                    resultTextBlock.Inlines.Add(new Run((data[1] >> 6).ToString()) { FontFamily = new FontFamily("Courier New"), FontWeight = FontWeights.Bold });
                    resultTextBlock.Inlines.Add(Environment.NewLine + "УИ\t\t\t= ");
                    resultTextBlock.Inlines.Add(new Run((data[1] & 0x3F).ToString()) { FontFamily = new FontFamily("Courier New"), FontWeight = FontWeights.Bold });
                    resultTextBlock.Inlines.Add(Environment.NewLine + "Контрольное слово\t= ");
                    resultTextBlock.Inlines.Add(new Run(String.Format("{0:X2}h", data[2])) { FontFamily = new FontFamily("Courier New"), FontWeight = FontWeights.Bold });
                    if (data[2] == 0x33)
                        resultTextBlock.Inlines.Add(" (верно)");
                    else resultTextBlock.Inlines.Add(new Run(" (не верно)") { Foreground = Brushes.Red });
                    resultTextBlock.Inlines.Add(Environment.NewLine + "Состояние\t\t= ");
                    resultTextBlock.Inlines.Add(new Run(Convert.ToString(data[3], 2).PadLeft(8, '0') + 'b') { FontFamily = new FontFamily("Courier New"), FontWeight = FontWeights.Bold });
                    resultTextBlock.Inlines.Add(Environment.NewLine + "КС\t\t\t= ");
                    resultTextBlock.Inlines.Add(new Run(String.Format("{0:X2}h", data[4])) { FontFamily = new FontFamily("Courier New"), FontWeight = FontWeights.Bold });
                    if (data[4] == CheckSumCalculator.Calculate(data, 0, 4))
                        resultTextBlock.Inlines.Add(" (верно)");
                    else resultTextBlock.Inlines.Add(new Run(" (не верно)") { Foreground = Brushes.Red });
                }
            }
        }

        /// <summary>
        /// Представление данных ЛС31 в режиме ПРОГ, подрежим - Передача адреса
        /// </summary>
        class LS31_PR_SendAdr_DataView : DataView
        {
            private Label lb1, lb2, lb3, lb4;
            private TwoDigitUpDown udN, udI;
            private HexadecimalMaskedTextBox tbAdr, tbSize;

            /// <summary>
            /// Конструктор
            /// </summary>
            public LS31_PR_SendAdr_DataView()
            {
                Data = new byte[14];
                lastDisplayedData = null;

                lb1 = new Label();
                lb1.Content = "Линейка";
                lb1.Margin = new Thickness(10, 10, 0, 0);
                udN = new TwoDigitUpDown(0, 3, 0);
                udN.Margin = new Thickness(120, 10, 0, 0);

                lb2 = new Label();
                lb2.Content = "УИ";
                lb2.Margin = new Thickness(10, 36, 0, 0);
                udI = new TwoDigitUpDown(0, 21, 0);
                udI.Margin = new Thickness(120, 36, 0, 0);

                lb3 = new Label();
                lb3.Content = "Адрес";
                lb3.Margin = new Thickness(10, 62, 0, 0);
                tbAdr = new HexadecimalMaskedTextBox(32);
                tbAdr.Margin = new Thickness(120, 62, 0, 0);

                lb4 = new Label();
                lb4.Content = "Размер массива";
                lb4.Margin = new Thickness(10, 88, 0, 0);
                tbSize = new HexadecimalMaskedTextBox(32);
                tbSize.Margin = new Thickness(120, 88, 0, 0);

                udN.ValueChanged += DefaultHandler;
                udI.ValueChanged += DefaultHandler;
                tbAdr.TextChanged += DefaultHandler;
                tbSize.TextChanged += DefaultHandler;
            }

            /// <summary>
            /// Инициализация представления
            /// </summary>
            /// <param name="testGrid">Сетка в которой необходимо поместить все элементы управления</param>
            protected override void Init(Grid testGrid)
            {
                Initialized = false;

                testGrid.Children.Add(lb1);
                testGrid.Children.Add(lb2);
                testGrid.Children.Add(lb3);
                testGrid.Children.Add(lb4);
                testGrid.Children.Add(udN);
                testGrid.Children.Add(udI);
                testGrid.Children.Add(tbAdr);
                testGrid.Children.Add(tbSize);

                Initialized = true;
            }

            /// <summary>
            /// Получить введенные данные и разместить в массиве Data
            /// </summary>
            public override void Calculate()
            {
                byte[] data = new byte[14];
                for (int i = 0; i < 14; ++i)
                    data[i] = 0;

                data[0] = 0xC3;
                data[1] = 0x1A;
                data[2] = (byte)((udI.Val & 0x3F) | ((udN.Val & 3) << 6));
                data[3] = 0xAA;
                data[4] = (byte)'L';
                data[5] = (byte)tbAdr.Val;
                data[6] = (byte)(tbAdr.Val >> 8);
                data[7] = (byte)(tbAdr.Val >> 16);
                data[8] = (byte)(tbAdr.Val >> 24);
                data[9] = (byte)tbSize.Val;
                data[10] = (byte)(tbSize.Val >> 8);
                data[11] = (byte)(tbSize.Val >> 16);
                data[12] = (byte)(tbSize.Val >> 24);
                data[13] = CheckSumCalculator.Calculate(data, 0, 13);

                Data = data;

                if (DataController != null)
                {
                    string s = "";
                    for (int i = 0; i < 13; ++i)
                        s += String.Format("{0:X2} ", Data[i]);
                    s += "КС";

                    DataController.DataChanged(s);
                }

            }

            /// <summary>
            /// Вывести данные в окно
            /// </summary>
            /// <param name="progData">Массив данных</param>
            /// <param name="resultTextBlock">Текстовый элемент в котором необходимо вывести данные</param>
            /// <param name="displayArray">Вывод массива данных в указанный TextBlock</param>
            /// <param name="noDataError">Вывод сообщения об отсутствии даннных в указанный TextBlock</param>
            /// <param name="checkSumError">Вывод сообщения об ошибке контрольной суммы в указанный TextBlock</param>
            /// <param name="packetError">Вывод сообщения об ошибке пакета в указанный TextBlock</param>
            public override void DisplayData(byte[] data, TextBlock resultTextBlock, Action<TextBlock, byte[]> displayArray, Action<TextBlock> noDataError, Action<TextBlock> checkSumError, Action<TextBlock> packetError, object parameter)
            {
                // Выход, если получены данные, которые уже были выведены
                if ((lastDisplayedData != null) && (data != null) && (lastDisplayedData.Length == data.Length))
                {
                    bool equal = true;
                    for (int i = 0; i < data.Length; ++i)
                        if (lastDisplayedData[i] != data[i])
                        {
                            equal = false;
                            break;
                        }
                    if (equal) return;
                }
                lastDisplayedData = data;

                // Проверяем наличие данных
                if ((data == null) || (data.Length <= 0))
                {
                    resultTextBlock.Inlines.Clear();
                    resultTextBlock.Inlines.Add(new Run("Нет данных") { FontWeight = FontWeights.Bold });
                    resultTextBlock.Inlines.Add(Environment.NewLine + Environment.NewLine + "Получение данных не предусмотрено");
                }
                // Проверяем длину пакета
                else
                {
                    packetError(resultTextBlock);
                    resultTextBlock.Inlines.Add(Environment.NewLine);
                    displayArray(resultTextBlock, data);
                    resultTextBlock.Inlines.Add(Environment.NewLine + Environment.NewLine + "Получение данных не предусмотрено");
                }
            }
        }

        /// <summary>
        /// Представление данных ЛС31 в режиме ПРОГ, подрежим - Передача данных
        /// </summary>
        class LS31_PR_SendData_DataView : DataView
        {
            private Label lb1, lb2, lb3;
            private HexadecimalMaskedTextBox tbData1, tbData2, tbData3;

            /// <summary>
            /// Конструктор
            /// </summary>
            public LS31_PR_SendData_DataView()
            {
                Data = new byte[14];
                lastDisplayedData = null;

                lb1 = new Label();
                lb1.Content = "Данные слово 1";
                lb1.Margin = new Thickness(10, 10, 0, 0);
                tbData1 = new HexadecimalMaskedTextBox(32);
                tbData1.Margin = new Thickness(120, 10, 0, 0);

                lb2 = new Label();
                lb2.Content = "Данные слово 2";
                lb2.Margin = new Thickness(10, 36, 0, 0);
                tbData2 = new HexadecimalMaskedTextBox(32);
                tbData2.Margin = new Thickness(120, 36, 0, 0);

                lb3 = new Label();
                lb3.Content = "Данные слово 3";
                lb3.Margin = new Thickness(10, 62, 0, 0);
                tbData3 = new HexadecimalMaskedTextBox(32);
                tbData3.Margin = new Thickness(120, 62, 0, 0);

                tbData1.TextChanged += DefaultHandler;
                tbData2.TextChanged += DefaultHandler;
                tbData3.TextChanged += DefaultHandler;
            }

            /// <summary>
            /// Инициализация представления
            /// </summary>
            /// <param name="testGrid">Сетка в которой необходимо поместить все элементы управления</param>
            protected override void Init(Grid testGrid)
            {
                Initialized = false;

                testGrid.Children.Add(lb1);
                testGrid.Children.Add(lb2);
                testGrid.Children.Add(lb3);
                testGrid.Children.Add(tbData1);
                testGrid.Children.Add(tbData2);
                testGrid.Children.Add(tbData3);

                Initialized = true;
            }

            /// <summary>
            /// Получить введенные данные и разместить в массиве Data
            /// </summary>
            public override void Calculate()
            {
                byte[] data = new byte[14];
                for (int i = 0; i < 14; ++i)
                    data[i] = 0;

                data[0] = 0xC4;
                data[1] = (byte)tbData1.Val;
                data[2] = (byte)(tbData1.Val >> 8);
                data[3] = (byte)(tbData1.Val >> 16);
                data[4] = (byte)(tbData1.Val >> 24);
                data[5] = (byte)tbData2.Val;
                data[6] = (byte)(tbData2.Val >> 8);
                data[7] = (byte)(tbData2.Val >> 16);
                data[8] = (byte)(tbData2.Val >> 24);
                data[9] = (byte)tbData3.Val;
                data[10] = (byte)(tbData3.Val >> 8);
                data[11] = (byte)(tbData3.Val >> 16);
                data[12] = (byte)(tbData3.Val >> 24);
                data[13] = CheckSumCalculator.Calculate(data, 0, 13);

                Data = data;

                if (DataController != null)
                {
                    string s = "";
                    for (int i = 0; i < 13; ++i)
                        s += String.Format("{0:X2} ", Data[i]);
                    s += "КС";

                    DataController.DataChanged(s);
                }

            }

            /// <summary>
            /// Вывести данные в окно
            /// </summary>
            /// <param name="progData">Массив данных</param>
            /// <param name="resultTextBlock">Текстовый элемент в котором необходимо вывести данные</param>
            /// <param name="displayArray">Вывод массива данных в указанный TextBlock</param>
            /// <param name="noDataError">Вывод сообщения об отсутствии даннных в указанный TextBlock</param>
            /// <param name="checkSumError">Вывод сообщения об ошибке контрольной суммы в указанный TextBlock</param>
            /// <param name="packetError">Вывод сообщения об ошибке пакета в указанный TextBlock</param>
            public override void DisplayData(byte[] data, TextBlock resultTextBlock, Action<TextBlock, byte[]> displayArray, Action<TextBlock> noDataError, Action<TextBlock> checkSumError, Action<TextBlock> packetError, object parameter)
            {
                // Выход, если получены данные, которые уже были выведены
                if ((lastDisplayedData != null) && (data != null) && (lastDisplayedData.Length == data.Length))
                {
                    bool equal = true;
                    for (int i = 0; i < data.Length; ++i)
                        if (lastDisplayedData[i] != data[i])
                        {
                            equal = false;
                            break;
                        }
                    if (equal) return;
                }
                lastDisplayedData = data;

                // Проверяем наличие данных
                if ((data == null) || (data.Length <= 0))
                {
                    resultTextBlock.Inlines.Clear();
                    resultTextBlock.Inlines.Add(new Run("Нет данных") { FontWeight = FontWeights.Bold });
                    resultTextBlock.Inlines.Add(Environment.NewLine + Environment.NewLine + "Получение данных не предусмотрено");
                }
                // Проверяем длину пакета
                else
                {
                    packetError(resultTextBlock);
                    resultTextBlock.Inlines.Add(Environment.NewLine);
                    displayArray(resultTextBlock, data);
                    resultTextBlock.Inlines.Add(Environment.NewLine + Environment.NewLine + "Получение данных не предусмотрено");
                }
            }
        }

        /// <summary>
        /// Представление данных ЛС31 в режиме ПРОГ, подрежим - Перезапись ПЗУ
        /// </summary>
        class LS31_PR_Overwrite_DataView : DataView
        {
            private Label lb1, lb2;
            private TwoDigitUpDown udN, udI;

            /// <summary>
            /// Конструктор
            /// </summary>
            public LS31_PR_Overwrite_DataView()
            {
                Data = new byte[14];
                lastDisplayedData = null;

                lb1 = new Label();
                lb1.Content = "Линейка";
                lb1.Margin = new Thickness(10, 10, 0, 0);
                udN = new TwoDigitUpDown(0, 3, 0);
                udN.Margin = new Thickness(120, 10, 0, 0);

                lb2 = new Label();
                lb2.Content = "УИ";
                lb2.Margin = new Thickness(10, 36, 0, 0);
                udI = new TwoDigitUpDown(1, 21, 1);
                udI.Margin = new Thickness(120, 36, 0, 0);

                udN.ValueChanged += DefaultHandler;
                udI.ValueChanged += DefaultHandler;
            }

            /// <summary>
            /// Инициализация представления
            /// </summary>
            /// <param name="testGrid">Сетка в которой необходимо поместить все элементы управления</param>
            protected override void Init(Grid testGrid)
            {
                Initialized = false;

                testGrid.Children.Add(lb1);
                testGrid.Children.Add(lb2);
                testGrid.Children.Add(udN);
                testGrid.Children.Add(udI);

                Initialized = true;
            }

            /// <summary>
            /// Получить введенные данные и разместить в массиве Data
            /// </summary>
            public override void Calculate()
            {
                byte[] data = new byte[14];
                for (int i = 0; i < 14; ++i)
                    data[i] = 0;

                data[0] = 0xC5;
                data[1] = 0x1A;
                data[2] = (byte)((udI.Val & 0x3F) | ((udN.Val & 3) << 6));
                data[3] = 0xAA;
                data[4] = (byte)(~((udI.Val & 0x3F) | ((udN.Val & 3) << 6)));
                data[5] = 0xCC;
                data[6] = (byte)((udI.Val & 0x3F) | ((udN.Val & 3) << 6));
                data[7] = 0x33;
                data[8] = (byte)'F';
                data[9] = (byte)'I';
                data[10] = (byte)'R';
                data[11] = (byte)'E';
                data[12] = (byte)'!';
                data[13] = CheckSumCalculator.Calculate(data, 0, 13);

                Data = data;

                if (DataController != null)
                {
                    string s = "";
                    for (int i = 0; i < 13; ++i)
                        s += String.Format("{0:X2} ", Data[i]);
                    s += "КС";

                    DataController.DataChanged(s);
                }

            }

            /// <summary>
            /// Вывести данные в окно
            /// </summary>
            /// <param name="progData">Массив данных</param>
            /// <param name="resultTextBlock">Текстовый элемент в котором необходимо вывести данные</param>
            /// <param name="displayArray">Вывод массива данных в указанный TextBlock</param>
            /// <param name="noDataError">Вывод сообщения об отсутствии даннных в указанный TextBlock</param>
            /// <param name="checkSumError">Вывод сообщения об ошибке контрольной суммы в указанный TextBlock</param>
            /// <param name="packetError">Вывод сообщения об ошибке пакета в указанный TextBlock</param>
            public override void DisplayData(byte[] data, TextBlock resultTextBlock, Action<TextBlock, byte[]> displayArray, Action<TextBlock> noDataError, Action<TextBlock> checkSumError, Action<TextBlock> packetError, object parameter)
            {
                // Выход, если получены данные, которые уже были выведены
                if ((lastDisplayedData != null) && (data != null) && (lastDisplayedData.Length == data.Length))
                {
                    bool equal = true;
                    for (int i = 0; i < data.Length; ++i)
                        if (lastDisplayedData[i] != data[i])
                        {
                            equal = false;
                            break;
                        }
                    if (equal) return;
                }
                lastDisplayedData = data;

                // Проверяем наличие данных
                if ((data == null) || (data.Length <= 0))
                {
                    resultTextBlock.Inlines.Clear();
                    resultTextBlock.Inlines.Add(new Run("Нет данных") { FontWeight = FontWeights.Bold });
                    resultTextBlock.Inlines.Add(Environment.NewLine + Environment.NewLine + "Получение данных не предусмотрено");
                }
                // Проверяем длину пакета
                else
                {
                    packetError(resultTextBlock);
                    resultTextBlock.Inlines.Add(Environment.NewLine);
                    displayArray(resultTextBlock, data);
                    resultTextBlock.Inlines.Add(Environment.NewLine + Environment.NewLine + "Получение данных не предусмотрено");
                }
            }
        }

        /// <summary>
        /// Представление данных ЛС31 в режиме ПРОГ, подрежим - Чтение данных
        /// </summary>
        class LS31_PR_ReadData_DataView : DataView
        {
            private Label lb1, lb2;
            private TwoDigitUpDown udN, udI;

            /// <summary>
            /// Конструктор
            /// </summary>
            public LS31_PR_ReadData_DataView()
            {
                Data = new byte[14];
                lastDisplayedData = null;

                lb1 = new Label();
                lb1.Content = "Линейка";
                lb1.Margin = new Thickness(10, 10, 0, 0);
                udN = new TwoDigitUpDown(0, 3, 0);
                udN.Margin = new Thickness(120, 10, 0, 0);

                lb2 = new Label();
                lb2.Content = "УИ";
                lb2.Margin = new Thickness(10, 36, 0, 0);
                udI = new TwoDigitUpDown(1, 21, 1);
                udI.Margin = new Thickness(120, 36, 0, 0);

                udN.ValueChanged += DefaultHandler;
                udI.ValueChanged += DefaultHandler;
            }

            /// <summary>
            /// Инициализация представления
            /// </summary>
            /// <param name="testGrid">Сетка в которой необходимо поместить все элементы управления</param>
            protected override void Init(Grid testGrid)
            {
                Initialized = false;

                testGrid.Children.Add(lb1);
                testGrid.Children.Add(lb2);
                testGrid.Children.Add(udN);
                testGrid.Children.Add(udI);

                Initialized = true;
            }

            /// <summary>
            /// Получить введенные данные и разместить в массиве Data
            /// </summary>
            public override void Calculate()
            {
                byte[] data = new byte[14];
                for (int i = 0; i < 14; ++i)
                    data[i] = 0;

                data[0] = 0xC6;
                data[1] = 0x1A;
                data[2] = (byte)((udI.Val & 0x3F) | ((udN.Val & 3) << 6));
                data[3] = 0xAA;
                data[4] = (byte)'R';
                data[5] = (byte)'E';
                data[6] = (byte)'A';
                data[7] = (byte)'D';
                data[8] = (byte)' ';
                data[9] = (byte)'D';
                data[10] = (byte)'A';
                data[11] = (byte)'T';
                data[12] = (byte)'A';
                data[13] = CheckSumCalculator.Calculate(data, 0, 13);

                Data = data;

                if (DataController != null)
                {
                    string s = "";
                    for (int i = 0; i < 13; ++i)
                        s += String.Format("{0:X2} ", Data[i]);
                    s += "КС";

                    DataController.DataChanged(s);
                }

            }

            /// <summary>
            /// Вывести данные в окно
            /// </summary>
            /// <param name="progData">Массив данных</param>
            /// <param name="resultTextBlock">Текстовый элемент в котором необходимо вывести данные</param>
            /// <param name="displayArray">Вывод массива данных в указанный TextBlock</param>
            /// <param name="noDataError">Вывод сообщения об отсутствии даннных в указанный TextBlock</param>
            /// <param name="checkSumError">Вывод сообщения об ошибке контрольной суммы в указанный TextBlock</param>
            /// <param name="packetError">Вывод сообщения об ошибке пакета в указанный TextBlock</param>
            public override void DisplayData(byte[] data, TextBlock resultTextBlock, Action<TextBlock, byte[]> displayArray, Action<TextBlock> noDataError, Action<TextBlock> checkSumError, Action<TextBlock> packetError, object parameter)
            {
                // Выход, если получены данные, которые уже были выведены
                if ((lastDisplayedData != null) && (data != null) && (lastDisplayedData.Length == data.Length))
                {
                    bool equal = true;
                    for (int i = 0; i < data.Length; ++i)
                        if (lastDisplayedData[i] != data[i])
                        {
                            equal = false;
                            break;
                        }
                    if (equal) return;
                }
                lastDisplayedData = data;

                // Проверяем наличие данных
                if ((data == null) || (data.Length <= 0))
                {
                    noDataError(resultTextBlock);
                }
                // Проверяем длину пакета
                else if (data.Length != 14)
                {
                    packetError(resultTextBlock);
                    resultTextBlock.Inlines.Add(Environment.NewLine);
                    displayArray(resultTextBlock, data);
                    resultTextBlock.Inlines.Add(Environment.NewLine + Environment.NewLine + "Ожидаемая длина пакета - 14 байт");
                }
                // Выводим данные
                else
                {
                    resultTextBlock.Inlines.Clear();
                    displayArray(resultTextBlock, data);

                    resultTextBlock.Inlines.Add(Environment.NewLine);
                    resultTextBlock.Inlines.Add(Environment.NewLine + "Квитанция реж. ПРОГ\t= ");
                    resultTextBlock.Inlines.Add(new Run(Convert.ToString(data[0] >> 4, 2).PadLeft(4, '0') + 'b') { FontFamily = new FontFamily("Courier New"), FontWeight = FontWeights.Bold });
                    if ((data[0] >> 4) == 0xC)
                        resultTextBlock.Inlines.Add(" (верно)");
                    else resultTextBlock.Inlines.Add(new Run(" (не верно)") { Foreground = Brushes.Red });
                    resultTextBlock.Inlines.Add(Environment.NewLine + "Квит. подр. Чт. Данных\t= ");
                    resultTextBlock.Inlines.Add(new Run(Convert.ToString(data[0] & 0xF, 2).PadLeft(4, '0') + 'b') { FontFamily = new FontFamily("Courier New"), FontWeight = FontWeights.Bold });
                    if ((data[0] & 0xF) == 6)
                        resultTextBlock.Inlines.Add(" (верно)");
                    else resultTextBlock.Inlines.Add(new Run(" (не верно)") { Foreground = Brushes.Red });
                    resultTextBlock.Inlines.Add(Environment.NewLine + "Данные слово 1\t\t= ");
                    resultTextBlock.Inlines.Add(new Run(String.Format("{0:X8}h", data[1] | (data[2] << 8) | (data[3] << 16) | (data[4] << 24))) { FontFamily = new FontFamily("Courier New"), FontWeight = FontWeights.Bold });
                    resultTextBlock.Inlines.Add(Environment.NewLine + "Данные слово 2\t\t= ");
                    resultTextBlock.Inlines.Add(new Run(String.Format("{0:X8}h", data[5] | (data[6] << 8) | (data[7] << 16) | (data[8] << 24))) { FontFamily = new FontFamily("Courier New"), FontWeight = FontWeights.Bold });
                    resultTextBlock.Inlines.Add(Environment.NewLine + "Данные слово 3\t\t= ");
                    resultTextBlock.Inlines.Add(new Run(String.Format("{0:X8}h", data[9] | (data[10] << 8) | (data[11] << 16) | (data[12] << 24))) { FontFamily = new FontFamily("Courier New"), FontWeight = FontWeights.Bold });
                    resultTextBlock.Inlines.Add(Environment.NewLine + "КС\t\t\t= ");
                    resultTextBlock.Inlines.Add(new Run(String.Format("{0:X2}h", data[13])) { FontFamily = new FontFamily("Courier New"), FontWeight = FontWeights.Bold });
                    if (data[13] == CheckSumCalculator.Calculate(data, 0, 13))
                        resultTextBlock.Inlines.Add(" (верно)");
                    else resultTextBlock.Inlines.Add(new Run(" (не верно)") { Foreground = Brushes.Red });
                }
            }
        }

        /// <summary>
        /// Представление данных ЛС31 в режиме ПРОГ, подрежим - Верификация
        /// </summary>
        class LS31_PR_Verification_DataView : DataView
        {
            private Label lb1, lb2, lb3;
            private TwoDigitUpDown udN, udI, udSector;

            /// <summary>
            /// Конструктор
            /// </summary>
            public LS31_PR_Verification_DataView()
            {
                Data = new byte[14];
                lastDisplayedData = null;

                lb1 = new Label();
                lb1.Content = "Линейка";
                lb1.Margin = new Thickness(10, 10, 0, 0);
                udN = new TwoDigitUpDown(0, 3, 0);
                udN.Margin = new Thickness(120, 10, 0, 0);

                lb2 = new Label();
                lb2.Content = "УИ";
                lb2.Margin = new Thickness(10, 36, 0, 0);
                udI = new TwoDigitUpDown(1, 21, 1);
                udI.Margin = new Thickness(120, 36, 0, 0);

                lb3 = new Label();
                lb3.Content = "N сектора";
                lb3.Margin = new Thickness(10, 62, 0, 0);
                udSector = new TwoDigitUpDown(0, 31, 0);
                udSector.Margin = new Thickness(120, 62, 0, 0);

                udN.ValueChanged += DefaultHandler;
                udI.ValueChanged += DefaultHandler;
                udSector.ValueChanged += DefaultHandler;
            }

            /// <summary>
            /// Инициализация представления
            /// </summary>
            /// <param name="testGrid">Сетка в которой необходимо поместить все элементы управления</param>
            protected override void Init(Grid testGrid)
            {
                Initialized = false;

                testGrid.Children.Add(lb1);
                testGrid.Children.Add(lb2);
                testGrid.Children.Add(lb3);
                testGrid.Children.Add(udN);
                testGrid.Children.Add(udI);
                testGrid.Children.Add(udSector);

                Initialized = true;
            }

            /// <summary>
            /// Получить введенные данные и разместить в массиве Data
            /// </summary>
            public override void Calculate()
            {
                byte[] data = new byte[14];
                for (int i = 0; i < 14; ++i)
                    data[i] = 0;

                data[0] = 0xC7;
                data[1] = 0x1A;
                data[2] = (byte)((udI.Val & 0x3F) | ((udN.Val & 3) << 6));
                data[3] = 0xAA;
                data[4] = (byte)udSector.Val;
                data[5] = (byte)'V';
                data[6] = (byte)'E';
                data[7] = (byte)'R';
                data[8] = (byte)'I';
                data[9] = (byte)'F';
                data[10] = (byte)'Y';
                data[11] = (byte)' ';
                data[12] = (byte)' ';
                data[13] = CheckSumCalculator.Calculate(data, 0, 13);

                Data = data;

                if (DataController != null)
                {
                    string s = "";
                    for (int i = 0; i < 13; ++i)
                        s += String.Format("{0:X2} ", Data[i]);
                    s += "КС";

                    DataController.DataChanged(s);
                }

            }

            /// <summary>
            /// Вывести данные в окно
            /// </summary>
            /// <param name="progData">Массив данных</param>
            /// <param name="resultTextBlock">Текстовый элемент в котором необходимо вывести данные</param>
            /// <param name="displayArray">Вывод массива данных в указанный TextBlock</param>
            /// <param name="noDataError">Вывод сообщения об отсутствии даннных в указанный TextBlock</param>
            /// <param name="checkSumError">Вывод сообщения об ошибке контрольной суммы в указанный TextBlock</param>
            /// <param name="packetError">Вывод сообщения об ошибке пакета в указанный TextBlock</param>
            public override void DisplayData(byte[] data, TextBlock resultTextBlock, Action<TextBlock, byte[]> displayArray, Action<TextBlock> noDataError, Action<TextBlock> checkSumError, Action<TextBlock> packetError, object parameter)
            {
                // Выход, если получены данные, которые уже были выведены
                if ((lastDisplayedData != null) && (data != null) && (lastDisplayedData.Length == data.Length))
                {
                    bool equal = true;
                    for (int i = 0; i < data.Length; ++i)
                        if (lastDisplayedData[i] != data[i])
                        {
                            equal = false;
                            break;
                        }
                    if (equal) return;
                }
                lastDisplayedData = data;

                // Проверяем наличие данных
                if ((data == null) || (data.Length <= 0))
                {
                    noDataError(resultTextBlock);
                }
                // Проверяем длину пакета
                else if (data.Length != 11)
                {
                    packetError(resultTextBlock);
                    resultTextBlock.Inlines.Add(Environment.NewLine);
                    displayArray(resultTextBlock, data);
                    resultTextBlock.Inlines.Add(Environment.NewLine + Environment.NewLine + "Ожидаемая длина пакета - 11 байт");
                }
                // Выводим данные
                else
                {
                    resultTextBlock.Inlines.Clear();
                    displayArray(resultTextBlock, data);

                    resultTextBlock.Inlines.Add(Environment.NewLine);
                    resultTextBlock.Inlines.Add(Environment.NewLine + "Квитанция реж. ПРОГ\t= ");
                    resultTextBlock.Inlines.Add(new Run(Convert.ToString(data[0] >> 4, 2).PadLeft(4, '0') + 'b') { FontFamily = new FontFamily("Courier New"), FontWeight = FontWeights.Bold });
                    if ((data[0] >> 4) == 0xC)
                        resultTextBlock.Inlines.Add(" (верно)");
                    else resultTextBlock.Inlines.Add(new Run(" (не верно)") { Foreground = Brushes.Red });
                    resultTextBlock.Inlines.Add(Environment.NewLine + "Квит. подр. Верификация\t= ");
                    resultTextBlock.Inlines.Add(new Run(Convert.ToString(data[0] & 0xF, 2).PadLeft(4, '0') + 'b') { FontFamily = new FontFamily("Courier New"), FontWeight = FontWeights.Bold });
                    if ((data[0] & 0xF) == 7)
                        resultTextBlock.Inlines.Add(" (верно)");
                    else resultTextBlock.Inlines.Add(new Run(" (не верно)") { Foreground = Brushes.Red });
                    resultTextBlock.Inlines.Add(Environment.NewLine + "Линейка\t\t\t= ");
                    resultTextBlock.Inlines.Add(new Run((data[1] >> 6).ToString()) { FontFamily = new FontFamily("Courier New"), FontWeight = FontWeights.Bold });
                    resultTextBlock.Inlines.Add(Environment.NewLine + "УИ\t\t\t= ");
                    resultTextBlock.Inlines.Add(new Run((data[1] & 0x3F).ToString()) { FontFamily = new FontFamily("Courier New"), FontWeight = FontWeights.Bold });
                    resultTextBlock.Inlines.Add(Environment.NewLine + "Контрольное слово 1\t= ");
                    resultTextBlock.Inlines.Add(new Run(String.Format("{0:X2}h", data[2])) { FontFamily = new FontFamily("Courier New"), FontWeight = FontWeights.Bold });
                    if (data[2] == (byte)'W')
                        resultTextBlock.Inlines.Add(" (верно)");
                    else resultTextBlock.Inlines.Add(new Run(" (не верно)") { Foreground = Brushes.Red });
                    resultTextBlock.Inlines.Add(Environment.NewLine + "Контрольное слово 2\t= ");
                    resultTextBlock.Inlines.Add(new Run(String.Format("{0:X2}h", data[3])) { FontFamily = new FontFamily("Courier New"), FontWeight = FontWeights.Bold });
                    if (data[3] == (byte)'Y')
                        resultTextBlock.Inlines.Add(" (верно)");
                    else resultTextBlock.Inlines.Add(new Run(" (не верно)") { Foreground = Brushes.Red });
                    resultTextBlock.Inlines.Add(Environment.NewLine + "N сектора\t\t= ");
                    resultTextBlock.Inlines.Add(new Run(data[4].ToString()) { FontFamily = new FontFamily("Courier New"), FontWeight = FontWeights.Bold });
                    resultTextBlock.Inlines.Add(Environment.NewLine + "CRC32 сектора\t\t= ");
                    resultTextBlock.Inlines.Add(new Run(String.Format("{0:X8}h", data[5] | (data[6] << 8) | (data[7] << 16) | (data[8] << 24))) { FontFamily = new FontFamily("Courier New"), FontWeight = FontWeights.Bold });
                    resultTextBlock.Inlines.Add(Environment.NewLine + "Состояние\t\t= ");
                    resultTextBlock.Inlines.Add(new Run(Convert.ToString(data[9], 2).PadLeft(8, '0') + 'b') { FontFamily = new FontFamily("Courier New"), FontWeight = FontWeights.Bold });
                    resultTextBlock.Inlines.Add(Environment.NewLine + "КС\t\t\t= ");
                    resultTextBlock.Inlines.Add(new Run(String.Format("{0:X2}h", data[10])) { FontFamily = new FontFamily("Courier New"), FontWeight = FontWeights.Bold });
                    if (data[10] == CheckSumCalculator.Calculate(data, 0, 10))
                        resultTextBlock.Inlines.Add(" (верно)");
                    else resultTextBlock.Inlines.Add(new Run(" (не верно)") { Foreground = Brushes.Red });
                }
            }
        }

        /// <summary>
        /// Представление данных ЛС31 в режиме ПРОГ, подрежим - Отмена программирования
        /// </summary>
        class LS31_PR_Cancel_DataView : DataView
        {
            private Label lb1, lb2;
            private TwoDigitUpDown udN, udI;

            /// <summary>
            /// Конструктор
            /// </summary>
            public LS31_PR_Cancel_DataView()
            {
                Data = new byte[14];
                lastDisplayedData = null;

                lb1 = new Label();
                lb1.Content = "Линейка";
                lb1.Margin = new Thickness(10, 10, 0, 0);
                udN = new TwoDigitUpDown(0, 3, 0);
                udN.Margin = new Thickness(120, 10, 0, 0);

                lb2 = new Label();
                lb2.Content = "УИ";
                lb2.Margin = new Thickness(10, 36, 0, 0);
                udI = new TwoDigitUpDown(0, 21, 0);
                udI.Margin = new Thickness(120, 36, 0, 0);

                udN.ValueChanged += DefaultHandler;
                udI.ValueChanged += DefaultHandler;
            }

            /// <summary>
            /// Инициализация представления
            /// </summary>
            /// <param name="testGrid">Сетка в которой необходимо поместить все элементы управления</param>
            protected override void Init(Grid testGrid)
            {
                Initialized = false;

                testGrid.Children.Add(lb1);
                testGrid.Children.Add(lb2);
                testGrid.Children.Add(udN);
                testGrid.Children.Add(udI);

                Initialized = true;
            }

            /// <summary>
            /// Получить введенные данные и разместить в массиве Data
            /// </summary>
            public override void Calculate()
            {
                byte[] data = new byte[14];
                for (int i = 0; i < 14; ++i)
                    data[i] = 0;

                data[0] = 0xC8;
                data[1] = 0x1A;
                data[2] = (byte)((udI.Val & 0x3F) | ((udN.Val & 3) << 6));
                data[3] = 0xAA;
                data[4] = (byte)'E';
                data[5] = (byte)'X';
                data[6] = (byte)'I';
                data[7] = (byte)'T';
                data[8] = (byte)' ';
                data[9] = (byte)'P';
                data[10] = (byte)'R';
                data[11] = (byte)'G';
                data[12] = (byte)'M';
                data[13] = CheckSumCalculator.Calculate(data, 0, 13);

                Data = data;

                if (DataController != null)
                {
                    string s = "";
                    for (int i = 0; i < 13; ++i)
                        s += String.Format("{0:X2} ", Data[i]);
                    s += "КС";

                    DataController.DataChanged(s);
                }

            }

            /// <summary>
            /// Вывести данные в окно
            /// </summary>
            /// <param name="progData">Массив данных</param>
            /// <param name="resultTextBlock">Текстовый элемент в котором необходимо вывести данные</param>
            /// <param name="displayArray">Вывод массива данных в указанный TextBlock</param>
            /// <param name="noDataError">Вывод сообщения об отсутствии даннных в указанный TextBlock</param>
            /// <param name="checkSumError">Вывод сообщения об ошибке контрольной суммы в указанный TextBlock</param>
            /// <param name="packetError">Вывод сообщения об ошибке пакета в указанный TextBlock</param>
            public override void DisplayData(byte[] data, TextBlock resultTextBlock, Action<TextBlock, byte[]> displayArray, Action<TextBlock> noDataError, Action<TextBlock> checkSumError, Action<TextBlock> packetError, object parameter)
            {
                // Выход, если получены данные, которые уже были выведены
                if ((lastDisplayedData != null) && (data != null) && (lastDisplayedData.Length == data.Length))
                {
                    bool equal = true;
                    for (int i = 0; i < data.Length; ++i)
                        if (lastDisplayedData[i] != data[i])
                        {
                            equal = false;
                            break;
                        }
                    if (equal) return;
                }
                lastDisplayedData = data;

                // Проверяем наличие данных
                if ((data == null) || (data.Length <= 0))
                {
                    resultTextBlock.Inlines.Clear();
                    resultTextBlock.Inlines.Add(new Run("Нет данных") { FontWeight = FontWeights.Bold });
                    resultTextBlock.Inlines.Add(Environment.NewLine + Environment.NewLine + "Получение данных не предусмотрено");
                }
                // Проверяем длину пакета
                else
                {
                    packetError(resultTextBlock);
                    resultTextBlock.Inlines.Add(Environment.NewLine);
                    displayArray(resultTextBlock, data);
                    resultTextBlock.Inlines.Add(Environment.NewLine + Environment.NewLine + "Получение данных не предусмотрено");
                }
            }
        }

        /// <summary>
        /// Представление данных ЛС31 в режиме Отладки, подрежим - Проверка синхросигналов
        /// </summary>
        class LS31_DB_Synchr_DataView : DataView
        {
            private Label lb1;

            /// <summary>
            /// Конструктор
            /// </summary>
            public LS31_DB_Synchr_DataView()
            {
                Data = new byte[14];
                for (int i = 0; i < 14; ++i)
                    Data[i] = 0;
                Data[0] = 0xE1;
                Data[1] = 0xA2;
                unchecked { Data[13] = (byte)(0xE1 + 0xA2 + 1); }

                lastDisplayedData = null;

                lb1 = new Label();
                lb1.Content = "<НЕТ ДАННЫХ>";
                lb1.Margin = new Thickness(10, 10, 0, 0);
            }

            /// <summary>
            /// Инициализация представления
            /// </summary>
            /// <param name="testGrid">Сетка в которой необходимо поместить все элементы управления</param>
            protected override void Init(Grid testGrid)
            {
                Initialized = false;
                testGrid.Children.Add(lb1);
                Initialized = true;
            }

            /// <summary>
            /// Получить введенные данные и разместить в массиве Data
            /// </summary>
            public override void Calculate()
            {
                if (DataController != null)
                {
                    string s = "";
                    for (int i = 0; i < 13; ++i)
                        s += String.Format("{0:X2} ", Data[i]);
                    s += "КС";

                    DataController.DataChanged(s);
                }
            }

            /// <summary>
            /// Вывести данные в окно
            /// </summary>
            /// <param name="progData">Массив данных</param>
            /// <param name="resultTextBlock">Текстовый элемент в котором необходимо вывести данные</param>
            /// <param name="displayArray">Вывод массива данных в указанный TextBlock</param>
            /// <param name="noDataError">Вывод сообщения об отсутствии даннных в указанный TextBlock</param>
            /// <param name="checkSumError">Вывод сообщения об ошибке контрольной суммы в указанный TextBlock</param>
            /// <param name="packetError">Вывод сообщения об ошибке пакета в указанный TextBlock</param>
            public override void DisplayData(byte[] data, TextBlock resultTextBlock, Action<TextBlock, byte[]> displayArray, Action<TextBlock> noDataError, Action<TextBlock> checkSumError, Action<TextBlock> packetError, object parameter)
            {
                // Выход, если получены данные, которые уже были выведены
                if ((lastDisplayedData != null) && (data != null) && (lastDisplayedData.Length == data.Length))
                {
                    bool equal = true;

                    for (int i = 0; i < data.Length; ++i)
                        if (lastDisplayedData[i] != data[i])
                        {
                            equal = false;
                            break;
                        }
                    if (equal) return;
                }
                lastDisplayedData = data;

                // Проверяем наличие данных
                if ((data == null) || (data.Length <= 0))
                {

                    noDataError(resultTextBlock);
                }
                // Проверяем длину пакета
                else if (data.Length != 14)
                {
                    packetError(resultTextBlock);
                    resultTextBlock.Inlines.Add(Environment.NewLine);
                    displayArray(resultTextBlock, data);
                    resultTextBlock.Inlines.Add(Environment.NewLine + Environment.NewLine + "Ожидаемая длина пакета - 14 байт");
                }
                else
                {
                    resultTextBlock.Inlines.Clear();
                    displayArray(resultTextBlock, data);

                    resultTextBlock.Inlines.Add(Environment.NewLine);
                    resultTextBlock.Inlines.Add(Environment.NewLine + "Квитанция реж. Отл.\t= ");
                    resultTextBlock.Inlines.Add(new Run(Convert.ToString(data[0] >> 4, 2).PadLeft(4, '0') + 'b') { FontFamily = new FontFamily("Courier New"), FontWeight = FontWeights.Bold });
                    if ((data[0] >> 4) == 0xE)
                        resultTextBlock.Inlines.Add(" (верно)");
                    else resultTextBlock.Inlines.Add(new Run(" (не верно)") { Foreground = Brushes.Red });
                    resultTextBlock.Inlines.Add(Environment.NewLine + "Квит. подр. Пр. СС\t= ");
                    resultTextBlock.Inlines.Add(new Run(Convert.ToString(data[0] & 0xF, 2).PadLeft(4, '0') + 'b') { FontFamily = new FontFamily("Courier New"), FontWeight = FontWeights.Bold });
                    if ((data[0] & 0xF) == 1)
                        resultTextBlock.Inlines.Add(" (верно)");
                    else resultTextBlock.Inlines.Add(new Run(" (не верно)") { Foreground = Brushes.Red });
                    for (int i = 0; i < 12; ++i)
                    {
                        resultTextBlock.Inlines.Add(Environment.NewLine + "R[" + (i + 7) + "]\t\t\t= ");
                        resultTextBlock.Inlines.Add(new Run(Convert.ToString(data[i + 1], 2).PadLeft(8, '0') + 'b') { FontFamily = new FontFamily("Courier New"), FontWeight = FontWeights.Bold });
                        if (i == 10) { resultTextBlock.Inlines.Add(new Run(String.Format(" КС пр. мк2: " + "{0:X2}", data[i+1])) { FontFamily = new FontFamily("Courier New"), FontWeight = FontWeights.Bold }); }
                        if (i == 11) { resultTextBlock.Inlines.Add(" № вер. прог. мк2: " + data[i+1]); }
                    }
                    resultTextBlock.Inlines.Add(Environment.NewLine + "КС\t\t\t= ");
                    resultTextBlock.Inlines.Add(new Run(String.Format("{0:X2}h", data[13])) { FontFamily = new FontFamily("Courier New"), FontWeight = FontWeights.Bold });
                    //for (data[12])
                    //resultTextBlock.Inlines.Add(" (верно)");
                    if (data[13] == CheckSumCalculator.Calculate(data, 0, 13))
                        resultTextBlock.Inlines.Add(" (верно)");
                    else resultTextBlock.Inlines.Add(new Run(" (не верно)") { Foreground = Brushes.Red });
                }
                /*
                // Выход, если получены данные, которые уже были выведены
                if ((lastDisplayedData != null) && (data != null) && (lastDisplayedData.Length == data.Length))
                {
                    bool equal = true;
                    for (int i = 0; i < data.Length; ++i)
                        if (lastDisplayedData[i] != data[i])
                        {
                            equal = false;
                            break;
                        }
                    if (equal) return;
                }
                lastDisplayedData = data;

                // Проверяем наличие данных
                if ((data == null) || (data.Length <= 0))
                {
                    resultTextBlock.Inlines.Clear();
                    resultTextBlock.Inlines.Add(new Run("Нет данных") { FontWeight = FontWeights.Bold });
                    resultTextBlock.Inlines.Add(Environment.NewLine + Environment.NewLine + "Получение данных не предусмотрено");
                }
                // Проверяем длину пакета
                else
                {
                    packetError(resultTextBlock);
                    resultTextBlock.Inlines.Add(Environment.NewLine);
                    displayArray(resultTextBlock, data);
                    resultTextBlock.Inlines.Add(Environment.NewLine + Environment.NewLine + "Получение данных не предусмотрено");
                }
                 */
            }
        }

        /// <summary>
        /// Представление данных ЛС31 в режиме Отладки, подрежим - Проверка амплитуды по интервалам
        /// </summary>
        class LS31_DB_Intervals_DataView : DataView
        {
            private Label lb1;

            /// <summary>
            /// Конструктор
            /// </summary>
            public LS31_DB_Intervals_DataView()
            {
                Data = new byte[14];
                for (int i = 0; i < 14; ++i)
                    Data[i] = 0;
                Data[0] = 0xE2;
                Data[1] = 0xA1;
                unchecked { Data[13] = (byte)(0xE2 + 0xA1 + 1); }

                lastDisplayedData = null;

                lb1 = new Label();
                lb1.Content = "<НЕТ ДАННЫХ>";
                lb1.Margin = new Thickness(10, 10, 0, 0);
            }

            /// <summary>
            /// Инициализация представления
            /// </summary>
            /// <param name="testGrid">Сетка в которой необходимо поместить все элементы управления</param>
            protected override void Init(Grid testGrid)
            {
                Initialized = false;
                testGrid.Children.Add(lb1);
                Initialized = true;
            }

            /// <summary>
            /// Получить введенные данные и разместить в массиве Data
            /// </summary>
            public override void Calculate()
            {
                if (DataController != null)
                {
                    string s = "";
                    for (int i = 0; i < 13; ++i)
                        s += String.Format("{0:X2} ", Data[i]);
                    s += "КС";

                    DataController.DataChanged(s);
                }
            }

            /// <summary>
            /// Вывести данные в окно
            /// </summary>
            /// <param name="progData">Массив данных</param>
            /// <param name="resultTextBlock">Текстовый элемент в котором необходимо вывести данные</param>
            /// <param name="displayArray">Вывод массива данных в указанный TextBlock</param>
            /// <param name="noDataError">Вывод сообщения об отсутствии даннных в указанный TextBlock</param>
            /// <param name="checkSumError">Вывод сообщения об ошибке контрольной суммы в указанный TextBlock</param>
            /// <param name="packetError">Вывод сообщения об ошибке пакета в указанный TextBlock</param>
            public override void DisplayData(byte[] data, TextBlock resultTextBlock, Action<TextBlock, byte[]> displayArray, Action<TextBlock> noDataError, Action<TextBlock> checkSumError, Action<TextBlock> packetError, object parameter)
            {
                // Выход, если получены данные, которые уже были выведены
                if ((lastDisplayedData != null) && (data != null) && (lastDisplayedData.Length == data.Length))
                {
                    bool equal = true;
                    for (int i = 0; i < data.Length; ++i)
                        if (lastDisplayedData[i] != data[i])
                        {
                            equal = false;
                            break;
                        }
                    if (equal) return;
                }
                lastDisplayedData = data;

                // Проверяем наличие данных
                if ((data == null) || (data.Length <= 0))
                {
                    resultTextBlock.Inlines.Clear();
                    resultTextBlock.Inlines.Add(new Run("Нет данных") { FontWeight = FontWeights.Bold });
                    resultTextBlock.Inlines.Add(Environment.NewLine + Environment.NewLine + "Получение данных не предусмотрено");
                }
                // Проверяем длину пакета
                else
                {
                    packetError(resultTextBlock);
                    resultTextBlock.Inlines.Add(Environment.NewLine);
                    displayArray(resultTextBlock, data);
                    resultTextBlock.Inlines.Add(Environment.NewLine + Environment.NewLine + "Получение данных не предусмотрено");
                }
            }
        }

        /// <summary>
        /// Представление данных ЛС31 в режиме Отладки, подрежим - Проверка формирования ПН1..6
        /// </summary>
        class LS31_DB_PN_DataView : DataView
        {
            private Label lb1;

            /// <summary>
            /// Конструктор
            /// </summary>
            public LS31_DB_PN_DataView()
            {
                Data = new byte[14];
                for (int i = 0; i < 14; ++i)
                    Data[i] = 0;
                Data[0] = 0xE3;
                Data[1] = 0xA1;
                unchecked { Data[13] = (byte)(0xE3 + 0xA1 + 1); }

                lastDisplayedData = null;

                lb1 = new Label();
                lb1.Content = "<НЕТ ДАННЫХ>";
                lb1.Margin = new Thickness(10, 10, 0, 0);
            }

            /// <summary>
            /// Инициализация представления
            /// </summary>
            /// <param name="testGrid">Сетка в которой необходимо поместить все элементы управления</param>
            protected override void Init(Grid testGrid)
            {
                Initialized = false;
                testGrid.Children.Add(lb1);
                Initialized = true;
            }

            /// <summary>
            /// Получить введенные данные и разместить в массиве Data
            /// </summary>
            public override void Calculate()
            {
                if (DataController != null)
                {
                    string s = "";
                    for (int i = 0; i < 13; ++i)
                        s += String.Format("{0:X2} ", Data[i]);
                    s += "КС";

                    DataController.DataChanged(s);
                }
            }

            /// <summary>
            /// Вывести данные в окно
            /// </summary>
            /// <param name="progData">Массив данных</param>
            /// <param name="resultTextBlock">Текстовый элемент в котором необходимо вывести данные</param>
            /// <param name="displayArray">Вывод массива данных в указанный TextBlock</param>
            /// <param name="noDataError">Вывод сообщения об отсутствии даннных в указанный TextBlock</param>
            /// <param name="checkSumError">Вывод сообщения об ошибке контрольной суммы в указанный TextBlock</param>
            /// <param name="packetError">Вывод сообщения об ошибке пакета в указанный TextBlock</param>
            public override void DisplayData(byte[] data, TextBlock resultTextBlock, Action<TextBlock, byte[]> displayArray, Action<TextBlock> noDataError, Action<TextBlock> checkSumError, Action<TextBlock> packetError, object parameter)
            {
                // Выход, если получены данные, которые уже были выведены
                if ((lastDisplayedData != null) && (data != null) && (lastDisplayedData.Length == data.Length))
                {
                    bool equal = true;
                    for (int i = 0; i < data.Length; ++i)
                        if (lastDisplayedData[i] != data[i])
                        {
                            equal = false;
                            break;
                        }
                    if (equal) return;
                }
                lastDisplayedData = data;

                // Проверяем наличие данных
                if ((data == null) || (data.Length <= 0))
                {
                    resultTextBlock.Inlines.Clear();
                    resultTextBlock.Inlines.Add(new Run("Нет данных") { FontWeight = FontWeights.Bold });
                    resultTextBlock.Inlines.Add(Environment.NewLine + Environment.NewLine + "Получение данных не предусмотрено");
                }
                // Проверяем длину пакета
                else
                {
                    packetError(resultTextBlock);
                    resultTextBlock.Inlines.Add(Environment.NewLine);
                    displayArray(resultTextBlock, data);
                    resultTextBlock.Inlines.Add(Environment.NewLine + Environment.NewLine + "Получение данных не предусмотрено");
                }
            }
        }

        /// <summary>
        /// Представление данных ШД21 в режиме БР
        /// </summary>
        class SD21_BR_DataView : DataView
        {
            private Label lb1, lb2, lb3, lb4, lb5, lb6, lb7, lb8, lb9;
            private BinaryMaskedTextBox tbStatus;
            private HexadecimalMaskedTextBox tbPOX, tbPOY, tbP1X, tbP1Y, tbP2X, tbP2Y, tbP3X, tbP3Y;
            private SimpleCheckBox chbPN1, chbPN2, chbPN3, chbPN4, chbPN20, chbPNP4, chbR1, chbR2, chbR3, chbR4, chbR5, chbR6;

            /// <summary>
            /// Конструктор
            /// </summary>
            public SD21_BR_DataView()
            {
                Data = new byte[20];

                lb1 = new Label();
                lb1.Content = "Состояние Н6.21.10.01";
                lb1.Margin = new Thickness(10, 10, 0, 0);
                tbStatus = new BinaryMaskedTextBox(8);
                tbStatus.Text = "00000001b";
                tbStatus.Margin = new Thickness(180, 10, 0, 0);

                lb2 = new Label();
                lb2.Content = "Пилот сигн. О X";
                lb2.Margin = new Thickness(10, 36, 0, 0);
                tbPOX = new HexadecimalMaskedTextBox(16);
                tbPOX.Margin = new Thickness(120, 36, 0, 0);

                lb3 = new Label();
                lb3.Content = "Пилот сигн. О Y";
                lb3.Margin = new Thickness(180, 36, 0, 0);
                tbPOY = new HexadecimalMaskedTextBox(16);
                tbPOY.Margin = new Thickness(290, 36, 0, 0);

                chbPNP4 = new SimpleCheckBox("ПНП4");
                chbPNP4.Margin = new Thickness(10 + 5, 62 + 5, 0, 0);
                chbPN20 = new SimpleCheckBox("ПН20");
                chbPN20.Margin = new Thickness(70 + 5, 62 + 5, 0, 0);
                chbPN4 = new SimpleCheckBox("ПН4");
                chbPN4.Margin = new Thickness(130 + 5, 62 + 5, 0, 0);
                chbPN3 = new SimpleCheckBox("ПН3");
                chbPN3.Margin = new Thickness(180 + 5, 62 + 5, 0, 0);
                chbPN2 = new SimpleCheckBox("ПН2");
                chbPN2.Margin = new Thickness(230 + 5, 62 + 5, 0, 0);
                chbPN1 = new SimpleCheckBox("ПН1");
                chbPN1.Margin = new Thickness(280 + 5, 62 + 5, 0, 0);

                lb4 = new Label();
                lb4.Content = "Пилот сигн. П1 X";
                lb4.Margin = new Thickness(10, 88, 0, 0);
                tbP1X = new HexadecimalMaskedTextBox(16);
                tbP1X.Margin = new Thickness(120, 88, 0, 0);

                lb5 = new Label();
                lb5.Content = "Пилот сигн. П1 Y";
                lb5.Margin = new Thickness(180, 88, 0, 0);
                tbP1Y = new HexadecimalMaskedTextBox(16);
                tbP1Y.Margin = new Thickness(290, 88, 0, 0);

                lb6 = new Label();
                lb6.Content = "Пилот сигн. П2 X";
                lb6.Margin = new Thickness(10, 114, 0, 0);
                tbP2X = new HexadecimalMaskedTextBox(16);
                tbP2X.Margin = new Thickness(120, 114, 0, 0);

                lb7 = new Label();
                lb7.Content = "Пилот сигн. П2 Y";
                lb7.Margin = new Thickness(180, 114, 0, 0);
                tbP2Y = new HexadecimalMaskedTextBox(16);
                tbP2Y.Margin = new Thickness(290, 114, 0, 0);

                lb8 = new Label();
                lb8.Content = "Пилот сигн. П3 X";
                lb8.Margin = new Thickness(10, 140, 0, 0);
                tbP3X = new HexadecimalMaskedTextBox(16);
                tbP3X.Margin = new Thickness(120, 140, 0, 0);

                lb9 = new Label();
                lb9.Content = "Пилот сигн. П3 Y";
                lb9.Margin = new Thickness(180, 140, 0, 0);
                tbP3Y = new HexadecimalMaskedTextBox(16);
                tbP3Y.Margin = new Thickness(290, 140, 0, 0);

                chbR6 = new SimpleCheckBox("Ранг6");
                chbR6.Margin = new Thickness(10 + 5, 166 + 5, 0, 0);
                chbR5 = new SimpleCheckBox("Ранг5");
                chbR5.Margin = new Thickness(65 + 5, 166 + 5, 0, 0);
                chbR4 = new SimpleCheckBox("Ранг4");
                chbR4.Margin = new Thickness(120 + 5, 166 + 5, 0, 0);
                chbR3 = new SimpleCheckBox("Ранг3");
                chbR3.Margin = new Thickness(175 + 5, 166 + 5, 0, 0);
                chbR2 = new SimpleCheckBox("Ранг2");
                chbR2.Margin = new Thickness(230 + 5, 166 + 5, 0, 0);
                chbR1 = new SimpleCheckBox("Ранг1");
                chbR1.Margin = new Thickness(285 + 5, 166 + 5, 0, 0);

                tbStatus.TextChanged += DefaultHandler;
                tbPOX.TextChanged += DefaultHandler;
                tbPOY.TextChanged += DefaultHandler;
                tbP1X.TextChanged += DefaultHandler;
                tbP1Y.TextChanged += DefaultHandler;
                tbP2X.TextChanged += DefaultHandler;
                tbP2Y.TextChanged += DefaultHandler;
                tbP3X.TextChanged += DefaultHandler;
                tbP3Y.TextChanged += DefaultHandler;
                chbPN1.Click += DefaultHandler;
                chbPN2.Click += DefaultHandler;
                chbPN3.Click += DefaultHandler;
                chbPN4.Click += DefaultHandler;
                chbPN20.Click += DefaultHandler;
                chbPNP4.Click += DefaultHandler;
                chbR1.Click += DefaultHandler;
                chbR2.Click += DefaultHandler;
                chbR3.Click += DefaultHandler;
                chbR4.Click += DefaultHandler;
                chbR5.Click += DefaultHandler;
                chbR6.Click += DefaultHandler;
            }

            /// <summary>
            /// Инициализация представления
            /// </summary>
            /// <param name="testGrid">Сетка в которой необходимо поместить все элементы управления</param>
            protected override void Init(Grid testGrid)
            {
                Initialized = false;
                testGrid.Children.Add(lb1);
                testGrid.Children.Add(lb2);
                testGrid.Children.Add(lb3);
                testGrid.Children.Add(lb4);
                testGrid.Children.Add(lb5);
                testGrid.Children.Add(lb6);
                testGrid.Children.Add(lb7);
                testGrid.Children.Add(lb8);
                testGrid.Children.Add(lb9);
                testGrid.Children.Add(tbStatus);
                testGrid.Children.Add(tbPOX);
                testGrid.Children.Add(tbPOY);
                testGrid.Children.Add(chbPNP4);
                testGrid.Children.Add(chbPN20);
                testGrid.Children.Add(chbPN4);
                testGrid.Children.Add(chbPN3);
                testGrid.Children.Add(chbPN2);
                testGrid.Children.Add(chbPN1);
                testGrid.Children.Add(tbP1X);
                testGrid.Children.Add(tbP1Y);
                testGrid.Children.Add(tbP2X);
                testGrid.Children.Add(tbP2Y);
                testGrid.Children.Add(tbP3X);
                testGrid.Children.Add(tbP3Y);
                testGrid.Children.Add(chbR6);
                testGrid.Children.Add(chbR5);
                testGrid.Children.Add(chbR4);
                testGrid.Children.Add(chbR3);
                testGrid.Children.Add(chbR2);
                testGrid.Children.Add(chbR1);
                Initialized = true;
            }

            /// <summary>
            /// Получить введенные данные и разместить в массиве Data
            /// </summary>
            public override void Calculate()
            {
                byte[] data = new byte[20];
                for (int i = 0; i < 20; ++i)
                    data[i] = 0;

                data[0] = (byte)tbStatus.Val;
                data[1] = (byte)(tbPOX.Val >> 8);
                data[2] = (byte)tbPOX.Val;
                data[3] = (byte)(tbPOY.Val >> 8);
                data[4] = (byte)tbPOY.Val;
                data[5] = 0;
                if (chbPNP4.Val) data[5] |= 0x20;
                if (chbPN20.Val) data[5] |= 0x10;
                if (chbPN4.Val) data[5] |= 8;
                if (chbPN3.Val) data[5] |= 4;
                if (chbPN2.Val) data[5] |= 2;
                if (chbPN1.Val) data[5] |= 1;
                data[6] = (byte)(tbP1X.Val >> 8);
                data[7] = (byte)tbP1X.Val;
                data[8] = (byte)(tbP1Y.Val >> 8);
                data[9] = (byte)tbP1Y.Val;
                data[10] = (byte)(tbP2X.Val >> 8);
                data[11] = (byte)tbP2X.Val;
                data[12] = (byte)(tbP2Y.Val >> 8);
                data[13] = (byte)tbP2Y.Val;
                data[14] = (byte)(tbP3X.Val >> 8);
                data[15] = (byte)tbP3X.Val;
                data[16] = (byte)(tbP3Y.Val >> 8);
                data[17] = (byte)tbP3Y.Val;
                data[18] = CheckSumCalculator.Calculate(data, 0, data.Length - 2);
                data[19] = 0;
                if (chbR6.Val) data[19] |= 0x20;
                if (chbR5.Val) data[19] |= 0x10;
                if (chbR4.Val) data[19] |= 8;
                if (chbR3.Val) data[19] |= 4;
                if (chbR2.Val) data[19] |= 2;
                if (chbR1.Val) data[19] |= 1;

                Data = data;

                if (DataController != null)
                {
                    string s = "";
                    for (int i = 0; i < 18; ++i)
                        s += String.Format("{0:X2} ", Data[i]);
                    s += "КС";
                    s += String.Format("{0:X2}", Data[19]);

                    DataController.DataChanged(s);
                }
            }

            /// <summary>
            /// Получить размер массива данных и позицию контрольной суммы
            /// </summary>
            /// <param name="length">Размер массиваданных</param>
            /// <param name="checkSumPos">Позиция контрольной суммы (начиная с нулевой)</param>
            public override void GetDataLength(out int length, out int checkSumPos)
            {
                length = 20;
                checkSumPos = 18;
            }

            public override void DisplayData(byte[] data, TextBlock resultTextBlock, Action<TextBlock, byte[]> displayArray, Action<TextBlock> noDataError, Action<TextBlock> checkSumError, Action<TextBlock> packetError, object parameter) { }
        }

        /// <summary>
        /// Представление данных ШД21 в режиме ФК
        /// </summary>
        class SD21_FC_DataView : DataView
        {
            private Label lb1, lb2, lb3, lb4, lb5, lb6, lb7, lb8, lb9;
            private BinaryMaskedTextBox tbStatus;
            private HexadecimalMaskedTextBox tbPOX, tbPOY, tbP1X, tbP1Y, tbP2X, tbP2Y, tbP3X, tbP3Y;

            /// <summary>
            /// Конструктор
            /// </summary>
            public SD21_FC_DataView()
            {
                Data = new byte[19];

                lb1 = new Label();
                lb1.Content = "Состояние Н6.21.10.01";
                lb1.Margin = new Thickness(10, 10, 0, 0);
                tbStatus = new BinaryMaskedTextBox(8);
                tbStatus.Text = "00000001b";
                tbStatus.Margin = new Thickness(180, 10, 0, 0);

                lb2 = new Label();
                lb2.Content = "Пилот сигн. О X";
                lb2.Margin = new Thickness(10, 36, 0, 0);
                tbPOX = new HexadecimalMaskedTextBox(16);
                tbPOX.Margin = new Thickness(120, 36, 0, 0);

                lb3 = new Label();
                lb3.Content = "Пилот сигн. О Y";
                lb3.Margin = new Thickness(180, 36, 0, 0);
                tbPOY = new HexadecimalMaskedTextBox(16);
                tbPOY.Margin = new Thickness(290, 36, 0, 0);

                lb4 = new Label();
                lb4.Content = "Пилот сигн. П1 X";
                lb4.Margin = new Thickness(10, 62, 0, 0);
                tbP1X = new HexadecimalMaskedTextBox(16);
                tbP1X.Margin = new Thickness(120, 62, 0, 0);

                lb5 = new Label();
                lb5.Content = "Пилот сигн. П1 Y";
                lb5.Margin = new Thickness(180, 62, 0, 0);
                tbP1Y = new HexadecimalMaskedTextBox(16);
                tbP1Y.Margin = new Thickness(290, 62, 0, 0);

                lb6 = new Label();
                lb6.Content = "Пилот сигн. П2 X";
                lb6.Margin = new Thickness(10, 88, 0, 0);
                tbP2X = new HexadecimalMaskedTextBox(16);
                tbP2X.Margin = new Thickness(120, 88, 0, 0);

                lb7 = new Label();
                lb7.Content = "Пилот сигн. П2 Y";
                lb7.Margin = new Thickness(180, 88, 0, 0);
                tbP2Y = new HexadecimalMaskedTextBox(16);
                tbP2Y.Margin = new Thickness(290, 88, 0, 0);

                lb8 = new Label();
                lb8.Content = "Пилот сигн. П3 X";
                lb8.Margin = new Thickness(10, 114, 0, 0);
                tbP3X = new HexadecimalMaskedTextBox(16);
                tbP3X.Margin = new Thickness(120, 114, 0, 0);

                lb9 = new Label();
                lb9.Content = "Пилот сигн. П3 Y";
                lb9.Margin = new Thickness(180, 114, 0, 0);
                tbP3Y = new HexadecimalMaskedTextBox(16);
                tbP3Y.Margin = new Thickness(290, 114, 0, 0);

                tbStatus.TextChanged += DefaultHandler;
                tbPOX.TextChanged += DefaultHandler;
                tbPOY.TextChanged += DefaultHandler;
                tbP1X.TextChanged += DefaultHandler;
                tbP1Y.TextChanged += DefaultHandler;
                tbP2X.TextChanged += DefaultHandler;
                tbP2Y.TextChanged += DefaultHandler;
                tbP3X.TextChanged += DefaultHandler;
                tbP3Y.TextChanged += DefaultHandler;
            }

            /// <summary>
            /// Инициализация представления
            /// </summary>
            /// <param name="testGrid">Сетка в которой необходимо поместить все элементы управления</param>
            protected override void Init(Grid testGrid)
            {
                Initialized = false;
                testGrid.Children.Add(lb1);
                testGrid.Children.Add(lb2);
                testGrid.Children.Add(lb3);
                testGrid.Children.Add(lb4);
                testGrid.Children.Add(lb5);
                testGrid.Children.Add(lb6);
                testGrid.Children.Add(lb7);
                testGrid.Children.Add(lb8);
                testGrid.Children.Add(lb9);
                testGrid.Children.Add(tbStatus);
                testGrid.Children.Add(tbPOX);
                testGrid.Children.Add(tbPOY);
                testGrid.Children.Add(tbP1X);
                testGrid.Children.Add(tbP1Y);
                testGrid.Children.Add(tbP2X);
                testGrid.Children.Add(tbP2Y);
                testGrid.Children.Add(tbP3X);
                testGrid.Children.Add(tbP3Y);
                Initialized = true;
            }

            /// <summary>
            /// Получить введенные данные и разместить в массиве Data
            /// </summary>
            public override void Calculate()
            {
                byte[] data = new byte[19];
                for (int i = 0; i < 19; ++i)
                    data[i] = 0;

                data[0] = (byte)tbStatus.Val;
                data[1] = (byte)(tbPOX.Val >> 8);
                data[2] = (byte)tbPOX.Val;
                data[3] = (byte)(tbPOY.Val >> 8);
                data[4] = (byte)tbPOY.Val;
                data[5] = 0;
                data[6] = (byte)(tbP1X.Val >> 8);
                data[7] = (byte)tbP1X.Val;
                data[8] = (byte)(tbP1Y.Val >> 8);
                data[9] = (byte)tbP1Y.Val;
                data[10] = (byte)(tbP2X.Val >> 8);
                data[11] = (byte)tbP2X.Val;
                data[12] = (byte)(tbP2Y.Val >> 8);
                data[13] = (byte)tbP2Y.Val;
                data[14] = (byte)(tbP3X.Val >> 8);
                data[15] = (byte)tbP3X.Val;
                data[16] = (byte)(tbP3Y.Val >> 8);
                data[17] = (byte)tbP3Y.Val;
                data[18] = CheckSumCalculator.Calculate(data, 0, data.Length - 2);

                Data = data;

                if (DataController != null)
                {
                    string s = "";
                    for (int i = 0; i < 18; ++i)
                        s += String.Format("{0:X2} ", Data[i]);
                    s += "КС";

                    DataController.DataChanged(s);
                }
            }

            /// <summary>
            /// Получить размер массива данных и позицию контрольной суммы
            /// </summary>
            /// <param name="length">Размер массиваданных</param>
            /// <param name="checkSumPos">Позиция контрольной суммы (начиная с нулевой)</param>
            public override void GetDataLength(out int length, out int checkSumPos)
            {
                length = 19;
                checkSumPos = 18;
            }

            public override void DisplayData(byte[] data, TextBlock resultTextBlock, Action<TextBlock, byte[]> displayArray, Action<TextBlock> noDataError, Action<TextBlock> checkSumError, Action<TextBlock> packetError, object parameter) { }
        }

        /// <summary>
        /// Представление данных ШД21 в режиме ТЕХН, подрежим - Измерение УАП
        /// </summary>
        class SD21_TC_UAP_DataView : DataView
        {
            private Label lb1, lb2, lb3, lb4, lb5, lb6;
            private BinaryMaskedTextBox tbStatus, tbUAP1, tbUAP2, tbUAP3, tbUAP4;
            private HexadecimalMaskedTextBox tbUAP5;

            /// <summary>
            /// Конструктор
            /// </summary>
            public SD21_TC_UAP_DataView()
            {
                Data = new byte[10];

                lb1 = new Label();
                lb1.Content = "Состояние Н6.21.10.01";
                lb1.Margin = new Thickness(10, 10, 0, 0);
                tbStatus = new BinaryMaskedTextBox(8);
                tbStatus.Text = "00000001b";
                tbStatus.Margin = new Thickness(150, 10, 0, 0);

                lb2 = new Label();
                lb2.Content = "УАП 1";
                lb2.Margin = new Thickness(10, 36, 0, 0);
                tbUAP1 = new BinaryMaskedTextBox(8);
                tbUAP1.Margin = new Thickness(150, 36, 0, 0);

                lb3 = new Label();
                lb3.Content = "УАП 2";
                lb3.Margin = new Thickness(10, 62, 0, 0);
                tbUAP2 = new BinaryMaskedTextBox(8);
                tbUAP2.Margin = new Thickness(150, 62, 0, 0);

                lb4 = new Label();
                lb4.Content = "УАП 3";
                lb4.Margin = new Thickness(10, 88, 0, 0);
                tbUAP3 = new BinaryMaskedTextBox(8);
                tbUAP3.Margin = new Thickness(150, 88, 0, 0);

                lb5 = new Label();
                lb5.Content = "УАП 4";
                lb5.Margin = new Thickness(10, 114, 0, 0);
                tbUAP4 = new BinaryMaskedTextBox(8);
                tbUAP4.Margin = new Thickness(150, 114, 0, 0);

                lb6 = new Label();
                lb6.Content = "УАП 5";
                lb6.Margin = new Thickness(10, 140, 0, 0);
                tbUAP5 = new HexadecimalMaskedTextBox(32);
                tbUAP5.Margin = new Thickness(150, 140, 0, 0);

                tbStatus.TextChanged += DefaultHandler;
                tbUAP1.TextChanged += DefaultHandler;
                tbUAP2.TextChanged += DefaultHandler;
                tbUAP3.TextChanged += DefaultHandler;
                tbUAP4.TextChanged += DefaultHandler;
                tbUAP5.TextChanged += DefaultHandler;
            }

            /// <summary>
            /// Инициализация представления
            /// </summary>
            /// <param name="testGrid">Сетка в которой необходимо поместить все элементы управления</param>
            protected override void Init(Grid testGrid)
            {
                Initialized = false;
                testGrid.Children.Add(lb1);
                testGrid.Children.Add(lb2);
                testGrid.Children.Add(lb3);
                testGrid.Children.Add(lb4);
                testGrid.Children.Add(lb5);
                testGrid.Children.Add(lb6);
                testGrid.Children.Add(tbStatus);
                testGrid.Children.Add(tbUAP1);
                testGrid.Children.Add(tbUAP2);
                testGrid.Children.Add(tbUAP3);
                testGrid.Children.Add(tbUAP4);
                testGrid.Children.Add(tbUAP5);
                Initialized = true;
            }

            /// <summary>
            /// Получить введенные данные и разместить в массиве Data
            /// </summary>
            public override void Calculate()
            {
                byte[] data = new byte[10];
                for (int i = 0; i < 10; ++i)
                    data[i] = 0;

                data[0] = (byte)tbStatus.Val;
                data[1] = (byte)tbUAP1.Val;
                data[2] = (byte)tbUAP2.Val;
                data[3] = (byte)tbUAP3.Val;
                data[4] = (byte)tbUAP4.Val;
                data[5] = (byte)tbUAP5.Val;
                data[6] = (byte)(tbUAP5.Val >> 8);
                data[7] = (byte)(tbUAP5.Val >> 16);
                data[8] = (byte)(tbUAP5.Val >> 24);
                data[9] = CheckSumCalculator.Calculate(data, 0, 9);

                Data = data;

                if (DataController != null)
                {
                    string s = "";
                    for (int i = 0; i < 9; ++i)
                        s += String.Format("{0:X2} ", Data[i]);
                    s += "КС";

                    DataController.DataChanged(s);
                }
            }

            /// <summary>
            /// Получить размер массива данных и позицию контрольной суммы
            /// </summary>
            /// <param name="length">Размер массива данных</param>
            /// <param name="checkSumPos">Позиция контрольной суммы (начиная с нулевой)</param>
            public override void GetDataLength(out int length, out int checkSumPos)
            {
                length = 10;
                checkSumPos = 9;
            }

            public override void DisplayData(byte[] data, TextBlock resultTextBlock, Action<TextBlock, byte[]> displayArray, Action<TextBlock> noDataError, Action<TextBlock> checkSumError, Action<TextBlock> packetError, object parameter) { }
        }

        /// <summary>
        /// Представление данных ШД21 в режиме ТЕХН, подрежим - Измерение рангов
        /// </summary>
        class SD21_TC_Rank_DataView : DataView
        {
            private Label lb1, lb2, lb3, lb4, lb5, lb6, lb7, lb8, lb9, lb10, lb11, lb12;
            private BinaryMaskedTextBox tbStatus, tbPO, tbP1, tbP2, tbP3, tbP4, tbRank1, tbRank2, tbRank3, tbRank4, tbRank5, tbRank6;

            /// <summary>
            /// Конструктор
            /// </summary>
            public SD21_TC_Rank_DataView()
            {
                Data = new byte[13];

                lb1 = new Label();
                lb1.Content = "Состояние Н6.21.10.01";
                lb1.Margin = new Thickness(10, 10, 0, 0);
                tbStatus = new BinaryMaskedTextBox(8);
                tbStatus.Text = "00000001b";
                tbStatus.Margin = new Thickness(180, 10, 0, 0);

                lb2 = new Label();
                lb2.Content = "PO";
                lb2.Margin = new Thickness(10, 36, 0, 0);
                tbPO = new BinaryMaskedTextBox(8);
                tbPO.Margin = new Thickness(60, 36, 0, 0);

                lb3 = new Label();
                lb3.Content = "P1";
                lb3.Margin = new Thickness(10, 62, 0, 0);
                tbP1 = new BinaryMaskedTextBox(8);
                tbP1.Margin = new Thickness(60, 62, 0, 0);

                lb4 = new Label();
                lb4.Content = "P2";
                lb4.Margin = new Thickness(180, 62, 0, 0);
                tbP2 = new BinaryMaskedTextBox(8);
                tbP2.Margin = new Thickness(230, 62, 0, 0);

                lb5 = new Label();
                lb5.Content = "P3";
                lb5.Margin = new Thickness(10, 88, 0, 0);
                tbP3 = new BinaryMaskedTextBox(8);
                tbP3.Margin = new Thickness(60, 88, 0, 0);

                lb6 = new Label();
                lb6.Content = "P4";
                lb6.Margin = new Thickness(180, 88, 0, 0);
                tbP4 = new BinaryMaskedTextBox(8);
                tbP4.Margin = new Thickness(230, 88, 0, 0);

                lb7 = new Label();
                lb7.Content = "Ранг1";
                lb7.Margin = new Thickness(10, 114, 0, 0);
                tbRank1 = new BinaryMaskedTextBox(8);
                tbRank1.Margin = new Thickness(60, 114, 0, 0);

                lb8 = new Label();
                lb8.Content = "Ранг2";
                lb8.Margin = new Thickness(180, 114, 0, 0);
                tbRank2 = new BinaryMaskedTextBox(8);
                tbRank2.Margin = new Thickness(230, 114, 0, 0);

                lb9 = new Label();
                lb9.Content = "Ранг3";
                lb9.Margin = new Thickness(10, 140, 0, 0);
                tbRank3 = new BinaryMaskedTextBox(8);
                tbRank3.Margin = new Thickness(60, 140, 0, 0);

                lb10 = new Label();
                lb10.Content = "Ранг4";
                lb10.Margin = new Thickness(180, 140, 0, 0);
                tbRank4 = new BinaryMaskedTextBox(8);
                tbRank4.Margin = new Thickness(230, 140, 0, 0);

                lb11 = new Label();
                lb11.Content = "Ранг5";
                lb11.Margin = new Thickness(10, 166, 0, 0);
                tbRank5 = new BinaryMaskedTextBox(8);
                tbRank5.Margin = new Thickness(60, 166, 0, 0);

                lb12 = new Label();
                lb12.Content = "Ранг6";
                lb12.Margin = new Thickness(180, 166, 0, 0);
                tbRank6 = new BinaryMaskedTextBox(8);
                tbRank6.Margin = new Thickness(230, 166, 0, 0);

                tbStatus.TextChanged += DefaultHandler;
                tbPO.TextChanged += DefaultHandler;
                tbP1.TextChanged += DefaultHandler;
                tbP2.TextChanged += DefaultHandler;
                tbP3.TextChanged += DefaultHandler;
                tbP4.TextChanged += DefaultHandler;
                tbRank1.TextChanged += DefaultHandler;
                tbRank2.TextChanged += DefaultHandler;
                tbRank3.TextChanged += DefaultHandler;
                tbRank4.TextChanged += DefaultHandler;
                tbRank5.TextChanged += DefaultHandler;
                tbRank6.TextChanged += DefaultHandler;
            }

            /// <summary>
            /// Инициализация представления
            /// </summary>
            /// <param name="testGrid">Сетка в которой необходимо поместить все элементы управления</param>
            protected override void Init(Grid testGrid)
            {
                Initialized = false;
                testGrid.Children.Add(lb1);
                testGrid.Children.Add(lb2);
                testGrid.Children.Add(lb3);
                testGrid.Children.Add(lb4);
                testGrid.Children.Add(lb5);
                testGrid.Children.Add(lb6);
                testGrid.Children.Add(lb7);
                testGrid.Children.Add(lb8);
                testGrid.Children.Add(lb9);
                testGrid.Children.Add(lb10);
                testGrid.Children.Add(lb11);
                testGrid.Children.Add(lb12);
                testGrid.Children.Add(tbPO);
                testGrid.Children.Add(tbP1);
                testGrid.Children.Add(tbP2);
                testGrid.Children.Add(tbP3);
                testGrid.Children.Add(tbP4);
                testGrid.Children.Add(tbStatus);
                testGrid.Children.Add(tbRank1);
                testGrid.Children.Add(tbRank2);
                testGrid.Children.Add(tbRank3);
                testGrid.Children.Add(tbRank4);
                testGrid.Children.Add(tbRank5);
                testGrid.Children.Add(tbRank6);
                Initialized = true;
            }

            /// <summary>
            /// Получить введенные данные и разместить в массиве Data
            /// </summary>
            public override void Calculate()
            {
                byte[] data = new byte[13];
                for (int i = 0; i < 13; ++i)
                    data[i] = 0;

                data[0] = (byte)tbStatus.Val;
                data[1] = (byte)tbPO.Val;
                data[2] = (byte)tbP1.Val;
                data[3] = (byte)tbP2.Val;
                data[4] = (byte)tbP3.Val;
                data[5] = (byte)tbP4.Val;
                data[6] = (byte)tbRank1.Val;
                data[7] = (byte)tbRank2.Val;
                data[8] = (byte)tbRank3.Val;
                data[9] = (byte)tbRank4.Val;
                data[10] = (byte)tbRank5.Val;
                data[11] = (byte)tbRank6.Val;
                data[12] = CheckSumCalculator.Calculate(data, 0, 12);

                Data = data;

                if (DataController != null)
                {
                    string s = "";
                    for (int i = 0; i < 12; ++i)
                        s += String.Format("{0:X2} ", Data[i]);
                    s += "КС";

                    DataController.DataChanged(s);
                }
            }

            /// <summary>
            /// Получить размер массива данных и позицию контрольной суммы
            /// </summary>
            /// <param name="length">Размер массиваданных</param>
            /// <param name="checkSumPos">Позиция контрольной суммы (начиная с нулевой)</param>
            public override void GetDataLength(out int length, out int checkSumPos)
            {
                length = 13;
                checkSumPos = 12;
            }

            public override void DisplayData(byte[] data, TextBlock resultTextBlock, Action<TextBlock, byte[]> displayArray, Action<TextBlock> noDataError, Action<TextBlock> checkSumError, Action<TextBlock> packetError, object parameter) { }
        }

        /// <summary>
        /// Представление данных ШД21 в режиме Отладки, подрежим - Проверка амплитуды по интервалам
        /// </summary>
        class SD21_DB_Intervals_DataView : DataView
        {
            private Label lb1, lb2, lb3, lb4, lb5, lb6, lb7;
            private ChannelComboBox cbChannel;
            private HexadecimalMaskedTextBox tbA_PN_P4, tbA_PN_20, tbA_PN1, tbA_PN2, tbA_PN3, tbA_PN4;

            /// <summary>
            /// Конструктор
            /// </summary>
            public SD21_DB_Intervals_DataView()
            {
                Data = new byte[14];

                lb1 = new Label();
                lb1.Content = "Канал";
                lb1.Margin = new Thickness(10, 10, 0, 0);
                cbChannel = new ChannelComboBox();
                cbChannel.Width = 46;
                cbChannel.Margin = new Thickness(120, 10, 0, 0);

                lb2 = new Label();
                lb2.Content = "Амплитуда ПН П4";
                lb2.Margin = new Thickness(10, 36, 0, 0);
                tbA_PN_P4 = new HexadecimalMaskedTextBox(16);
                tbA_PN_P4.Margin = new Thickness(120, 36, 0, 0);

                lb3 = new Label();
                lb3.Content = "Амплитуда ПН 20";
                lb3.Margin = new Thickness(180, 36, 0, 0);
                tbA_PN_20 = new HexadecimalMaskedTextBox(16);
                tbA_PN_20.Margin = new Thickness(290, 36, 0, 0);

                lb4 = new Label();
                lb4.Content = "Амплитуда ПН1";
                lb4.Margin = new Thickness(10, 62, 0, 0);
                tbA_PN1 = new HexadecimalMaskedTextBox(16);
                tbA_PN1.Margin = new Thickness(120, 62, 0, 0);

                lb5 = new Label();
                lb5.Content = "Амплитуда ПН2";
                lb5.Margin = new Thickness(180, 62, 0, 0);
                tbA_PN2 = new HexadecimalMaskedTextBox(16);
                tbA_PN2.Margin = new Thickness(290, 62, 0, 0);

                lb6 = new Label();
                lb6.Content = "Амплитуда ПН3";
                lb6.Margin = new Thickness(10, 88, 0, 0);
                tbA_PN3 = new HexadecimalMaskedTextBox(16);
                tbA_PN3.Margin = new Thickness(120, 88, 0, 0);

                lb7 = new Label();
                lb7.Content = "Амплитуда ПН4";
                lb7.Margin = new Thickness(180, 88, 0, 0);
                tbA_PN4 = new HexadecimalMaskedTextBox(16);
                tbA_PN4.Margin = new Thickness(290, 88, 0, 0);

                cbChannel.SelectionChanged += DefaultHandler;
                tbA_PN_P4.TextChanged += DefaultHandler;
                tbA_PN_20.TextChanged += DefaultHandler;
                tbA_PN1.TextChanged += DefaultHandler;
                tbA_PN2.TextChanged += DefaultHandler;
                tbA_PN3.TextChanged += DefaultHandler;
                tbA_PN4.TextChanged += DefaultHandler;
            }

            /// <summary>
            /// Инициализация представления
            /// </summary>
            /// <param name="testGrid">Сетка в которой необходимо поместить все элементы управления</param>
            protected override void Init(Grid testGrid)
            {
                Initialized = false;
                testGrid.Children.Add(lb1);
                testGrid.Children.Add(lb2);
                testGrid.Children.Add(lb3);
                testGrid.Children.Add(lb4);
                testGrid.Children.Add(lb5);
                testGrid.Children.Add(lb6);
                testGrid.Children.Add(lb7);
                testGrid.Children.Add(cbChannel);
                testGrid.Children.Add(tbA_PN_P4);
                testGrid.Children.Add(tbA_PN_20);
                testGrid.Children.Add(tbA_PN1);
                testGrid.Children.Add(tbA_PN2);
                testGrid.Children.Add(tbA_PN3);
                testGrid.Children.Add(tbA_PN4);
                Initialized = true;
            }

            /// <summary>
            /// Получить введенные данные и разместить в массиве Data
            /// </summary>
            public override void Calculate()
            {
                byte[] data = new byte[14];
                for (int i = 0; i < 14; ++i)
                    data[i] = 0;

                data[0] = (byte)(0xE0 | (cbChannel.Val & 0xF));
                data[1] = (byte)(tbA_PN_P4.Val >> 8);
                data[2] = (byte)tbA_PN_P4.Val;
                data[3] = (byte)(tbA_PN_20.Val >> 8);
                data[4] = (byte)tbA_PN_20.Val;
                data[5] = (byte)(tbA_PN1.Val >> 8);
                data[6] = (byte)tbA_PN1.Val;
                data[7] = (byte)(tbA_PN2.Val >> 8);
                data[8] = (byte)tbA_PN2.Val;
                data[9] = (byte)(tbA_PN3.Val >> 8);
                data[10] = (byte)tbA_PN3.Val;
                data[11] = (byte)(tbA_PN4.Val >> 8);
                data[12] = (byte)tbA_PN4.Val;
                data[13] = CheckSumCalculator.Calculate(data, 0, data.Length - 2);

                Data = data;

                if (DataController != null)
                {
                    string s = "";
                    for (int i = 0; i < 13; ++i)
                        s += String.Format("{0:X2} ", Data[i]);
                    s += "КС";

                    DataController.DataChanged(s);
                }
            }

            /// <summary>
            /// Получить размер массива данных и позицию контрольной суммы
            /// </summary>
            /// <param name="length">Размер массиваданных</param>
            /// <param name="checkSumPos">Позиция контрольной суммы (начиная с нулевой)</param>
            public override void GetDataLength(out int length, out int checkSumPos)
            {
                length = 14;
                checkSumPos = 13;
            }

            public override void DisplayData(byte[] data, TextBlock resultTextBlock, Action<TextBlock, byte[]> displayArray, Action<TextBlock> noDataError, Action<TextBlock> checkSumError, Action<TextBlock> packetError, object parameter) { }
        }

        /// <summary>
        /// Представление данных ШД21 в режиме Отладки, подрежим - Проверка формирования ПН1..6
        /// </summary>
        class SD21_DB_PN_DataView : DataView
        {
            private Label lb1, lb2, lb3, lb4, lb5, lb6, lb7;
            private IntervalComboBox cbInterval;
            private HexadecimalMaskedTextBox tbThreshold, tbA_O, tbA_P1, tbA_P2, tbA_P3, tbA_P4;
            private SimpleCheckBox chbResult, chbP4, chbP3, chbP2, chbP1, chbThreshold;

            /// <summary>
            /// Конструктор
            /// </summary>
            public SD21_DB_PN_DataView()
            {
                Data = new byte[14];

                lb1 = new Label();
                lb1.Content = "Интервал";
                lb1.Margin = new Thickness(10, 10, 0, 0);
                cbInterval = new IntervalComboBox();
                cbInterval.Width = 65;
                cbInterval.Margin = new Thickness(120, 10, 0, 0);

                lb2 = new Label();
                lb2.Content = "Порог";
                lb2.Margin = new Thickness(10, 36, 0, 0);
                tbThreshold = new HexadecimalMaskedTextBox(16);
                tbThreshold.Margin = new Thickness(120, 36, 0, 0);

                lb3 = new Label();
                lb3.Content = "Амплитуда О К";
                lb3.Margin = new Thickness(180, 36, 0, 0);
                tbA_O = new HexadecimalMaskedTextBox(16);
                tbA_O.Margin = new Thickness(290, 36, 0, 0);

                lb4 = new Label();
                lb4.Content = "Амплитуда П1";
                lb4.Margin = new Thickness(10, 62, 0, 0);
                tbA_P1 = new HexadecimalMaskedTextBox(16);
                tbA_P1.Margin = new Thickness(120, 62, 0, 0);

                lb5 = new Label();
                lb5.Content = "Амплитуда П2";
                lb5.Margin = new Thickness(180, 62, 0, 0);
                tbA_P2 = new HexadecimalMaskedTextBox(16);
                tbA_P2.Margin = new Thickness(290, 62, 0, 0);

                lb6 = new Label();
                lb6.Content = "Амплитуда П3";
                lb6.Margin = new Thickness(10, 88, 0, 0);
                tbA_P3 = new HexadecimalMaskedTextBox(16);
                tbA_P3.Margin = new Thickness(120, 88, 0, 0);

                lb7 = new Label();
                lb7.Content = "Амплитуда П4";
                lb7.Margin = new Thickness(180, 88, 0, 0);
                tbA_P4 = new HexadecimalMaskedTextBox(16);
                tbA_P4.Margin = new Thickness(290, 88, 0, 0);

                chbResult = new SimpleCheckBox("ПН");
                chbResult.Margin = new Thickness(10 + 5, 114 + 5, 0, 0);
                chbResult.IsChecked = true;
                chbP4 = new SimpleCheckBox("ПН4");
                chbP4.Margin = new Thickness(65 + 5, 114 + 5, 0, 0);
                chbP4.IsChecked = true;
                chbP3 = new SimpleCheckBox("ПН3");
                chbP3.Margin = new Thickness(120 + 5, 114 + 5, 0, 0);
                chbP3.IsChecked = true;
                chbP2 = new SimpleCheckBox("ПН2");
                chbP2.Margin = new Thickness(175 + 5, 114 + 5, 0, 0);
                chbP2.IsChecked = true;
                chbP1 = new SimpleCheckBox("ПН1");
                chbP1.Margin = new Thickness(230 + 5, 114 + 5, 0, 0);
                chbP1.IsChecked = true;
                chbThreshold = new SimpleCheckBox("Порог");
                chbThreshold.Margin = new Thickness(285 + 5, 114 + 5, 0, 0);
                chbThreshold.IsChecked = true;

                cbInterval.SelectionChanged += DefaultHandler;
                tbThreshold.TextChanged += DefaultHandler;
                tbA_O.TextChanged += DefaultHandler;
                tbA_P1.TextChanged += DefaultHandler;
                tbA_P2.TextChanged += DefaultHandler;
                tbA_P3.TextChanged += DefaultHandler;
                tbA_P4.TextChanged += DefaultHandler;
                chbResult.Click += DefaultHandler;
                chbP4.Click += DefaultHandler;
                chbP3.Click += DefaultHandler;
                chbP2.Click += DefaultHandler;
                chbP1.Click += DefaultHandler;
                chbThreshold.Click += DefaultHandler;
            }

            /// <summary>
            /// Инициализация представления
            /// </summary>
            /// <param name="testGrid">Сетка в которой необходимо поместить все элементы управления</param>
            protected override void Init(Grid testGrid)
            {
                Initialized = false;
                testGrid.Children.Add(lb1);
                testGrid.Children.Add(lb2);
                testGrid.Children.Add(lb3);
                testGrid.Children.Add(lb4);
                testGrid.Children.Add(lb5);
                testGrid.Children.Add(lb6);
                testGrid.Children.Add(lb7);
                testGrid.Children.Add(cbInterval);
                testGrid.Children.Add(tbThreshold);
                testGrid.Children.Add(tbA_O);
                testGrid.Children.Add(tbA_P1);
                testGrid.Children.Add(tbA_P2);
                testGrid.Children.Add(tbA_P3);
                testGrid.Children.Add(tbA_P4);
                testGrid.Children.Add(chbResult);
                testGrid.Children.Add(chbP4);
                testGrid.Children.Add(chbP3);
                testGrid.Children.Add(chbP2);
                testGrid.Children.Add(chbP1);
                testGrid.Children.Add(chbThreshold);
                Initialized = true;
            }

            /// <summary>
            /// Получить введенные данные и разместить в массиве Data
            /// </summary>
            public override void Calculate()
            {
                byte[] data = new byte[14];
                for (int i = 0; i < 14; ++i)
                    data[i] = 0;

                data[0] = (byte)(0xE0 | (cbInterval.Val & 0xF));
                data[1] = (byte)(tbThreshold.Val >> 8);
                data[2] = (byte)tbThreshold.Val;
                data[3] = (byte)(tbA_O.Val >> 8);
                data[4] = (byte)tbA_O.Val;
                data[5] = (byte)(tbA_P1.Val >> 8);
                data[6] = (byte)tbA_P1.Val;
                data[7] = (byte)(tbA_P2.Val >> 8);
                data[8] = (byte)tbA_P2.Val;
                data[9] = (byte)(tbA_P3.Val >> 8);
                data[10] = (byte)tbA_P3.Val;
                data[11] = (byte)(tbA_P4.Val >> 8);
                data[12] = (byte)tbA_P4.Val;
                data[13] = 0;
                if (chbResult.Val) data[13] |= (1 << 7);
                if (chbP4.Val) data[13] |= (1 << 4);
                if (chbP3.Val) data[13] |= (1 << 3);
                if (chbP2.Val) data[13] |= (1 << 2);
                if (chbP1.Val) data[13] |= (1 << 1);
                if (chbThreshold.Val) data[13] |= 1;

                Data = data;

                if (DataController != null)
                {
                    string s = "";
                    for (int i = 0; i < 13; ++i)
                        s += String.Format("{0:X2} ", Data[i]);
                    s += String.Format("{0:X2}", Data[13]);

                    DataController.DataChanged(s);
                }
            }

            /// <summary>
            /// Получить размер массива данных и позицию контрольной суммы
            /// </summary>
            /// <param name="length">Размер массиваданных</param>
            /// <param name="checkSumPos">Позиция контрольной суммы (начиная с нулевой)</param>
            public override void GetDataLength(out int length, out int checkSumPos)
            {
                length = 14;
                checkSumPos = -1;
            }

            public override void DisplayData(byte[] data, TextBlock resultTextBlock, Action<TextBlock, byte[]> displayArray, Action<TextBlock> noDataError, Action<TextBlock> checkSumError, Action<TextBlock> packetError, object parameter) { }
        }

        class CommandEditor : IDateSource
        {
            private MaskedTextBox textBox;
            private int length, checkSumPos;
            private byte[] data;

            public byte[] GetData() { return data; }
            public IDataReceiver DataController { get; set; }
            public bool IsEnabled
            {
                get { return textBox.IsEnabled; }
                set
                {
                    textBox.IsEnabled = value;
                    if (value) CommandEditorHandler(null, null);
                }
            }
            public string Text
            {
                get { return textBox.Text; }
                set { textBox.Text = value; }
            }

            /// <summary>
            /// Конструктор
            /// </summary>
            /// <param name="textBox">Используемое текстовое поле</param>
            /// <param name="length">Длина команды (количество байт)</param>
            /// <param name="checkSumPos">Позиция в массиве контрольной суммы</param>
            public CommandEditor(MaskedTextBox textBox, int length, int checkSumPos)
            {
                this.textBox = textBox;
                this.length = length;
                this.checkSumPos = checkSumPos;

                SetLength(length, checkSumPos);
                textBox.PreviewTextInput += HexadecimalMaskedTextBox.PreviewTextInputHandler;
                textBox.PreviewKeyDown += HexadecimalMaskedTextBox.PreviewKeyDownHandler;

                textBox.TextChanged += CommandEditorHandler;
            }

            /// <summary>
            /// Установить длину команды
            /// </summary>
            /// <param name="length">Длина команды (количество байт)</param>
            /// <param name="checkSumPos">Позиция в массиве контрольной суммы</param>
            public void SetLength(int length, int checkSumPos)
            {
                if (length < 0) throw new Exception("Недопустимое значение length");

                data = new byte[length];
                this.length = length;
                this.checkSumPos = checkSumPos;

                string s = ">";
                for (int i = 0; i < length; ++i)
                {
                    if (i == checkSumPos) s += "КС ";
                    else s += "AA ";
                }
                if (length == 0)
                {
                    IsEnabled = false;
                    textBox.Text = " ";
                    textBox.Mask = " ";
                    IsEnabled = true;
                }
                else
                {
                    try
                    {
                        textBox.Mask = s.Substring(0, s.Length - 1);
                    }
                    catch (ArgumentException)
                    {
                        IsEnabled = false;
                        textBox.Text = " ";
                        textBox.Mask = s.Substring(0, s.Length - 1);
                        IsEnabled = true;
                    }
                }
            }

            private void CommandEditorHandler(object sender, TextChangedEventArgs e)
            {
                if (IsEnabled)
                {
                    // Получаем введенные данные
                    string[] sData = textBox.Text.Split(new char[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
                    if (sData.Length != length) throw new Exception("Размер массива введенных данных не совпадает с установленным");

                    // Преобразуем данные из строкового в числовой формат
                    byte[] newData = new byte[length];
                    List<byte> bData = new List<byte>(sData.Length + 1);
                    for (int i = 0; i < length; ++i)
                    {
                            try
                            {
                                newData[i] = Convert.ToByte(sData[i], 16);
                            }
                            catch (Exception) { newData[i] = 0; }
                    }

                    if((checkSumPos > 0) && (checkSumPos < length))
                        newData[checkSumPos] = CheckSumCalculator.Calculate(newData, 0, checkSumPos);

                    data = newData;

                    if(DataController != null) DataController.DataChanged(null);
                }
            }

            public void ChangeMode(int mode) { }
            public void DisplayData(byte[] data, Action<TextBlock, byte[]> displayArray, Action<TextBlock> noDataError, Action<TextBlock> checkSumError, Action<TextBlock> packetError) { }
        }

        class BinaryMaskedTextBox : MaskedTextBox
        {
            public BinaryMaskedTextBox(int length) : base()
            {
                Height = 23;
                Width = (double)length * 7.2 + 18;
                TextWrapping = TextWrapping.NoWrap;
                VerticalAlignment = VerticalAlignment.Top;
                HorizontalAlignment = HorizontalAlignment.Left;
                VerticalContentAlignment = VerticalAlignment.Center;
                HorizontalContentAlignment = HorizontalAlignment.Left;
                FontFamily = new FontFamily("Courier New");
                InsertKeyMode = InsertKeyMode.Overwrite;

                string mask = "";
                for (int i = 0; i < length; ++i)
                    mask += '0';
                Mask = mask + 'b';
                PromptChar = '0';

                Text = mask + 'b';

                PreviewTextInput += PreviewTextInputHandler;
                PreviewKeyDown += PreviewKeyDownHandler;
            }

            public uint Val
            {
                get
                {
                    string text = Text;
                    return Convert.ToUInt32(text.Substring(0, text.Length - 1), 2);
                }
                private set{}
            }

            public static void PreviewTextInputHandler(object sender, System.Windows.Input.TextCompositionEventArgs e)
            {
                e.Handled = Regex.IsMatch(e.Text, "[^01]+");
            }

            public static void PreviewKeyDownHandler(object sender, KeyEventArgs e)
            {
                if ((e.Key == Key.Back) || (e.Key == Key.Delete)) e.Handled = true;
            }
        }

        class HexadecimalMaskedTextBox : MaskedTextBox
        {
            public HexadecimalMaskedTextBox(int length) : base()
            {
                Height = 23;
                Width = (double)(int)(length / 4) * 7.2 + 18;
                TextWrapping = TextWrapping.NoWrap;
                VerticalAlignment = VerticalAlignment.Top;
                HorizontalAlignment = HorizontalAlignment.Left;
                VerticalContentAlignment = VerticalAlignment.Center;
                HorizontalContentAlignment = HorizontalAlignment.Left;
                FontFamily = new FontFamily("Courier New");
                InsertKeyMode = InsertKeyMode.Overwrite;

                string mask = ">";
                for (int i = length; i >= 4; i -= 4)
                    mask += 'A';
                Mask = mask + "h";
                PromptChar = '0';

                PreviewTextInput += PreviewTextInputHandler;
                PreviewKeyDown += PreviewKeyDownHandler;
            }

            public uint Val
            {
                get
                {
                    string text = Text;
                    return Convert.ToUInt32(text.Substring(0, text.Length - 1), 16);
                }
                private set {}
            }

            public static void PreviewTextInputHandler(object sender, TextCompositionEventArgs e)
            {
                e.Handled = Regex.IsMatch(e.Text, "[^0-9a-fA-F]+");
            }

            public static void PreviewKeyDownHandler(object sender, KeyEventArgs e)
            {
                if ((e.Key == Key.Back) || (e.Key == Key.Delete)) e.Handled = true;
            }
        }

        class TwoDigitUpDown : IntegerUpDown
        {
            public TwoDigitUpDown(int min, int max, int val) : base()
            {
                Minimum = min;
                Maximum = max;
                Increment = 1;
                Value = val;
                Height = 23;
                Width = 45;
                VerticalAlignment = VerticalAlignment.Top;
                HorizontalAlignment = HorizontalAlignment.Left;

                LostFocus += LostFocusHandler;
            }

            public void LostFocusHandler(object sender, RoutedEventArgs e)
            {
                if (Value == null) Value = Minimum;
            }

            public uint Val
            {
                get
                {
                    return (uint)(Value ?? 0);
                }
                private set { }
            }
        }

        class ImitComboBox : ComboBox
        {
            public ImitComboBox() : base()
            {
                Height = 23;
                Width = 125;
                VerticalAlignment = VerticalAlignment.Top;
                HorizontalAlignment = HorizontalAlignment.Left;
                Items.Add("Внешн.");
                Items.Add("Автономн. 200 Гц");
                Items.Add("Автономн. 400 Гц");
                Items.Add("Автономн. 800 Гц");
                SelectedIndex = 0;
            }

            public uint Val
            {
                get
                {
                    switch(SelectedIndex)
                    {
                        case 1: return 6;
                        case 2: return 5;
                        case 3: return 3;
                        default: return 0;
                    }
                }
                private set { }
            }
        }

        class ChannelComboBox : ComboBox
        {
            public ChannelComboBox()
                : base()
            {
                Height = 23;
                Width = 125;
                VerticalAlignment = VerticalAlignment.Top;
                HorizontalAlignment = HorizontalAlignment.Left;
                Items.Add("О");
                Items.Add("П1");
                Items.Add("П2");
                Items.Add("П3");
                Items.Add("П4");
                SelectedIndex = 0;
            }

            public uint Val
            {
                get { return (uint)SelectedIndex; }
                private set { }
            }
        }

        class IntervalComboBox : ComboBox
        {
            public IntervalComboBox()
                : base()
            {
                Height = 23;
                Width = 125;
                VerticalAlignment = VerticalAlignment.Top;
                HorizontalAlignment = HorizontalAlignment.Left;
                Items.Add("ПН П4");
                Items.Add("ПН 20");
                Items.Add("ПН1");
                Items.Add("ПН2");
                Items.Add("ПН3");
                Items.Add("ПН4");
                SelectedIndex = 0;
            }

            public uint Val
            {
                get { return (uint)SelectedIndex; }
                private set { }
            }
        }

        class SimpleCheckBox : CheckBox
        {
            public SimpleCheckBox(string text) : base()
            {
                Content = text;
                VerticalAlignment = VerticalAlignment.Top;
                HorizontalAlignment = HorizontalAlignment.Left;
            }

            public bool Val
            {
                get
                {
                    return IsChecked ?? false;
                }
                private set { }
            }
        }

        public class GraphData
        {
            public int mode;
            public int NChMsrFC;
            public byte N621control;
            public byte pelNapr;
            public byte[] data;
            public int length;

            public GraphData()
            {
                mode = 0;
                NChMsrFC = 0;
                N621control = 0;
                pelNapr = 0;
                data = new byte[1024*8];
                length = 0;
            }
        }
    }

    public partial class MainWindow
    {
        public class Test2 : Test, IDataReceiver, ICycleWriteAndReadSubject
        {
            /// <summary>
            /// Экземпляр класса
            /// </summary>
            private static Test2 instance = null;   

            /// <summary>
            /// Окно для отображения графиков
            /// </summary>
            private GraphWindow_test2 graphWnd;

            /// <summary>
            /// Код текущего режима
            /// </summary>
            private int mode;

            /// <summary>
            /// Признак изменения рабочих данных
            /// </summary>
            private bool dataChanged;

            /// <summary>
            /// Признак завершения инициализации (нужен для предотвращения попытки получения данных до окончательной инициализации всех представлений)
            /// </summary>
            private bool initialized;

            /// <summary>
            /// Состояние рабочего потока (запущен/не запущен)
            /// </summary>
            private bool running;

            /// <summary>
            /// Признак остановки потока
            /// </summary>
            private bool stopThread;

            /// <summary>
            /// Признак работы в режиме построения графиков
            /// </summary>
            private bool graphMode;

            /// <summary>
            /// Признак одиночного запроса данных
            /// </summary>
            private bool singleRequest;

            /// <summary>
            /// Разрешение работы ШД21
            /// </summary>
            private bool sd21enabled;

            /// <summary>
            /// Текущая передаваемая команда
            /// </summary>
            private byte[] command;

            /// <summary>
            /// Команда запроса данных
            /// </summary>
            private byte[] cmRequestData;

            /// <summary>
            /// Команда запроса данных для построения графиков
            /// </summary>
            private byte[] cmRequestGraphData;

            /// <summary>
            /// Команда остановки
            /// </summary>
            private byte[] cmStop;

            /// <summary>
            /// Номер отправляемого пакета (в режиме построения графиков)
            /// </summary>
            private int packetNumber;

            /// <summary>
            /// Массивы данных для построения графиков и ссылка на активный массив
            /// </summary>
            private GraphData graphData0, graphData1, currentGraphData;

            /// <summary>
            /// Источник данных
            /// </summary>
            private IDateSource n8Mode, errorFlags, sd21Control, n8Sygnals, ls21, ls31, twoLs, sd21;

            /// <summary>
            /// Метод-фабрика, предназначенный для создания единственного экземпляра класса
            /// </summary>
            /// <param name="wnd">Ссылка на главное окно программы</param>
            /// <param name="portModel">Ссылка на контроллер порта</param>
            /// <returns>Возвращает ссылку на объект Test2</returns>
            public static Test2 GetInstance(MainWindow wnd, PortModel portModel)
            {
                if (instance == null) instance = new Test2(wnd, portModel);
                return instance;
            }

            /// <summary>
            /// Конструктор
            /// </summary>
            /// <param name="wnd">Ссылка на главное окно программы</param>
            /// <param name="portModel">Ссылка на контроллер порта</param>
            private Test2(MainWindow wnd, PortModel portModel) : base(wnd, portModel) { }

            /// <summary>
            /// Инициализация теста
            /// </summary>
            protected override void Init()
            {
                mode = 0;
                dataChanged = true;
                initialized = false;
                running = false;
                stopThread = false;
                graphMode = false;
                singleRequest = false;
                sd21enabled = true;
                cmRequestData = new byte[] { 0xAA, 2, 0xAC };
                cmRequestGraphData = new byte[] { 0xAA, 0x04, 0 };
                cmStop = new byte[] { 0xAA, 3, 0xAD };
                graphData0 = new GraphData();
                graphData1 = new GraphData();
                currentGraphData = graphData0;

                // Выбор режима работы
                SelectionChangedEventHandler modeChangingHandler = (sender, e) =>
                {
                    //выбор и добавление режима, подрежима работы
                    if (sender == wnd.cbMode_test2)
                    {
                        //переключатель выбора количества подрежимов с добавлением названия
                        switch (wnd.cbMode_test2.SelectedIndex)
                        {
                            case 2: wnd.cbSubMode_test2.Items.Clear();
                                wnd.cbSubMode_test2.Items.Add("Сдвиг ИД");
                                wnd.cbSubMode_test2.Items.Add("Передача адр. по зав. №");
                                wnd.cbSubMode_test2.Items.Add("Чтение конст. ППМ");
                                wnd.cbSubMode_test2.Items.Add("Передача конст. ППМ");
                                wnd.cbSubMode_test2.Items.Add("Запись конст. в ПЗУ");
                                wnd.cbSubMode_test2.SelectedIndex = 0;
                                wnd.cbSubMode_test2.IsEnabled = true;
                                break;

                            case 3: wnd.cbSubMode_test2.Items.Clear();
                                wnd.cbSubMode_test2.Items.Add("Измерение УАП");
                                wnd.cbSubMode_test2.Items.Add("Измерение рангов");
                                wnd.cbSubMode_test2.SelectedIndex = 0;
                                wnd.cbSubMode_test2.IsEnabled = true;
                                break;

                            case 5: wnd.cbSubMode_test2.Items.Clear();
                                wnd.cbSubMode_test2.Items.Add("Вход в сост. прогр.");
                                wnd.cbSubMode_test2.Items.Add("Запрос сост.");
                                wnd.cbSubMode_test2.Items.Add("Передача адреса");
                                wnd.cbSubMode_test2.Items.Add("Передача данных");
                                wnd.cbSubMode_test2.Items.Add("Перезапись ПЗУ");
                                wnd.cbSubMode_test2.Items.Add("Чтение данных");
                                wnd.cbSubMode_test2.Items.Add("Верификация");
                                wnd.cbSubMode_test2.Items.Add("Отмена прогр.");
                                wnd.cbSubMode_test2.SelectedIndex = 0;
                                wnd.cbSubMode_test2.IsEnabled = true;
                                break;

                            case 6: wnd.cbSubMode_test2.Items.Clear();
                                wnd.cbSubMode_test2.Items.Add("Проверка синхросигн.");
                                wnd.cbSubMode_test2.Items.Add("Проверка по интервалам");
                                wnd.cbSubMode_test2.Items.Add("Проверка ПН1..6");
                                wnd.cbSubMode_test2.SelectedIndex = 0;
                                wnd.cbSubMode_test2.IsEnabled = true;
                                break;

                            case 7: wnd.cbSubMode_test2.Items.Clear();
                                //wnd.cbSubMode_test2.Items.Add("Проверка синхросигн.");
                                //wnd.cbSubMode_test2.Items.Add("Проверка по интервалам");
                                //wnd.cbSubMode_test2.Items.Add("Проверка ПН1..6");
                                //wnd.cbSubMode_test2.SelectedIndex = 0;
                                wnd.cbSubMode_test2.IsEnabled = false;
                                break;

                            default: wnd.cbSubMode_test2.IsEnabled = false;
                                wnd.cbSubMode_test2.Items.Clear();
                                break;
                        }
                    }

                    //переключатель индексов режимов подрежимов по порядку с обработкой исключений
                    switch (wnd.cbMode_test2.SelectedIndex)
                    {
                        case 0: mode = 0;
                            break;

                        case 1: mode = 1;
                            break;

                        case 2: if (wnd.cbSubMode_test2.SelectedIndex > 4) throw new Exception("Неизвестный элемент в списке");
                            mode = 2 + wnd.cbSubMode_test2.SelectedIndex;
                            break;

                        case 3: if (wnd.cbSubMode_test2.SelectedIndex > 1) throw new Exception("Неизвестный элемент в списке");
                            mode = 7 + wnd.cbSubMode_test2.SelectedIndex;
                            break;

                        case 4: mode = 9;
                            break;

                        case 5: if (wnd.cbSubMode_test2.SelectedIndex > 7) throw new Exception("Неизвестный элемент в списке");
                            mode = 10 + wnd.cbSubMode_test2.SelectedIndex;
                            break;

                        case 6: if (wnd.cbSubMode_test2.SelectedIndex > 2) throw new Exception("Неизвестный элемент в списке");
                            mode = 18 + wnd.cbSubMode_test2.SelectedIndex;
                            break;

                        case 7: //if (wnd.cbSubMode_test2.SelectedIndex > 2) throw new Exception("Неизвестный элемент в списке");
                            mode = 21;// + wnd.cbSubMode_test2.SelectedIndex;
                            break;

                        case 8: //if (wnd.cbSubMode_test2.SelectedIndex > 2) throw new Exception("Неизвестный элемент в списке");
                            mode = 22;// + wnd.cbSubMode_test2.SelectedIndex;
                            break;

                        case 9: //if (wnd.cbSubMode_test2.SelectedIndex > 2) throw new Exception("Неизвестный элемент в списке");
                            mode = 23;// + wnd.cbSubMode_test2.SelectedIndex;
                            break;

                        default: throw new Exception("Неизвестный элемент в списке");
                    }

                    //if (mode > 9) return;

                    initialized = false;
                    dataChanged = false;
                    ls21.ChangeMode(mode);
                    ls31.ChangeMode(mode);
                    twoLs.ChangeMode(mode);
                    sd21.ChangeMode(mode);
                    initialized = true;
                    dataChanged = true;

                    //режим данных для построения графиков
                    currentGraphData.mode = mode;
                    if (graphMode) { graphWnd.renewData(currentGraphData); }
                };

                wnd.cbMode_test2.SelectionChanged += modeChangingHandler;
                wnd.cbSubMode_test2.SelectionChanged += modeChangingHandler;

                // Выбор режима обновления данных
                RoutedEventHandler dataUpdateMode = (sender, e) =>
                {
                    if (sender == wnd.rbRequestingDataUpdate_test2)
                    {
                        wnd.bnUpdateData_test2.IsEnabled = true;
                        wnd.bnStop_test2.IsEnabled = true;
                        if (running) stopThread = true;
                    }
                    else
                    {
                        wnd.bnUpdateData_test2.IsEnabled = false;
                        wnd.bnStop_test2.IsEnabled = false;
                    }
                };

                //проверка переключателя постояного обновления данных:
                //постоянное обновление
                wnd.rbContinuousDataUpdate_test2.Checked += dataUpdateMode;
                //обновление один раз по запросу
                wnd.rbRequestingDataUpdate_test2.Checked += dataUpdateMode;

                // Выбор режима работы ШД21
                RoutedEventHandler sd21mode = (sender, e) =>
                {
                    if(sender == wnd.rbStandControlSD21_test2)
                    {
                        sd21enabled = true;
                        wnd.gbSD21_test2.Visibility = Visibility.Visible;
                    }
                    else
                    {
                        sd21enabled = false;
                        wnd.gbSD21_test2.Visibility = Visibility.Hidden;
                    }
                };
                //проверка переключателя выбора работы со стендом или с блочком 1001 по ШД21
                wnd.rbStandControlSD21_test2.Checked += sd21mode;
                wnd.rb1001ControlSD21_test2.Checked += sd21mode;

                // Кнопка "Запустить/остановить"
                wnd.bnRun_test2.Click += (sender, e) =>
                {
                    if (wnd.rbRequestingDataUpdate_test2.IsChecked ?? false)
                    {
                        command = GetNewCommand();
                        SendCommand();
                    }
                    else
                    {
                        if (!running)
                        {
                            command = GetNewCommand();
                            RunCycle();
                        }
                        else
                        {
                            stopThread = true;
                        }
                    }
                };

                // Кнопка "Остановить"
                wnd.bnStop_test2.Click += (sender, e) =>
                {
                    if (running)
                    {
                        singleRequest = false;
                        stopThread = true;
                    }
                    else
                    {
                        command = cmStop;
                        SendCommand();
                    }
                };

                // Кнопка "Обновить данные"
                wnd.bnUpdateData_test2.Click += (sender, e) =>
                {
                    if (graphMode)
                    {
                        if (!running)
                        {
                            if (dataChanged) { command = GetNewCommand(); }
                            else
                            {
                                command = cmRequestGraphData;
                                command[1] = 4;
                                int cs = command[0] + command[1];
                                command[2] = (byte)((cs >= 0x100) ? (cs & 0xFF) + 1 : cs);
                            }
                            singleRequest = true;
                            RunCycle();
                        }
                    }
                    else
                    {
                        if (dataChanged) { command = GetNewCommand(); }
                        else { command = cmRequestData; }
                        SendCommand();
                    }
                };

                // Кнопка отображения/закрытия графика
                wnd.tbGraph_test2.Click += (sender, e) =>
                {
                    if (wnd.tbGraph_test2.IsChecked ?? false)
                    {
                        packetNumber = 0;
                        graphMode = true;

                        graphWnd = new GraphWindow_test2(new EventHandler((sender1, e1) =>
                        {
                            wnd.tbGraph_test2.IsChecked = false;
                            graphMode = false;
                            graphWnd = null;
                        }));
                        graphWnd.Owner = wnd;
                        
                        graphWnd.Show();
                        graphWnd.renewData(currentGraphData);
                    }
                    else if (graphWnd != null) { graphWnd.Close(); }
                };

                n8Mode = new N8ModeView(this, wnd.rbN8Mode200_test2, wnd.rbN8Mode400_test2, wnd.rbN8Mode800_test2, wnd.rbN8ModeSDC_test2, wnd.rbN8Disabled_test2);
                errorFlags = new ErrorFlagsView(this, wnd.chbErrorFlag0_test2, wnd.chbErrorFlag1_test2, wnd.chbErrorFlag2_test2, wnd.chbErrorFlag3_test2, wnd.chbErrorFlag4_test2, wnd.chbErrorFlag5_test2, wnd.chbErrorFlag6_test2, wnd.chbErrorFlag7_test2);
                sd21Control = new SD21ControlView(this, wnd.rbStandControlSD21_test2, wnd.rb1001ControlSD21_test2);
                n8Sygnals = new N8SygnalsView(this, wnd.chbN8StrobePRD_test2, wnd.chbN8SynchrN6_test2);

                //-------------------------------------------------------------------------------
                // ЛС21
                //-------------------------------------------------------------------------------

                DataView[] ls21ViewList = new DataView[24];
                ls21ViewList[0] = new LS21_BR_DataView((N8SygnalsView)n8Sygnals);
                ls21ViewList[1] = new LS21_FC_DataView((N8SygnalsView)n8Sygnals);
                ls21ViewList[2] = new LS21_ID_Shift_DataView();
                ls21ViewList[3] = new LS21_ID_FactNum_DataView();
                ls21ViewList[4] = new LS21_ID_RdConst_DataView();
                ls21ViewList[5] = new LS21_ID_WrConst_DataView();
                ls21ViewList[6] = new LS21_ID_Overwrite_DataView();
                ls21ViewList[7] = new LS21_TC_UAP_DataView((N8SygnalsView)n8Sygnals);
                ls21ViewList[8] = new LS21_TC_Rank_DataView((N8SygnalsView)n8Sygnals);
                ls21ViewList[9] = new LS21_TR_DataView();
                ls21ViewList[10] = new LS21_PR_EnterProg_DataView();
                ls21ViewList[11] = new LS21_PR_RequestState_DataView();
                ls21ViewList[12] = new LS21_PR_SendAdr_DataView();
                ls21ViewList[13] = new LS21_PR_SendData_DataView();
                ls21ViewList[14] = new LS21_PR_Overwrite_DataView();
                ls21ViewList[15] = new LS21_PR_ReadData_DataView();
                ls21ViewList[16] = new LS21_PR_Verification_DataView();
                ls21ViewList[17] = new LS21_PR_Cancel_DataView();
                ls21ViewList[18] = new LS21_DB_Synchr_DataView();
                ls21ViewList[19] = new LS21_DB_Intervals_DataView((N8SygnalsView)n8Sygnals);
                ls21ViewList[20] = new LS21_DB_PN_DataView((N8SygnalsView)n8Sygnals);
                ls21ViewList[21] = new NULL_DataView();
                ls21ViewList[22] = new LS21_ZAPRANG_DataView();
                ls21ViewList[23] = new LS21_READRANG_DataView();

                ls21 = new LS321DataController(this, ls21ViewList, wnd.grLS21_test2, wnd.mtbLS21_test2, wnd.rbSelectLS21_test2, wnd.rbInputLS21_test2, wnd.tbLS21_test2, wnd.tbStatus21_test2);

                //-------------------------------------------------------------------------------
                // ЛС31
                //-------------------------------------------------------------------------------

                DataView[] ls31ViewList = new DataView[24];
                ls31ViewList[0] = new LS31_BR_DataView((N8SygnalsView)n8Sygnals);
                ls31ViewList[1] = new LS31_FC_DataView((N8SygnalsView)n8Sygnals);
                ls31ViewList[2] = new LS31_ID_Shift_DataView();
                ls31ViewList[3] = new LS31_ID_FactNum_DataView();
                ls31ViewList[4] = new LS31_ID_RdConst_DataView();
                ls31ViewList[5] = new LS31_ID_WrConst_DataView();
                ls31ViewList[6] = new LS31_ID_Overwrite_DataView();
                ls31ViewList[7] = new LS31_TC_UAP_DataView();
                ls31ViewList[8] = new LS31_TC_Rank_DataView();
                ls31ViewList[9] = new LS31_TR_DataView((N8SygnalsView)n8Sygnals);
                ls31ViewList[10] = new LS31_PR_EnterProg_DataView();
                ls31ViewList[11] = new LS31_PR_RequestState_DataView();
                ls31ViewList[12] = new LS31_PR_SendAdr_DataView();
                ls31ViewList[13] = new LS31_PR_SendData_DataView();
                ls31ViewList[14] = new LS31_PR_Overwrite_DataView();
                ls31ViewList[15] = new LS31_PR_ReadData_DataView();
                ls31ViewList[16] = new LS31_PR_Verification_DataView();
                ls31ViewList[17] = new LS31_PR_Cancel_DataView();
                ls31ViewList[18] = new LS31_DB_Synchr_DataView();
                ls31ViewList[19] = new LS31_DB_Intervals_DataView();
                ls31ViewList[20] = new LS31_DB_PN_DataView();
                ls31ViewList[21] = new NULL_DataView();
                ls31ViewList[22] = new NULL_DataView();
                ls31ViewList[23] = new NULL_DataView();

                ls31 = new LS321DataController(this, ls31ViewList, wnd.grLS31_test2, wnd.mtbLS31_test2, wnd.rbSelectLS31_test2, wnd.rbInputLS31_test2, wnd.tbLS31_test2, wnd.tbStatus31_test2);

                //-------------------------------------------------------------------------------
                // 2ЛС
                //-------------------------------------------------------------------------------

                twoLs = new TwoLSDataController(this, wnd.gr2LS_test2, wnd.mtb2LS_test2, wnd.rbSelect2LS_test2, wnd.rbInput2LS_test2);

                //-------------------------------------------------------------------------------
                // ШД21
                //-------------------------------------------------------------------------------

                DataView[] sd21ViewList = new DataView[24];
                sd21ViewList[0] = new SD21_BR_DataView();
                sd21ViewList[1] = new SD21_FC_DataView();
                sd21ViewList[2] = new NULL_DataView();
                sd21ViewList[3] = new NULL_DataView();
                sd21ViewList[4] = new NULL_DataView();
                sd21ViewList[5] = new NULL_DataView();
                sd21ViewList[6] = new NULL_DataView();
                sd21ViewList[7] = new SD21_TC_UAP_DataView();
                sd21ViewList[8] = new SD21_TC_Rank_DataView();
                sd21ViewList[9] = new NULL_DataView();
                sd21ViewList[10] = new NULL_DataView();
                sd21ViewList[11] = new NULL_DataView();
                sd21ViewList[12] = new NULL_DataView();
                sd21ViewList[13] = new NULL_DataView();
                sd21ViewList[14] = new NULL_DataView();
                sd21ViewList[15] = new NULL_DataView();
                sd21ViewList[16] = new NULL_DataView();
                sd21ViewList[17] = new NULL_DataView();
                sd21ViewList[18] = new NULL_DataView();
                sd21ViewList[19] = new SD21_DB_Intervals_DataView();
                sd21ViewList[20] = new SD21_DB_PN_DataView();
                sd21ViewList[21] = new NULL_DataView();
                sd21ViewList[22] = new NULL_DataView();
                sd21ViewList[23] = new NULL_DataView();

                sd21 = new SD21DataController(this, sd21ViewList, wnd.grSD21_test2, wnd.mtbSD21_test2, wnd.rbSelectSD21_test2, wnd.rbInputSD21_test2);

                ls21.ChangeMode(mode);
                ls31.ChangeMode(mode);
                twoLs.ChangeMode(mode);
                sd21.ChangeMode(mode);

                initialized = true;
            }

            public void DataChanged(object param)
            {
                if(initialized)
                    dataChanged = true;
            }

            /// <summary>
            /// Отправить команду
            /// </summary>
            private void SendCommand()
            {
                try
                {
                    portModel.WriteAndRead(command, 100, this);
                }
                catch (Exception ex)
                {
                    System.Windows.MessageBox.Show(wnd, ex.Message, "Ошибка записи данных на порт", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }

            /// <summary>
            /// Запустить цикл
            /// </summary>
            private void RunCycle()
            {
                // Запуск циклической передачи данных
                try
                {
                    packetNumber = 0;
                    //command = GetNewCommand();
                    stopThread = false;
                    portModel.RunCyclicWriteAndRead(command, 100, this);
                    if (!singleRequest) { wnd.bnRun_test2.Content = "Остановить"; }
                    running = true;
                }
                catch (Exception ex)
                {
                    System.Windows.MessageBox.Show(wnd, ex.Message, "Ошибка записи данных на порт", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }

            /// <summary>
            /// Получить новую команду
            /// </summary>
            /// <returns>Возвращает команду (массив данных), составленную из актуальных данных</returns>
            private byte[] GetNewCommand()
            {
                dataChanged = false;
                byte[] data, n8mode, errorFlagsData, sd21ControlData, lastSygnalData, ls21Data, ls31Data, twoLsData, sd21Data;
                n8mode = n8Mode.GetData();
                errorFlagsData = errorFlags.GetData();
                sd21ControlData = sd21Control.GetData();
                lastSygnalData = n8Sygnals.GetData();
                ls21Data = ls21.GetData();
                ls31Data = ls31.GetData();
                twoLsData = twoLs.GetData();
                sd21Data = sd21.GetData();
                data = new byte[9 + ls21Data.Length + ls31Data.Length + twoLsData.Length + (sd21enabled ? sd21Data.Length : 0)];

                int index = 0;
                data[index++] = 0xAA;
                data[index++] = (byte)(((n8mode[0] & 7) << 4) | ((sd21ControlData[0] & 1) << 7) | 1);
                data[index++] = (byte)(lastSygnalData[0] & 3);
                data[index++] = errorFlagsData[0];
                data[index++] = (byte)twoLsData.Length;
                for (int i = 0; i < twoLsData.Length; ++i)
                    data[index++] = twoLsData[i];
                if (sd21enabled)
                {
                    data[index++] = (byte)sd21Data.Length;
                    for (int i = 0; i < sd21Data.Length; ++i)
                        data[index++] = sd21Data[i];
                }
                else { data[index++] = 0; }
                data[index++] = (byte)ls21Data.Length;
                for (int i = 0; i < ls21Data.Length; ++i)
                    data[index++] = ls21Data[i];
                data[index++] = (byte)ls31Data.Length;
                for (int i = 0; i < ls31Data.Length; ++i)
                    data[index++] = ls31Data[i];
                data[index] = CheckSumCalculator.Calculate(data, 0, index + 1);

                return data;
            }

            /// <summary>
            /// Метод, вызываемый после остановки цикла
            /// </summary>
            public void CyclicWriteCompleted()
            {
                wnd.Dispatcher.BeginInvoke(new Action(() =>
                {
                    wnd.bnRun_test2.Content = "Запустить";
                    running = false;
                }));
            }

            /// <summary>
            /// Метод, вызываемый при получении данных с порта
            /// </summary>
            /// <param name="progData">Массив данных</param>
            /// <param name="count">Число данных</param>
            public void DataReceived(byte[] data, int count)
            {
                bool error = !HandleData(data, count);

                // Если данные передаются в цикле
                if (running)
                {
                    // При одиночном запросе останавливаем поток по завершению загрузки данных или при обнаружении ошибки
                    if(singleRequest && (error || stopThread))
                    {
                        singleRequest = false;
                        portModel.StopCyclicWrite();
                    }
                    // Если установлен флаг остановки потока, то передаем команду завершения работы и останавливаем поток
                    else if (stopThread)
                    {
                        if (command != cmStop)
                        {
                            command = cmStop;
                            portModel.RenewWritingData(command, 100);
                        }
                        else portModel.StopCyclicWrite();
                    }
                    // Если изменились данные или получен ответ с ошибкой, то передаем новые данные
                    else if (dataChanged || error)
                    {
                        wnd.Dispatcher.Invoke(DispatcherPriority.Send, new Action(() =>
                        {
                            command = GetNewCommand();
                        }));
                        portModel.RenewWritingData(command, 100);
                        packetNumber = 0;
                    }
                    // Работа в режиме построения графика
                    else if(graphMode)
                    {
                        if (command != cmRequestGraphData) { command = cmRequestGraphData; }
                        command[1] = (byte)((packetNumber << 4) | 4);
                        int cs = command[0] + command[1];
                        command[2] = (byte)((cs >= 0x100) ? (cs & 0xFF) + 1 : cs);
                        portModel.RenewWritingData(command, 100);
                    }
                    // Если данные были успешно переданы, то меняем команду на "запрос данных"
                    else if (command != cmRequestData)
                    {
                        command = cmRequestData;
                        portModel.RenewWritingData(command, 100);
                    }
                }
            }

            /// <summary>
            /// Обработать принятые данные
            /// </summary>
            /// <param name="progData">Массив данных</param>
            /// <param name="count">Число данных</param>
            /// <returns>Возвращает признак успешной обработки данных (нет ошибки)</returns>
            private bool HandleData(byte[] data, int count)
            {
                // Проверяем наличие данных
                if (count == 0)
                {
                    wnd.Dispatcher.BeginInvoke(DispatcherPriority.Send, new Action(() =>
                    {
                        wnd.tbStatus_test2.Inlines.Clear();
                        wnd.tbStatus_test2.Inlines.Add(new Run("Нет ответа") { Foreground = Brushes.Red, FontWeight = FontWeights.Bold });
                        wnd.tbStatus21_test2.Text = "";
                        wnd.tbStatus31_test2.Text = "";
                        NoDataError(wnd.tbLS21_test2);
                        NoDataError(wnd.tbLS31_test2);
                    }));
                    return false;
                }
                // Проверяем минимальный размер пакета и заголовочное слово
                else if ((count < 8) || (data[0] != 0xAA))
                {
                    wnd.Dispatcher.BeginInvoke(DispatcherPriority.Send, new Action(() =>
                    {
                        PacketError(wnd.tbStatus_test2);
                        wnd.tbStatus21_test2.Text = "";
                        wnd.tbStatus31_test2.Text = "";
                        NoDataError(wnd.tbLS21_test2);
                        NoDataError(wnd.tbLS31_test2);
                    }));
                    return false;
                }

                // Проверяем код команды во 2 слове
                int commandCode = data[1] & 0xF;
                int Npacket = data[1] >> 4;
                if(commandCode > 4)
                {
                    wnd.Dispatcher.BeginInvoke(DispatcherPriority.Send, new Action(() =>
                    {
                        PacketError(wnd.tbStatus_test2);
                        wnd.tbStatus21_test2.Text = "";
                        wnd.tbStatus31_test2.Text = "";
                        NoDataError(wnd.tbLS21_test2);
                        NoDataError(wnd.tbLS31_test2);
                    }));
                    return false;
                }

                byte mode01 = 0, err01 = 0, nSD21 = 0, n_ls21 = 0, n_ls31 = 0;
                byte[] ls21_data = null, ls31_data = null;

                // Обработка стандартного пакета данных
                if ((commandCode <= 3) || ((commandCode == 4) && (Npacket == 0)))
                {
                    // Проверяем контрольную сумму
                    if (data[count - 1] != CheckSumCalculator.Calculate(data, 0, count - 1))
                    {
                        wnd.Dispatcher.BeginInvoke(DispatcherPriority.Send, new Action(() =>
                        {
                            CheckSumError(wnd.tbStatus_test2);
                            wnd.tbStatus21_test2.Text = "";
                            wnd.tbStatus31_test2.Text = "";
                            NoDataError(wnd.tbLS21_test2);
                            NoDataError(wnd.tbLS31_test2);
                        }));
                        return false;
                    }

                    // Состояние Н6.21.10.01
                    //режим 1001 в ответном пакете от усма1_3е слово
                    mode01 = data[2];
                    //регистр ошибок младшие 4ре разряда_1й-Д21 не переданы данные шд21_2й-Р21 не переданы ранги шд21_3й-М21 не принят номер режима_4й-имит есть или нет
                    err01 = data[3];
                    //количество слов переданных по шд21
                    nSD21 = data[4];

                    // Данные ЛС21
                    //кол-во слов по лс21
                    n_ls21 = data[5];
                    //тут какая проверка на количество и выдача ошибок
                    ls21_data = (n_ls21 != 0 ? new byte[n_ls21] : null);
                    int index = 5;
                    for (int i = 0; i < n_ls21; ++i)
                    {
                        if ((++index + 1) > count)
                        {
                            wnd.Dispatcher.BeginInvoke(DispatcherPriority.Send, new Action(() =>
                            {
                                PacketError(wnd.tbStatus_test2);
                                wnd.tbStatus21_test2.Text = "";
                                wnd.tbStatus31_test2.Text = "";
                                NoDataError(wnd.tbLS21_test2);
                                NoDataError(wnd.tbLS31_test2);
                            }));
                            return false;
                        }
                        //запись слова в соответствующий массив для ЛС21 видимо для обработки 
                        ls21_data[i] = data[index];
                    }

                    // Данные ЛС31 аналогично ЛС21
                    if ((++index + 1) > count)
                    {
                        wnd.Dispatcher.BeginInvoke(DispatcherPriority.Send, new Action(() =>
                        {
                            PacketError(wnd.tbStatus_test2);
                            wnd.tbStatus21_test2.Text = "";
                            wnd.tbStatus31_test2.Text = "";
                            NoDataError(wnd.tbLS21_test2);
                            NoDataError(wnd.tbLS31_test2);
                        }));
                        return false;
                    }
                    n_ls31 = data[index];
                    ls31_data = (n_ls31 != 0 ? new byte[n_ls31] : null);
                    for (int i = 0; i < n_ls31; ++i)
                    {
                        if ((++index + 1) > count)
                        {
                            wnd.Dispatcher.BeginInvoke(DispatcherPriority.Send, new Action(() =>
                            {
                                PacketError(wnd.tbStatus_test2);
                                wnd.tbStatus21_test2.Text = "";
                                wnd.tbStatus31_test2.Text = "";
                                NoDataError(wnd.tbLS21_test2);
                                NoDataError(wnd.tbLS31_test2);
                            }));
                            return false;
                        }
                        ls31_data[i] = data[index];
                    }

                    // Проверяем длину пакета
                    if ((++index + 1) != count)
                    {
                        wnd.Dispatcher.BeginInvoke(DispatcherPriority.Send, new Action(() =>
                        {
                            PacketError(wnd.tbStatus_test2);
                            wnd.tbStatus21_test2.Text = "";
                            wnd.tbStatus31_test2.Text = "";
                            NoDataError(wnd.tbLS21_test2);
                            NoDataError(wnd.tbLS31_test2);
                        }));
                        return false;
                    }

                    // Выводим данные, если не обнаружено ошибок_вывод регистра состояния 1001 в окошко и отображение конкретной ошибки если есть
                    wnd.Dispatcher.BeginInvoke(DispatcherPriority.Send, new Action(() =>
                    {
                        if (sd21enabled)
                        {
                            wnd.tbStatus_test2.Text = "Режим Н6.21.11.01: ";
                            wnd.tbStatus_test2.Inlines.Add(new Run(Convert.ToString(mode01, 2).PadLeft(8, '0') + 'b') { FontFamily = new FontFamily("Courier New"), FontWeight = FontWeights.Bold });
                            if ((err01 & 4) != 0) wnd.tbStatus_test2.Inlines.Add(new Run("   ModeErr") { FontWeight = FontWeights.Bold, Foreground = Brushes.Red });
                            if ((err01 & 2) != 0) wnd.tbStatus_test2.Inlines.Add(new Run("   CosErr") { FontWeight = FontWeights.Bold, Foreground = Brushes.Red });
                            if ((err01 & 1) != 0) wnd.tbStatus_test2.Inlines.Add(new Run("   Data21Err_" + nSD21.ToString()) { FontWeight = FontWeights.Bold, Foreground = Brushes.Red });
                        }
                        else { wnd.tbStatus_test2.Text = ""; }
                        ls21.DisplayData(ls21_data, DisplayArray, NoDataError, CheckSumError, PacketError);
                        ls31.DisplayData(ls31_data, DisplayArray, NoDataError, CheckSumError, PacketError);
                    }));
                }
                // Обработка пакета с данными для построения графика
                if(commandCode == 4)
                {
                    // Проверяем номер пакета
                    if ((Npacket > 6) || (Npacket != packetNumber))
                    {
                        wnd.Dispatcher.BeginInvoke(DispatcherPriority.Send, new Action(() =>
                        {
                            PacketError(wnd.tbStatus_test2);
                        }));
                        return false;
                    }

                    // Первый пакет
                    if(Npacket == 0)
                    {
                        currentGraphData = (currentGraphData == graphData0 ? graphData1 : graphData0);
                        currentGraphData.length = 0;

                        switch(mode)
                        {
                            // БР
                            case 0:     if (n_ls21 == 14)
                                        {
                                            currentGraphData.N621control = (byte)(ls21_data[0] & 0x7F);
                                            currentGraphData.pelNapr = (byte)(ls21_data[5] & 0x3F);
                                        }
                                        currentGraphData.mode = mode;
                                        break;

                            // ФК, ТЕХН УАП, ТЕХН РАНГИ
                            case 1:
                            case 7:
                            case 8:     if (n_ls21 == 14)
                                        {
                                            if (mode == 1) { currentGraphData.NChMsrFC = ls21_data[5] & 7; }
                                            currentGraphData.N621control = (byte)(ls21_data[0] & 0x7F);
                                        }
                                        currentGraphData.mode = mode;
                                        break;

                            default:    currentGraphData.mode = -1;
                                        break;
                        }
                    }
                    else
                    {
                        if (count != (data[2] | (data[3] << 8)))
                        {
                            wnd.Dispatcher.BeginInvoke(DispatcherPriority.Send, new Action(() =>
                            {
                                PacketError(wnd.tbStatus_test2);
                            }));
                            return false;
                        }

                        for (int i = 0; i < count - 4; ++i)
                            currentGraphData.data[currentGraphData.length + i] = data[i + 4];

                        currentGraphData.length += (count - 4);
                    }

                    // Последний пакет
                    if (++packetNumber > 5)
                    {
                        packetNumber = 0;

                        // Проверяем размер объединенного пакета
                        if (currentGraphData.length != (int)GraphConst.SL_DATALIST_SIZE)
                        {
                            wnd.Dispatcher.BeginInvoke(DispatcherPriority.Send, new Action(() =>
                            {
                                PacketError(wnd.tbStatus_test2);
                            }));
                            return false;
                        }
                        else
                        {
                            // Передаем данные окну с графиком
                            if (graphWnd != null)
                            {
                                GraphData gd = currentGraphData;
                                graphWnd.Dispatcher.BeginInvoke(DispatcherPriority.Send, new Action(() =>
                                {
                                    graphWnd.renewData(gd);
                                }));
                            }
                            if (singleRequest) { stopThread = true; }
                        }
                    }
                }
                return true;
            }

            /// <summary>
            /// Вывод массива данных на форму
            /// </summary>
            /// <param name="textBlock">Текстовый блок для ввывода данных</param>
            /// <param name="progData">Массив данных</param>
            public void DisplayArray(TextBlock textBlock, byte[] data)
            {
                textBlock.Inlines.Add(new Run("Получено " + data.Length + " байт(а):" + Environment.NewLine));
                StringBuilder sb = new StringBuilder(14 * 3 + 3);
                for (int i = 0; (i < data.Length) && (i < 14); ++i)
                    sb.Append(String.Format("{0:X2} ", data[i]));
                if (data.Length > 14) sb.Append("...");
                textBlock.Inlines.Add(new Run(sb.ToString()) { FontFamily = new FontFamily("Courier New"), FontWeight = FontWeights.Bold });
            }

            public void NoDataError(TextBlock textBlock)
            {
                textBlock.Inlines.Clear();
                textBlock.Inlines.Add(new Run("Нет данных") { Foreground = Brushes.Red, FontWeight = FontWeights.Bold });
            }

            public void CheckSumError(TextBlock textBlock)
            {
                textBlock.Inlines.Clear();
                textBlock.Inlines.Add(new Run("Ошибка контрольной суммы") { Foreground = Brushes.Red, FontWeight = FontWeights.Bold });
            }

            public void PacketError(TextBlock textBlock)
            {
                textBlock.Inlines.Clear();
                textBlock.Inlines.Add(new Run("Ошибка пакета") { Foreground = Brushes.Red, FontWeight = FontWeights.Bold });
            }
        }
    }
}
