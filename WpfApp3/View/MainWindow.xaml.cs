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

            //DataContext = new MainWindowViewModel();

            //StreamWriter logWriter = new StreamWriter("log.txt", false);
        }

        public delegate void MethodContainer(string mes);

        private void TabControl_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {

        }

        private Boolean AutoScroll = true;
        private void TermScroll_ScrollChanged(object sender, ScrollChangedEventArgs e)
        {
            if (e.ExtentHeightChange == 0)                                    // User scroll event : set or unset auto-scroll mode
            {                                                                 // Content unchanged : user scroll event
                if (TermScroll.VerticalOffset == TermScroll.ScrollableHeight) // Scroll bar is in bottom -  Set auto-scroll mode
                    AutoScroll = true;
                else                                                          // Scroll bar isn't in bottom - Unset auto-scroll mode
                    AutoScroll = false;
            }
            // Content scroll event : auto-scroll eventually
            if (AutoScroll && e.ExtentHeightChange != 0)                      // Content changed and auto-scroll mode set Autoscroll
            {
                TermScroll.ScrollToVerticalOffset(TermScroll.ExtentHeight);
            }
        }
    }
}
