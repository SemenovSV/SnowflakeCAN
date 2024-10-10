using System;
using System.Collections.Generic;
using System.IO.Ports;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;

namespace SFC.Models
{
    public class CanAdapter
    {
        public ComPortModel Port = new ComPortModel();

        private J1939_GAZ Parent;//ToDo Возможно получится избавиться от связи

        public byte[] MessageIn = new byte[MessageMaxLength];
        public byte[] MessageOut = new byte[11];

        public ushort MessageCnt;
        private const int MessageMaxLength = 32;

        private bool SuccessTransmission_f = false;

        public CanAdapter(J1939_GAZ parent)
        {
            Port.ComPort.DataReceived += new SerialDataReceivedEventHandler(DataReceivedHandler); // Add DataReceived Event Handler
            Parent=parent;
        }


        public string Id = "";
        public byte[] RxData = new byte[8];
        public bool ProcessPacket_f = false;


        public void DataReceivedHandler(object sender, SerialDataReceivedEventArgs args)
        {
            if (Port.ComPort.IsOpen)
            {
                string existing = Port.ComPort.ReadExisting();
                ParseMessage(existing);
            }
        }

        public void ParseMessage(string _mes)
        {
            string message = _mes;
            for(int  j = 0; j<message.Length; j++)
            {
                if (message[j] == 'T' && message[j+26] =='\r')
                {
                    Id = message.Substring(j+1, 8);
                    for (int i = 0; i<8; i++)
                    {
                        string tmp = message.Substring(j + 2 + 8 + i * 2, 2);
                        RxData[i] = byte.Parse(tmp, System.Globalization.NumberStyles.HexNumber);
                    }
                    Parent.ParseMessage();
                    j+=26;
                }
                else if ((message[j] == 'Z' || message[j] == 'z')&& message[j+1] =='\r')
                {
                    j+=1;
                    SuccessTransmission_f = true;
                }
            }
        }

        public void Connect()
        {
            Thread.Sleep(10);
            SendApiBaudrate((uint)Port.ComPort.BaudRate);
            Thread.Sleep(10);
            SendApiAcceptCode(0);
            Thread.Sleep(10);
            SendApiOpen();
        }
        public void Disconnect() {
            SendApiClose();
        }

        public void SendApiOpen() => Port.ComPort.Write("O" + "\r");

        public void SendApiClose() => Port.ComPort.Write("C" + "\r");

        public void SendApiAcceptCode(uint code) => Port.ComPort.Write($"M{code:X08}\r");

        public void SendMessage(string _id, byte[] _data)
        {
            try
            {
                SuccessTransmission_f = false;
                Port.ComPort.Write("T"+_id+Convert.ToString(_data.Length)+Convert.ToHexString(_data)+ "\r");
            }
            catch (Exception)
            {
                //ToDo
            }
        }
        public void GetVersionAPI()
        {

        }
        public void SendApiBaudrate(uint _baudrate) => Port.ComPort.Write("S"+Convert.ToString(_baudrate)+"\r");

        public void SetBaudrate(uint _baudrate)
        {
            if (Port.ComPort.IsOpen)
            {
                SendApiClose();
                Thread.Sleep(10);
                SendApiBaudrate(_baudrate);
                Thread.Sleep(10);
                Port.ComPort.BaudRate = (int)_baudrate;
                Thread.Sleep(10);
                SendApiOpen();
            }
        }

        public bool TransmissionSuccessed()
        {
            return SuccessTransmission_f;
        }
    }
}
