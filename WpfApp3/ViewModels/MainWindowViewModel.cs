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

        public ProtocolUDS UDS;
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
                UDS.Hex.LoadHexFile(FilePath);
                UDS.Hex.insertCRC16(false);
                ConsoleContent += "Версия выбранной прошивки: ";
                ConsoleContent += UDS.Hex.Version[0] + "." + UDS.Hex.Version[1] + "." + UDS.Hex.Version[2] + "." + UDS.Hex.Version[3] + "\r";
                ConsoleContent += "Дата создания: ";
                ConsoleContent += UDS.Hex.Date[0] + "." + UDS.Hex.Date[1] + ".20" + UDS.Hex.Date[2]+"\r";
                UDS.SetReceiverId(UDS.Hex.Version);
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
            ConsoleContent += UDS.Hex.Version[0] + "." + UDS.Hex.Version[1] + "." + UDS.Hex.Version[2] + "." + UDS.Hex.Version[3] + "\r";
            ConsoleContent += "Дата создания: ";
            ConsoleContent += UDS.Hex.Date[0] + "." + UDS.Hex.Date[1] + ".20" + UDS.Hex.Date[2]+ "\r";
            EnableAction = false;
            UDS.StartPricessLoading();
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
            switch (state)
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
            }
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
            Protocol.StartProcessReqUds();
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

        public class Param
        {
            public Param(string _name)
            {
                Name = _name;
                Value = "";
            }
            public Param(string _name, string _value)
            {
                Name = _name;
                Value = _value;
            }
            public string Name { get; set; }

            public string Value { get; set; }
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


        #endregion

        public MainWindowViewModel()
        {


            UDS = new ProtocolUDS(this);
            Protocol = new J1939_GAZ(this);

            Protocol.Adapter.Port.ComPort.DataReceived += new SerialDataReceivedEventHandler(DataReceivedHandler); // Add DataReceived Event Handler
            OpenPortCommand = new LambdaCommand(OnOpenPortCommandExecuted, CanOpenPortCommandExecute);
            SetBaudrateCommand = new LambdaCommand(OnSetBaudrateCommandExecuted, CanSetBaudrateCommandExecute);
            ReadFileCommand = new LambdaCommand(OnReadFileCommandExecuted, CanReadFileCommandExecute);
            RefreshPortsCommand = new LambdaCommand(OnRefreshPortsCommandExecuted, CanRefreshPortsCommandExecute);
            LoadFirmwareCommand = new LambdaCommand(OnLoadFirmwareCommandExecuted, CanLoadFirmwareCommandExecute);
            RegularReqHTRCommand = new LambdaCommand(OnRegularReqHTRCommandExecuted, CanRegularReqHTRCommandExecute);
            RegularReqUDSCommand = new LambdaCommand(OnRegularReqUDSCommandExecuted, CanRegularReqUDSCommandExecute);

            UDS.LoadProgress = new Progress<ushort>(status => LoadProgress = status);
            UDS.LoadTimeS = new Progress<uint>(status => LoadTimeS = status);

            //Protocol.StateProcess = new Progress<short>(status => AddMessageToConsole(status));
        }
    }


}
