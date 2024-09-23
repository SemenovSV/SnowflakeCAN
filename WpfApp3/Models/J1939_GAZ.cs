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

        static uint[] UDSReqList = { 1, 3, 4, 5, 15, 16, 41, 77, 78, 79, 83, 84 };

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
                    if (Adapter.RxData[1] == 0x22+0x40)
                    {
                        if (Adapter.RxData[2] == 0x44)
                        {
                            if (Adapter.RxData[3]==1)//stage/mode
                            {
                                VM.ParamsUDS[0].Value = Convert.ToString(Adapter.RxData[4]);
                                VM.ParamsUDS[1].Value = Convert.ToString(Adapter.RxData[5]);
                                VM.SetFooterState(Adapter.RxData[4], Adapter.RxData[5], 255);
                            }
                            else if (Adapter.RxData[3]==3)//work time
                            {
                                VM.ParamsUDS[2].Value = Convert.ToString(((Adapter.RxData[4]<<8)+Adapter.RxData[5])*60+Adapter.RxData[6]);
                            }
                            else if (Adapter.RxData[3]==4)//mode time
                            {
                                VM.ParamsUDS[3].Value = Convert.ToString((Adapter.RxData[4]<<8)+Adapter.RxData[5]);
                            }
                            else if (Adapter.RxData[3]==5)//voltage
                            {
                                VM.ParamsUDS[4].Value = Convert.ToString(((Adapter.RxData[4]<<8)+Adapter.RxData[5])/10.0);
                            }
                            else if (Adapter.RxData[3]==15)//rev defined
                            {
                                VM.ParamsUDS[5].Value = Convert.ToString(Adapter.RxData[4]);
                            }
                            else if (Adapter.RxData[3]==16)//rev measured
                            {
                                VM.ParamsUDS[6].Value = Convert.ToString(Adapter.RxData[4]);
                            }
                            else if (Adapter.RxData[3]==41)// T overheat
                            {
                                VM.ParamsUDS[7].Value = Convert.ToString(Adapter.RxData[4]-40);
                            }
                            else if (Adapter.RxData[3]==77)//spark plug
                            {
                                VM.ParamsUDS[8].Value = Convert.ToString(Adapter.RxData[4]);
                            }
                            else if (Adapter.RxData[3]==78)//fuel valve
                            {
                                VM.ParamsUDS[9].Value = Convert.ToString(Adapter.RxData[4]);
                            }
                            else if (Adapter.RxData[3]==79)//injector heater
                            {
                                VM.ParamsUDS[10].Value = Convert.ToString(Adapter.RxData[4]);
                            }
                            else if (Adapter.RxData[3]==83)//photodiode
                            {
                                VM.ParamsUDS[11].Value = Convert.ToString((Adapter.RxData[4]<<8)+Adapter.RxData[5]);
                            }
                            else if (Adapter.RxData[3]==84)//water pump
                            {
                                VM.ParamsUDS[12].Value = Convert.ToString(Adapter.RxData[4]);
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

                Adapter.SendMessage("18DA44F1", TxData);

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
                    case (3<<19)+168:   code = 12; break;
                    case (4<<19)+168:   code = 15; break;
                    case 0:
                    case (0<<19)+854:   code = 1 ; break;
                    case (12<<19)+854:  code = 3 ; break;
                    case (5<<19)+855:   code = 5 ; break;
                    case (12<<19)+856:  code = 9 ; break;
                    case (18<<19)+857:  code = 10; break;
                    case (1<<19)+857:   code = 27; break;
                    case (0<<19)+857:   code = 28; break;
                    case (0<<19)+858:   code = 22; break;
                    case (0<<19)+859:   code = 24; break;
                    case (6<<19)+860:   code = 29; break;
                    case (12<<19)+1044: code = 14; break;
                    case (12<<19)+1442: code = 17; break;
                    case (0<<19)+1677:  code = 37; break;
                    case (0<<12)+1687:  code = 4 ; break;
                    case (0<<19)+10760: code = 90; break;
                    default:            code = (uint)_spnfmi; break;
                }
            }
            return code;
        }
    }
}
