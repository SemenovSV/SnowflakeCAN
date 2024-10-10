using SFC;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using static MaterialDesignThemes.Wpf.Theme.ToolBar;
using SFC.ViewModels;
using System.Windows;


namespace SFC.Models
{
    public class ProtocolUDS
    {
        //public CanAdapter Adapter = new CanAdapter();
        public IntelHEX Hex = new IntelHEX();

        private J1939_GAZ Parent;//ToDo Возможно получится избавиться от связи

        //const string IdTxUDS = "18DA44F1";
        string IdTxUDS = "";
        string IdRxUDS = "";

        byte ReceiverId = 0;

        byte[] TxData = new byte[8];

        public const byte SERVICE_SESSION_CONTROL          = 0x10;
        public const byte SERVICE_ECU_RESET                = 0x11;
        public const byte SERVICE_READ_DTC_INFO            = 0x19;
        public const byte SERVICE_READ_DATA_BY_ID          = 0x22;
        public const byte SERVICE_SECURITY_ACCESS          = 0x27;
        public const byte SERVICE_WRITE_DATA_BY_ID         = 0x2E;
        public const byte SERVICE_ROUTINE_CONTROL          = 0x31;
        public const byte SERVICE_IO_CONTROL_BY_ID         = 0x2F;
        public const byte SERVICE_PROGRAMM_START_ADDRESS   = 0x34;
        public const byte SERVICE_TRANSFER_DATA            = 0x35;
        public const byte SERVICE_PROGRAMM_DATA            = 0x36;
        public const byte SERVICE_PROGRAMM_STOP            = 0x37;
        public const byte SERVICE_TESTER_PRESENT           = 0x3E;
        public const byte SERVICE_DTC_CONTROL              = 0x85;


        public const byte SESSION_DEFAULT                  = 1;
        public const byte SESSION_PROGRAMMING              = 2;
        public const byte SESSION_EXTENDED_DIAGNOSTIC      = 3;
        public const byte SESSION_SAFETY_SYSYEM_DIAGNOSTIC = 4;

        public const short WAITING             = 0;
        public const short ACCESSING           = 1;
        public const short SWITCH_TO_BOOT      = 2;
        public const short START_LOADING       = 3;
        public const short PROCESS_LOADING     = 4;
        public const short END_LOADING         = 5;
        public const short CHECK_CRC           = 6;
        public const short RUN_MAIN_PROGRAM    = 7;
        public const short BAD_ACCESS          = -1;
        public const short BAD_SWITCH_TO_BOOT  = -2;
        public const short BAD_START_LOADING   = -3;
        public const short BAD_PROCESS_LOADING = -4;
        public const short BAD_END_LOADING     = -5;
        public const short BAD_CRC             = -6;
        public const short BAD_MAIN_PROGRAM    = -7;


        public IProgress<short> StateProcess;

        private bool
            Accesed_f = false,
            SwitchedToBoot_f = false,
            SwitchedToSession_f = false,
            StartLoading_f= false,
            Loaded_f = false,
            CheckedCrc_f = false,
            RunnedMainApplication_f = false;
        private bool
            GetSeed_f = true;

        private const ushort DELAY_ATTEMPTING = 500;

        private bool
            WaitFlowControl_f = false,
            WaitCorrectTransfer_f = false;
        ushort
            FragmentCounter = 0,
            PacketCounter,
            PacketNum;

        byte[] CodeFragment = new byte[IntelHEX.FragmentSize];

        public IProgress<ushort> LoadProgress;

        //private Stopwatch TimerFault = new Stopwatch();
        private Stopwatch LoadTime = new Stopwatch();
        public IProgress<uint> LoadTimeS;

        public uint LoadFaultCnt = 0;

        public ProtocolUDS(J1939_GAZ parent)
        {
            Parent = parent;
        }

        public void ParseMessage(byte[] message)
        {
            if ((message[0] & 0xF0) == 0)//single frame
            {
                byte Service = message[1];
                if (Service == SERVICE_SESSION_CONTROL+0x40)
                {
                    if(message[2] == SESSION_PROGRAMMING)
                    {
                        Parent.VM.ConsoleContent +="Переход на сессию программирования осуществлён\r";
                        SwitchedToSession_f = true;
                        LoadFaultCnt = 0;
                    }
                }
                else if (Service == SERVICE_ECU_RESET+0x40)
                {
                    if(message[2] == 1)
                    {
                        Parent.VM.ConsoleContent +="Переход в загрузчик осуществлён\r";
                        SwitchedToBoot_f = true;
                        LoadFaultCnt = 0;
                    }
                    else if(message[2] == 2)
                    {
                        Parent.VM.ConsoleContent +="Обновление ПО успешно завершено\r";
                        RunnedMainApplication_f = true;
                        LoadFaultCnt = 0;
                    }
                }
                else if (Service == SERVICE_READ_DATA_BY_ID+0x40)
                {
                    if (message[3]==1)//stage/mode
                    {
                        Parent.VM.ParamsUDS[0].Value = Convert.ToString(message[4]);
                        Parent.VM.ParamsUDS[1].Value = Convert.ToString(message[5]);
                        Parent.VM.SetFooterState(message[4], message[5], 255);
                    }
                    else if (message[3]==3)//work time
                    {
                        Parent.VM.ParamsUDS[2].Value = Convert.ToString(((message[4]<<8)+message[5])*60+message[6]);
                    }
                    else if (message[3]==4)//mode time
                    {
                        Parent.VM.ParamsUDS[3].Value = Convert.ToString((message[4]<<8)+message[5]);
                    }
                    else if (message[3]==15)//rev defined
                    {
                        Parent.VM.ParamsUDS[4].Value = Convert.ToString(message[4]);
                    }
                    else if (message[3]==16)//rev measured
                    {
                        Parent.VM.ParamsUDS[5].Value = Convert.ToString(message[4]);
                    }
                    else if (message[3]==41)// T overheat
                    {
                        Parent.VM.ParamsUDS[6].Value = Convert.ToString(message[4]-40);
                    }
                    else if (message[3]==77)//spark plug
                    {
                        Parent.VM.ParamsUDS[7].Value = Convert.ToString(message[4]);
                    }
                    else if (message[3]==78)//fuel valve
                    {
                        Parent.VM.ParamsUDS[8].Value = Convert.ToString(message[4]);
                    }
                    else if (message[3]==79)//injector heater
                    {
                        Parent.VM.ParamsUDS[9].Value = Convert.ToString(message[4]);
                    }
                    else if (message[3]==83)//photodiode
                    {
                        Parent.VM.ParamsUDS[10].Value = Convert.ToString((message[4]<<8)+message[5]);
                    }
                    else if (message[3]==84)//water pump
                    {
                        Parent.VM.ParamsUDS[11].Value = Convert.ToString(message[4]);
                    }
                }
                else if (Service == SERVICE_SECURITY_ACCESS+0x40)
                {
                    if(message[2] == 7) //получен Seed
                    {
                        GetSeed_f = true;
                        LoadFaultCnt = 0;
                        uint seed = (uint)(((message[3] & 0xFF) << 24) + ((message[4] & 0xFF) << 16) + ((message[5] & 0xFF) << 8) + (message[6] & 0xFF));//ToDo
                        if(seed == 0xFFFFFFFF) Accesed_f = true;

                        uint countKey = (seed + 0x29C) * 0x573B4;
                        countKey ^= seed;
                        TxData[0] = 6;
                        TxData[1] = SERVICE_SECURITY_ACCESS;
                        TxData[2] = 8;
                        TxData[3] = (byte)((countKey>>24) & 0xFF);
                        TxData[4] = (byte)((countKey>>16) & 0xFF);
                        TxData[5] = (byte)((countKey>>8) & 0xFF);
                        TxData[6] = (byte)((countKey) & 0xFF);
                        TxData[7] = 0xFF;
                        Parent.SendMessage(IdTxUDS, TxData);
                    }
                    else if(message[2] == 8)//резутьтат Key-verification положительный
                    {
                        Parent.VM.ConsoleContent +="Уровень доступа получен\r";
                        Accesed_f = true;
                        LoadFaultCnt = 0;
                    }
                }
                else if(Service == SERVICE_PROGRAMM_START_ADDRESS+0x40)
                {
                    Parent.VM.ConsoleContent +="Загрузка управляющей программы\r";
                    StartLoading_f = true;
                    LoadFaultCnt = 0;
                }
                else if(Service == SERVICE_PROGRAMM_DATA+0x40)
                {
                    WaitCorrectTransfer_f = false;
                    FragmentCounter++;
                    LoadFaultCnt = 0;

                if (FragmentCounter == Hex.getFragmentsNumb())
                {
                        Loaded_f = true;
                        StopLoading();
                        Parent.VM.ConsoleContent +="Загрузка завершена\r";
                    }
                    LoadProgress?.Report((ushort)(100*(FragmentCounter+1)/Hex.getFragmentsNumb()));
                    LoadTimeS?.Report((uint)LoadTime.ElapsedMilliseconds/1000);
                }
                else if (Service == SERVICE_ROUTINE_CONTROL+0x40)
                {
                    if (message[2] == 1)//check CRC
                    {
                        Parent.VM.ConsoleContent +="Контрольная сумма проверена\r";
                        CheckedCrc_f = true;
                        LoadFaultCnt = 0;
                        LoadTime.Stop();
                    }
                }
            }
            else if(message[0] == 0x30)//FlowControl
            {
                LoadFaultCnt = 0;
                WaitFlowControl_f = false;
            }
        }

        public Thread StartPricessLoading()
        {

            Thread LoadThread = new Thread(LoadProcess);
            LoadThread.Name = "LoadProcess";
            LoadThread.IsBackground = true;
            LoadThread.Start();

            LoadTime.Restart();
            Accesed_f = false;
            SwitchedToBoot_f = false;
            SwitchedToSession_f = false;
            StartLoading_f= false;
            Loaded_f = false;
            CheckedCrc_f = false;
            RunnedMainApplication_f = false;

            WaitFlowControl_f = false;
            WaitCorrectTransfer_f = false;

            FragmentCounter=0;

            LoadFaultCnt = 0;

            LoadProgress?.Report(0);
            LoadTimeS?.Report(0);

            return LoadThread;

        }

        private void LoadProcess()
        {
            while (true)
            {
                if (!Accesed_f) GetAccess();
                else if (!SwitchedToSession_f) SwitchToSession();
                else if (!SwitchedToBoot_f) SwitchToBootloader();
                else if (!StartLoading_f) StartLoading();
                else if (!Loaded_f) LoadFirmware();
                else if (!CheckedCrc_f) CheckCrc();
                else if (!RunnedMainApplication_f) RunMainApplication();
                else
                {
                    Parent.VM.EnableAction = true;
                    return;
                }

                if (LoadFaultCnt>20)
                {
                    StopProcessLoading();
                    Parent.VM.EnableAction = true;
                    return;
                }
            }
        }

        private void StopProcessLoading()
        {
            Parent.VM.ConsoleContent +="Загрузка не удалась\r";
        }

        private void GetAccess()
        {
            if (GetSeed_f) {
                StateProcess?.Report(ACCESSING);
                TxData[0]=2;
                TxData[1]=SERVICE_SECURITY_ACCESS;
                TxData[2]=7;
                TxData[3]=0xFF;
                TxData[4]=0xFF;
                TxData[5]=0xFF;
                TxData[6]=0xFF;
                TxData[7]=0xFF;
                Parent.SendMessage(IdTxUDS, TxData);
                LoadFaultCnt++;
            }
            Thread.Sleep(DELAY_ATTEMPTING);
        }

        private void SwitchToSession()
        {
            TxData[0]=2;
            TxData[1]=SERVICE_SESSION_CONTROL;
            TxData[2]=SESSION_PROGRAMMING;
            TxData[3]=0xFF;
            TxData[4]=0xFF;
            TxData[5]=0xFF;
            TxData[6]=0xFF;
            TxData[7]=0xFF;
            Parent.SendMessage(IdTxUDS, TxData);
            LoadFaultCnt++;

            Thread.Sleep(DELAY_ATTEMPTING);
        }

        private void SwitchToBootloader()
        {
            StateProcess?.Report(SWITCH_TO_BOOT);
            TxData[0]=2;
            TxData[1]=SERVICE_ECU_RESET;
            TxData[2]=1;
            TxData[3]=0xFF;
            TxData[4]=0xFF;
            TxData[5]=0xFF;
            TxData[6]=0xFF;
            TxData[7]=0xFF;
            Parent.SendMessage(IdTxUDS, TxData);
            LoadFaultCnt++;

            Thread.Sleep(DELAY_ATTEMPTING);
        }

        private void StartLoading()
        {
            Parent.VM.RegularReqUDS = false;
            Parent.VM.RegularReqHTR = false;
            StateProcess?.Report(START_LOADING);
            int start_addr = Hex.getProgrammStartAdress();
            TxData[0]=5;
            TxData[1]=SERVICE_PROGRAMM_START_ADDRESS;
            TxData[2]=(byte)((start_addr>>24)&0xFF);
            TxData[3]=(byte)((start_addr>>16)&0xFF);
            TxData[4]=(byte)((start_addr>>8)&0xFF);
            TxData[5]=(byte)((start_addr)&0xFF);
            TxData[6]=0xFF;
            TxData[7]=0xFF;
            Parent.SendMessage(IdTxUDS, TxData);
            LoadFaultCnt++;

            Thread.Sleep(DELAY_ATTEMPTING);
        }

        private void LoadFirmware()
        {
            StateProcess?.Report(PROCESS_LOADING);
            if (Parent.Adapter.TransmissionSuccessed() && !WaitFlowControl_f && !WaitCorrectTransfer_f)
            {
                if (PacketCounter == 0)
                {
                    WaitFlowControl_f = true;

                    CodeFragment = Hex.getFragmentData(FragmentCounter);
                    if (CodeFragment == null) return;

                    ushort size = (ushort)Hex.getFragmentSize(FragmentCounter);

                    if((size-4)%7 == 0)
                        PacketNum = (ushort)((size-4)/7);
                    else
                        PacketNum = (ushort)(((size-4)/7)+1);
                    TxData[0] = (byte)(0x10+((size & 0xF00)>>8));
                    TxData[1] = (byte)(size & 0xFF);
                    TxData[2] = 0x36;
                    TxData[3] = (byte)(FragmentCounter&0xFF);
                    TxData[4] = CodeFragment[0];
                    TxData[5] = CodeFragment[1];
                    TxData[6] = CodeFragment[2];
                    TxData[7] = CodeFragment[3];
                    PacketCounter = 1;
                }
                else
                {
                    TxData[0] = (byte)(0x20+((PacketCounter) & 0x0F));
                    for (byte i = 1; i<8; i++)
                    {
                        TxData[i]=CodeFragment[3+7*(PacketCounter-1)+i];
                    }

                    PacketCounter++;
                    if (PacketCounter == PacketNum+1)
                    {
                        WaitCorrectTransfer_f = true;
                        PacketCounter = 0;
                    }
                }
                Parent.SendMessage(IdTxUDS, TxData);
            }
            else
            {
                //LoadFaultCnt++;
            }
        }

        private void StopLoading()
        {
            StateProcess?.Report(END_LOADING);
            TxData[0]=1;
            TxData[1]=SERVICE_PROGRAMM_STOP;
            TxData[2]=0xFF;
            TxData[3]=0xFF;
            TxData[4]=0xFF;
            TxData[5]=0xFF;
            TxData[6]=0xFF;
            TxData[7]=0xFF;

            Parent.SendMessage(IdTxUDS, TxData);
            LoadFaultCnt++;
        }


        private void CheckCrc()
        {
            StateProcess?.Report(CHECK_CRC);
            TxData[0]=2;
            TxData[1]=SERVICE_ROUTINE_CONTROL;
            TxData[2]=1;
            TxData[3]=0xFF;
            TxData[4]=0xFF;
            TxData[5]=0xFF;
            TxData[6]=0xFF;
            TxData[7]=0xFF;

            Parent.SendMessage(IdTxUDS, TxData);
            LoadFaultCnt++;
            Thread.Sleep(2*DELAY_ATTEMPTING);
        }

        private void RunMainApplication()
        {
            TxData[0]=2;
            TxData[1]=SERVICE_ECU_RESET;
            TxData[2]=2;
            TxData[3]=0xFF;
            TxData[4]=0xFF;
            TxData[5]=0xFF;
            TxData[6]=0xFF;
            TxData[7]=0xFF;

            Parent.SendMessage(IdTxUDS, TxData);
            LoadFaultCnt++;
            Thread.Sleep(DELAY_ATTEMPTING);
        }

        public void SetReceiverId(byte[] version)
        {
            if (version[0] == 25 && version[2] == 26)//MAZ 35SP
            {
                ReceiverId = 0x39;
            }
            if (version[0] == 25 && version[2] == 27)//LIAZ 35SP
            {
                ReceiverId = 0x44;
            }
            else if(version[0] == 2)//MAZ Planar 2
            {
                ReceiverId = 0x44;
            }
            else if(version[0] == 1)//KAMAZ 14TS-mini
            {
                ReceiverId = 0x44;
            }
            else if(version[0] == 33)//KAMAZ Planar 5
            {
                ReceiverId = 0x45;
            }

            IdTxUDS = "18DA"+ReceiverId.ToString("X2")+"F1";
            IdRxUDS = IdTxUDS.Substring(0, 4)+IdTxUDS.Substring(6, 2)+IdTxUDS.Substring(4, 2);
        }
    }
}
