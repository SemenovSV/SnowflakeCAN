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

namespace SFC.ViewModels
{
    public class MainWindowViewModel : ViewModel, IDisposable
    {

        public ProtocolUDS Protocol;
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
                Protocol.Hex.LoadHexFile(FilePath);
                Protocol.Hex.insertCRC16(false);
                ConsoleContent += "Версия выбранной прошивки: ";
                ConsoleContent += Protocol.Hex.Version[0] + "." + Protocol.Hex.Version[1] + "." + Protocol.Hex.Version[2] + "." + Protocol.Hex.Version[3] + "\r";
                ConsoleContent += "Дата создания: ";
                ConsoleContent += Protocol.Hex.Date[0] + "." + Protocol.Hex.Date[1] + ".20" + Protocol.Hex.Date[2]+"\r";
                Protocol.SetReceiverId(Protocol.Hex.Version);
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
            ConsoleContent += Protocol.Hex.Version[0] + "." + Protocol.Hex.Version[1] + "." + Protocol.Hex.Version[2] + "." + Protocol.Hex.Version[3] + "\r";
            ConsoleContent += "Дата создания: ";
            ConsoleContent += Protocol.Hex.Date[0] + "." + Protocol.Hex.Date[1] + ".20" + Protocol.Hex.Date[2]+ "\r";
            EnableAction = false;
            Protocol.StartPricessLoading();
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

        public MainWindowViewModel()
        {
            Protocol = new ProtocolUDS(this);

            Protocol.Adapter.Port.ComPort.DataReceived += new SerialDataReceivedEventHandler(DataReceivedHandler); // Add DataReceived Event Handler
            OpenPortCommand = new LambdaCommand(OnOpenPortCommandExecuted, CanOpenPortCommandExecute);
            ReadFileCommand = new LambdaCommand(OnReadFileCommandExecuted, CanReadFileCommandExecute);
            RefreshPortsCommand = new LambdaCommand(OnRefreshPortsCommandExecuted, CanRefreshPortsCommandExecute);
            LoadFirmwareCommand = new LambdaCommand(OnLoadFirmwareCommandExecuted, CanLoadFirmwareCommandExecute);

            Protocol.LoadProgress = new Progress<ushort>(status => LoadProgress = status);
            Protocol.LoadTimeS = new Progress<uint>(status => LoadTimeS = status);
            //Protocol.StateProcess = new Progress<short>(status => AddMessageToConsole(status));
        }
    }


}
