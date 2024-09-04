using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using System.Runtime.InteropServices;
using System.Reflection.Emit;
using System.ComponentModel;
using System.Threading.Tasks;
using Windows.Networking.Connectivity;
using System.Data;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Media.Animation;
using System.Windows.Threading;
using System.Windows.Forms;
using System.Reflection;
using Microsoft.Win32;
using System.Collections.ObjectModel;
using static System.Resources.ResXFileRef;
using System.Net.NetworkInformation;
using System.Threading;
using Windows.UI.ViewManagement;

namespace CharmsBarPort
{
    public partial class CharmsMenu : Window
    {
        Window CharmsClock = new CharmsClock();
        BrushConverter converter = new();
        
        public Microsoft.Win32.RegistryKey localKey = RegistryKey.OpenBaseKey(Microsoft.Win32.RegistryHive.LocalMachine, RegistryView.Registry64);
        public bool charmsMenuOpen = false;
        
        public CharmsMenu()
        {
            Topmost = true;
            ShowInTaskbar = false;
            WindowStyle = WindowStyle.None;
            ResizeMode = ResizeMode.NoResize;
            Background = (Brush)converter.ConvertFromString("#00111111");
            WindowStartupLocation = WindowStartupLocation.Manual;
            Left = 0;
            Top = SystemParameters.PrimaryScreenHeight - 200;

            System.Windows.Forms.Application.ThreadException += new ThreadExceptionEventHandler(CharmsMenu.Form1_UIThreadException);
            InitializeComponent();

            var accentColor = new UISettings().GetColorValue(UIColorType.Accent);
            MetroColor.Background = new SolidColorBrush(Color.FromRgb(accentColor.R, accentColor.G, accentColor.B));

            _initTimer();
        }

        private System.Windows.Forms.Timer t = null;
        private readonly Dispatcher dispatcher = Dispatcher.CurrentDispatcher;

        private void _initTimer()
        {
            t = new();
            t.Interval = 1;
            t.Tick += OnTimedEvent;
            t.Enabled = true;
            t.Start();
        }

        private void OnTimedEvent(object sender, EventArgs e)
        {
            dispatcher.BeginInvoke((Action)(() =>
            {
                try
                {
                    RegistryKey key = Registry.CurrentUser.OpenSubKey(@"SOFTWARE\\Microsoft\\Windows\\CurrentVersion\\ImmersiveShell\\EdgeUi", false);
                    if (key != null)
                    {
                        // (Not in 8.1) Remove the clock
                        string charmMenuUse = key.GetValue("EnableCharmsMenu", -1, RegistryValueOptions.None).ToString();
                        useMenu.Content = (charmMenuUse == "-1") ? "0" : charmMenuUse;
                        key.Close();
                    }
                }

                catch (Exception ex)
                {
                    //react appropriately
                }

                if (charmsMenuOpen)
                {
                    CharmsClock.Left = SystemParameters.PrimaryScreenWidth - 527;
                }

            }));
        }

        // Handle the UI exceptions by showing a dialog box, and asking the user whether
        // or not they wish to abort execution.
        private static void Form1_UIThreadException(object sender, ThreadExceptionEventArgs t)
        {
            System.Windows.Forms.Application.Restart();
        }
    }
}