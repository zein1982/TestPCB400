using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;
using System.Windows.Threading;
using Test20311M.Test2;

namespace Test20311M
{
    namespace Test2
    {
        /// <summary>
        /// Параметры массива данных для построения графиков
        /// </summary>
        enum GraphConst
        {
            SL_LS21_INDEX = (0),
            SL_LS31_INDEX = (1),
            SL_DATALIST_SIZE = (1024*4  + 4),
            SL_PSO_HEAD = (4),
            SL_PS1_HEAD = (SL_PSO_HEAD + 1024),
            SL_PS2_HEAD = (SL_PS1_HEAD + 1024),
            SL_PS3_HEAD = (SL_PS2_HEAD + 1024),
            SL_PSX_HEAD = (4),
            SL_PSY_HEAD = (SL_PSX_HEAD + 1024),
            SL_UAP1_HEAD = (4),
            SL_UAP2_HEAD = (SL_UAP1_HEAD + 256),
            SL_UAP3_HEAD = (SL_UAP2_HEAD + 256),
            SL_UAP4_HEAD = (SL_UAP3_HEAD + 256),
            SL_PO_HEAD = (4),
            SL_P1_HEAD = (SL_PO_HEAD + 256),
            SL_P2_HEAD = (SL_P1_HEAD + 256),
            SL_P3_HEAD = (SL_P2_HEAD + 256),
            SL_P4_HEAD = (SL_P3_HEAD + 256),
            SL_R1_HEAD = (SL_P4_HEAD + 256),
            SL_R2_HEAD = (SL_R1_HEAD + 256),
            SL_R3_HEAD = (SL_R2_HEAD + 256),
            SL_R4_HEAD = (SL_R3_HEAD + 256),
            SL_R5_HEAD = (SL_R4_HEAD + 256),
            SL_R6_HEAD = (SL_R5_HEAD + 256)
        }

        public enum LineGraphType { grUAP, grPower, grRank }

        /// <summary>
        /// Абстрактный класс графика
        /// </summary>
        public abstract class GraphUnit : Canvas
        {
            /// <summary>
            /// Размер пикселя (количество точек на дюйм)
            /// </summary>
            protected static double pixelSize;

            /// <summary>
            /// Пиксельная поправка
            /// </summary>
            protected static double pc;

            /// <summary>
            /// Данные для построения графиков
            /// </summary>
            public static GraphData graphData { get; set; }

            /// <summary>
            /// Число точек для построения графиков
            /// </summary>
            public static int pointNumber { get; set; }

            /// <summary>
            /// Статический конструктор
            /// </summary>
            static GraphUnit()
            {
                graphData = null;
                pointNumber = 256;
            }

            /// <summary>
            /// Инициализация статических полей
            /// </summary>
            /// <param name="pixelSize">Размер пикселя (количество точек на дюйм)</param>
            /// <param name="pixelCorrection">Пиксельная коррекция</param>
            public static void setPixelParameters(double pixelSize, double pixelCorrection)
            {
                GraphUnit.pixelSize = pixelSize;
                GraphUnit.pc = pixelCorrection;
            }

            /// <summary>
            /// Выравнивание по пикселям
            /// </summary>
            /// <param name="val">Выравниваемая величина</param>
            public static void align(ref double val)
            {
                double div = (val * 1000) / (pixelSize * 1000);
                val = (int)div * pixelSize;
            }

            /// <summary>
            /// Инициализация дополнительных элементов на графике
            /// </summary>
            /// <param name="grid">Сетка для размещения элементов</param>
            /// <param name="row">Номер ряда в сетке</param>
            public abstract void init(Grid grid, int row);
        }

        /// <summary>
        /// График пилот-сигнала
        /// </summary>
        public class PilotSignalGraph : GraphUnit
        {
            private Pen black1 = new Pen(Brushes.Black, 1) { LineJoin = PenLineJoin.Round, StartLineCap = PenLineCap.Round, EndLineCap = PenLineCap.Round };
            private Pen black1p5 = new Pen(Brushes.Black, 1.5) { LineJoin = PenLineJoin.Round, StartLineCap = PenLineCap.Round, EndLineCap = PenLineCap.Round };
            private Pen black2 = new Pen(Brushes.Black, 2) { LineJoin = PenLineJoin.Round, StartLineCap = PenLineCap.Round, EndLineCap = PenLineCap.Round };
            private Typeface tfSegoe = new Typeface(new FontFamily("Segoe UI"), FontStyles.Normal, FontWeights.Normal, FontStretches.Condensed);
            private Typeface tfSegoeBold = new Typeface(new FontFamily("Segoe UI"), FontStyles.Normal, FontWeights.Bold, FontStretches.Condensed);

            /// <summary>
            /// Номер канала (0 - Канал О; 1..3 - Каналы П1..П3)
            /// </summary>
            public int channelNumber { get; set; }

            /// <summary>
            /// Признак двойного слова данных
            /// </summary>
            private bool dwordSize;

            /// <summary>
            /// Конструтор
            /// </summary>
            /// <param name="channelNumber">Номер канала (0 - Канал О; 1..4 - Каналы П1..П4)</param>
            public PilotSignalGraph(int channelNumber, bool dwordSize) : base()
            {
                this.channelNumber = channelNumber;
                this.dwordSize = dwordSize;
            }

            public override void init(Grid grid, int row) { }

            /// <summary>
            /// Прорисовка графика
            /// </summary>
            /// <param name="dc">"Контекст рисования"</param>
            protected override void OnRender( DrawingContext dc )
            {
                base.OnRender(dc);

                // Определяем размер единичного отрезка для графика
                double x0,
                    width = ActualWidth,
                    heigth = ActualHeight;

                if((width - 50) < (heigth - 70))
                    x0 = (width - 50) / 2 + pc;
                else
                    x0 = (heigth - 70) / 2 + pc;

                // Выраниваем величину единичного отрезка по пикселям
                align(ref x0);

                // Рисуем оси
                dc.DrawLine(black1, new Point(10 + pc, x0 + 60 + pc), new Point(x0 + x0 + 40 + pc, x0 + 60 + pc));
                dc.DrawLine(black1, new Point(x0 + 10 + pc, 30 + pc), new Point(x0 + 10 + pc, x0 + x0 + 60 + pc));

                // Отмечаем единичные отрезки на осях
                dc.DrawLine(black1, new Point(10 + pc, x0 + 57 + pc), new Point(10 + pc, x0 + 63 + pc));
                dc.DrawLine(black1, new Point(x0 + x0 + 10 + pc, x0 + 57 + pc), new Point(x0 + x0 + 10 + pc, x0 + 63 + pc));
                dc.DrawLine(black1, new Point(x0 + 7 + pc, 60 + pc), new Point(x0 + 13 + pc, 60 + pc));
                dc.DrawLine(black1, new Point(x0 + 7 + pc, x0 + x0 + 60 + pc), new Point(x0 + 13 + pc, x0 + x0 + 60 + pc));

                // стрелки
                dc.DrawLine(black1, new Point(x0 + x0 + 40 + pc, x0 + 60 + pc), new Point(x0 + x0 + 30 + pc, x0 + 55 + pc));
                dc.DrawLine(black1, new Point(x0 + x0 + 40 + pc, x0 + 60 + pc), new Point(x0 + x0 + 30 + pc, x0 + 65 + pc));
                dc.DrawLine(black1, new Point(x0 + 10 + pc, 30 + pc), new Point(x0 + 5 + pc, 40 + pc));
                dc.DrawLine(black1, new Point(x0 + 10 + pc, 30 + pc), new Point(x0 + 15 + pc, 40 + pc));

                // Название графика
                dc.DrawText(new FormattedText("Пилот-сигнал " + (channelNumber == 0 ? "O" : "П" + channelNumber.ToString()), System.Globalization.CultureInfo.CurrentCulture, FlowDirection.LeftToRight, tfSegoeBold, 16, Brushes.Black), new Point(20 + pc, 5 + pc));

                // Подписи осей
                dc.DrawText(new FormattedText("Re", System.Globalization.CultureInfo.CurrentCulture, FlowDirection.LeftToRight, tfSegoe, 14, Brushes.Black), new Point(x0 + x0 + 20 + pc, x0 + 65 + pc));
                dc.DrawText(new FormattedText("Im", System.Globalization.CultureInfo.CurrentCulture, FlowDirection.LeftToRight, tfSegoe, 14, Brushes.Black), new Point(x0 - 20 + pc, 30 + pc));

                // Подписи единичных отрезков
                dc.DrawText(new FormattedText("1", System.Globalization.CultureInfo.CurrentCulture, FlowDirection.LeftToRight, tfSegoe, 14, Brushes.Black), new Point(x0 + x0 + 5 + pc, x0 + 65 + pc));
                dc.DrawText(new FormattedText("1", System.Globalization.CultureInfo.CurrentCulture, FlowDirection.LeftToRight, tfSegoe, 14, Brushes.Black), new Point(x0 - 5 + pc, 50 + pc));
                dc.DrawText(new FormattedText("0", System.Globalization.CultureInfo.CurrentCulture, FlowDirection.LeftToRight, tfSegoe, 14, Brushes.Black), new Point(x0 - 5 + pc, x0 + 65 + pc));

                // Точки, матожидание, дисперсия
                if ((graphData != null) && (graphData.data != null) && (graphData.length > 0) && (pointNumber > 0))
                {
                    // Определяем базовый адрес и начальный индекс массива данных для построения графика
                    int baseAdr1, baseAdr2 = 0, dataIndex1;
                    if (dwordSize)
                    {
                        baseAdr1 = (int)GraphConst.SL_PSX_HEAD;
                        baseAdr2 = (int)GraphConst.SL_PSY_HEAD;
                        dataIndex1 = graphData.data[(int)GraphConst.SL_LS21_INDEX];
                    }
                    else
                    {
                        switch (channelNumber)
                        {
                            case 0: baseAdr1 = (int)GraphConst.SL_PSO_HEAD;
                                dataIndex1 = graphData.data[(int)GraphConst.SL_LS21_INDEX];
                                break;
                            case 1: baseAdr1 = (int)GraphConst.SL_PS1_HEAD;
                                dataIndex1 = graphData.data[(int)GraphConst.SL_LS31_INDEX];
                                break;
                            case 2: baseAdr1 = (int)GraphConst.SL_PS2_HEAD;
                                dataIndex1 = graphData.data[(int)GraphConst.SL_LS31_INDEX];
                                break;
                            case 3: baseAdr1 = (int)GraphConst.SL_PS3_HEAD;
                                dataIndex1 = graphData.data[(int)GraphConst.SL_LS31_INDEX];
                                break;
                            default: throw new Exception("Номер канала должен быть числом от 0 до 3");
                        }
                    }

                    // Рисуем точки и вычисляем матожидание и дисперсию
                    long Mx = 0, My = 0, Xsq = 0, Ysq = 0;
                    //int DwordMaxVal = int.MaxValue;
                    int DwordMaxVal = 0x007FFFFF;           // NB! Когда понадобится вернуться к 32-разрядным значениям, то помимо замены DwordMaxVal на int.MaxValue также понадобится добавить пробелов, добавляемым перед числами, когда они выводятся в файл в десятичном ыиде (сейчас 10 пробелов)
                    if (dwordSize)
                    {
                        
                        for (int i = 0; i < (pointNumber << 2); i += 4)
                        {
                            int index1 = (((dataIndex1 + i) & 0xFF) << 2) + baseAdr1;
                            int index2 = (((dataIndex1 + i) & 0xFF) << 2) + baseAdr2;
                            int X1 = (int)(graphData.data[index1] | (graphData.data[index1 + 1] << 8) | (graphData.data[index1 + 2] << 16) | (graphData.data[index1 + 3] << 24));
                            int Y1 = (int)(graphData.data[index2] | (graphData.data[index2 + 1] << 8) | (graphData.data[index2 + 2] << 16) | (graphData.data[index2 + 3] << 24));
                            double x1 = (double)X1 / DwordMaxVal * x0;
                            double y1 = (double)Y1 / DwordMaxVal * x0;
                            dc.DrawLine(black1p5, new Point(x0 + 10 + x1 + pc, x0 + 60 - y1 + pc), new Point(x0 + 10 + x1 + pc, x0 + 60 - y1 + pc));

                            Mx += X1;
                            My += Y1;
                            Xsq += (long)X1 * X1;
                            Ysq += (long)Y1 * Y1;
                        }
                    }
                    else
                    {
                        for (int i = 0; i < (pointNumber << 2); i += 4)
                        {
                            int index = (((dataIndex1 + i) & 0xFF) << 2) + baseAdr1;
                            short X1 = (short)(graphData.data[index] | (graphData.data[index + 1] << 8));
                            short Y1 = (short)(graphData.data[index + 2] | (graphData.data[index + 3] << 8));
                            double x1 = (double)X1 / short.MaxValue * x0;
                            double y1 = (double)Y1 / short.MaxValue * x0;
                            dc.DrawLine(black1p5, new Point(x0 + 10 + x1 + pc, x0 + 60 - y1 + pc), new Point(x0 + 10 + x1 + pc, x0 + 60 - y1 + pc));

                            Mx += X1;
                            My += Y1;
                            Xsq += (long)X1 * X1;
                            Ysq += (long)Y1 * Y1;
                        }
                    }

                    // Матожидание и дисперсия
                    Mx /= pointNumber;
                    My /= pointNumber;
                    Xsq /= pointNumber;
                    Ysq /= pointNumber;

                    long Dx = Xsq - (long)Mx * Mx;
                    long Dy = Ysq - (long)My * My;

                    double mx, my;
                    if (dwordSize)
                    {
                        dc.DrawText(new FormattedText(String.Format("M = {0:0.000;–0.000;0.000}{1: + 0.000; – 0.000; + 0.000}", (double)Mx / DwordMaxVal, (double)My / DwordMaxVal), System.Globalization.CultureInfo.CurrentCulture, FlowDirection.LeftToRight, tfSegoe, 12, Brushes.Black), new Point(x0 + 40 + pc, 25 + pc));
                        dc.DrawText(new FormattedText(String.Format("D = {0:0.000000}", (double)Dx / DwordMaxVal / DwordMaxVal + (double)Dy / DwordMaxVal / short.MaxValue), System.Globalization.CultureInfo.CurrentCulture, FlowDirection.LeftToRight, tfSegoe, 12, Brushes.Black), new Point(x0 + 40 + pc, 40 + pc));

                        mx = (double)Mx / DwordMaxVal * x0;
                        my = (double)My / DwordMaxVal * x0;
                    }
                    else
                    {
                        dc.DrawText(new FormattedText(String.Format("M = {0:0.000;–0.000;0.000}{1: + 0.000; – 0.000; + 0.000}", (double)Mx / short.MaxValue, (double)My / short.MaxValue), System.Globalization.CultureInfo.CurrentCulture, FlowDirection.LeftToRight, tfSegoe, 12, Brushes.Black), new Point(x0 + 40 + pc, 25 + pc));
                        dc.DrawText(new FormattedText(String.Format("D = {0:0.000000}", (double)Dx / short.MaxValue / short.MaxValue + (double)Dy / short.MaxValue / short.MaxValue), System.Globalization.CultureInfo.CurrentCulture, FlowDirection.LeftToRight, tfSegoe, 12, Brushes.Black), new Point(x0 + 40 + pc, 40 + pc));

                        mx = (double)Mx / short.MaxValue * x0;
                        my = (double)My / short.MaxValue * x0;
                    }

                    // Рисуем вектор матожидания
                    dc.DrawLine(black2, new Point(x0 + 10 + pc, x0 + 60 + pc), new Point(x0 + 10 + mx + pc, x0 + 60 - my + pc));

                    // Стрелка на векторе
                    if ((dwordSize && (Math.Abs(Mx) > (4096 * (DwordMaxVal == int.MaxValue ? (1 << 16) : (1 << 8)))) && (Math.Abs(My) > (4096 * (DwordMaxVal == int.MaxValue ? (1 << 16) : (1 << 8))))) || (!dwordSize && (Math.Abs(Mx) > 4096) && (Math.Abs(My) > 4096)))
                    {
                        double alpha = Math.Atan2(Mx, My) - 26.57 * Math.PI / 180;
                        //double beta = Math.Atan2(Y1, X1) - 26.57 * Math.PI / 180;
                        double beta = Math.PI / 2 - alpha - 26.57 * Math.PI / 180 - 26.57 * Math.PI / 180;
                        double posx1 = Math.Sin(alpha) * 11.18;
                        double posy1 = Math.Cos(alpha) * 11.18;
                        double posx2 = Math.Cos(beta) * 11.18;
                        double posy2 = Math.Sin(beta) * 11.18;
                        dc.DrawLine(black2, new Point(x0 + 10 + mx + pc, x0 + 60 - my + pc), new Point(x0 + 10 + mx - posx1 + pc, x0 + 60 - my + posy1 + pc));
                        dc.DrawLine(black2, new Point(x0 + 10 + mx + pc, x0 + 60 - my + pc), new Point(x0 + 10 + mx - posx2 + pc, x0 + 60 - my + posy2 + pc));
                    }
                }
            }
        }

        /// <summary>
        /// Линейный график (УАП, мощность, ранги)
        /// </summary>
        public class LineGraph : GraphUnit
        {
            private Pen black1 = new Pen(Brushes.Black, 1) { LineJoin = PenLineJoin.Round, StartLineCap = PenLineCap.Round, EndLineCap = PenLineCap.Round };
            private Pen dashedGrey1 = new Pen(Brushes.LightGray, 1) { LineJoin = PenLineJoin.Round, StartLineCap = PenLineCap.Round, EndLineCap = PenLineCap.Round, DashStyle = DashStyles.Dash};
            private Pen[] pens = new Pen[6] {
                new Pen(Brushes.Red, 2) { LineJoin = PenLineJoin.Round, StartLineCap = PenLineCap.Round, EndLineCap = PenLineCap.Round },
                new Pen(Brushes.Blue, 2) { LineJoin = PenLineJoin.Round, StartLineCap = PenLineCap.Round, EndLineCap = PenLineCap.Round },
                new Pen(Brushes.Violet, 2) { LineJoin = PenLineJoin.Round, StartLineCap = PenLineCap.Round, EndLineCap = PenLineCap.Round },
                new Pen(Brushes.Indigo, 2) { LineJoin = PenLineJoin.Round, StartLineCap = PenLineCap.Round, EndLineCap = PenLineCap.Round },
                new Pen(Brushes.Green, 2) { LineJoin = PenLineJoin.Round, StartLineCap = PenLineCap.Round, EndLineCap = PenLineCap.Round },
                new Pen(Brushes.Gray, 2) { LineJoin = PenLineJoin.Round, StartLineCap = PenLineCap.Round, EndLineCap = PenLineCap.Round }
            };
            private Typeface tfSegoe = new Typeface(new FontFamily("Segoe UI"), FontStyles.Normal, FontWeights.Normal, FontStretches.Condensed);
            private Typeface tfSegoeBold = new Typeface(new FontFamily("Segoe UI"), FontStyles.Normal, FontWeights.Bold, FontStretches.Condensed);

            /// <summary>
            /// Флаги разрешения вывода отдельных графиков
            /// </summary>
            private CheckBox[] graphEnabledCheckBox;

            /// <summary>
            /// Тип графика (УАП/мощность/ранги)
            /// </summary>
            private LineGraphType graphType;

            /// <summary>
            /// Число графиков, базовый адрес массива данных
            /// </summary>
            private int Ngraphs, baseAdr;

            /// <summary>
            /// Заголовок графика, название графиков в легенде
            /// </summary>
            private string graphLabel, legendLabel;

            /// <summary>
            /// Начальный номер нумерации графиков
            /// </summary>
            private int initialLabelNumber;

            /// <summary>
            /// Конструктор
            /// </summary>
            /// <param name="graphType">Тип графика (УАП/мощность/ранги)</param>
            public LineGraph(LineGraphType graphType) : base()
            {
                this.graphType = graphType;
                switch (graphType)
                {
                    case LineGraphType.grUAP:
                        graphLabel = "УАП";
                        legendLabel = "УАП";
                        initialLabelNumber = 1;
                        Ngraphs = 4;
                        baseAdr = (int)GraphConst.SL_UAP1_HEAD;
                        break;

                    case LineGraphType.grPower:
                        graphLabel = "Мощность";
                        legendLabel = "P";
                        initialLabelNumber = 0;
                        Ngraphs = 5;
                        baseAdr = (int)GraphConst.SL_PO_HEAD;
                        break;

                    case LineGraphType.grRank:
                        graphLabel = "Ранги";
                        legendLabel = "Ранг";
                        initialLabelNumber = 1;
                        Ngraphs = 6;
                        baseAdr = (int)GraphConst.SL_R1_HEAD;
                        break;

                    default:
                        throw new Exception("Неизвестное значение параметра graphType");
                }

                // Создаем на графике флаги разрешения вывода отдельных кривых
                RoutedEventHandler evh = (sender, e) => { InvalidateVisual(); };
                graphEnabledCheckBox = new CheckBox[Ngraphs];
                for (int i = 0; i < Ngraphs; ++i)
                {
                    graphEnabledCheckBox[i] = new CheckBox() { IsChecked = true, Margin = new Thickness(0, 38 + i * 15, 62, 0), VerticalAlignment = VerticalAlignment.Top, HorizontalAlignment = HorizontalAlignment.Right };
                    graphEnabledCheckBox[i].Click += evh;
                }
            }

            /// <summary>
            /// Инициализация дополнительных элементов на графике
            /// </summary>
            /// <param name="grid">Сетка для размещения элементов</param>
            /// <param name="row">Номер ряда в сетке</param>
            public override void init(Grid grid, int row)
            {
                for (int i = 0; i < Ngraphs; ++i)
                {
                    grid.Children.Add(graphEnabledCheckBox[i]);
                    Grid.SetRow(graphEnabledCheckBox[i], row);
                    Grid.SetColumnSpan(graphEnabledCheckBox[i], 2);
                }
            }

            /// <summary>
            /// Прорисовка графика
            /// </summary>
            /// <param name="dc">"Контекст рисования"</param>
            protected override void OnRender(DrawingContext dc)
            {
                base.OnRender(dc);

                // Определяем единичные отрезки по осям
                double x0, y0,
                    width = ActualWidth,
                    heigth = ActualHeight;

                x0 = width - 70 - 180 + pc;
                y0 = heigth - 90 + pc;

                // Выравниваем по пикселям величины единичных отрезков
                align(ref x0);
                align(ref y0);

                // Рисуем оси
                dc.DrawLine(black1, new Point(37 + pc, y0 + 60 + pc), new Point(x0 + 70 + pc, y0 + 60 + pc));
                dc.DrawLine(black1, new Point(40 + pc, 30 + pc), new Point(40 + pc, y0 + 63 + pc));

                // Единичные отрезки по оси Y
                for (int i = 0; i < 6; ++i)
                {
                    double y1 = y0 * i / 5 + pc;
                    align(ref y1);
                    dc.DrawText(new FormattedText(String.Format("{0:0.0}", 1 - 0.2 * i), System.Globalization.CultureInfo.CurrentCulture, FlowDirection.LeftToRight, tfSegoe, 14, Brushes.Black), new Point(10 + pc, y1 + 50 + pc));
                    if(i != 5)
                    {
                        dc.DrawLine(black1, new Point(37 + pc, y1 + 60 + pc), new Point(43 + pc, y1 + 60 + pc));
                        dc.DrawLine(dashedGrey1, new Point(40 + pc, y1 + 60 + pc), new Point(x0 + 50 + pc, y1 + 60 + pc));
                    }
                }


                // Единичные отрезки по оси X
                int[] levelX = new int[]{ 1, 2, 5, 10, 15, 20, 25, 50, 75, 100 };
                int baseX = 0, maxPoints = (int)(x0 / 50);
                for (int i = 0; i < levelX.Length; ++i )
                {
                    if((pointNumber - 1) / levelX[i] <= maxPoints)
                    {
                        baseX = levelX[i];
                        maxPoints = (pointNumber - 1) / baseX;

                        if((x0 - x0 * maxPoints * baseX / (pointNumber - 1)) < 30) { --maxPoints; }

                        break;
                    }
                }
                for (int i = 0; i <= maxPoints; ++i)
                {
                    double x1 = x0 * i * baseX / (pointNumber - 1);
                    align(ref x1);
                    dc.DrawText(new FormattedText((i * baseX).ToString(), System.Globalization.CultureInfo.CurrentCulture, FlowDirection.LeftToRight, tfSegoe, 14, Brushes.Black), new Point(x1 + 35 + pc, y0 + 65 + pc));
                    if (i != 0)
                    {
                        dc.DrawLine(black1, new Point(x1 + 40 + pc, y0 + 57 + pc), new Point(x1 + 40 + pc, y0 + 63 + pc));
                    }
                }

                // Последний отрезок по оси X
                dc.DrawLine(black1, new Point(x0 + 40 + pc, y0 + 57 + pc), new Point(x0 + 40 + pc, y0 + 63 + pc));
                dc.DrawText(new FormattedText((pointNumber - 1).ToString(), System.Globalization.CultureInfo.CurrentCulture, FlowDirection.LeftToRight, tfSegoe, 14, Brushes.Black), new Point(x0 + 35 + pc, y0 + 65 + pc));
                
                // стрелки
                dc.DrawLine(black1, new Point(x0 + 70 + pc, y0 + 60 + pc), new Point(x0 + 60 + pc, y0 + 55 + pc));
                dc.DrawLine(black1, new Point(x0 + 70 + pc, y0 + 60 + pc), new Point(x0 + 60 + pc, y0 + 65 + pc));
                dc.DrawLine(black1, new Point(40 + pc, 30 + pc), new Point(35 + pc, 40 + pc));
                dc.DrawLine(black1, new Point(40 + pc, 30 + pc), new Point(45 + pc, 40 + pc));

                // Название графика
                dc.DrawText(new FormattedText(graphLabel, System.Globalization.CultureInfo.CurrentCulture, FlowDirection.LeftToRight, tfSegoeBold, 16, Brushes.Black), new Point(60 + pc, 5 + pc));

                
                // Подпись оси X
                dc.DrawText(new FormattedText("№ изм.", System.Globalization.CultureInfo.CurrentCulture, FlowDirection.LeftToRight, tfSegoe, 14, Brushes.Black), new Point(x0 + 70 + pc, y0 + 65 + pc));

                // Легенда
                dc.DrawRectangle(null, black1, new Rect(new Point(x0 + 80 + pc, 30 + pc), new Point(x0 + 80 + 120 + pc, 45 + 15 * Ngraphs + pc)));
                for (int i = 0; i < Ngraphs; ++i)
                {
                    dc.DrawLine(pens[i], new Point(x0 + 80 + 10 + pc, 45 + i * 15), new Point(x0 + 80 + 40 + pc, 45 + i * 15));
                    dc.DrawText(new FormattedText(legendLabel + (initialLabelNumber + i).ToString(), System.Globalization.CultureInfo.CurrentCulture, FlowDirection.LeftToRight, tfSegoe, 12, Brushes.Black), new Point(x0 + 80 + 50 + pc, 35 + i * 15 + pc));
                }
                
                // Линии, матожидание, дисперсия
                if ((graphData != null) && (graphData.data != null) && (graphData.length > 0) && (pointNumber > 0))
                {
                    int Mcounter = 0;
                    for(int nGraph = 0; nGraph < Ngraphs; ++nGraph)
                    {
                        // Не выводим кривую, если сброшен соответствующий флаг
                        if (!(graphEnabledCheckBox[nGraph].IsChecked ?? false)) continue;

                        // Начальный индекс массива данных
                        int dataIndex = graphData.data[(int)GraphConst.SL_LS21_INDEX];

                        // Рисуем линию
                        Point p0 = new Point(), p1;
                        double Mx = 0, Xsq = 0;
                        for(int i = 0; i < pointNumber; ++i)
                        {
                            int index = ((dataIndex + i) & 0xFF) + baseAdr + 256 * nGraph;

                            double val = (double)graphData.data[index] / byte.MaxValue;
                            double valX = x0 * i / (pointNumber - 1) + 40 + pc;
                            Mx += val;
                            Xsq += val * val;

                            align(ref valX);
                            double valY = y0 - y0 * val + 60 + pc;
                            align(ref valY);

                            if(i == 0)
                            {
                                p0 = new Point(valX + pc, valY + pc);
                            }
                            else
                            {
                                p1 = new Point(valX + pc, valY + pc);
                                dc.DrawLine(pens[nGraph], p0, p1);
                                p0 = p1;
                            }
                        }

                        // Матожидание и дисперсия
                        Mx /= pointNumber;
                        Xsq /= pointNumber;

                        dc.DrawText(new FormattedText(String.Format("M{0} = {1:0.000}", initialLabelNumber + nGraph, Mx), System.Globalization.CultureInfo.CurrentCulture, FlowDirection.LeftToRight, tfSegoe, 12, Brushes.Black), new Point(x0 + 80 + pc, 160 + 15 * Mcounter + pc));
                        dc.DrawText(new FormattedText(String.Format("D{0} = {1:0.000000}", initialLabelNumber + nGraph, Xsq - Mx * Mx), System.Globalization.CultureInfo.CurrentCulture, FlowDirection.LeftToRight, tfSegoe, 12, Brushes.Black), new Point(x0 + 155 + pc, 160 + 15 * Mcounter + pc));
                        ++Mcounter;
                    }

                }
            }
        }
    }

    /// <summary>
    /// Interaction logic for GraphWindow_test2.xaml
    /// </summary>
    public partial class GraphWindow_test2 : Window
    {
        /// <summary>
        /// Действие, которое необходимо совершить по закрытию окна
        /// </summary>
        private EventHandler closingAction;

        /// <summary>
        /// Данные для построения графиков
        /// </summary>
        private GraphData graphData;

        /// <summary>
        /// Режим работы
        /// </summary>
        private int mode;

        /// <summary>
        /// Графики пилот-сигналов
        /// </summary>
        private PilotSignalGraph pilotSignalGraph1, pilotSignalGraph2, pilotSignalGraph3, pilotSignalGraph4, pilotSignalGraphFC;

        /// <summary>
        /// Графики УАП, мощностей, рангов
        /// </summary>
        private LineGraph uapGraph, powerGraph, rankGraph;

        /// <summary>
        /// Список активных (отображаемых) графиков
        /// </summary>
        private LinkedList<GraphUnit> activeGraphs;

        /// <summary>
        /// Файл для сохранения данных
        /// </summary>
        private string filename = null;

        /// <summary>
        /// Признак сохранения данных
        /// </summary>
        private bool saveDataFlg = false;

        /// <summary>
        /// Конструктор
        /// </summary>
        /// <param name="closingAction">Действие, которое необходимо совершить по закрытию окна</param>
        public GraphWindow_test2(EventHandler closingAction)
        {
            UseLayoutRounding = true;
            InitializeComponent();

            mode = -1;
            graphData = null;
            activeGraphs = new LinkedList<GraphUnit>();
            this.closingAction = closingAction;
            Closed += closingAction;

            // Получаем коррекцию пикселя
            var flags = BindingFlags.NonPublic | BindingFlags.Static;
            var dpiProperty = typeof(SystemParameters).GetProperty("Dpi", flags);
            int Dpi = (int)dpiProperty.GetValue(null, null);
            double pixelSize = 96.0 / Dpi;
            double pixelCorrection = pixelSize / 2;

            // Устанавливаем параметры пикселей
            GraphUnit.setPixelParameters(pixelSize, pixelCorrection);

            // Создаем графики
            pilotSignalGraph1 = new PilotSignalGraph(0, false);
            pilotSignalGraph2 = new PilotSignalGraph(1, false);
            pilotSignalGraph3 = new PilotSignalGraph(2, false);
            pilotSignalGraph4 = new PilotSignalGraph(3, false);
            pilotSignalGraphFC = new PilotSignalGraph(0, true);
            uapGraph = new LineGraph(LineGraphType.grUAP);
            powerGraph = new LineGraph(LineGraphType.grPower);
            rankGraph = new LineGraph(LineGraphType.grRank);

            // Кнопка изменения количества точек на графиках
            udPointNumber.ValueChanged += (sender, e) =>
            {
                GraphUnit.pointNumber = udPointNumber.Value ?? 0;
                foreach (GraphUnit graph in activeGraphs)
                    graph.InvalidateVisual();
            };

            udPointNumber.LostFocus += (sender, e) =>
            {
                if (udPointNumber.Value == null) udPointNumber.Value = udPointNumber.Minimum;
            };

            bnSave.Click += (sender, e) =>
            {
                //if(filename == null)
                //{
                    Microsoft.Win32.SaveFileDialog dlg = new Microsoft.Win32.SaveFileDialog();
                    dlg.FileName = "progData"; // Default file name
                    dlg.DefaultExt = ".txt"; // Default file extension
                    dlg.Filter = "Text documents (.txt)|*.txt"; // Filter files by extension

                    // Show save file dialog box
                    bool? result = dlg.ShowDialog();

                    // Process save file dialog box results
                    if (result == true)
                    {
                        // Save document
                        filename = dlg.FileName;
                        saveDataFlg = true;
                    }
                //}
                //else
                //{
                //    saveDataFlg = true;
                //}
            };

            /*
            // Тестирование
            GraphData testData = new GraphData();
            testData.length = (int)GraphConst.SL_DATALIST_SIZE;
            testData.mode = 8;
            testData.N621control = 0;
            testData.pelNapr = 0;

            short data21index = 128 & 0xFFFC;
            short data31index = 64 & 0xFFFC;
            byte uap21index = 20;
            byte rank21index = 32;

            testData.progData[0] = (byte)data21index;
            testData.progData[1] = (byte)(data21index >> 8);
            testData.progData[2] = (byte)data31index;
            testData.progData[3] = (byte)(data31index >> 8);
            testData.progData[4] = uap21index;
            testData.progData[5] = rank21index;

            data21index = (short)(testData.progData[0] | (testData.progData[1] << 8));
            data31index = (short)(testData.progData[2] | (testData.progData[3] << 8));
            uap21index = testData.progData[4];
            rank21index = testData.progData[5];

            Random rand = new Random();

            short   XO = 32400,
                    YO = 22200,
                    X1 = -32400,
                    Y1 = 22200,
                    X2 = -32400,
                    Y2 = -22200,
                    X3 = 32400,
                    Y3 = -22200;
            for(int addr = 0; addr < 1024; addr += 4)
            {
                XO = (short)rand.Next(10000, short.MaxValue);
                YO = (short)rand.Next(15000, 30000);
                int index = ((data21index + addr) & 0x3FF) + (int)GraphConst.SL_PSO_HEAD;
                testData.progData[index] = (byte)XO;
                testData.progData[index + 1] = (byte)(XO >> 8);
                testData.progData[index + 2] = (byte)YO;
                testData.progData[index + 3] = (byte)(YO >> 8);

                X1 = (short)rand.Next(short.MinValue, -10000);
                Y1 = (short)rand.Next(15000, 30000);
                index = ((data31index + addr) & 0x3FF) + (int)GraphConst.SL_PS1_HEAD;
                testData.progData[index] = (byte)X1;
                testData.progData[index + 1] = (byte)(X1 >> 8);
                testData.progData[index + 2] = (byte)Y1;
                testData.progData[index + 3] = (byte)(Y1 >> 8);

                X2 = (short)rand.Next(short.MinValue , -10000);
                Y2 = (short)rand.Next(-30000, -15000);
                index = ((data31index + addr) & 0x3FF) + (int)GraphConst.SL_PS2_HEAD;
                testData.progData[index] = (byte)X2;
                testData.progData[index + 1] = (byte)(X2 >> 8);
                testData.progData[index + 2] = (byte)Y2;
                testData.progData[index + 3] = (byte)(Y2 >> 8);

                X3 = (short)rand.Next(10000, short.MaxValue);
                Y3 = (short)rand.Next(-30000, -15000);
                index = ((data31index + addr) & 0x3FF) + (int)GraphConst.SL_PS3_HEAD;
                testData.progData[index] = (byte)X3;
                testData.progData[index + 1] = (byte)(X3 >> 8);
                testData.progData[index + 2] = (byte)Y3;
                testData.progData[index + 3] = (byte)(Y3 >> 8);
            }

            byte    UAP1 = 10,
                    UAP2 = 50,
                    UAP3 = 150,
                    UAP4 = 200,
                    PO = 40,
                    P1 = 80,
                    P2 = 120,
                    P3 = 160,
                    P4 = 200,
                    R1 = 10,
                    R2 = 50,
                    R3 = 80,
                    R4 = 140,
                    R5 = 190,
                    R6 = 240;
            for(int addr = 0; addr < 256; ++addr)
            {
                int index = ((uap21index + addr) & 0xFF) + (int)GraphConst.SL_UAP1_HEAD;
                testData.progData[index] = (byte)rand.Next(UAP1 - 10, UAP1 + 10);
                index = ((uap21index + addr) & 0xFF) + (int)GraphConst.SL_UAP2_HEAD;
                testData.progData[index] = (byte)rand.Next(UAP2 - 10, UAP2 + 10);
                index = ((uap21index + addr) & 0xFF) + (int)GraphConst.SL_UAP3_HEAD;
                testData.progData[index] = (byte)rand.Next(UAP3 - 10, UAP3 + 10);
                index = ((uap21index + addr) & 0xFF) + (int)GraphConst.SL_UAP4_HEAD;
                testData.progData[index] = (byte)rand.Next(UAP4 - 10, UAP4 + 10);

                index = ((rank21index + addr) & 0xFF) + (int)GraphConst.SL_PO_HEAD;
                testData.progData[index] = (byte)rand.Next(PO - 10, PO + 10);
                index = ((rank21index + addr) & 0xFF) + (int)GraphConst.SL_P1_HEAD;
                testData.progData[index] = (byte)rand.Next(P1 - 10, P1 + 10);
                index = ((rank21index + addr) & 0xFF) + (int)GraphConst.SL_P2_HEAD;
                testData.progData[index] = (byte)rand.Next(P2 - 10, P2 + 10);
                index = ((rank21index + addr) & 0xFF) + (int)GraphConst.SL_P3_HEAD;
                testData.progData[index] = (byte)rand.Next(P3 - 10, P3 + 10);
                index = ((rank21index + addr) & 0xFF) + (int)GraphConst.SL_P4_HEAD;
                testData.progData[index] = (byte)rand.Next(P4 - 10, P4 + 10);

                index = ((rank21index + addr) & 0xFF) + (int)GraphConst.SL_R1_HEAD;
                testData.progData[index] = (byte)rand.Next(R1 - 10, R1 + 10);
                index = ((rank21index + addr) & 0xFF) + (int)GraphConst.SL_R2_HEAD;
                testData.progData[index] = (byte)rand.Next(R2 - 10, R2 + 10);
                index = ((rank21index + addr) & 0xFF) + (int)GraphConst.SL_R3_HEAD;
                testData.progData[index] = (byte)rand.Next(R3 - 10, R3 + 10);
                index = ((rank21index + addr) & 0xFF) + (int)GraphConst.SL_R4_HEAD;
                testData.progData[index] = (byte)rand.Next(R4 - 10, R4 + 10);
                index = ((rank21index + addr) & 0xFF) + (int)GraphConst.SL_R5_HEAD;
                testData.progData[index] = (byte)rand.Next(R5 - 10, R5 + 10);
                index = ((rank21index + addr) & 0xFF) + (int)GraphConst.SL_R6_HEAD;
                testData.progData[index] = (byte)rand.Next(R6 - 10, R6 + 10);
            }

            renewData(testData);*/
        }

        /// <summary>
        /// Отобразить окно
        /// </summary>
        public new void Show()
        {
            base.Show();

            // Пытаемся разместить окно с графиками справа или слева от главного окна
            if ((SystemParameters.PrimaryScreenWidth - Owner.Left - Owner.ActualWidth) >= ActualWidth)
            {
                Left = Owner.Left + Owner.ActualWidth;
                Top = Owner.Top;
            }
            else if(Owner.Left >= ActualWidth)
            {
                Left = Owner.Left - ActualWidth;
                Top = Owner.Top;
            }
        }

        /// <summary>
        /// Обновить данные для построения графиков
        /// </summary>
        /// <param name="graphData">Данные для построения графиков</param>
        public void renewData(GraphData graphData)
        {
            this.graphData = graphData;
            GraphUnit.graphData = graphData;

            // Если изменился режим, инициализируем графики заново
            if((graphData != null) && (graphData.mode != mode))
            {
                mode = graphData.mode;
                mainGrid.Children.Clear();
                activeGraphs.Clear();

                switch(mode)
                {
                    // БР
                    case 0:
                        mainGrid.Children.Add(pilotSignalGraph1);
                        Grid.SetRow(pilotSignalGraph1, 0);
                        Grid.SetColumn(pilotSignalGraph1, 0);
                        activeGraphs.AddLast(pilotSignalGraph1);

                        mainGrid.Children.Add(pilotSignalGraph2);
                        Grid.SetRow(pilotSignalGraph2, 0);
                        Grid.SetColumn(pilotSignalGraph2, 1);
                        activeGraphs.AddLast(pilotSignalGraph2);

                        mainGrid.Children.Add(pilotSignalGraph3);
                        Grid.SetRow(pilotSignalGraph3, 1);
                        Grid.SetColumn(pilotSignalGraph3, 0);
                        activeGraphs.AddLast(pilotSignalGraph3);

                        mainGrid.Children.Add(pilotSignalGraph4);
                        Grid.SetRow(pilotSignalGraph4, 1);
                        Grid.SetColumn(pilotSignalGraph4, 1);
                        activeGraphs.AddLast(pilotSignalGraph4);

                        bnSave.Visibility = Visibility.Hidden;

                        break;

                    // ФК
                    case 1:
                        mainGrid.Children.Add(pilotSignalGraphFC);
                        Grid.SetRow(pilotSignalGraphFC, 0);
                        Grid.SetColumn(pilotSignalGraphFC, 0);
                        Grid.SetRowSpan(pilotSignalGraphFC, 2);
                        Grid.SetColumnSpan(pilotSignalGraphFC, 2);
                        activeGraphs.AddLast(pilotSignalGraphFC);

                        bnSave.Visibility = Visibility.Visible;

                        break;

                    // ТЕХН УАП
                    case 7:     
                        tbStatus.Inlines.Add("Контроль Н6.21\t= ");
                        tbStatus.Inlines.Add(new Run(Convert.ToString(graphData.N621control & 0x7F, 2).PadLeft(7, '0') + 'b') { FontFamily = new FontFamily("Courier New"), FontWeight = FontWeights.Bold });
                        mainGrid.Children.Add(uapGraph);
                        Grid.SetRow(uapGraph, 0);
                        Grid.SetColumnSpan(uapGraph, 2);
                        Grid.SetRowSpan(uapGraph, 2);
                        activeGraphs.AddLast(uapGraph);
                        uapGraph.init(mainGrid, 0);

                        bnSave.Visibility = Visibility.Hidden;

                        break;

                    // ТЕХН ранги
                    case 8:     
                        tbStatus.Inlines.Add("Контроль Н6.21\t= ");
                        tbStatus.Inlines.Add(new Run(Convert.ToString(graphData.N621control & 0x7F, 2).PadLeft(7, '0') + 'b') { FontFamily = new FontFamily("Courier New"), FontWeight = FontWeights.Bold });
                        mainGrid.Children.Add(powerGraph);
                        Grid.SetRow(powerGraph, 0);
                        Grid.SetColumnSpan(powerGraph, 2);
                        activeGraphs.AddLast(powerGraph);
                        powerGraph.init(mainGrid, 0);

                        mainGrid.Children.Add(rankGraph);
                        Grid.SetRow(rankGraph, 1);
                        Grid.SetColumnSpan(rankGraph, 2);
                        activeGraphs.AddLast(rankGraph);
                        rankGraph.init(mainGrid, 1);

                        bnSave.Visibility = Visibility.Hidden;

                        break;

                    default:
                        Label lb = new Label() { Content = "Нет графиков для выбранного режима", Margin = new Thickness(10, 10, 0, 0), FontSize = 18/*FontSize = new FontFamily("Segoe UI")*/ };
                        mainGrid.Children.Add(lb);
                        Grid.SetRow(lb, 0);
                        Grid.SetColumnSpan(lb, 2);

                        bnSave.Visibility = Visibility.Hidden;

                        break;
                }
            }
            else if (saveDataFlg && (mode == 1))
            {
                bool err = false;
                StreamWriter sw = null;
                try
                {
                    sw = new StreamWriter(filename);
                }
                catch(Exception ex)
                {
                    System.Windows.MessageBox.Show(this, ex.Message, "Ошибка открытия файла", MessageBoxButton.OK, MessageBoxImage.Error);
                    err = true;
                }

                if(!err)
                {
                    int pointNumber = udPointNumber.Value ?? 0;
                    int baseAdr1 = (int)GraphConst.SL_PSX_HEAD;
                    int baseAdr2 = (int)GraphConst.SL_PSY_HEAD;
                    int dataIndex1 = graphData.data[(int)GraphConst.SL_LS21_INDEX];

                    try
                    {
                        for (int i = 0; i < (pointNumber << 2); i += 4)
                        {
                            int index1 = (((dataIndex1 + i) & 0xFF) << 2) + baseAdr1;
                            int index2 = (((dataIndex1 + i) & 0xFF) << 2) + baseAdr2;
                            int X1 = (int)(graphData.data[index1] | (graphData.data[index1 + 1] << 8) | (graphData.data[index1 + 2] << 16) | (graphData.data[index1 + 3] << 24));
                            int Y1 = (int)(graphData.data[index2] | (graphData.data[index2 + 1] << 8) | (graphData.data[index2 + 2] << 16) | (graphData.data[index2 + 3] << 24));

                            sw.WriteLine(String.Format("{0:X8} {1:X8}", X1, Y1) + String.Format("{0}", X1).PadLeft(10, ' ') + String.Format("{0}", Y1).PadLeft(10, ' '));

                            saveDataFlg = false;
                        }
                    }
                    catch(Exception ex)
                    {
                        System.Windows.MessageBox.Show(this, ex.Message, "Ошибка ввода-вывода", MessageBoxButton.OK, MessageBoxImage.Error);
                        err = true;
                    }
                    finally
                    {
                        sw.Close();
                    }
                }
            }

            // Выводим слово состояния Н6.21.10 и пел. напр
            tbStatus.Text = "";
            if((mode == 0) || (mode == 1) || (mode == 7) || (mode == 8))
            {
                tbStatus.Inlines.Add("Контроль Н6.21\t= ");
                tbStatus.Inlines.Add(new Run(Convert.ToString(graphData.N621control & 0x7F, 2).PadLeft(7, '0') + 'b') { FontFamily = new FontFamily("Courier New"), FontWeight = FontWeights.Bold });
                if (mode == 0)
                {
                    tbStatus.Inlines.Add(Environment.NewLine + "Пел напр\t= ");
                    tbStatus.Inlines.Add(new Run(Convert.ToString(graphData.pelNapr & 0x3F, 2).PadLeft(6, '0') + 'b') { FontFamily = new FontFamily("Courier New"), FontWeight = FontWeights.Bold });
                }
                else if(mode == 1)
                {
                    pilotSignalGraphFC.channelNumber = graphData.NChMsrFC;
                }
            }

            foreach (GraphUnit graph in activeGraphs)
                graph.InvalidateVisual();
        }
    }
}
