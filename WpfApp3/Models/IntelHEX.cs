using System;
using System.Collections.Generic;
using System.IO;
//using System.Linq;
//using System.Runtime.Intrinsics.Arm;
//using System.Text;
//using System.Threading.Tasks;
//using System.Windows.Media;
//using static System.Net.Mime.MediaTypeNames;

namespace SFC
{
    
    public class IntelHEX
    {
        public const int FragmentSize = 256; // y=7x+4, x - целое число
        List<CodeFragment> fragments = new List<CodeFragment>();
        private int ProgrammStartAdress = 0;
        public byte[] Version;
        public byte[] Date;

        public IntelHEX() {
            fragments.Clear();
            Version = new byte[5];
            Date = new byte[3];
        }

        internal class CodeFragment
        {
            public CodeFragment(int len)
            {
                Data = new byte[len];
                StartAdress = 0;
                Length = 0;
                for (int i = 0; i < len; i++) Data[i] = 0xff;
            }
            public int StartAdress;
            public int Length;
            public byte[] Data;

        }

        public void LoadHexFile(string path)
        { // расшифровка hex файла и перевод его в список bin кусков
            int pageAdress = 0;
            int localAdress = 0;

            if (path != null && path.Length > 0)
                using (StreamReader sr = new StreamReader(path))
                {
                    CodeFragment fragment = new CodeFragment(FragmentSize);
                    fragments.Clear();
                    bool endf = false;
                    while (!sr.EndOfStream || !endf)
                    {
                        string line = sr.ReadLine().Substring(1);
                        byte[] bytes = new byte[25];
                        for (int i = 0; i < line.Length / 2; i++)
                            bytes[i] = Convert.ToByte(line.Substring(i * 2, 2), 16);
                        int recordLen = bytes[0];
                        int recType = (int)bytes[3];

                        switch (recType)
                        {
                            case 0:
                                localAdress = (int)(bytes[1] * 256 + bytes[2]);

                                if (fragment.StartAdress == 0) fragment.StartAdress = pageAdress + localAdress;
                                int subAdr = localAdress + pageAdress - fragment.StartAdress;
                                if (subAdr > fragment.Length) // если скачек по адресам, то сохраняем сегмент и создаем новый
                                {
                                    fragments.Add(fragment);
                                    fragment = new CodeFragment(FragmentSize);
                                    fragment.StartAdress = pageAdress + localAdress;
                                }
                                for (int i = 0; i < recordLen; i++)
                                {
                                    fragment.Data[fragment.Length++] = bytes[i + 4];
                                    if (fragment.Length == FragmentSize)
                                    {
                                        fragments.Add(fragment);
                                        fragment = new CodeFragment(FragmentSize);
                                    }
                                }
                                break;
                            case 4:
                                if (fragment.Length > 0)
                                {
                                    fragments.Add(fragment);
                                    fragment = new CodeFragment(FragmentSize);
                                }
                                pageAdress = (int)((int)bytes[4] * 256 + bytes[5]) << 16;
                                break;
                            case 1:
                                if (fragment.Length > 0)
                                    fragments.Add(fragment);
                                endf = true;
                                break;

                        }
                    }
                    if (fragments != null)
                    {
                        ProgrammStartAdress = fragments[0].StartAdress;
                    }
                    searchVersion();

                }

        }

        public bool OffsetProgSubtract(int offset)
        {
            foreach (CodeFragment fragment in fragments)
            {
                if (fragment.StartAdress >= offset) fragment.StartAdress -= offset;
                else return false;
            }
            return true;
        }

        public void searchVersion()
        {
            for (int j = 0; j < 5; j++)
            {
                Version[j] = 0;
            }
            for (int N = 0; N < fragments.Count; N++)
            {
                CodeFragment fragment = fragments[N];
                for (int i=0; i < fragment.Length; i=i+4)
                {
                    if ((fragment.Data[i+0] == (byte)0x56) && (fragment.Data[i + 1] == (byte)0x44) && (fragment.Data[i + 2] == (byte)0x43)) // "VDC"
                    {
                        if (i <= (fragment.Length-12))
                        {
                            for (int j = 0; j < 5; j++)
                            {
                                Version[j] = fragment.Data[j + 8+i];
                            }
                        } else
                        {
                            int stdi = fragment.Length - i + 8;
                            fragment = fragments[N+1];
                            for (int j = 0; j < 5; j++)
                            {
                                Version[j] = fragment.Data[j + stdi];
                            }
                        }
                    }
                }
                if (fragment.StartAdress == 0x0801c000)
                {
                    for (int j = 0; j < 4; j++)
                    {
                        Version[j] = fragment.Data[j + 13];
                    }
                    for (int j = 0; j < 3; j++)
                    {
                        Date[j] = fragment.Data[j + 10];
                    }
                    break;
                }
            }
        }
        //-------------------
        public void insertCRC16()
        {
            int len = 0;
            int CRCfragmentNumb = 0;

            uint CRC = 0xffff;
            len = 0;
            byte Bt = 0xFF;
            for (int N = 0; N < fragments.Count; N++)
            {
                CodeFragment fragment = fragments[N];
                for (int i = 0; i < fragment.Length; i++)
                {
                    if (fragment.StartAdress == 0x0801C000 && i >= 0  && i <= 5)
                    {
                        if (CRCfragmentNumb == 0) CRCfragmentNumb = N;
                        Bt = 0xFF;
                    }
                    else
                    {
                        Bt = fragment.Data[i];
                    }

                    calcCrc(ref CRC, Bt);
                    len++;
                }

            }
            fragments[CRCfragmentNumb].Data[0] = (byte)((len >> 0) & 0xff);
            fragments[CRCfragmentNumb].Data[1] = (byte)((len >> 8) & 0xff);
            fragments[CRCfragmentNumb].Data[2] = (byte)((len >> 16) & 0xff);
            fragments[CRCfragmentNumb].Data[3] = (byte)((len >> 24) & 0xff);
            fragments[CRCfragmentNumb].Data[4] = (byte)((CRC >> 0) & 0xff);
            fragments[CRCfragmentNumb].Data[5] = (byte)((CRC >> 8) & 0xff);
        }
        //-------------------------------------------
        private void calcCrc(ref uint CRC, byte val)
        {
            uint bt = 0;
            uint nextByte = (uint)val;
            for (int j = 0; j < 8; j++)
            {
                bt = CRC & 1;
                CRC = CRC >> 1;
                if ((nextByte & 1) != bt) CRC = CRC ^ 0xA001;
                nextByte = nextByte >> 1;
            }
        }

        public byte[] getFragmentData(int index)
        {
            if (index >= fragments.Count) return null;
            return fragments[index].Data;
        }
        public int getFragmentAdr(int index)
        {
            if (index >= fragments.Count) return 0;
            return fragments[index].StartAdress;
        }
        public int getFragmentSize(int index)
        {
            if (index >= fragments.Count) return 0;
            return fragments[index].Length;
        }
        public int getFragmentsNumb()
        {
            return fragments.Count;
        }
        public int getProgrammStartAdress()
        {
            return ProgrammStartAdress;
        }
    }
}
