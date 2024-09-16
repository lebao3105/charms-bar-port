using System;
using System.ComponentModel;
using System.Collections.Generic;
using System.Diagnostics;
using System.Data;
using System.Linq;
using System.Text;
using System.Reflection;
using System.Threading.Tasks;
using System.Reflection.Emit;
using System.Runtime.InteropServices;
using System.Collections.ObjectModel;
using System.Net.NetworkInformation;
using System.Threading;
using System.Configuration;
using static System.Resources.ResXFileRef;

using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Media.Animation;
using System.Windows.Navigation;
using System.Windows.Shapes;
using System.Windows.Threading;
using System.Windows.Forms;

using Microsoft.Win32;
using Windows.Networking.Connectivity;

namespace CharmsBarPort
{
    public partial class CharmsClock : Window
    {
        private BrushConverter converter = new();
        private string colorScheme = "";

        #region Images
        private BitmapImage NoDriversImg
        {
            get => new BitmapImage(new Uri(@$"/Assets/Images/Networking/Icon103{colorScheme}.png", UriKind.Relative));
        }

        private BitmapImage NoInternetImg
        {
            get => new BitmapImage(new Uri(@$"/Assets/Images/Networking/Icon115{colorScheme}.png", UriKind.Relative));
        }

        private BitmapImage EthernetImg
        {
            get => new BitmapImage(new Uri($@"/Assets/Images/Networking/Icon106{colorScheme}.png", UriKind.Relative));
        }

        private BitmapImage NoInternetFoundImg
        {
            get => new BitmapImage(new Uri($@"/Assets/Images/Networking/Icon112{colorScheme}.png", UriKind.Relative));
        }

        private BitmapImage BatteryChargeImg
        {
            get => new BitmapImage(new Uri($@"/Assets/Images/Battery/BatteryCharge{colorScheme}.png", UriKind.Relative));
        }

        private BitmapImage AirplaneImg
        {
            get => new BitmapImage(new Uri($@"/Assets/Images/Networking/Icon118{colorScheme}.png", UriKind.Relative));
        }

        private BitmapImage HasInternetImg
        {
            get => new BitmapImage(new Uri($@"/Assets/Images/Networking/Icon151{colorScheme}.png", UriKind.Relative));
        }

        private BitmapImage WeakInternetImg
        {
            get => new BitmapImage(new Uri($@"/Assets/Images/Networking/Icon133{colorScheme}.png", UriKind.Relative));
        }
        #endregion

        private System.Windows.Forms.Timer t = new()
        {
            Interval = 1,
            Enabled = true
        };

        public CharmsClock()
        {
            Top = SystemParameters.PrimaryScreenHeight - 188;

            System.Windows.Forms.Application.ThreadException += new ThreadExceptionEventHandler(CharmsClock.Form1_UIThreadException);

            InitializeComponent();
        }

        private void Window_Initialized(object sender, EventArgs e)
        {
            ClockBorder.Width = this.Width;

            t.Tick += OnTimedEvent;
            t.Start();
        }

        private void OnTimedEvent(object sender, EventArgs e)
        {
            this.Dispatcher.BeginInvoke((Action)(() =>
            {
                bool isAirPlaneOn = false;
                bool useTransparency = true;

                if (SystemParameters.HighContrast)
                {
                    System.Drawing.Color col = System.Drawing.ColorTranslator.FromHtml(SystemColors.WindowBrush.ToString());

                    colorScheme = ((col.R * 0.2126 + col.G * 0.7152 + col.B * 0.0722) < 255 / 2) ? "" : "Dark";

                    ClockBorder.Visibility = Visibility.Visible;

                    Week.Foreground = Clock_Hour.Foreground = Clock_Sepa.Foreground =
                        Clock_Mins.Foreground = Date.Foreground = SystemColors.WindowTextBrush;
                }
                else
                {
                    // Normally Charm Bar uses dark background and light elements
                    colorScheme = "";
                    ClockBorder.Visibility = Visibility.Hidden;
                    Week.Foreground = Clock_Hour.Foreground = Clock_Sepa.Foreground =
                        Clock_Mins.Foreground = Date.Foreground = (Brush)converter.ConvertFromString("#ffffff");
                }

                try
                {
                    // TODO: Transparency level
                    using (var key = Registry.CurrentUser.OpenSubKey(@"SOFTWARE\\Microsoft\\Windows\\CurrentVersion\\Themes\\Personalize", false))
                    {
                        if ((key != null) && (key.GetValueKind("EnableTransparency") == RegistryValueKind.DWord)
                            && (key.GetValue("EnableTransparency", 0) as int? == 1))
                        {
                            this.Background = (Brush)converter.ConvertFromString("#f0111111");
                            key.Close();
                        }

                        else
                        {
                            this.Background = SystemParameters.HighContrast ? SystemColors.WindowBrush :
                                                    (Brush)converter.ConvertFromString("#11111111");
                        }
                    }

                    using (var key = Registry.LocalMachine.OpenSubKey("SYSTEM\\ControlSet001\\Control\\RadioManagement\\SystemRadioState"))
                    {
                        if (key != null)
                        {
                            isAirPlaneOn = key.GetValue("", "").ToString() != "0";
                            key.Close();
                        }
                    }
                }

                catch (Exception ex)
                {
                    this.Background = SystemParameters.HighContrast ? SystemColors.WindowBrush :
                                            (Brush)converter.ConvertFromString("#11111111");
                }

                #region Time & Date

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
                var currHour = now.ToString("h ");
                Clock_Hour.Content = currHour;
                Clock_Mins.Content = now.ToString("mm");

                if (currHour.Length <= 3 && !currHour.StartsWith("1"))
                {
                    Clock_Hour.Margin = new Thickness(94, 3, 0, -106);
                    Clock_Sepa.Margin = new Thickness(138, -24.99, -190, -98);
                    Clock_Mins.Margin = new Thickness(157, -17, -190, -198);
                    Week.Margin = new Thickness(267, 2, 0, -18);
                    Date.Margin = new Thickness(269, 3, 0, -24);
                }

                else if (currHour.StartsWith("1 "))
                {
                    Clock_Hour.Margin = new Thickness(95, 3, 0, -106);
                    Clock_Sepa.Margin = new Thickness(125, -24.99, -190, -98);
                    Clock_Mins.Margin = new Thickness(144, -17, -190, -198);
                    Week.Margin = new Thickness(255, 2, 0, -18);
                    Date.Margin = new Thickness(255, 3, 0, -24);
                }

                else if (currHour.StartsWith("10") || currHour.StartsWith("12"))
                {
                    Clock_Hour.Margin = new Thickness(95, 3, 0, -106);
                    Clock_Sepa.Margin = new Thickness(169, -24.99, -190, -98);
                    Clock_Mins.Margin = new Thickness(188, -17, -190, -198);
                    Week.Margin = new Thickness(298, 2, 0, -18);
                    Date.Margin = new Thickness(300, 4, 0, -24);
                }

                else if (currHour.StartsWith("11"))
                {
                    Clock_Hour.Margin = new Thickness(95, 3, 0, -106);
                    Clock_Sepa.Margin = new Thickness(156, -24.99, -190, -98);
                    Clock_Mins.Margin = new Thickness(174.5, -17, -190, -198);
                    Week.Margin = new Thickness(284, 2, 0, -18);
                    Date.Margin = new Thickness(284, 4, 0, -24);
                }

                if (currHour.Length == 3 && !currHour.StartsWith("10") && !currHour.StartsWith("12"))
                {
                    Clock_Hour.Margin = new Thickness(94, 3, 0, -106);
                    Clock_Sepa.Margin = new Thickness(157, -24.99, -190, -98);
                    Clock_Mins.Margin = new Thickness(175, -17, -190, -198);
                    Week.Margin = new Thickness(287, 2, 0, -18);
                    Date.Margin = new Thickness(287, 4, 0, -24);
                }

                if (Date.Content.ToString().Length > 6 || Date.Content.ToString().Length < 7 && Week.Content.ToString().Length > 9)
                {
                    if (currHour.Length == 2 || currHour.Length == 3)
                    {
                        CharmClock.Margin = new Thickness(0, 0, 10, 10);

                        if (!currHour.StartsWith("1"))
                        {
                            AutoResizer.Width = 391 + Date.Content.ToString().Length + 25;
                        }

                        else
                        {
                            if (currHour.Length == 3 &&
                                (currHour.StartsWith("10") ||
                                 currHour.StartsWith("11") ||
                                 currHour.StartsWith("12")))
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

                #endregion

                CheckBatteryStatus();

                #region Network checking

                var connectionProfile = NetworkInformation.GetInternetConnectionProfile();
                bool Nwking_IsConnected = false;
                bool Nwking_IsLocalNw = false;
                bool Nwking_IsSignalWeak = false;
                string Nwking_Strength = "";
                bool usesEthernet = false;
                bool hasDrivers = true;

                if (connectionProfile != null)
                {
                    switch (connectionProfile.GetNetworkConnectivityLevel())
                    {
                        case NetworkConnectivityLevel.InternetAccess:
                            Nwking_IsConnected = true;
                            break;

                        case NetworkConnectivityLevel.LocalAccess:
                            Nwking_IsLocalNw = true;
                            break;

                        case NetworkConnectivityLevel.ConstrainedInternetAccess:
                            Nwking_IsSignalWeak = true;
                            break;
                    }
                    Nwking_Strength = (connectionProfile.GetSignalBars() * 20).ToString();
                    usesEthernet = !(connectionProfile.IsWlanConnectionProfile || connectionProfile.IsWwanConnectionProfile);
                    hasDrivers = connectionProfile != null; // or NetworkInformation.GetInternetConnectionProfiles()'s length
                }

                // From API documentation:
                //     An integer value within a range of 0-5 that corresponds to the number of signal
                //     bars displayed by the UI.
                // 0 -> no signal
                // 1 -> very weak
                // 2 -> weak
                // 3 -> moderate
                // 4 -> good
                // 5 -> excellent
                //switch (connectionProfile.GetSignalBars())
                //{
                //    case 0:
                //        InternetStrength.Source = new BitmapImage(new Uri($@"/Assets/Images/Networking/Icon121{colorScheme}.png", UriKind.Relative));
                //        break;

                //    case 1:
                //        InternetStrength.Source = new BitmapImage(new Uri($@"/Assets/Images/Networking/Icon139{colorScheme}.png", UriKind.Relative));
                //        break;

                //    case 2:
                //        break;
                //}

                #endregion

                #region Network images
                //if (usesEthernet)
                //{
                //    InternetType.Source = EthernetImg;
                //}

                //else if (!hasDrivers)
                //{
                //    InternetType.Source = NoDriversImg;
                //}
                //else
                //{
                //if (Nwking_IsConnected && !Nwking_IsLocalNw && !Nwking_IsSignalWeak && !isAirPlaneOn)
                //{

                //    if (Nwking_Strength.StartsWith("100") || Nwking_Strength.StartsWith("9") || Nwking_Strength.StartsWith("8"))
                //    {
                //        HasInternet.Source = new BitmapImage(new Uri(@$"/Assets/Images/Networking/Icon151{colorScheme}.png", UriKind.Relative));
                //        WeakInternet.Source = new BitmapImage(new Uri(@$"/Assets/Images/Networking/Icon133.png{colorScheme}", UriKind.Relative));
                //    }

                //    if (Nwking_Strength.StartsWith("6") || Nwking_Strength.StartsWith("7"))
                //    {
                //        HasInternet.Source = new BitmapImage(new Uri(@$"/Assets/Images/Networking/Icon148{colorScheme}.png", UriKind.Relative));
                //        WeakInternet.Source = new BitmapImage(new Uri(@$"/Assets/Images/Networking/Icon130{colorScheme}.png", UriKind.Relative));
                //    }

                //    if (Nwking_Strength.StartsWith("4") || Nwking_Strength.StartsWith("5"))
                //    {
                //        HasInternet.Source = new BitmapImage(new Uri(@$"/Assets/Images/Networking/Icon145{colorScheme}.png", UriKind.Relative));
                //        WeakInternet.Source = new BitmapImage(new Uri(@$"/Assets/Images/Networking/Icon127{colorScheme}.png", UriKind.Relative));
                //    }

                //    if (Nwking_Strength.StartsWith("2") || Nwking_Strength.StartsWith("3"))
                //    {
                //        HasInternet.Source = new BitmapImage(new Uri(@"/Assets/Images/Networking/Icon142.png", UriKind.Relative));
                //        WeakInternet.Source = new BitmapImage(new Uri(@"/Assets/Images/Networking/Icon124.png", UriKind.Relative));
                //    }

                //    if (Nwking_Strength.StartsWith("0") || Nwking_Strength.StartsWith("1"))
                //    {
                //        HasInternet.Source = new BitmapImage(new Uri(@"/Assets/Images/Networking/Icon139.png", UriKind.Relative));
                //        WeakInternet.Source = new BitmapImage(new Uri(@"/Assets/Images/Networking/Icon121.png", UriKind.Relative));
                //    }
                //}

                //if (Nwking_IsConnected && !Nwking_IsLocalNw && !Nwking_IsSignalWeak && isAirPlaneOn == "1")
                //{
                //    NoDrivers.Visibility = Visibility.Hidden;
                //    NoInternet.Visibility = Visibility.Hidden;
                //    NoInternetFound.Visibility = Visibility.Hidden;
                //    Ethernet.Visibility = Visibility.Hidden;
                //    HasInternet.Visibility = Visibility.Hidden;
                //    WeakInternet.Visibility = Visibility.Hidden;
                //    Airplane.Visibility = Visibility.Visible;
                //}

                //if (Nwking_IsLocalNw && !Nwking_IsConnected && !Nwking_IsSignalWeak && isAirPlaneOn == "0")
                //{
                //    NoDrivers.Visibility = Visibility.Hidden;
                //    NoInternet.Visibility = Visibility.Hidden;
                //    NoInternetFound.Visibility = Visibility.Hidden;
                //    Ethernet.Visibility = Visibility.Hidden;
                //    HasInternet.Visibility = Visibility.Hidden;
                //    WeakInternet.Visibility = Visibility.Visible;
                //    Airplane.Visibility = Visibility.Hidden;
                //}

                //if (!Nwking_IsConnected && !Nwking_IsLocalNw && !Nwking_IsSignalWeak && isAirPlaneOn == "0" && Nwking_IsConnected5 == "SoftwareOn")
                //{
                //    NoDrivers.Visibility = Visibility.Hidden;
                //    NoInternet.Visibility = Visibility.Visible;
                //    NoInternetFound.Visibility = Visibility.Hidden;
                //    Ethernet.Visibility = Visibility.Hidden;
                //    HasInternet.Visibility = Visibility.Hidden;
                //    WeakInternet.Visibility = Visibility.Hidden;
                //    Airplane.Visibility = Visibility.Hidden;
                //}

                //if (!Nwking_IsConnected && !Nwking_IsLocalNw && !Nwking_IsSignalWeak && isAirPlaneOn == "0" && Nwking_IsConnected5 == "SoftwareOff")
                //{
                //    NoDrivers.Visibility = Visibility.Hidden;
                //    NoInternet.Visibility = Visibility.Hidden;
                //    NoInternetFound.Visibility = Visibility.Visible;
                //    Ethernet.Visibility = Visibility.Hidden;
                //    HasInternet.Visibility = Visibility.Hidden;
                //    WeakInternet.Visibility = Visibility.Hidden;
                //    Airplane.Visibility = Visibility.Hidden;
                //}

                //if (!Nwking_IsConnected && !Nwking_IsLocalNw && !Nwking_IsSignalWeak && isAirPlaneOn == "0")
                //{
                //    NoDrivers.Visibility = Visibility.Hidden;
                //    NoInternet.Visibility = Visibility.Hidden;
                //    NoInternetFound.Visibility = Visibility.Hidden;
                //    Ethernet.Visibility = Visibility.Hidden;
                //    HasInternet.Visibility = Visibility.Hidden;
                //    WeakInternet.Visibility = Visibility.Visible;
                //    Airplane.Visibility = Visibility.Hidden;
                //}

                //if (!Nwking_IsConnected && !Nwking_IsConnected && !Nwking_IsSignalWeak && isAirPlaneOn == "1")
                //{
                //    NoDrivers.Visibility = Visibility.Hidden;
                //    NoInternet.Visibility = Visibility.Hidden;
                //    NoInternetFound.Visibility = Visibility.Hidden;
                //    Ethernet.Visibility = Visibility.Hidden;
                //    HasInternet.Visibility = Visibility.Hidden;
                //    WeakInternet.Visibility = Visibility.Hidden;
                //    Airplane.Visibility = Visibility.Visible;
                //}
                //}
                #endregion
            }));
        }

        // Handle the UI exceptions by showing a dialog box, and asking the user whether
        // or not they wish to abort execution.
        private static void Form1_UIThreadException(object sender, ThreadExceptionEventArgs t)
        {
            System.Windows.Forms.Application.Restart();
        }

        private void CheckBatteryStatus()
        {
            void setBatteryIcon()
            {
                // This syntax is available since C# 9
                BatteryLife.Source = (double)SystemInformation.PowerStatus.BatteryLifePercent switch
                {
                    >= 0.96 => new BitmapImage(new Uri(@$"/Assets/Images/BatteryFull{colorScheme}.png", UriKind.Relative)),
                    >= 0.86 and < 0.96 => new BitmapImage(new Uri(@$"/Assets/Images/Battery/Battery90{colorScheme}.png", UriKind.Relative)),
                    >= 0.76 and < 0.86 => new BitmapImage(new Uri(@$"/Assets/Images/Battery/Battery80{colorScheme}.png", UriKind.Relative)),
                    >= 0.66 and < 0.76 => new BitmapImage(new Uri(@$"/Assets/Images/Battery/Battery70{colorScheme}.png", UriKind.Relative)),
                    >= 0.56 and < 0.66 => new BitmapImage(new Uri(@$"/Assets/Images/Battery/Battery60{colorScheme}.png", UriKind.Relative)),
                    >= 0.46 and < 0.56 => new BitmapImage(new Uri(@$"/Assets/Images/Battery/Battery50{colorScheme}.png", UriKind.Relative)),
                    >= 0.36 and < 0.46 => new BitmapImage(new Uri(@$"/Assets/Images/Battery/Battery40{colorScheme}.png", UriKind.Relative)),
                    >= 0.26 and < 0.36 => new BitmapImage(new Uri(@$"/Assets/Images/Battery/Battery30{colorScheme}.png", UriKind.Relative)),
                    >= 0.16 and < 0.26 => new BitmapImage(new Uri(@$"/Assets/Images/Battery/Battery20{colorScheme}.png", UriKind.Relative)),
                    >= 0.06 and < 0.16 => new BitmapImage(new Uri(@$"/Assets/Images/Battery/Battery10{colorScheme}.png", UriKind.Relative)),
                    >= 0.01 and < 0.06 => new BitmapImage(new Uri(@$"/Assets/Images/Battery/Battery5{colorScheme}.png", UriKind.Relative)),
                    _ => new BitmapImage(new Uri(@$"/Assets/Images/Battery/Battery0{colorScheme}.png", UriKind.Relative))
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
                    BatteryLife.Source = new BitmapImage(new Uri(@$"/Assets/Images/Battery0{colorScheme}.png", UriKind.Relative));
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