using SFC.ViewModels;
using System;
using System.Collections.Generic;
using System.ComponentModel;
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
        public CanAdapter Adapter;

        public MainWindowViewModel VM;

        public ProtocolUDS UDS;

        byte[] TxData = new byte[8];

        static uint[] UDSReqList = { 1, 3, 4, 15, 16, 41, 77, 78, 79, 83, 84 };

        public J1939_GAZ(MainWindowViewModel parent)
        {
            VM = parent;
            UDS = new ProtocolUDS(this);
            Adapter = new CanAdapter(this);
            Adapter.Port.ComPort.BaudRate = VM.BaudrateList[VM.BaudrateIndex];
        }

        public void ParseMessage()
        {
            VM.AddMessageToRxTerminal(Adapter.Id, Adapter.RxData);
            VM.MessageLog+=DateTime.Now.ToString("HH:mm:ss:fff")+"   >> "+Adapter.Id +" |   "+Convert.ToHexString(Adapter.RxData)+"\r";
            Adapter.ProcessPacket_f = false;
            if (Adapter.Id == "18FE6D44")//HTR_STATUS
            {
                if(VM.ParamsJ1939[0].Value!=null) VM.ParamsJ1939[0].Value = Convert.ToString(Adapter.RxData[0]-40);
                VM.ParamsJ1939[1].Value = Convert.ToString(Adapter.RxData[2]);
                VM.ParamsJ1939[2].Value = Convert.ToString(Adapter.RxData[3]);
                VM.ParamsJ1939[3].Value = Convert.ToString((Adapter.RxData[6]<<8)+Adapter.RxData[7]);
            }
            else if(Adapter.Id == "18FEF744")//VEP
            {
                VM.ParamsVEP[0].Value = Convert.ToString((Adapter.RxData[4]+(Adapter.RxData[5]<<8))/20.0);
            }
            else if(Adapter.Id == "18FEF944")//SOFT
            {

            }
            else if (Adapter.Id == "18FECA44")//DM1
            {
                VM.ParamsDM1[0].Value = Convert.ToString(Adapter.RxData[0]);
                VM.ParamsDM1[1].Value = Convert.ToString(Adapter.RxData[2]+(Adapter.RxData[3]<<8));
                VM.ParamsDM1[2].Value = Convert.ToString(Adapter.RxData[4]);
                VM.ParamsDM1[3].Value = Convert.ToString(Adapter.RxData[5]);

                VM.SetFooterState(255, 255, GetFaultCode((Adapter.RxData[4]<<19)+Adapter.RxData[2]+(Adapter.RxData[3]<<8)));
            }
            else if(Adapter.Id == "18DAF144")//UDS
            {
                UDS.ParseMessage(Adapter.RxData);
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

                SendMessage("18FE6D3A", TxData);
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
                Thread.Sleep(80);
                TxData[0] = 3;
                TxData[1] = 0x22;
                TxData[2] = 0x44;
                TxData[3] = (byte)UDSReqList[i];
                TxData[4] = 0xFF;
                TxData[5] = 0xFF;
                TxData[6] = 0xFF;
                TxData[7] = 0xFF;

                SendMessage("18DA44F1", TxData);

                if (i<UDSReqList.Length-1) i++;
                else i = 0;
            }
        }

        private uint GetFaultCode(long _spnfmi)
        {
            uint code = 0;
            if (_spnfmi>100)
            {
                switch (_spnfmi)
                {
                    case (3<<19)+168: code = 12; break;
                    case (4<<19)+168: code = 15; break;
                    case (0<<19)+854: code = 1; break;
                    case (12<<19)+854: code = 3; break;
                    case (5<<19)+855: code = 5; break;
                    case (12<<19)+856: code = 9; break;
                    case (18<<19)+857: code = 10; break;
                    case (1<<19)+857: code = 27; break;
                    case (0<<19)+857: code = 28; break;
                    case (0<<19)+858: code = 22; break;
                    case (0<<19)+859: code = 24; break;
                    case (6<<19)+860: code = 29; break;
                    case (12<<19)+1044: code = 14; break;
                    case (12<<19)+1442: code = 17; break;
                    case (0<<19)+1677: code = 37; break;
                    case (0<<12)+1687: code = 4; break;
                    case (0<<19)+10760: code = 90; break;
                    default: code = (uint)_spnfmi; break;
                }
            }
            else code = (uint)_spnfmi;
            return code;
        }

        public void SendMessage(string _id, byte[] _data)
        { 
            Adapter.SendMessage(_id, _data);
            VM.AddMessageToTxTerminal(_id, _data);
        }
    }
}
