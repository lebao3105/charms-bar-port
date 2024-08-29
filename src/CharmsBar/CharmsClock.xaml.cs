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

namespace CharmsBarPort
{
    public partial class CharmsClock : Window
    {
        public BackgroundWorker CheckSignal = new BackgroundWorker();
        public Microsoft.Win32.RegistryKey localKey = RegistryKey.OpenBaseKey(Microsoft.Win32.RegistryHive.LocalMachine, RegistryView.Registry64);
        public int isAirPlaneOn = 0;
        public bool useTransparency = true;
        
        public string nw4 = "";
        public string nw5 = "";
        public string isDark = "";
        public string hasDrivers = "";
        public string isEthernet = "";

        public CharmsClock()
        {
            var dispWidth = SystemParameters.PrimaryScreenWidth;
            var dispHeight = SystemParameters.PrimaryScreenHeight;
        
            Topmost = true;
            ShowInTaskbar = false;
            WindowStyle = WindowStyle.None;
            ResizeMode = ResizeMode.NoResize;
            AllowsTransparency = true;
            Height = 140;
            WindowStartupLocation = WindowStartupLocation.Manual;
            Left = 51;
            Top = dispHeight - 188;
            Background = (Brush)new BrushConverter().ConvertFromString("#f0111111");

            CheckSignal.DoWork += CheckSignal_DoWork;
            CheckSignal.ProgressChanged += CheckSignal_ProgressChanged;
            CheckSignal.WorkerReportsProgress = true;
            System.Windows.Forms.Application.ThreadException += new ThreadExceptionEventHandler(CharmsClock.Form1_UIThreadException);
            
            InitializeComponent();
            _initTimer();
        }

        private System.Windows.Forms.Timer t = null;
        private readonly Dispatcher dispatcher = Dispatcher.CurrentDispatcher;

        private void _initTimer()
        {
            if (SystemParameters.HighContrast)
            {
                HasInternet.Source = new BitmapImage(new Uri(@"/Assets/Images/Icon151Dark.png", UriKind.Relative));
                WeakInternet.Source = new BitmapImage(new Uri(@"/Assets/Images/Icon133Dark.png", UriKind.Relative));
            }
            else
            {
                HasInternet.Source = new BitmapImage(new Uri(@"/Assets/Images/Icon151.png", UriKind.Relative));
                WeakInternet.Source = new BitmapImage(new Uri(@"/Assets/Images/Icon133.png", UriKind.Relative));
            }

            System.Windows.Forms.Timer t = new System.Windows.Forms.Timer();
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
                    RegistryKey? key = Registry.CurrentUser.OpenSubKey(@"SOFTWARE\\Microsoft\\Windows\\CurrentVersion\\Themes\\Personalize", false);
                    if (key != null)
                    {
                        // get value 
                        string noTransparency = key.GetValue("EnableTransparency", -1, RegistryValueOptions.None).ToString(); //this is not in Windows 8.1, but used to remove transparency
                        useTransparency = (noTransparency == "-1") || (noTransparency == "1");
                        key.Close();
                    }

                    RegistryKey? key2 = Registry.CurrentUser.OpenSubKey(@"SOFTWARE\\Microsoft\\Windows\\CurrentVersion\\ImmersiveShell\\EdgeUi", false);
                    if (key2 != null)
                    {
                        // get value 
                        string noClock = key2.GetValue("DisableCharmsClock", -1, RegistryValueOptions.None).ToString(); //this is not in Windows 8.1, but used to remove the Charms Clock

                        noClocks.Content = noClock == "-1" ? "0" : noClock;
                        key2.Close();
                    }
                    else
                    {
                        noClocks.Content = "0";
                    }
                }

                catch (Exception ex)  //just for demonstration...it's always best to handle specific exceptions
                {
                    //react appropriately
                }

                if (noClocks.Content == "-1" || noClocks.Content == "0")
                {

                    if (SystemParameters.HighContrast == false)
                    {
                        isDark = "";
                    }

                    else
                    {
                        System.Drawing.Color col = System.Drawing.ColorTranslator.FromHtml(SystemColors.WindowBrush.ToString());

                        isDark = ((col.R * 0.2126 +
                                  col.G * 0.7152 +
                                  col.B * 0.0722) < 255 / 2)
                                 ? "" : "Dark";
                    }

                    // Time & date format strings
                    // https://www.freepascal.org/docs-html/rtl/sysutils/formatchars.html

                    var now = DateTime.Now;

                    Date.Content = now.ToString("MMMM d");

                    // Days of week in English:
                    // Monday, Friday and Sunday have 6 characters each.
                    // Tuesday has 7. Wednesday has 9 and Thursday have 8.
                    var dayofweek = now.DayOfWeek.ToString();
                    Week.Content = dayofweek + ((dayofweek.Length == 6) ? "  " : "      ");

                    // This follows 12 hours clock.
                    // For 24 hours clock, use now.Hours.ToString() or HH format.
                    // TODO.
                    Clocks.Content = now.ToString("h ");
                    Clocked.Content = now.Month.ToString();

                    if (SystemParameters.HighContrast == false)
                    {
                        ClockBorder.Visibility = Visibility.Hidden;
                        BrushConverter converter = new();

                        this.Background = (Brush)converter.ConvertFromString(useTransparency ? "#f0111111" : "#111111");
                        Week.Foreground = Clocks.Foreground = ClockLines.Foreground =
                            Clocked.Foreground = Date.Foreground =
                            (Brush)converter.ConvertFromString("#ffffff");
                    }

                    else
                    {
                        this.Background = SystemColors.WindowBrush;
                        ClockBorder.Visibility = Visibility.Visible;

                        Week.Foreground = Clocks.Foreground = ClockLines.Foreground =
                            Clocked.Foreground = Date.Foreground = SystemColors.WindowTextBrush;
                    }

                    while (!CheckSignal.IsBusy && this.IsVisible)
                    {
                        CheckSignal.RunWorkerAsync();
                    }
                    CheckBatteryStatus();
                    ClockBorder.BorderBrush = SystemColors.WindowTextBrush;
                    ClockBorder.Background = SystemColors.WindowBrush;
                    
                    var localKey = RegistryKey.OpenBaseKey(Microsoft.Win32.RegistryHive.LocalMachine, RegistryView.Registry64);
                    var localKey2 = localKey.OpenSubKey("SYSTEM\\ControlSet001\\Control\\RadioManagement\\SystemRadioState");
                    var isAirPlaneOn = localKey2.GetValue("", "").ToString();
                    

                    var nw = IsConnected();
                    var nw2 = IsLocal();
                    var nw3 = IsWeak();

                    NoDrivers.Source = new BitmapImage(new Uri(@$"/Assets/Images/Icon103{isDark}.png", UriKind.Relative));
                    NoInternet.Source = new BitmapImage(new Uri(@$"/Assets/Images/Icon115{isDark}.png", UriKind.Relative));
                    Ethernet.Source = new BitmapImage(new Uri($@"/Assets/Images/Icon106{isDark}.png", UriKind.Relative));
                    NoInternetFound.Source = new BitmapImage(new Uri($@"/Assets/Images/Icon112{isDark}.png", UriKind.Relative));
                    IsCharging.Source = new BitmapImage(new Uri($@"/Assets/Images/BatteryFullCharging{isDark}.png", UriKind.Relative));
                    Airplane.Source = new BitmapImage(new Uri($@"/Assets/Images/Icon118{isDark}.png", UriKind.Relative));

                    // Configure whether to show stuff or not.
                    // By default all are hidden.

                    if (isEthernet.IndexOf("Ethernet") != -1)
                    {
                        NoDrivers.Visibility = Visibility.Hidden;
                        NoInternet.Visibility = Visibility.Hidden;
                        NoInternetFound.Visibility = Visibility.Hidden;
                        Ethernet.Visibility = Visibility.Visible;
                        HasInternet.Visibility = Visibility.Hidden;
                        WeakInternet.Visibility = Visibility.Hidden;
                        Airplane.Visibility = Visibility.Hidden;
                    }

                    if (hasDrivers.IndexOf("Thereisno") != -1)
                    {
                        NoDrivers.Visibility = Visibility.Visible;
                        NoInternet.Visibility = Visibility.Hidden;
                        NoInternetFound.Visibility = Visibility.Hidden;
                        Ethernet.Visibility = Visibility.Hidden;
                        HasInternet.Visibility = Visibility.Hidden;
                        WeakInternet.Visibility = Visibility.Hidden;
                        Airplane.Visibility = Visibility.Hidden;
                    }
                    else
                    {
                        if (nw && !nw2 && !nw3 && isAirPlaneOn == "0")
                        {
                            NoDrivers.Visibility = Visibility.Hidden;
                            NoInternet.Visibility = Visibility.Hidden;
                            NoInternetFound.Visibility = Visibility.Hidden;
                            Ethernet.Visibility = Visibility.Hidden;
                            HasInternet.Visibility = Visibility.Visible;
                            WeakInternet.Visibility = Visibility.Hidden;
                            Airplane.Visibility = Visibility.Hidden;

                            if (nw4.StartsWith("100") || nw4.StartsWith("9") || nw4.StartsWith("8"))
                            {
                                HasInternet.Source = new BitmapImage(new Uri(@$"/Assets/Images/Icon151{isDark}.png", UriKind.Relative));
                                WeakInternet.Source = new BitmapImage(new Uri(@$"/Assets/Images/Icon133.png", UriKind.Relative));
                            }

                            if (nw4.StartsWith("6") || nw4.StartsWith("7"))
                            {
                                HasInternet.Source = new BitmapImage(new Uri(@$"/Assets/Images/Icon148{isDark}.png", UriKind.Relative));
                                WeakInternet.Source = new BitmapImage(new Uri(@$"/Assets/Images/Icon130{isDark}.png", UriKind.Relative));
                            }

                            if (nw4.StartsWith("4") || nw4.StartsWith("5"))
                            {
                                HasInternet.Source = new BitmapImage(new Uri(@$"/Assets/Images/Icon145{isDark}.png", UriKind.Relative));
                                WeakInternet.Source = new BitmapImage(new Uri(@$"/Assets/Images/Icon127{isDark}.png", UriKind.Relative));
                            }

                            if (nw4.StartsWith("2") || nw4.StartsWith("3"))
                            {
                                HasInternet.Source = new BitmapImage(new Uri(@"/Assets/Images/Icon142.png", UriKind.Relative));
                                WeakInternet.Source = new BitmapImage(new Uri(@"/Assets/Images/Icon124.png", UriKind.Relative));
                            }

                            if (nw4.StartsWith("0") || nw4.StartsWith("1"))
                            {
                                HasInternet.Source = new BitmapImage(new Uri(@"/Assets/Images/Icon139.png", UriKind.Relative));
                                WeakInternet.Source = new BitmapImage(new Uri(@"/Assets/Images/Icon121.png", UriKind.Relative));
                            }
                        }

                        if (nw && !nw2 && !nw3 && isAirPlaneOn == "1")
                        {
                            NoDrivers.Visibility = Visibility.Hidden;
                            NoInternet.Visibility = Visibility.Hidden;
                            NoInternetFound.Visibility = Visibility.Hidden;
                            Ethernet.Visibility = Visibility.Hidden;
                            HasInternet.Visibility = Visibility.Hidden;
                            WeakInternet.Visibility = Visibility.Hidden;
                            Airplane.Visibility = Visibility.Visible;
                        }

                        if (nw2 && !nw && !nw3 && isAirPlaneOn == "0")
                        {
                            NoDrivers.Visibility = Visibility.Hidden;
                            NoInternet.Visibility = Visibility.Hidden;
                            NoInternetFound.Visibility = Visibility.Hidden;
                            Ethernet.Visibility = Visibility.Hidden;
                            HasInternet.Visibility = Visibility.Hidden;
                            WeakInternet.Visibility = Visibility.Visible;
                            Airplane.Visibility = Visibility.Hidden;
                        }

                        if (!nw && !nw2 && !nw3 && isAirPlaneOn == "0" && nw5 == "SoftwareOn")
                        {
                            NoDrivers.Visibility = Visibility.Hidden;
                            NoInternet.Visibility = Visibility.Visible;
                            NoInternetFound.Visibility = Visibility.Hidden;
                            Ethernet.Visibility = Visibility.Hidden;
                            HasInternet.Visibility = Visibility.Hidden;
                            WeakInternet.Visibility = Visibility.Hidden;
                            Airplane.Visibility = Visibility.Hidden;
                        }

                        if (!nw && !nw2 && !nw3 && isAirPlaneOn == "0" && nw5 == "SoftwareOff")
                        {
                            NoDrivers.Visibility = Visibility.Hidden;
                            NoInternet.Visibility = Visibility.Hidden;
                            NoInternetFound.Visibility = Visibility.Visible;
                            Ethernet.Visibility = Visibility.Hidden;
                            HasInternet.Visibility = Visibility.Hidden;
                            WeakInternet.Visibility = Visibility.Hidden;
                            Airplane.Visibility = Visibility.Hidden;
                        }

                        if (!nw && !nw2 && !nw3  && isAirPlaneOn == "0")
                        {
                            NoDrivers.Visibility = Visibility.Hidden;
                            NoInternet.Visibility = Visibility.Hidden;
                            NoInternetFound.Visibility = Visibility.Hidden;
                            Ethernet.Visibility = Visibility.Hidden;
                            HasInternet.Visibility = Visibility.Hidden;
                            WeakInternet.Visibility = Visibility.Visible;
                            Airplane.Visibility = Visibility.Hidden;
                        }

                        if (!nw && !nw && !nw3 && isAirPlaneOn == "1")
                        {
                            NoDrivers.Visibility = Visibility.Hidden;
                            NoInternet.Visibility = Visibility.Hidden;
                            NoInternetFound.Visibility = Visibility.Hidden;
                            Ethernet.Visibility = Visibility.Hidden;
                            HasInternet.Visibility = Visibility.Hidden;
                            WeakInternet.Visibility = Visibility.Hidden;
                            Airplane.Visibility = Visibility.Visible;
                        }
                    }

                    var clocksContent = Clocks.Content.ToString();

                    if (clocksContent.Length < 3 && !clocksContent.StartsWith("1"))
                    {
                        Clocks.Margin = new Thickness(94, 3, 0, -106);
                        ClockLines.Margin = new Thickness(138, -24.99, -190, -98);
                        Clocked.Margin = new Thickness(157, -17, -190, -198);
                        Week.Margin = new Thickness(267, 2, 0, -18);
                        Date.Margin = new Thickness(269, 3, 0, -24);
                    }

                    if (clocksContent.StartsWith("1 "))
                    {
                        Clocks.Margin = new Thickness(95, 3, 0, -106);
                        ClockLines.Margin = new Thickness(125, -24.99, -190, -98);
                        Clocked.Margin = new Thickness(144, -17, -190, -198);
                        Week.Margin = new Thickness(255, 2, 0, -18);
                        Date.Margin = new Thickness(255, 3, 0, -24);
                    }

                    if (clocksContent.StartsWith("10") || clocksContent.StartsWith("12"))
                    {
                        Clocks.Margin = new Thickness(95, 3, 0, -106);
                        ClockLines.Margin = new Thickness(169, -24.99, -190, -98);
                        Clocked.Margin = new Thickness(188, -17, -190, -198);
                        Week.Margin = new Thickness(298, 2, 0, -18);
                        Date.Margin = new Thickness(300, 4, 0, -24);
                    }

                    if (clocksContent.StartsWith("11"))
                    {
                        Clocks.Margin = new Thickness(95, 3, 0, -106);
                        ClockLines.Margin = new Thickness(156, -24.99, -190, -98);
                        Clocked.Margin = new Thickness(174.5, -17, -190, -198);
                        Week.Margin = new Thickness(284, 2, 0, -18);
                        Date.Margin = new Thickness(284, 4, 0, -24);
                    }

                    if (clocksContent.Length == 3 && !clocksContent.StartsWith("10") && !clocksContent.StartsWith("12"))
                    {
                        Clocks.Margin = new Thickness(94, 3, 0, -106);
                        ClockLines.Margin = new Thickness(157, -24.99, -190, -98);
                        Clocked.Margin = new Thickness(175, -17, -190, -198);
                        Week.Margin = new Thickness(287, 2, 0, -18);
                        Date.Margin = new Thickness(287, 4, 0, -24);
                    }

                    if (Date.Content.ToString().Length > 6 || Date.Content.ToString().Length < 7 && Week.Content.ToString().Length > 9)
                    {
                        if (clocksContent.Length == 2 || clocksContent.Length == 3)
                        {
                            CharmClock.Margin = new Thickness(0, 0, 10, 10);

                            if (!clocksContent.StartsWith("1"))
                            {
                                AutoResizer.Width = 391 + Date.Content.ToString().Length + 25;
                            }

                            else
                            {
                                if (clocksContent.Length == 3 &&
                                    (clocksContent.StartsWith("10") ||
                                     clocksContent.StartsWith("11") ||
                                     clocksContent.StartsWith("12")))
                                {
                                    AutoResizer.Width = 391 + Date.Content.ToString().Length + 63;
                                }
                                else
                                {
                                    AutoResizer.Width = 391 + Date.Content.ToString().Length;
                                }
                            }
                        }
                    }

                    ClockBorder.Width = this.Width;
                    localKey.Close();
                }
            }));
        }

        // Handle the UI exceptions by showing a dialog box, and asking the user whether
        // or not they wish to abort execution.
        private static void Form1_UIThreadException(object sender, ThreadExceptionEventArgs t)
        {
            System.Windows.Forms.Application.Restart();
        }

        private bool IsConnected()
        {
            try
            {
                var connectionProfile = NetworkInformation.GetInternetConnectionProfile();
                return (connectionProfile != null &&
                      (connectionProfile.GetNetworkConnectivityLevel() == NetworkConnectivityLevel.InternetAccess));
            }

            catch (Exception err)
            {
                System.Windows.Forms.Application.Restart();
                return false;
            }
        }
        private bool IsWeak()
        {
            try
            {
                var connectionProfile = NetworkInformation.GetInternetConnectionProfile();
                return (connectionProfile != null &&
                      (connectionProfile.GetNetworkConnectivityLevel() == NetworkConnectivityLevel.ConstrainedInternetAccess));
            }

            catch (Exception err)
            {
                System.Windows.Forms.Application.Restart();
                return false;
            }
        }

        private bool IsLocal()
        {
            try
            {
                var connectionProfile = NetworkInformation.GetInternetConnectionProfile();
                return (connectionProfile != null &&
                      (connectionProfile.GetNetworkConnectivityLevel() == NetworkConnectivityLevel.LocalAccess));
            }

            catch (Exception err)
            {
                System.Windows.Forms.Application.Restart();
                return false;
            }
        }

        private void CheckSignal_DoWork(object sender, DoWorkEventArgs e)
        {
            if (this.IsVisible)
            {
                try
                {
                    Process proc = new Process
                    {
                        StartInfo = new ProcessStartInfo
                        {
                            FileName = "netsh.exe",
                            Arguments = "wlan show interfaces",
                            UseShellExecute = false,
                            RedirectStandardOutput = true,
                            CreateNoWindow = true
                        }
                    };
                    while (true)
                    {
                        proc.Start();
                        string line;
                        int strength = 0;
                        string wifi;
                        while (!proc.StandardOutput.EndOfStream)
                        {
                            line = proc.StandardOutput.ReadLine();

                            if (line.Contains("Name"))
                            {
                                string tmpx = line.Split(':')[1].Split("%")[0];
                                isEthernet = tmpx.ToString();
                            }

                            if (line.Contains("There is"))
                            {
                                string tmp = line;
                                hasDrivers = tmp.Replace(" ", "");
                            }

                            if (line.Contains("Software"))
                            {
                                string tmp2 = line;
                                nw5 = tmp2.Replace(" ", "");
                            }

                            if (line.Contains("Signal"))
                            {
                                string tmp3 = line.Split(':')[1].Split("%")[0];
                                Int32.TryParse(tmp3, out strength);
                                nw4 = strength.ToString();
                                CheckSignal.ReportProgress(strength);
                            }


                        }
                        proc.WaitForExit();
                    }
                }

                catch (Exception ex)
                {

                }
            }
        }

        static void CheckSignal_ProgressChanged(object sender, ProgressChangedEventArgs e)
        {

        }

        private void CheckBatteryStatus()
        {
            var pw = SystemInformation.PowerStatus.BatteryChargeStatus;
            var pw2 = SystemInformation.PowerStatus.PowerLineStatus.ToString();
            double pw3 = SystemInformation.PowerStatus.BatteryLifePercent;

            // TODO: Fix the mess below

            if (pw3 >= 0.96)
            {
                BatteryLife.Source = new BitmapImage(new Uri(@$"/Assets/Images/BatteryFull{isDark}.png", UriKind.Relative));
            }

            if (0.96 > pw3 && pw3 >= 0.86) // WHY NOT 0.9 <= pw3 < 0.96 MICROSOFT
            {
                BatteryLife.Source = new BitmapImage(new Uri(@$"/Assets/Images/Battery90{isDark}.png", UriKind.Relative));
            }

            if (pw3 >= 0.76 && pw3 < 0.86)
            {
                BatteryLife.Source = new BitmapImage(new Uri(@$"/Assets/Images/Battery80{isDark}.png", UriKind.Relative));
            }

            if (pw3 >= 0.66 && pw3 < 0.76)
            {
                BatteryLife.Source = new BitmapImage(new Uri(@$"/Assets/Images/Battery70{isDark}.png", UriKind.Relative));
            }

            if (pw3 >= 0.56 && pw3 < 0.66)
            {
                BatteryLife.Source = new BitmapImage(new Uri(@$"/Assets/Images/Battery60{isDark}.png", UriKind.Relative));
            }

            if (pw3 >= 0.46 && pw3 < 0.56)
            {
                BatteryLife.Source = new BitmapImage(new Uri(@$"/Assets/Images/Battery50{isDark}.png", UriKind.Relative));
            }

            if (pw3 >= 0.36 && pw3 < 0.46)
            {
                BatteryLife.Source = new BitmapImage(new Uri(@$"/Assets/Images/Battery40{isDark}.png", UriKind.Relative));
            }

            if (pw3 >= 0.26 && pw3 < 0.36)
            {
                BatteryLife.Source = new BitmapImage(new Uri(@$"/Assets/Images/Battery30{isDark}.png", UriKind.Relative));
            }

            if (pw3 >= 0.16 && pw3 < 0.26)
            {
                BatteryLife.Source = new BitmapImage(new Uri(@$"/Assets/Images/Battery20{isDark}.png", UriKind.Relative));
            }

            if (pw3 >= 0.06 && pw3 < 0.16)
            {
                BatteryLife.Source = new BitmapImage(new Uri(@$"/Assets/Images/Battery1{isDark}.png", UriKind.Relative));
            }

            if (pw3 > 0.1 && pw3 < 0.6)
            {
                BatteryLife.Source = new BitmapImage(new Uri(@$"/Assets/Images/Battery5{isDark}.png", UriKind.Relative));
            }

            if (pw3 <= 0.1)
            {
                BatteryLife.Source = new BitmapImage(new Uri(@$"/Assets/Images/Battery0{isDark}.png", UriKind.Relative));
            }

            if (pw != BatteryChargeStatus.NoSystemBattery)
            {
                if (pw2 == "Online")
                {
                    BatteryLife.Visibility = Visibility.Visible;
                    IsCharging.Visibility = Visibility.Visible;
                }
                else
                {
                    BatteryLife.Visibility = pw2 == "Offline" ? Visibility.Visible : Visibility.Hidden;
                    IsCharging.Visibility = Visibility.Hidden;
                }
            }
            else
            {
                BatteryLife.Visibility = Visibility.Hidden;
                IsCharging.Visibility = Visibility.Hidden;
            }
        }
    }

}