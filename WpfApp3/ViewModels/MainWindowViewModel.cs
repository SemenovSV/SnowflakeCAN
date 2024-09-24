using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.IO.Ports;
using System.IO;
using System.Windows.Input;
using System.Windows.Media;
using Microsoft.Win32;
using System.Diagnostics;

using SFC.Commands;
using SFC.Models;
using SFC.Services;
using System.Windows.Data;
using System.Windows.Controls;
using System.IO.Packaging;
using System.Xml.Linq;
using System.Collections.ObjectModel;
using static SFC.ViewModels.MainWindowViewModel;


namespace SFC.ViewModels
{
    public class MainWindowViewModel : ViewModel, IDisposable
    {

        //public ProtocolUDS UDS;
        public J1939_GAZ Protocol;
        #region Font
        private ushort _TxtFontSize = 18;
        public ushort TxtFontSize
        { set => Set(ref _TxtFontSize, value); get => _TxtFontSize; }
        #endregion


        #region ComPort

        private bool _ChangePortEnable = true;
        public bool ChangePortEnable
        { set => Set(ref _ChangePortEnable, value); get => _ChangePortEnable; }

        private bool _CommunicationEnable = false;
        public bool CommunicationEnable
        { set => Set(ref _CommunicationEnable, value); get => _CommunicationEnable; }

        private List<int> _BaudrateList = new List<int>() {20000, 50000, 100000, 125000, 250000, 500000, 800000, 1000000};
        public List<int> BaudrateList
        { set => Set(ref _BaudrateList, value); get => _BaudrateList; }

        private int _BaudrateIndex = 5;
        public int BaudrateIndex
        { set => Set(ref _BaudrateIndex, value); get => _BaudrateIndex; }

        public ICommand SetBaudrateCommand { get; }
        private void OnSetBaudrateCommandExecuted(object parameter)
        {
            Protocol.Adapter.SetBaudrate((uint)BaudrateList[BaudrateIndex]);
        }

        private bool CanSetBaudrateCommandExecute(object parameter)
        {
            return true;
        }

        private List<string> _PortList = SerialPort.GetPortNames().ToList();
        public List<string> PortList
        { set => Set(ref _PortList, value); get => _PortList; }

        private string _PortName;
        public string PortName { get => _PortName; set { Set(ref _PortName, value); Protocol.Adapter.Port.ComPort.PortName = value; } }

        private string _butportContent = "Открыть";
        public string ButPortContent
        { set => Set(ref _butportContent, value); get => _butportContent; }

        private SolidColorBrush _butportForeground = Brushes.Green;
        public SolidColorBrush ButPortForeground
        { set => Set(ref _butportForeground, value); get => _butportForeground; }


        public ICommand OpenPortCommand { get; }
        private void OnOpenPortCommandExecuted(object parameter)
        {
            try
            {
                if (Protocol.Adapter.Port.ComPort.IsOpen == true)
                {
                    Protocol.Adapter.Disconnect();
                    Protocol.Adapter.Port.ComPort.Close();
                }
                else
                {
                    Protocol.Adapter.Port.ComPort.Open();
                    Protocol.Adapter.Connect();

                    Protocol.StartProcessReqHtr();
                }
                Thread.Sleep(500);

                if (Protocol.Adapter.Port.ComPort.IsOpen == true)
                {
                    ButPortContent = "Закрыть";
                    ButPortForeground = Brushes.Red;
                    ChangePortEnable = false;
                }
                else
                {
                    ButPortContent = "Открыть";
                    ButPortForeground = Brushes.Green;
                    ChangePortEnable = true;
                }
                CommunicationEnable = !ChangePortEnable;
            }
            catch (Exception)
            {
                MessageBox.Show("Проверьте подключение или освободите порт");
                return;
            }
        }

        private bool CanOpenPortCommandExecute(object parameter)
        {
            return true;
        }

        public ICommand RefreshPortsCommand { get; }
        private void OnRefreshPortsCommandExecuted(object parameter)
        {
            PortList = SerialPort.GetPortNames().ToList<string>();
        }

        private bool CanRefreshPortsCommandExecute(object parameter)
        {
            return true;
        }

        public void DataReceivedHandler(object sender, SerialDataReceivedEventArgs args)
        {
            Protocol.Adapter.Port.Recieve();
            Protocol.Adapter.ReadBuffer();



            Protocol.ParseMessage();
        }
        #endregion

        #region FilePath

        private string _FilePath = ".../...";
        public string FilePath
        { set => Set(ref _FilePath, value); get => _FilePath; }

        public ICommand ReadFileCommand { get; }
        private void OnReadFileCommandExecuted(object parameter)
        {
            OpenFileDialog dialog = new OpenFileDialog();
            dialog.DefaultExt = ".hex";
            dialog.Filter = "HEX file|*.hex";
            dialog.ShowDialog();
            FilePath = dialog.FileName;

            if (FilePath != null && FilePath.Length > 0)
            {
                Protocol.UDS.Hex.LoadHexFile(FilePath);
                Protocol.UDS.Hex.insertCRC16(false);
                ConsoleContent += "Версия выбранной прошивки: ";
                ConsoleContent += Protocol.UDS.Hex.Version[0] + "." + Protocol.UDS.Hex.Version[1] + "." + Protocol.UDS.Hex.Version[2] + "." + Protocol.UDS.Hex.Version[3] + "\r";
                ConsoleContent += "Дата создания: ";
                ConsoleContent += Protocol.UDS.Hex.Date[0] + "." + Protocol.UDS.Hex.Date[1] + ".20" +Protocol.UDS.Hex.Date[2]+"\r";
                Protocol.UDS.SetReceiverId(Protocol.UDS.Hex.Version);
            }
        }

        private bool CanReadFileCommandExecute(object parameter) => true;

        #endregion

        #region LoadFirmwareContent

        private bool _EnableAction = true;
        public bool EnableAction
        { set => Set(ref _EnableAction, value); get => _EnableAction; }

        private ushort _LoadProgress = 0;
        public ushort LoadProgress
        { set => Set(ref _LoadProgress, value); get => _LoadProgress; }

        private uint _LoadTimeS = 0;
        public uint LoadTimeS
        { set => Set(ref _LoadTimeS, value); get => _LoadTimeS; }

        private string _LoadFirmwareContent = "Загрузить";
        public string LoadFirmwareContent
        { set => Set(ref _LoadFirmwareContent, value); get => _LoadFirmwareContent; }

        public ICommand LoadFirmwareCommand { get; }
        private void OnLoadFirmwareCommandExecuted(object parameter)
        {
            ConsoleContent = "";
            ConsoleContent += "Версия выбранной прошивки: ";
            ConsoleContent += Protocol.UDS.Hex.Version[0] + "." + Protocol.UDS.Hex.Version[1] + "." + Protocol.UDS.Hex.Version[2] + "." + Protocol.UDS.Hex.Version[3] + "\r";
            ConsoleContent += "Дата создания: ";
            ConsoleContent += Protocol.UDS.Hex.Date[0] + "." + Protocol.UDS.Hex.Date[1] + ".20" + Protocol.UDS.Hex.Date[2]+ "\r";
            EnableAction = false;
            Protocol.UDS.StartPricessLoading();
        }

        private bool CanLoadFirmwareCommandExecute(object parameter) => true;

        #endregion


        #region  ConsoleContent
        private string _ConsoleContent = "";
        public string ConsoleContent
        { set => Set(ref _ConsoleContent, value); get => _ConsoleContent; }
        #endregion

        #region Console
        private void AddMessageToConsole(short state)
        {
            /*switch (state)
            {
                case ProtocolUDS.WAITING:
                    ConsoleContent = "";
                    break;
                case ProtocolUDS.ACCESSING:
                    ConsoleContent+="Получение доступа";
                    break;
                case ProtocolUDS.SWITCH_TO_BOOT:
                    ConsoleContent+="Переход в программу загрузчика";
                    break;
                case ProtocolUDS.START_LOADING:
                    ConsoleContent+="Старт загрузки";
                    break;
                case ProtocolUDS.PROCESS_LOADING:
                    ConsoleContent+="Загрузка";
                    break;
                case ProtocolUDS.END_LOADING:
                    ConsoleContent+="Завершение загрузки";
                    break;
                case ProtocolUDS.CHECK_CRC:
                    ConsoleContent+="Проверка контрольной суммы";
                    break;
                case ProtocolUDS.RUN_MAIN_PROGRAM:
                    ConsoleContent+="Запуск основной программы";
                    break;
                case ProtocolUDS.BAD_ACCESS:
                    ConsoleContent+="";
                    break;
                case ProtocolUDS.BAD_SWITCH_TO_BOOT:
                    ConsoleContent+="";
                    break;
                case ProtocolUDS.BAD_START_LOADING:
                    ConsoleContent+="";
                    break;
                case ProtocolUDS.BAD_PROCESS_LOADING:
                    ConsoleContent+="";
                    break;
                case ProtocolUDS.BAD_END_LOADING:
                    ConsoleContent+="";
                    break;
                case ProtocolUDS.BAD_CRC:
                    ConsoleContent+="";
                    break;
                case ProtocolUDS.BAD_MAIN_PROGRAM:
                    ConsoleContent+="";
                    break;
            }*/
            ConsoleContent+="\r";
        }
        #endregion

        #region Control
        private bool _RegularReqHTR =true;
        public bool RegularReqHTR
        { set => Set(ref _RegularReqHTR, value); get => _RegularReqHTR; }

        public ICommand RegularReqHTRCommand { get; }
        private void OnRegularReqHTRCommandExecuted(object parameter)
        {
            if(RegularReqHTR == true)
            {
                Protocol.StartProcessReqHtr();
            }
        }

        private bool CanRegularReqHTRCommandExecute(object parameter)
        {
            return true;
        }

        private bool _RegularReqUDS = false;
        public bool RegularReqUDS
        { set => Set(ref _RegularReqUDS, value); get => _RegularReqUDS; }

        public ICommand RegularReqUDSCommand { get; }
        private void OnRegularReqUDSCommandExecuted(object parameter)
        {
            if(RegularReqUDS == true)
            {
                Protocol.StartProcessReqUds();
            }
        }

        private bool CanRegularReqUDSCommandExecute(object parameter)
        {
            return true;
        }

        private List<string> _WorkModeList = new List<string>()
        {
            "(0)Выключен", "(2)Экономичный", "(3)Подогрев", "(4)Помпа", "(10)Догреватель", "(11)Дополнителный"
        };
        public List<string> WorkModeList
        { set => Set(ref _WorkModeList, value); get => _WorkModeList; }

        private int _ModeIndex = 0;
        public int ModeIndex
        { set => Set(ref _ModeIndex, value); get => _ModeIndex; }

        private int _WorkMode = 0;
        public int WorkMode
        { set => Set(ref _WorkMode, value); get => ConvertIndexToMode(ModeIndex); }

        private int ConvertIndexToMode(int ind)
        {
            switch (ind)
            {
                case 1: return 2;
                case 2: return 3;
                case 3: return 4;
                case 4: return 10;
                case 5: return 11;
            }
            return 0;
        }
        private int _Tsetpoint = 85;
        public int Tsetpoint
        { set => Set(ref _Tsetpoint, value); get => _Tsetpoint; }

        private int _WorkTime = 10;
        public int WorkTime
        { set => Set(ref _WorkTime, value); get => _WorkTime; }

        #endregion

        #region J1939_Grid

        public class Param : ObservableObject
        {
            public Param(string _name)
            {
                _Name = _name;
                _Value = "";
            }
            public Param(string _name, string _value)
            {
                _Name = _name;
                _Value = _value;
            }
            private string _Name;
            public string Name { get => _Name; set { _Name = value; RaisePropertyChangedEvent(nameof(Name)); } }

            private string _Value;
            public string Value { get => _Value; set { _Value = value; RaisePropertyChangedEvent(nameof(Value)); } }
        }

        private ObservableCollection<Param> _ParamsJ1939 = new ObservableCollection<Param>()
        {
            new Param("Т жидкости"),
            new Param("Выходная мощн."),
            new Param("Режим работы"),
            new Param("Оставшееся время")
        };
        public ObservableCollection<Param> ParamsJ1939
        { set => Set(ref _ParamsJ1939, value); get => _ParamsJ1939; }


        private ObservableCollection<Param> _ParamsUDS = new ObservableCollection<Param>()
        {
            new Param("Стадия "),
            new Param("Режим "),
            new Param("Время работы"),
            new Param("Время режима"),
            new Param("Обороты зад"),
            new Param("Обороты изм"),
            new Param("Т перегрева"),
            new Param("Искра"),
            new Param("Клапан"),
            new Param("ТЭН"),
            new Param("Индикатор пламени"),
            new Param("Помпа"),
        };
        public ObservableCollection<Param> ParamsUDS
        { set => Set(ref _ParamsUDS, value); get => _ParamsUDS; }

        private ObservableCollection<Param> _ParamsDM1 = new ObservableCollection<Param>()
        {
            new Param("Сигнализатор"),
            new Param("SPN"),
            new Param("FMI"),
            new Param("Кол-во повторений"),
        };
        public ObservableCollection<Param> ParamsDM1
        { set => Set(ref _ParamsDM1, value); get => _ParamsDM1; }


        private ObservableCollection<Param> _ParamsVEP = new ObservableCollection<Param>()
        {
            new Param("Потенциал АКБ")
        };
        public ObservableCollection<Param> ParamsVEP
        { set => Set(ref _ParamsVEP, value); get => _ParamsVEP; }


        #endregion

        #region FooterState
        private string _FooterState = "";
        public string FooterState
        { set => Set(ref _FooterState, value); get => _FooterState; }

        static uint s_stage = 255;
        static uint s_mode = 255;
        static uint s_faultCode = 255;
        public void SetFooterState(uint _stage, uint _mode, uint _faultCode)
        {
            if (_stage != 255) s_stage = _stage;
            if (_mode != 255) s_mode = _mode;
            if (_faultCode != 255) s_faultCode =_faultCode;

            if (s_faultCode == 0)
            {
                if(s_stage == 0)
                {
                    if(s_mode == 1)
                    {
                        FooterState = "Ожидание команды (0:1)";
                    }
                    else if (s_mode == 5)
                    {
                        FooterState = "Сохранение данных о пуске (0:5)";
                    }
                    else if (s_mode == 6)
                    {
                        FooterState = "ДОПОГ-блокировка (0:6)";
                    }
                }
                if (s_stage == 1)
                {
                    if (s_mode == 0)
                    {
                        FooterState = "Стартовая диагностика (1:0)";
                    }
                    else if (s_mode == 2)
                    {
                        FooterState = "Ожидание снижения температуры (1:2)";
                    }
                }
                if (s_stage == 2)
                {
                    if (s_mode == 1)
                    {
                        FooterState = "Розжиг 1 (2:1)";
                    }
                    else if (s_mode == 3)
                    {
                        FooterState = "Розжиг 2 (2:3)";
                    }
                    else if (s_mode == 7)
                    {
                        FooterState = "Подготовка к розжигу (2:7)";
                    }
                    else if (s_mode == 8)
                    {
                        FooterState = "Альтернативный розжиг 1 (2:8)";
                    }
                    else if (s_mode == 9)
                    {
                        FooterState = "Альтернативный розжиг 2 (2:9)";
                    }
                }
                if (s_stage == 3)
                {
                    if (s_mode == 59)
                    {
                        FooterState = "Нагрев на максимальной ступени (3:59)";
                    }
                    else if (s_mode == 28)
                    {
                        FooterState = "Продувка перед ждущим (3:28)";
                    }
                    else if (s_mode == 22)
                    {
                        FooterState = "Продувка после срыва пламени (3:22)";
                    }
                    else if (s_mode == 30)
                    {
                        FooterState = "Помпа (3:30)";
                    }
                }
                if (s_stage == 4)
                {
                    if (s_mode == 0)
                    {
                        FooterState = "Нормальная продувка (4:0)";
                    }
                    else if (s_mode == 1)
                    {
                        FooterState = "Продувка при перегреве (4:1)";
                    }
                    else if (s_mode == 6)
                    {
                        FooterState = "Продувка по ДОПОГ (4:6)";
                    }
                }
            }
            else
            {
                FooterState = "Неисправность: ("+s_faultCode+") ";
                switch (s_faultCode)
                {
                    case 1:
                    case 2:
                        FooterState += "перегрев";
                        break;      
                    case 3:         
                        FooterState += "датчик перегрева";
                        break;      
                    case 4:         
                        FooterState += "датчик температуры";
                        break;      
                    case 13:        
                        FooterState += "нерозжиг";
                        break;      
                    case 10:        
                        FooterState += "несоответствие оборотов";
                        break;      
                    case 27:        
                        FooterState += "нет вращения";
                        break;      
                    case 28:        
                        FooterState += "самовращение";
                        break;      
                    case 9:         
                        FooterState += "блок искрового розжига";
                        break;      
                    case 14:        
                        FooterState += "помпа";
                        break;      
                    case 17:        
                        FooterState += "топливный клапан";
                        break;      
                    case 12:        
                        FooterState += "повышенное напряжение";
                        break;      
                    case 15:        
                        FooterState += "пониженное напряжение";
                        break;      
                    case 22:        
                        FooterState += "пламя есть до розжига";
                        break;      
                    case 24:        
                        FooterState += "пламя есть после продувки";
                        break;      
                    case 29:        
                        FooterState += "КЗ ТЭН";
                        break;      
                    case 37:        
                        FooterState += "блокировка при нерозжиге";
                        break;      
                    case 90:        
                        FooterState += "перегрузка по току";
                        break;
                }
            }
        }
        #endregion

        public MainWindowViewModel()
        {
            Protocol = new J1939_GAZ(this);

            Protocol.Adapter.Port.ComPort.DataReceived += new SerialDataReceivedEventHandler(DataReceivedHandler); // Add DataReceived Event Handler
            OpenPortCommand = new LambdaCommand(OnOpenPortCommandExecuted, CanOpenPortCommandExecute);
            SetBaudrateCommand = new LambdaCommand(OnSetBaudrateCommandExecuted, CanSetBaudrateCommandExecute);
            ReadFileCommand = new LambdaCommand(OnReadFileCommandExecuted, CanReadFileCommandExecute);
            RefreshPortsCommand = new LambdaCommand(OnRefreshPortsCommandExecuted, CanRefreshPortsCommandExecute);
            LoadFirmwareCommand = new LambdaCommand(OnLoadFirmwareCommandExecuted, CanLoadFirmwareCommandExecute);
            RegularReqHTRCommand = new LambdaCommand(OnRegularReqHTRCommandExecuted, CanRegularReqHTRCommandExecute);
            RegularReqUDSCommand = new LambdaCommand(OnRegularReqUDSCommandExecuted, CanRegularReqUDSCommandExecute);

            Protocol.UDS.LoadProgress = new Progress<ushort>(status => LoadProgress = status);
            Protocol.UDS.LoadTimeS = new Progress<uint>(status => LoadTimeS = status);

            //Protocol.StateProcess = new Progress<short>(status => AddMessageToConsole(status));
        }
    }


}
