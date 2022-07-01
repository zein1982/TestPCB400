using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;

namespace Test20311M
{
    class ReProgTask : IReadDisplay
    {
        private static ReProgTask instance = null;
        private MainWindow wnd;
        private PortModel portModel;
        private Task task = null;
        private byte devId;
        private bool extControlMode;
        private CancellationTokenSource tokenSrc = null;
        List<byte[]> progData;
        volatile private byte[] data;
        volatile private int count;

        private ReProgTask() {}

        public static ReProgTask GetInstance()
        {
            if (instance == null) { instance = new ReProgTask(); }
            return instance;
        }

        public void Run(MainWindow wnd, PortModel portModel, List<byte[]> progData, byte devId, bool extControlMode)
        {
            this.wnd = wnd;
            this.portModel = portModel;
            this.progData = progData;
            this.devId = devId;
            this.extControlMode = extControlMode;

            tokenSrc = new CancellationTokenSource();
            task = Task.Factory.StartNew(TaskFunc, tokenSrc.Token, tokenSrc.Token);
        }

        private void TaskFunc(object ct)
        {
            CancellationToken cancelTok = (CancellationToken)ct;

            try
            {
                portModel.StopCyclicWrite();

                byte[] BufWr;

                uint devCode = 0, pos, checksum;
                if(devId == 22) { devCode = 3; }
                else throw new Exception("Неизвестный идентификатор устройства");

                ProgressWindow.ShowProgressWnd(wnd, "Перепрогр. МК", "Запуск...", null, () => { if (tokenSrc != null) { tokenSrc.Cancel(); } });

                //------------------------------| ЗАПУСК ПРОЦЕДУРЫ ИМИТ Л60 |------------------------------//

                if(extControlMode)
                {
                    ProgressWindow.SetStatus("Запуск Л60...");

                    extControlMode = false;

                    BufWr = new byte[48];
                    BufWr[0] = 0x21;
                    BufWr[1] = 0;
                    BufWr[2] = 0x10;
                    BufWr[3] = 0;
                    BufWr[4] = 0;
                    BufWr[5] = 0;
                    BufWr[6] = 0;
                    BufWr[7] = 0;
                    BufWr[8] = 0;
                    BufWr[9] = 0;
                    BufWr[10] = 0;
                    BufWr[11] = 0x18;
                    BufWr[12] = 0;
                    BufWr[13] = 0;
                    BufWr[14] = 0;
                    BufWr[15] = Test.CalcCheckSum(BufWr, 2, 13);

                    BufWr[16] = 0x22;
                    BufWr[17] = 0;
                    BufWr[18] = 0x10;
                    BufWr[19] = 0;
                    BufWr[20] = 0;
                    BufWr[21] = 0;
                    BufWr[22] = 0;
                    BufWr[23] = 0;
                    BufWr[24] = 0;
                    BufWr[25] = 0;
                    BufWr[26] = 0;
                    BufWr[27] = 0;
                    BufWr[28] = 0;
                    BufWr[29] = 0;
                    BufWr[30] = 0;
                    BufWr[31] = Test.CalcCheckSum(BufWr, 18, 13);

                    BufWr[32] = (byte)(0x30 | devCode);
                    BufWr[33] = 0;
                    BufWr[34] = 0xEF;
                    BufWr[35] = 0;
                    BufWr[36] = 0;
                    BufWr[37] = 0;
                    BufWr[38] = 0;
                    BufWr[39] = 0;
                    BufWr[40] = 0;
                    BufWr[41] = 0;
                    BufWr[42] = 0;
                    BufWr[43] = 0;
                    BufWr[44] = 0;
                    BufWr[45] = 0;
                    BufWr[46] = 0;
                    BufWr[47] = Test.CalcCheckSum(BufWr, 34, 13);

                    portModel.WriteAndRead(BufWr, 50, this, 50, false);

                    if (cancelTok.IsCancellationRequested) return;
                    if ((count == 0) || (data == null) || (data.Length == 0))
                    {
                        throw new Exception("Нет ответа от СЭВМ");
                    }
                    else if (count != 3)
                    {
                        throw new Exception("Ошибка обмена с СЭВМ.\nПринято " + count + " байт(а) (должно быть 3)");
                    }
                    else if (((data[0] & 0xF0) != 0) || ((data[1] & 0xF0) != 0) || ((data[2] & 0xF0) != 0))
                    {
                        throw new Exception("Ошибка обмена с СЭВМ.\nПолучены коды ошибок: " + ((data[0] >> 4) & 0xF).ToString() + ", " + ((data[1] >> 4) & 0xF).ToString() + ", " + ((data[2] >> 4) & 0xF).ToString());
                    }

                    BufWr[0] = 0x21;
                    BufWr[1] = 0;
                    BufWr[2] = 0xEF;
                    BufWr[3] = 0;
                    BufWr[4] = 0;
                    BufWr[5] = 0;
                    BufWr[6] = 0;
                    BufWr[7] = 0;
                    BufWr[8] = 0;
                    BufWr[9] = 0;
                    BufWr[10] = 0;
                    BufWr[11] = 0;
                    BufWr[12] = 0;
                    BufWr[13] = 0;
                    BufWr[14] = 0;
                    BufWr[15] = Test.CalcCheckSum(BufWr, 2, 13);

                    BufWr[16] = 0x22;
                    BufWr[17] = 0;
                    BufWr[18] = 0xEF;
                    BufWr[19] = 0;
                    BufWr[20] = 0;
                    BufWr[21] = 0;
                    BufWr[22] = 0;
                    BufWr[23] = 0;
                    BufWr[24] = 0;
                    BufWr[25] = 0;
                    BufWr[26] = 0;
                    BufWr[27] = 0;
                    BufWr[28] = 0;
                    BufWr[29] = 0;
                    BufWr[30] = 0;
                    BufWr[31] = Test.CalcCheckSum(BufWr, 18, 13);

                    BufWr[32] = (byte)(0x30 | devCode);
                    BufWr[33] = 0;
                    BufWr[34] = 0xEF;
                    BufWr[35] = 0;
                    BufWr[36] = 0;
                    BufWr[37] = 0;
                    BufWr[38] = 0;
                    BufWr[39] = 0;
                    BufWr[40] = 0;
                    BufWr[41] = 0;
                    BufWr[42] = 0;
                    BufWr[43] = 0;
                    BufWr[44] = 0;
                    BufWr[45] = 0;
                    BufWr[46] = 0;
                    BufWr[47] = Test.CalcCheckSum(BufWr, 34, 13);

                    portModel.WriteAndRead(BufWr, 50, this, 50, false);

                    if (cancelTok.IsCancellationRequested) return;
                    if ((count == 0) || (data == null) || (data.Length == 0))
                    {
                        throw new Exception("Нет ответа от СЭВМ");
                    }
                    else if (count != 3)
                    {
                        throw new Exception("Ошибка обмена с СЭВМ.\nПринято " + count + " байт(а) (должно быть 3)");
                    }
                    else if (((data[0] & 0xF0) != 0) || ((data[1] & 0xF0) != 0) || ((data[2] & 0xF0) != 0))
                    {
                        throw new Exception("Ошибка обмена с СЭВМ.\nПолучены коды ошибок: " + ((data[0] >> 4) & 0xF).ToString() + ", " + ((data[1] >> 4) & 0xF).ToString() + ", " + ((data[2] >> 4) & 0xF).ToString());
                    }

                    extControlMode = true;
                    BufWr = new byte[16];
                }
                else
                {
                    BufWr = new byte[14];
                }

                //------------------------------| ЗАПРОС СОСТОЯНИЯ |------------------------------//

                ProgressWindow.SetStatus("Запрос сотояния...");

                pos = 0;
                if(extControlMode)
                {
                    BufWr[pos++] = (byte)(0x50 | devCode);
                    BufWr[pos++] = 1;
                }

                BufWr[pos++] = (byte)0xC2;
                BufWr[pos++] = devId;
                BufWr[pos++] = 0x55;
                BufWr[pos++] = 0xAA;
                BufWr[pos++] = (byte)'S';
                checksum = Test.CalcCheckSum(BufWr, pos - 5, 5);
                BufWr[pos++] = (byte)checksum;
                BufWr[pos++] = (byte)'R';
                BufWr[pos++] = (byte)'E';
                BufWr[pos++] = (byte)'Q';
                BufWr[pos++] = (byte)'U';
                BufWr[pos++] = (byte)'E';
                BufWr[pos++] = (byte)'S';
                BufWr[pos++] = (byte)'T';
                BufWr[pos++] = (byte)' ';

                portModel.WriteAndRead(BufWr, 40, this, 20, false);

                if (cancelTok.IsCancellationRequested) return;
                if(count <= 0)
                {
                    throw new Exception("Не удалось запросить состояние.\nНет ответа от МК");
                }
                else if((count != 5) || (data[0] != 0xC2) || (data[1] != 0x55) || (data[2] != 0xAA))
                {
                    throw new Exception("Не удалось запросить состояние.\nПолучен неверный ответ от МК");
                }
                else if(data[4] != Test.CalcCheckSum(data, 0, 4))
                {
                    throw new Exception("Не удалось запросить состояние.\nНеверная контрольная сумма");
                }

                //------------------------------| ВХОД В СОСТОЯНИЕ ПРОГРАММИРОВАНИЯ |------------------------------//

                ProgressWindow.SetStatus("Вход в сост. прогр...");

                pos = 0;
                if (extControlMode)
                {
                    BufWr[pos++] = (byte)(0x50 | devCode);
                    BufWr[pos++] = 1;
                }

                BufWr[pos++] = (byte)0xC1;
                BufWr[pos++] = devId;
                BufWr[pos++] = 0x55;
                BufWr[pos++] = 0xAA;
                BufWr[pos++] = (byte)'E';
                BufWr[pos++] = (byte)'N';
                BufWr[pos++] = (byte)'T';
                BufWr[pos++] = (byte)'E';
                BufWr[pos++] = (byte)'R';
                BufWr[pos++] = (byte)' ';
                BufWr[pos++] = (byte)'P';
                BufWr[pos++] = (byte)'R';
                BufWr[pos++] = (byte)'G';
                checksum = Test.CalcCheckSum(BufWr, pos - 13, 13);
                BufWr[pos++] = (byte)checksum;

                portModel.WriteAndRead(BufWr, 40, this, 20, false);

                if (cancelTok.IsCancellationRequested) return;
                if (count != 0)
                {
                    throw new Exception("Не удалось войти в состояние программирования.\nПолучен неверный ответ от МК");
                }

                pos = 0;
                if (extControlMode)
                {
                    BufWr[pos++] = (byte)(0x50 | devCode);
                    BufWr[pos++] = 1;
                }

                BufWr[pos++] = (byte)0xC2;
                BufWr[pos++] = devId;
                BufWr[pos++] = 0x55;
                BufWr[pos++] = 0xAA;
                BufWr[pos++] = (byte)'S';
                checksum = Test.CalcCheckSum(BufWr, pos - 5, 5);
                BufWr[pos++] = (byte)checksum;
                BufWr[pos++] = (byte)'R';
                BufWr[pos++] = (byte)'E';
                BufWr[pos++] = (byte)'Q';
                BufWr[pos++] = (byte)'U';
                BufWr[pos++] = (byte)'E';
                BufWr[pos++] = (byte)'S';
                BufWr[pos++] = (byte)'T';
                BufWr[pos++] = (byte)' ';

                portModel.WriteAndRead(BufWr, 40, this, 20, false);

                if (cancelTok.IsCancellationRequested) return;
                if (count <= 0)
                {
                    throw new Exception("Не удалось войти в состояние программирования.\nНет ответа от МК");
                }
                else if ((count != 5) || (data[0] != 0xC2) || (data[1] != 0x55) || (data[2] != 0xAA))
                {
                    throw new Exception("Не удалось войти в состояние программирования.\nПолучен неверный ответ от МК");
                }
                else if (data[4] != Test.CalcCheckSum(data, 0, 4))
                {
                    throw new Exception("Не удалось войти в состояние программирования.\nНеверная контрольная сумма");
                }
                else if(data[3] != 1)
                {
                    throw new Exception(String.Format("Не удалось войти в состояние программирования.\nКод состояния: 0x{0:X2}", data[3]));
                }

                //------------------------------| ОЧИСТКА БУФЕРА ПРИЕМА ДАННЫХ |------------------------------//

                ProgressWindow.SetStatus("Очистка буфера...");

                pos = 0;
                if (extControlMode)
                {
                    BufWr[pos++] = (byte)(0x50 | devCode);
                    BufWr[pos++] = 1;
                }

                BufWr[pos++] = (byte)0xC9;
                BufWr[pos++] = devId;
                BufWr[pos++] = 0x55;
                BufWr[pos++] = 0xAA;
                BufWr[pos++] = (byte)'C';
                BufWr[pos++] = (byte)'L';
                BufWr[pos++] = (byte)'E';
                BufWr[pos++] = (byte)'A';
                BufWr[pos++] = (byte)'R';
                BufWr[pos++] = (byte)' ';
                BufWr[pos++] = (byte)'B';
                BufWr[pos++] = (byte)'U';
                BufWr[pos++] = (byte)'F';
                checksum = Test.CalcCheckSum(BufWr, pos - 13, 13);
                BufWr[pos++] = (byte)checksum;

                portModel.WriteAndRead(BufWr, 5000, this, 0, false);

                if (cancelTok.IsCancellationRequested) return;
                if (count != 0)
                {
                    throw new Exception("Не удалось очистить буфер.\nПолучен неверный ответ от МК");
                }

                pos = 0;
                if (extControlMode)
                {
                    BufWr[pos++] = (byte)(0x50 | devCode);
                    BufWr[pos++] = 1;
                }

                BufWr[pos++] = (byte)0xC2;
                BufWr[pos++] = devId;
                BufWr[pos++] = 0x55;
                BufWr[pos++] = 0xAA;
                BufWr[pos++] = (byte)'S';
                checksum = Test.CalcCheckSum(BufWr, pos - 5, 5);
                BufWr[pos++] = (byte)checksum;
                BufWr[pos++] = (byte)'R';
                BufWr[pos++] = (byte)'E';
                BufWr[pos++] = (byte)'Q';
                BufWr[pos++] = (byte)'U';
                BufWr[pos++] = (byte)'E';
                BufWr[pos++] = (byte)'S';
                BufWr[pos++] = (byte)'T';
                BufWr[pos++] = (byte)' ';

                portModel.WriteAndRead(BufWr, 40, this, 20, false);

                if (cancelTok.IsCancellationRequested) return;
                if (count <= 0)
                {
                    throw new Exception("Не удалось очистить буфер.\nНет ответа от МК");
                }
                else if ((count != 5) || (data[0] != 0xC2) || (data[1] != 0x55) || (data[2] != 0xAA))
                {
                    throw new Exception("Не удалось очистить буфер.\nПолучен неверный ответ от МК");
                }
                else if (data[4] != Test.CalcCheckSum(data, 0, 4))
                {
                    throw new Exception("Не удалось очистить буфер.\nНеверная контрольная сумма");
                }
                else if (data[3] != 1)
                {
                    throw new Exception(String.Format("Не удалось очистить буфер.\nКод состояния: 0x{0:X2}", data[3]));
                }

                //------------------------------| ПЕРЕДАЧА АДРЕСА |------------------------------//

                ProgressWindow.SetStatus("Передача адреса...");

                pos = 0;
                if (extControlMode)
                {
                    BufWr[pos++] = (byte)(0x50 | devCode);
                    BufWr[pos++] = 1;
                }

                BufWr[pos++] = (byte)0xC3;
                BufWr[pos++] = devId;
                BufWr[pos++] = 0x55;
                BufWr[pos++] = 0xAA;
                BufWr[pos++] = (byte)'L';
                BufWr[pos++] = 0;
                BufWr[pos++] = 0;
                BufWr[pos++] = 0;
                BufWr[pos++] = 8;
                BufWr[pos++] = (byte)(progData.Count * 16);
                BufWr[pos++] = (byte)((progData.Count * 16) >> 8);
                BufWr[pos++] = (byte)((progData.Count * 16) >> 16);
                BufWr[pos++] = (byte)((progData.Count * 16) >> 24);
                checksum = Test.CalcCheckSum(BufWr, pos - 13, 13);
                BufWr[pos++] = (byte)checksum;

                portModel.WriteAndRead(BufWr, 40, this, 20, false);

                if (cancelTok.IsCancellationRequested) return;
                if (count != 0)
                {
                    throw new Exception("Не удалось передать адрес.\nПолучен неверный ответ от МК");
                }

                pos = 0;
                if (extControlMode)
                {
                    BufWr[pos++] = (byte)(0x50 | devCode);
                    BufWr[pos++] = 1;
                }

                BufWr[pos++] = (byte)0xC2;
                BufWr[pos++] = devId;
                BufWr[pos++] = 0x55;
                BufWr[pos++] = 0xAA;
                BufWr[pos++] = (byte)'S';
                checksum = Test.CalcCheckSum(BufWr, pos - 5, 5);
                BufWr[pos++] = (byte)checksum;
                BufWr[pos++] = (byte)'R';
                BufWr[pos++] = (byte)'E';
                BufWr[pos++] = (byte)'Q';
                BufWr[pos++] = (byte)'U';
                BufWr[pos++] = (byte)'E';
                BufWr[pos++] = (byte)'S';
                BufWr[pos++] = (byte)'T';
                BufWr[pos++] = (byte)' ';

                portModel.WriteAndRead(BufWr, 40, this, 20, false);

                if (cancelTok.IsCancellationRequested) return;
                if (count <= 0)
                {
                    throw new Exception("Не удалось передать адрес.\nНет ответа от МК");
                }
                else if ((count != 5) || (data[0] != 0xC2) || (data[1] != 0x55) || (data[2] != 0xAA))
                {
                    throw new Exception("Не удалось передать адрес.\nПолучен неверный ответ от МК");
                }
                else if (data[4] != Test.CalcCheckSum(data, 0, 4))
                {
                    throw new Exception("Не удалось передать адрес.\nНеверная контрольная сумма");
                }
                else if (data[3] != 2)
                {
                    throw new Exception(String.Format("Не удалось передать адрес.\nКод состояния: 0x{0:X2}", data[3]));
                }

                //------------------------------| ПЕРЕДАЧА ДАННЫХ |------------------------------//

                ProgressWindow.SetStatus("Передача данных...");

                bool extControlMode1 = extControlMode;
                extControlMode = false;

                BufWr = new byte[extControlMode1 ? 16 * 64 : 14 * 64];

                // Передаем данные блоками по 768 байт данных + 128/256 служебных байт
                for (int addr = 0; (addr < progData.Count * 16) && ((addr + 12 * 64) <= (progData.Count * 16)); addr += 12 * 64)
                {
                    pos = 0;
                    
                    for (int i = addr; i < (addr + 12 * 64); i += 12)
                    {
                        if (extControlMode1)
                        {
                            BufWr[pos++] = (byte)(0x10 | devCode);
                            BufWr[pos++] = 0;
                        }

                        BufWr[pos++] = 0xC4;
                        for (int j = 0; j < 12; ++j)
                        {
                            BufWr[pos++] = progData[(i + j) / 16][(i + j) % 16];
                        }
                        checksum = Test.CalcCheckSum(BufWr, pos - 13, 13);
                        BufWr[pos++] = (byte)checksum;
                    }

                    portModel.WriteAndRead(BufWr, 500, this, 0, false);

                    if (cancelTok.IsCancellationRequested) return;
                    if (extControlMode1)
                    {
                        if (count != 64)
                        {
                            throw new Exception("Не удалось передать данные.\nОшибка обмена с СЭВМ");
                        }
                    }
                    else if(count != 0)
                    {
                        throw new Exception("Не удалось передать данные.\nПолучен неверный ответ");
                    }
                }

                // Передаем последний пакет данных (размер пакета < 768)
                int lastData = (progData.Count * 16) % (12 * 64);
                if (lastData != 0)
                {
                    int addr0 = progData.Count * 16 - lastData;

                    if((lastData % 12) != 0)
                    {
                        lastData += 12 - lastData % 12;
                    }
                    BufWr = new byte[extControlMode1 ? lastData / 12 * 4 + lastData : lastData / 12 * 2 + lastData];

                    pos = 0;

                    for (int i = addr0; i < addr0 + lastData; i += 12)
                    {
                        if (extControlMode1)
                        {
                            BufWr[pos++] = (byte)(0x10 | devCode);
                            BufWr[pos++] = 0;
                        }

                        BufWr[pos++] = 0xC4;
                        for (int j = 0; j < 12; ++j)
                        {
                            if ((i + j) >= (progData.Count * 16))
                            {
                                BufWr[pos++] = 0xFF;
                            }
                            else
                            {
                                BufWr[pos++] = progData[(i + j) / 16][(i + j) % 16];
                            }
                        }
                        checksum = Test.CalcCheckSum(BufWr, pos - 13, 13);
                        BufWr[pos++] = (byte)checksum;
                    }

                    portModel.WriteAndRead(BufWr, 500, this, 0, false);

                    if (cancelTok.IsCancellationRequested) return;
                    if (extControlMode1)
                    {
                        if (count != lastData / 12)
                        {
                            throw new Exception("Не удалось передать данные.\nОшибка обмена с СЭВМ");
                        }
                    }
                    else if (count != 0)
                    {
                        throw new Exception("Не удалось передать данные.\nПолучен неверный ответ");
                    }
                }

                extControlMode = extControlMode1;
                BufWr = new byte[extControlMode ? 16 : 14];

                pos = 0;
                if (extControlMode)
                {
                    BufWr[pos++] = (byte)(0x50 | devCode);
                    BufWr[pos++] = 1;
                }

                BufWr[pos++] = (byte)0xC2;
                BufWr[pos++] = devId;
                BufWr[pos++] = 0x55;
                BufWr[pos++] = 0xAA;
                BufWr[pos++] = (byte)'S';
                checksum = Test.CalcCheckSum(BufWr, pos - 5, 5);
                BufWr[pos++] = (byte)checksum;
                BufWr[pos++] = (byte)'R';
                BufWr[pos++] = (byte)'E';
                BufWr[pos++] = (byte)'Q';
                BufWr[pos++] = (byte)'U';
                BufWr[pos++] = (byte)'E';
                BufWr[pos++] = (byte)'S';
                BufWr[pos++] = (byte)'T';
                BufWr[pos++] = (byte)' ';

                portModel.WriteAndRead(BufWr, 40, this, 20, false);

                if (cancelTok.IsCancellationRequested) return;
                if (count <= 0)
                {
                    throw new Exception("Не удалось передать данные.\nНет ответа от МК");
                }
                else if ((count != 5) || (data[0] != 0xC2) || (data[1] != 0x55) || (data[2] != 0xAA))
                {
                    throw new Exception("Не удалось передать данные.\nПолучен неверный ответ от МК");
                }
                else if (data[4] != Test.CalcCheckSum(data, 0, 4))
                {
                    throw new Exception("Не удалось передать данные.\nНеверная контрольная сумма");
                }
                else if (data[3] != 4)
                {
                    throw new Exception(String.Format("Не удалось передать данные.\nКод состояния: 0x{0:X2}", data[3]));
                }

                //------------------------------| ПЕРЕЗАПИСЬ ПЗУ |------------------------------//

                ProgressWindow.SetStatus("Перезапись ПЗУ...");

                extControlMode1 = extControlMode;
                extControlMode = false;

                pos = 0;
                if (extControlMode1)
                {
                    BufWr[pos++] = (byte)(0x10 | devCode);
                    BufWr[pos++] = 0;
                }

                BufWr[pos++] = (byte)0xC5;
                BufWr[pos++] = devId;
                BufWr[pos++] = 0xAA;
                BufWr[pos++] = 0xAA;
                BufWr[pos++] = 0x55;
                BufWr[pos++] = 0x55;
                BufWr[pos++] = 0xAA;
                BufWr[pos++] = 0x99;
                BufWr[pos++] = (byte)'F';
                BufWr[pos++] = (byte)'I';
                BufWr[pos++] = (byte)'R';
                BufWr[pos++] = (byte)'E';
                BufWr[pos++] = (byte)'!';
                checksum = Test.CalcCheckSum(BufWr, pos - 13, 13);
                BufWr[pos++] = (byte)checksum;

                portModel.WriteAndRead(BufWr, 6000, this, 0, false);

                extControlMode = extControlMode1;

                if (cancelTok.IsCancellationRequested) return;
                if (extControlMode)
                {
                    if (count != 1)
                    {
                        throw new Exception("Не удалось передать данные.\nОшибка обмена с СЭВМ");
                    }
                }
                else if (count != 0)
                {
                    throw new Exception("Не удалось перезаписать программу.\nПолучен неверный ответ от МК");
                }

                pos = 0;
                if (extControlMode)
                {
                    BufWr[pos++] = (byte)(0x50 | devCode);
                    BufWr[pos++] = 1;
                }

                BufWr[pos++] = (byte)0xC2;
                BufWr[pos++] = devId;
                BufWr[pos++] = 0x55;
                BufWr[pos++] = 0xAA;
                BufWr[pos++] = (byte)'S';
                checksum = Test.CalcCheckSum(BufWr, pos - 5, 5);
                BufWr[pos++] = (byte)checksum;
                BufWr[pos++] = (byte)'R';
                BufWr[pos++] = (byte)'E';
                BufWr[pos++] = (byte)'Q';
                BufWr[pos++] = (byte)'U';
                BufWr[pos++] = (byte)'E';
                BufWr[pos++] = (byte)'S';
                BufWr[pos++] = (byte)'T';
                BufWr[pos++] = (byte)' ';

                portModel.WriteAndRead(BufWr, 40, this, 20, false);

                if (cancelTok.IsCancellationRequested) return;
                if (count <= 0)
                {
                    throw new Exception("Не удалось перезаписать программу.\nНет ответа от МК");
                }
                else if ((count != 5) || (data[0] != 0xC2) || (data[1] != 0x55) || (data[2] != 0xAA))
                {
                    throw new Exception("Не удалось перезаписать программу.\nПолучен неверный ответ от МК");
                }
                else if (data[4] != Test.CalcCheckSum(data, 0, 4))
                {
                    throw new Exception("Не удалось перезаписать программу.\nНеверная контрольная сумма");
                }
                else if (data[3] != 0)
                {
                    throw new Exception(String.Format("Не перезаписать программу.\nКод состояния: 0x{0:X2}", data[3]));
                }

                wnd.Dispatcher.BeginInvoke(new Action(() =>
                {
                    System.Windows.MessageBox.Show(wnd, "Перепрограммирование МК успешно выполнено", "Сообщение", MessageBoxButton.OK, MessageBoxImage.Information);
                }));
            }
            catch (Exception ex)
            {
                ProgressWindow.CloseProgressWnd();
                ShowErrorWnd(ex.Message);
            }
            finally
            {
                try
                {
                    ProgressWindow.CloseProgressWnd();
                    task = null;
                    tokenSrc = null;
                }
                catch { }
            }
        }

        public void DataReceived(byte[] data, int count)
        {
            if (extControlMode)
            {
                if ((count == 0) || (data == null) || (data.Length == 0))
                {
                    throw new Exception("Нет ответа от СЭВМ");
                }
                else if((count != 16) || (data.Length != 16))
                {
                    throw new Exception("Ошибка обмена с СЭВМ");
                }

                count = data[1] <= 14 ? data[1] : 14;
                for (int i = 0; i < count; ++i)
                {
                    data[i] = data[2 + i];
                }
            }

            this.data = data;
            this.count = count;
        }

        private void ShowErrorWnd(string message)
        {
            wnd.Dispatcher.BeginInvoke(new Action(() =>
            {
                System.Windows.MessageBox.Show(wnd, message, "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
            }));
        }
    }
}
