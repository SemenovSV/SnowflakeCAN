using System;
using System.Collections.Generic;
using System.Text;
using System.IO.Ports;
using System.Timers;
using System.Linq;
using System.Threading.Tasks;
using System.Threading;
using SFC.Commands;
using System.Windows.Input;



namespace SFC.Models
{
    public class ComPortModel
    {
        public ICommand ParseMessage { get; }

        public SerialPort ComPort = new SerialPort();

        public const uint BufSize = 2048;
        public uint WritePtr = 0;
        public uint ReadPtr = 0;
        public Byte[] Buf = new Byte[BufSize];

        public ComPortModel()
        {
            ComPort.BaudRate = 250000;
            ComPort.Parity = Parity.None;
            ComPort.StopBits = StopBits.One;
            ComPort.DataBits = 8;
        }

        public void Recieve()
        {
            while (ComPort.BytesToRead>0)
            {
                Buf[WritePtr++] = RecvUInt8(ComPort);//write to circular buffer
                if (WritePtr>=BufSize) WritePtr = 0;
            }
        }

        private byte RecvUInt8(SerialPort serial)
        {
            byte[] rxbuffer = new byte[1];
            int got = 0;
            while (got < rxbuffer.Length)
                got += serial.Read(rxbuffer, got, rxbuffer.Length - got);

            return rxbuffer[0];
        }
    }
}
