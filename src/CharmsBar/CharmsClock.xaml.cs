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
                HasInternet.Source = new BitmapImage(new Uri(@"/Assets/Images/Networking/Icon151Dark.png", UriKind.Relative));
                WeakInternet.Source = new BitmapImage(new Uri(@"/Assets/Images/Networking/Icon133Dark.png", UriKind.Relative));
            }
            else
            {
                HasInternet.Source = new BitmapImage(new Uri(@"/Assets/Images/Networking/Icon151.png", UriKind.Relative));
                WeakInternet.Source = new BitmapImage(new Uri(@"/Assets/Images/Networking/Icon133.png", UriKind.Relative));
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
                        string noTransparency = key.GetValue("EnableTransparency", -1, RegistryValueOptions.None).ToString();
                        useTransparency = (noTransparency == "-1") || (noTransparency == "1");
                        key.Close();
                    }

                    RegistryKey? key2 = Registry.CurrentUser.OpenSubKey(@"SOFTWARE\\Microsoft\\Windows\\CurrentVersion\\ImmersiveShell\\EdgeUi", false);
                    if (key2 != null)
                    {
                        // get value 
                        string noClock = key2.GetValue("DisableCharmsClock", -1, RegistryValueOptions.None).ToString();

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
                    Clocked.Content = now.Minute.ToString();

                    if (!SystemParameters.HighContrast)
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

                    NoDrivers.Source = new BitmapImage(new Uri(@$"/Assets/Images/Networking/Icon103{isDark}.png", UriKind.Relative));
                    NoInternet.Source = new BitmapImage(new Uri(@$"/Assets/Images/Networking/Icon115{isDark}.png", UriKind.Relative));
                    Ethernet.Source = new BitmapImage(new Uri($@"/Assets/Images/Networking/Icon106{isDark}.png", UriKind.Relative));
                    NoInternetFound.Source = new BitmapImage(new Uri($@"/Assets/Images/Networking/Icon112{isDark}.png", UriKind.Relative));
                    IsCharging.Source = new BitmapImage(new Uri($@"/Assets/Images/Battery/BatteryFullCharging{isDark}.png", UriKind.Relative));
                    Airplane.Source = new BitmapImage(new Uri($@"/Assets/Images/Networking/Icon118{isDark}.png", UriKind.Relative));

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
                                HasInternet.Source = new BitmapImage(new Uri(@$"/Assets/Images/Networking/Icon151{isDark}.png", UriKind.Relative));
                                WeakInternet.Source = new BitmapImage(new Uri(@$"/Assets/Images/Networking/Icon133.png", UriKind.Relative));
                            }

                            if (nw4.StartsWith("6") || nw4.StartsWith("7"))
                            {
                                HasInternet.Source = new BitmapImage(new Uri(@$"/Assets/Images/Networking/Icon148{isDark}.png", UriKind.Relative));
                                WeakInternet.Source = new BitmapImage(new Uri(@$"/Assets/Images/Networking/Icon130{isDark}.png", UriKind.Relative));
                            }

                            if (nw4.StartsWith("4") || nw4.StartsWith("5"))
                            {
                                HasInternet.Source = new BitmapImage(new Uri(@$"/Assets/Images/Networking/Icon145{isDark}.png", UriKind.Relative));
                                WeakInternet.Source = new BitmapImage(new Uri(@$"/Assets/Images/Networking/Icon127{isDark}.png", UriKind.Relative));
                            }

                            if (nw4.StartsWith("2") || nw4.StartsWith("3"))
                            {
                                HasInternet.Source = new BitmapImage(new Uri(@"/Assets/Images/Networking/Icon142.png", UriKind.Relative));
                                WeakInternet.Source = new BitmapImage(new Uri(@"/Assets/Images/Networking/Icon124.png", UriKind.Relative));
                            }

                            if (nw4.StartsWith("0") || nw4.StartsWith("1"))
                            {
                                HasInternet.Source = new BitmapImage(new Uri(@"/Assets/Images/Networking/Icon139.png", UriKind.Relative));
                                WeakInternet.Source = new BitmapImage(new Uri(@"/Assets/Images/Networking/Icon121.png", UriKind.Relative));
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

                    if (clocksContent.Length <= 3 && !clocksContent.StartsWith("1"))
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
                                isEthernet = line.Split(':')[1].Split("%")[0];
                            }

                            if (line.Contains("There is"))
                            {
                                hasDrivers = line.Replace(" ", "");
                            }

                            if (line.Contains("Software"))
                            {
                                nw5 = line.Replace(" ", "");
                            }

                            if (line.Contains("Signal"))
                            {
                                Int32.TryParse(line.Split(':')[1].Split("%")[0], out strength);
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
            void setBatteryIcon() {
                // This syntax is available since C# 9
                BatteryLife.Source = (double)SystemInformation.PowerStatus.BatteryLifePercent switch
                {
                    >= 0.96 => new BitmapImage(new Uri(@$"/Assets/Images/BatteryFull{isDark}.png", UriKind.Relative)),
                    >= 0.86 and < 0.96 => new BitmapImage(new Uri(@$"/Assets/Images/Battery/Battery90{isDark}.png", UriKind.Relative)),
                    >= 0.76 and < 0.86 => new BitmapImage(new Uri(@$"/Assets/Images/Battery/Battery80{isDark}.png", UriKind.Relative)),
                    >= 0.66 and < 0.76 => new BitmapImage(new Uri(@$"/Assets/Images/Battery/Battery70{isDark}.png", UriKind.Relative)),
                    >= 0.56 and < 0.66 => new BitmapImage(new Uri(@$"/Assets/Images/Battery/Battery60{isDark}.png", UriKind.Relative)),
                    >= 0.46 and < 0.56 => new BitmapImage(new Uri(@$"/Assets/Images/Battery/Battery50{isDark}.png", UriKind.Relative)),
                    >= 0.36 and < 0.46 => new BitmapImage(new Uri(@$"/Assets/Images/Battery/Battery40{isDark}.png", UriKind.Relative)),
                    >= 0.26 and < 0.36 => new BitmapImage(new Uri(@$"/Assets/Images/Battery/Battery30{isDark}.png", UriKind.Relative)),
                    >= 0.16 and < 0.26 => new BitmapImage(new Uri(@$"/Assets/Images/Battery/Battery20{isDark}.png", UriKind.Relative)),
                    >= 0.06 and < 0.16 => new BitmapImage(new Uri(@$"/Assets/Images/Battery/Battery10{isDark}.png", UriKind.Relative)),
                    >= 0.01 and < 0.06 => new BitmapImage(new Uri(@$"/Assets/Images/Battery/Battery5{isDark}.png", UriKind.Relative)),
                    _ => new BitmapImage(new Uri(@$"/Assets/Images/Battery/Battery0{isDark}.png", UriKind.Relative))
                };
            };

            switch (SystemInformation.PowerStatus.BatteryChargeStatus)
            {
                case BatteryChargeStatus.NoSystemBattery:
                    BatteryLife.Visibility = Visibility.Hidden;
                    IsCharging.Visibility = Visibility.Hidden;
                    break;

                case BatteryChargeStatus.Charging:
                    BatteryLife.Visibility = Visibility.Visible;
                    IsCharging.Visibility = Visibility.Visible;
                    setBatteryIcon();
                    break;

                case BatteryChargeStatus.Unknown:
                    BatteryLife.Source = new BitmapImage(new Uri(@$"/Assets/Images/Battery0{isDark}.png", UriKind.Relative));
                    BatteryLife.Visibility = Visibility.Visible;
                    IsCharging.Visibility = Visibility.Hidden;
                    break;

                default:
                    BatteryLife.Visibility = Visibility.Visible;
                    IsCharging.Visibility = Visibility.Hidden;
                    setBatteryIcon();
                    break;
            };
        }
    }

}