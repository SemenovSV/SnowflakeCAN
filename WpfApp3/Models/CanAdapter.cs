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

        public byte[] MessageIn = new byte[MessageMaxLength];
        public byte[] MessageOut = new byte[11];

        public ushort MessageCnt;
        private const int MessageMaxLength = 32;

        private bool SuccessTransmission_f = false;


        public string Id = "";
        public byte[] RxData = new byte[8];
        public bool ProcessPacket_f = false;


        public void ReadBuffer()
        {
            while (Port.ReadPtr!=Port.WritePtr)
            {

                MessageIn[MessageCnt++] = Port.Buf[Port.ReadPtr];

                if (Port.Buf[Port.ReadPtr] is 7 or 13) //end of packet
                {
                    ParseMessage();
                    MessageCnt = 0;
                }
                if(Port.Buf[Port.ReadPtr] == 0) MessageCnt = 0;

                if (++Port.ReadPtr>=ComPortModel.BufSize) Port.ReadPtr = 0;
            }
        }

        public void ParseMessage()
        {
            string message = Encoding.Default.GetString(MessageIn);
            if(MessageIn[0] == 'T')
            {
                Id = message.Substring(1, 8);
                
                byte length = (byte)(Convert.ToByte(MessageIn[9])-48);

                for (int i = 0; i<8; i++)
                {
                    string tmp = message.Substring(2 + 8 + i * 2, 2);
                    RxData[i] = byte.Parse(tmp, System.Globalization.NumberStyles.HexNumber);
                    ProcessPacket_f = true;
                }
            }
            else if(MessageIn[0] == 'Z' || MessageIn[0] == 'z')
            {
                SuccessTransmission_f = true;
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
