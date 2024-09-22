using SFC.ViewModels;
using System;
using System.Collections.Generic;
using System.Data.Common;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Markup;

namespace SFC.Models
{
    public class J1939_GAZ
    {
        public CanAdapter Adapter = new CanAdapter();

        private MainWindowViewModel VM;

        byte[] TxData = new byte[8];

        static uint[] UDSReqList = { 1, 3, 4, 5, 15, 16, 40, 41, 77, 78, 79, 83, 84 };

        public J1939_GAZ(MainWindowViewModel parent)
        {
            VM = parent;

            Adapter.Port.ComPort.BaudRate = VM.BaudrateList[VM.BaudrateIndex];
        }

        public void ParseMessage()
        {
            if (Adapter.ProcessPacket_f)
            {
                Adapter.ProcessPacket_f = false;
                if (Adapter.Id == "18FE6D44")//HTR_STATUS
                {
                    VM.ParamsJ1939[0].Value = Convert.ToString(Adapter.RxData[0]-40);
                    VM.ParamsJ1939[1].Value = Convert.ToString(Adapter.RxData[2]);
                    VM.ParamsJ1939[2].Value = Convert.ToString(Adapter.RxData[3]);
                    VM.ParamsJ1939[3].Value = Convert.ToString((Adapter.RxData[6]<<8)+Adapter.RxData[7]);
                }
                else if(Adapter.Id == "18FEF744")//VEP
                {

                }
                else if(Adapter.Id == "18FEF944")//SOFT
                {

                }
                else if (Adapter.Id == "18FECA44")//DM1
                {

                }
                else if(Adapter.Id == "18DAF144")//UDS
                {
                    if (Adapter.RxData[1] == 0x22)
                    {
                        if (Adapter.RxData[2] == 0x44)
                        {
                            if (Adapter.RxData[3]==1)//stage/mode
                            {

                            }
                            else if (Adapter.RxData[3]==3)//work time
                            {

                            }
                            else if (Adapter.RxData[3]==4)//mode time
                            {

                            }
                            else if (Adapter.RxData[3]==5)//voltage
                            {

                            }
                            else if (Adapter.RxData[3]==15)//rev defined
                            {

                            }
                            else if (Adapter.RxData[3]==16)//rev measured
                            {

                            }
                            else if (Adapter.RxData[3]==40)//T liquid
                            {

                            }
                            else if (Adapter.RxData[3]==41)// T overheat
                            {

                            }
                            else if (Adapter.RxData[3]==77)//spark plug
                            {

                            }
                            else if (Adapter.RxData[3]==78)//fuel valve
                            {

                            }
                            else if (Adapter.RxData[3]==79)//injector heater
                            {

                            }
                            else if (Adapter.RxData[3]==83)//photodiode
                            {

                            }
                            else if (Adapter.RxData[3]==84)//water pump
                            {

                            }
                        }
                    }
                }
            }
        }


        public Thread StartProcessReqHtr()
        {

            Thread ReqThread = new Thread(HtrProcess);
            ReqThread.Name = "HtrProcess";
            ReqThread.IsBackground = true;
            ReqThread.Start();


            return ReqThread;

        }

        private void HtrProcess()
        {
            while (true)
            {
                if (VM.RegularReqHTR == false) return;
                Thread.Sleep(1000);
                TxData[0] = (byte)(VM.Tsetpoint+40);
                TxData[1] = 0xFF;
                TxData[2] = 0xFF;
                TxData[3] = (byte)VM.WorkMode;
                TxData[4] = 0xFF;
                TxData[5] = 0xFF;
                TxData[6] = (byte)(VM.WorkTime/10);
                TxData[7] = 0xFF;

                Adapter.SendMessage("18FE6D3A", TxData);
            }
        }

        public Thread StartProcessReqUds()
        {

            Thread ReqThread = new Thread(UdsProcess);
            ReqThread.Name = "udsProcess";
            ReqThread.IsBackground = true;
            ReqThread.Start();


            return ReqThread;

        }

        private void UdsProcess()
        {
            uint i = 0;
            while (true)
            {
                if (VM.RegularReqUDS == false) return;
                Thread.Sleep(100);
                TxData[0] = 3;
                TxData[1] = 0x22;
                TxData[2] = 0x44;
                TxData[3] = (byte)UDSReqList[i];
                TxData[4] = 0xFF;
                TxData[5] = 0xFF;
                TxData[6] = 0xFF;
                TxData[7] = 0xFF;

                Adapter.SendMessage("18FE6D3A", TxData);

                if (i<UDSReqList.Length-1) i++;
                else i = 0;
            }
        }
    }
}
