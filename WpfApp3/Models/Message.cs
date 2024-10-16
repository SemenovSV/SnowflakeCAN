using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using static MaterialDesignThemes.Wpf.Theme.ToolBar;

namespace SFC.Models
{
    public class Message : ObservableObject
    {
        public Message()
        {
            _ID = "";
            _Name = "";
            _Cnt = 1;
            _D1 = 255;
            _D2 = 255;
            _D3 = 255;
            _D4 = 255;
            _D5 = 255;
            _D6 = 255;
            _D7 = 255;
            _D8 = 255;
        }
        public Message(string _name)
        {
            _Name = _name;
        }
        public Message(string _id, byte[] _data, string _name)
        {
            _ID = _id;
            _Name = _name;
            _Cnt = 1;

            _D1 = _data[0];
            _D2 = _data[1];
            _D3 = _data[2];
            _D4 = _data[3];
            _D5 = _data[4];
            _D6 = _data[5];
            _D7 = _data[6];
            _D8 = _data[7];
        }
        private string _Name;
        public string Name { get => _Name; set { _Name = value; RaisePropertyChangedEvent(nameof(Name)); } }

        private uint _Cnt;
        public uint Cnt { get => _Cnt; set { _Cnt = value; RaisePropertyChangedEvent(nameof(Cnt)); } }

        private string _ID;
        public string ID { get => _ID; set { _ID = value; RaisePropertyChangedEvent(nameof(ID)); } }

        private byte _D1;
        public byte D1 { get => _D1; set { _D1 = value; RaisePropertyChangedEvent(nameof(D1)); } }
        private byte _D2;
        public byte D2 { get => _D2; set { _D2 = value; RaisePropertyChangedEvent(nameof(D2)); } }
        private byte _D3;
        public byte D3 { get => _D3; set { _D3 = value; RaisePropertyChangedEvent(nameof(D3)); } }
        private byte _D4;
        public byte D4 { get => _D4; set { _D4 = value; RaisePropertyChangedEvent(nameof(D4)); } }
        private byte _D5;
        public byte D5 { get => _D5; set { _D5 = value; RaisePropertyChangedEvent(nameof(D5)); } }
        private byte _D6;
        public byte D6 { get => _D6; set { _D6 = value; RaisePropertyChangedEvent(nameof(D6)); } }
        private byte _D7;
        public byte D7 { get => _D7; set { _D7 = value; RaisePropertyChangedEvent(nameof(D7)); } }
        private byte _D8;
        public byte D8 { get => _D8; set { _D8 = value; RaisePropertyChangedEvent(nameof(D8)); } }

        private string _Descriptor;
        public string Descriptor { get => _Descriptor; set { _Descriptor = value; RaisePropertyChangedEvent(nameof(Descriptor)); } }
    }
}
