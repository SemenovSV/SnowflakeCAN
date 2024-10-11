using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;

namespace SFC.Models
{
    public class Message : ObservableObject
    {
        public Message()
        {
            _ID = "";
            _Name = "";
            for (var item = 0; item<8; item++)
            {
                _D[item] = 255;
            }
        }
        public Message(string _name)
        {
            _Name = _name;
        }
        public Message(string _id, byte[] _data, string _name)
        {
            _ID = _id;
            _Name = _name;
            for (var item = 0; item<8; item++)
            {
                _D[item] = _data[item];
            }
        }
        private string _Name;
        public string Name { get => _Name; set { _Name = value; RaisePropertyChangedEvent(nameof(Name)); } }

        private uint _Cnt;
        public uint Cnt { get => _Cnt; set { _Cnt = value; RaisePropertyChangedEvent(nameof(Cnt)); } }

        private string _ID;
        public string ID { get => _ID; set { _ID = value; RaisePropertyChangedEvent(nameof(ID)); } }

        private byte[] _D = new byte[8];
        public byte[] D { get => _D; set { _D = value; RaisePropertyChangedEvent(nameof(D)); } }

        private string _Descriptor;
        public string Descriptor { get => _Descriptor; set { _Descriptor = value; RaisePropertyChangedEvent(nameof(Descriptor)); } }
    }
}
