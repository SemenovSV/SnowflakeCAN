using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO.Ports;
using System.Threading;
using System.Windows.Markup;
using System.ComponentModel;
using System.Windows;

namespace WpfApp3.Model
{
    public class ComPortModel
    {

        private const uint bufSize = 8*1024;
        public SerialPort ComPort = new SerialPort();
        public byte[] Buf = new byte[bufSize];
        private uint bufPtr = 0;
        private SynchronizationContext UIContext;

        public ComPortModel() {
            ComPort.BaudRate = 2400;
            ComPort.Parity = Parity.None;
            ComPort.StopBits = StopBits.One;
            ComPort.DataBits = 8;
            ComPort.DataReceived += new SerialDataReceivedEventHandler(DataReceivedHandler);
 
            UIContext = SynchronizationContext.Current;
        }

        public void SetName(String name)
        {
            bool wasOpen = false;
            if (ComPort.IsOpen)
            {
                ComPort.Close();
                wasOpen = true;
            }
            ComPort.PortName = name;
            if (wasOpen)
            {
                ComPort.Open();
            }

        }
        public String GetName() { return ComPort.PortName; }

        public void SetBaudrate(int bdrt)
        {
            bool wasOpen = false;
            if (ComPort.IsOpen)
            {
                ComPort.Close();
                wasOpen = true;
            }
            ComPort.BaudRate = bdrt;
            if (wasOpen)
            {
                ComPort.Open();
            }
        }
        public int GetBaudrate() { return ComPort.BaudRate; }

        public List<string> GetComportList()
        {
            return new List<string>(SerialPort.GetPortNames());
        }
        public void Open()
        {
            if ((ComPort.PortName != null) && (ComPort.PortName.StartsWith("COM"))) {
                try {
                    ComPort.Open();
                    //bufPtr = 0;
                } catch (Exception ex) { }
                
            }
        
        }
        public void Close()
        {
            try {            
                if (ComPort.IsOpen)
                    ComPort.Close();
            } catch (Exception ex) { }
        }

        public bool isOpen()
        {
            return ComPort.IsOpen;
        }

        //---------------------------------------------------------------------------------
        public void Transmit(byte[] data, int size)
        {
            if (ComPort.IsOpen)
            {
                try
                {
                    ComPort.Write(data, 0, (int)size);
                }
                catch (Exception ex)
                {
                    ComPort.Close();
                    MessageBox.Show(ex.Message);
                }
            }
        }
        public uint GetBufferPointer()
        {
            return bufPtr;
        }
        public byte GetBufferByte(uint indx)
        {
            return Buf[indx];
        }
        public uint GetBufferSize()
        {
            return bufSize;
        }

    }
}
