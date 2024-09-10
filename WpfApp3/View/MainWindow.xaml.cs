using SFC.ViewModels;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.IO.Ports;

using System.Threading;
using System.IO;

using SFC.Models;

namespace SFC
{

    /// <summary>
    /// Логика взаимодействия для MainWindow.xaml
    /// </summary>
    /// 

    public partial class MainWindow : Window
    {
        public static Thread periodicalSend;
        public static Thread ReqParamsSend;

        public MainWindow()
        {
            InitializeComponent();

            //StreamWriter logWriter = new StreamWriter("log.txt", false);

        }

        public delegate void MethodContainer(string mes);

        private void TabControl_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {

        }
    }
}
