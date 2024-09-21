using SFC.ViewModels;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;

namespace SFC.Models
{
    public class J1939_GAZ
    {
        public CanAdapter Adapter = new CanAdapter();

        private MainWindowViewModel VM;

        byte[] TxData = new byte[8];

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
                    
                }
            }
        }

        public Thread StartProcessReq()
        {

            Thread ReqThread = new Thread(ReqProcess);
            ReqThread.Name = "ReqProcess";
            ReqThread.IsBackground = true;
            ReqThread.Start();


            return ReqThread;

        }

        private void ReqProcess()
        {
            while (true)
            {
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
    }
}
