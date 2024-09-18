using Charms_Bar_Port.Properties;
using Microsoft.Win32;
using NativeCode;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Diagnostics.Eventing.Reader;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Reflection.Metadata;
using System.Runtime;
using System.Runtime.InteropServices;
using System.Runtime.InteropServices.WindowsRuntime;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Timers;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Forms;
using System.Windows.Input;
using System.Windows.Interop;
using System.Windows.Markup;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Media.Media3D;
using System.Windows.Navigation;
using System.Windows.Shapes;
using System.Windows.Threading;
using System.Xml;
using System.Xml.Linq;
using Windows.ApplicationModel;
using Windows.ApplicationModel.DataTransfer;
using Windows.ApplicationModel.DataTransfer.ShareTarget;
using Windows.Storage;
using Windows.Storage.Pickers;
using Windows.Storage.Provider;
using Windows.Storage.Search;
using Windows.Storage.Streams;
using Windows.UI;
using Windows.UI.Core;
using Windows.UI.Text;
using WinRT;

namespace CharmsBarPort
{
    #region Cursor info
    public static class CursorExtensions
    {
        [StructLayout(LayoutKind.Sequential)]
        struct PointStruct
        {
            public Int32 x;
            public Int32 y;
        }

        [StructLayout(LayoutKind.Sequential)]
        struct CursorInfoStruct
        {
            /// <summary> The structure size in bytes that must be set via calling Marshal.SizeOf(typeof(CursorInfoStruct)).</summary>
            public Int32 cbSize;
            /// <summary> The cursor state: 0 == hidden, 1 == showing, 2 == suppressed (is supposed to be when finger touch is used, but in practice finger touch results in 0, not 2)</summary>
            public Int32 flags;
            /// <summary> A handle to the cursor. </summary>
            public IntPtr hCursor;
            /// <summary> The cursor screen coordinates.</summary>
            public PointStruct pt;
        }

        /// <summary> Must initialize cbSize</summary>
        [DllImport("user32.dll")]
        static extern bool GetCursorInfo(ref CursorInfoStruct pci);
        public static bool IsVisible(this System.Windows.Forms.Cursor cursor)
        {
            CursorInfoStruct pci = new CursorInfoStruct();
            pci.cbSize = Marshal.SizeOf(typeof(CursorInfoStruct));
            GetCursorInfo(ref pci);
            // const Int32 hidden = 0x00;
            const Int32 showing = 0x01;
            // const Int32 suppressed = 0x02;
            bool isVisible = ((pci.flags & showing) != 0);
            return isVisible;
        }

    }
    #endregion Cursor info

    /// <summary>
    /// Handles mouse/touch activations and animations.
    /// Requires a massive rewrite/improvements.
    /// </summary>
    public sealed partial class CharmsBar : Window
    {

        #region Bunch of external calls

        [ComImport]
        [Guid("3A3DCD6C-3EAB-43DC-BCDE-45671CE800C8")]
        [InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
        public interface IDataTransferManagerInterop
        {
            IntPtr GetForWindow([In] IntPtr appWindow, [In] ref Guid riid);
            void ShowShareUIForWindow(IntPtr appWindow);
        }

        /// <summary>
        /// DataTransferManager Global Unique Identifier.
        /// </summary>
        static readonly Guid _dtm_iid =
            new(0xa5caee9b, 0x8708, 0x49d1, 0x8d, 0x36,
                0x67, 0xd2, 0x5a, 0x8d, 0xa0, 0x0c);

        static readonly IntPtr HWND_TOPMOST = new IntPtr(-1);

        [DllImport("user32.dll")]
        [return: MarshalAs(UnmanagedType.Bool)]
        public static extern bool SetWindowPos(IntPtr hWnd, IntPtr hWndInsertAfter, int X, int Y, int cx, int cy, uint uFlags);

        [DllImport("user32.dll")]
        public static extern bool SetForegroundWindow(IntPtr hWnd);

        [DllImport("user32.dll")]
        public static extern IntPtr GetForegroundWindow();

        [DllImport("user32.dll")]
        static extern int GetWindowText(IntPtr hWnd, StringBuilder text, int count);

        [DllImport("user32.dll", CharSet = CharSet.Auto, CallingConvention = CallingConvention.StdCall)]
        public static extern void mouse_event(uint dwFlags, uint dx, uint dy, uint cButtons, uint dwExtraInfo);

        const UInt32 SWP_NOSIZE = 0x0001;
        const UInt32 SWP_NOMOVE = 0x0002;
        const UInt32 SWP_SHOWWINDOW = 0x4000;
        const UInt32 TOPMOST_FLAGS = SWP_SHOWWINDOW | SWP_NOMOVE | SWP_NOSIZE;

        private const int MOUSEEVENTF_LEFTDOWN = 0x02;
        private const int MOUSEEVENTF_LEFTUP = 0x04;
        private const int MOUSEEVENTF_RIGHTDOWN = 0x08;
        private const int MOUSEEVENTF_RIGHTUP = 0x10;

        [DllImport("User32.dll")]
        private static extern bool SetCursorPos(int X, int Y);

        [DllImport("user32.dll", EntryPoint = "FindWindow", SetLastError = true)]
        static extern IntPtr FindWindow(string lpClassName, string lpWindowName);

        [DllImport("user32.dll", EntryPoint = "SendMessage", SetLastError = true)]
        static extern IntPtr SendMessage(IntPtr hWnd, Int32 Msg, IntPtr wParam, IntPtr lParam);

        const int WM_COMMAND = 0x111;
        const int MIN_ALL = 419;
        const int MIN_ALL_UNDO = 416;

        [DllImport("user32.dll", SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        static extern bool SystemParametersInfo(uint uiAction, uint uiParam, out bool pvParam, uint fWinIni);

        private static uint SPI_GETCLIENTAREAANIMATION = 0x1042;

        [DllImport("User32")]
        private static extern int keybd_event(byte bVk, byte bScan, uint dwFlags, long dwExtraInfo);

        //for Metro Apps
        [DllImport("user32.dll", SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        static extern bool GetWindowRect(IntPtr hWnd, ref RECT lpRect);

        [StructLayout(LayoutKind.Sequential)]
        private struct RECT
        {
            public int Left;
            public int Top;
            public int Right;
            public int Bottom;
        }

        [DllImport("user32.dll", SetLastError = true, CharSet = CharSet.Auto)]
        static extern int GetClassName(IntPtr hWnd, StringBuilder lpClassName, int nMaxCount);

        #endregion

        #region Unsorted variables

        public int rctLeft = 0;
        public int rctTop = 0;

        public bool findDevices = false;
        public bool openSettings = false;

        public int findTimer = 0;


        public bool holder = false;
        public int dasBoot = 0;


        public bool preventReload = false;
        public int blockRepeating = 0;
        public int cursorStay = 0;
        public bool charmsMenuOpen = false;

        public bool forceClose = false;
        public bool charmsFade = false;
        public int activeScreen = 0;
        public bool swipeIn = false;
        public bool keyboardShortcut = false;
        public bool charmsAppear = false;
        public bool charmsUse = false;
        public int charmsTimer = 0;
        public int charmsWait = 0;
        public bool WinCharmUse = false;
        public int charmsDelay = 50;
        public int myCharmsDelay = 50; // Desktop delay: you can customize the delay option through Regedit.
        public int charmsDelay2 = 50; // Metro delay: you can customize the delay option through Regedit.
        public int charmsReturn = 0; //to fix a problem where you can't swipe back in after it's gone
        public int activeIcon = 2;
        public bool mouseIn = false;
        public bool twoInputs = false;
        public int waitTimer = 0;
        public int keyboardTimer = 0;
        public bool charmsActivate = false;
        public double IHOb = 1.0;
        public bool escKey = false;
        public bool pokeCharms = false;
        public bool usingTouch = false;
        public bool isMetro = false; //Metro apps use their unique ways for stuff
        public bool isGui = false; //Fixing a problem where it appears behind the taskbar.
        public IntPtr mWnd = GetForegroundWindow();

        #endregion

        #region Animations

        //Supports Windows 8.1 / Windows 10 registry hacks!
        public string customDelay = "100";

        //For the animations!
        public bool useAnimations = false;
        public double winStretch = 80.31;
        public int dasSlide = 0;
        public int scrollSearch = 200;
        public int scrollShare = 150;
        public int scrollWin = 100;
        public int scrollDevices = 150;
        public int scrollSettings = 200;

        public int textSearch = 170;
        public int textShare = 150;
        public int textWin = 100;
        public int textDevices = 150;
        public int textSettings = 200;

        #endregion

        //mouse
        public bool ignoreMouseIn = false;
        public bool outofTime = false;
        public int numVal = 0;
        public int numVal2 = 0;

        #region Multiple monitor

        public bool dasSwiper = false;

        public int mainX = 0;

        public int screenwidth = 0; //just to make things more reliable
        public int screenheight = 0; //just to make things more reliable
        public int screenX = 0; //just to make things more reliable

        #endregion

        BrushConverter converter = new();
        Window CharmsClock = new CharmsClock();
        Window CharmsMenu = new CharmsMenu();

        System.Timers.Timer Timer = new()
        {
            Interval = 15,
            AutoReset = true,
            Enabled = true
        };

        public CharmsBar()
        {
            #region Basic window properties

            Height = SystemParameters.PrimaryScreenHeight;
            WindowStartupLocation = WindowStartupLocation.Manual;
            Left = Screen.PrimaryScreen.Bounds.Width - 86;
            Background = (Brush)converter.ConvertFromString("#111111");
            System.Windows.Forms.Application.ThreadException += new ThreadExceptionEventHandler(CharmsBar.Form1_UIThreadException);

            SystemParameters.StaticPropertyChanged += this.SystemParameters_StaticPropertyChanged;
            this.Loaded += ControlLoaded;
            this.Closing += new System.ComponentModel.CancelEventHandler(MainWindow_Closing);
            this.KeyDown += new System.Windows.Input.KeyEventHandler(MainWindow_KeyDown);

            #endregion

            InitializeComponent();
        }

        #region Utilities

        /// <summary>
        /// Presses Win+whatever keyboard shortcut.
        /// </summary>
        /// <param name="key">Thing to be pressed with Left Windows.</param>
        private void WinPlusKey(Key key)
        {
            if (this.IsActive)
            {
                byte winKey = (byte)KeyInterop.VirtualKeyFromKey(Key.LWin);
                byte plusKey = (byte)KeyInterop.VirtualKeyFromKey(key);

                const uint KeyEVT_Extended = 0x0001;
                const uint KeyEVT_Up = 0x0002;

                keybd_event(winKey, 0, KeyEVT_Extended, 0);
                keybd_event(plusKey, 0, KeyEVT_Extended, 0);

                keybd_event(winKey, 0, KeyEVT_Up, 0);
                keybd_event(plusKey, 0, KeyEVT_Up, 0);
            }
        }

        /// <summary>
        /// Presses a key.
        /// </summary>
        /// <param name="key"></param>
        private void PressAKey(Key key)
        {
            if (this.IsActive)
            {
                byte target = (byte)KeyInterop.VirtualKeyFromKey(key);
                keybd_event(target, 0, 0x0001, 0);
                keybd_event(target, 0, 0x0002, 0);
            }
        }

        private void HideEverythingAndShowInactiveIcons()
        {
            //if (useAnimations == false)
            //{
                this.Opacity = 0.000;
                CharmsClock.Opacity = 0.000;
                Background = (Brush)converter.ConvertFromString("#00111111");
            //}

            SearchBtn.UseInactiveMode();
            ShareBtn.UseInactiveMode();
            StartBtn.UseInactiveMode();
            DevicesBtn.UseInactiveMode();
            SettingsBtn.UseInactiveMode();
        }

        #endregion

        #region Window events

        void MainWindow_KeyDown(object sender, System.Windows.Input.KeyEventArgs e)
        {
            switch (e.Key)
            {
                case (Key.LWin | Key.C):
                case (Key.RWin | Key.C):
                {
                    HideEverythingAndShowInactiveIcons();
                    break;
                }
            }
        }

        void MainWindow_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            // Nuh uh don't close the program now
            e.Cancel = true;
        }

        protected override void OnClosed(EventArgs e)
        {
            SystemParameters.StaticPropertyChanged -= this.SystemParameters_StaticPropertyChanged;
            base.OnClosed(e);
        }

        public void ControlLoaded(object sender, EventArgs e)
        {
            var wih = new System.Windows.Interop.WindowInteropHelper(this);
            SetWindowPos(wih.Handle, HWND_TOPMOST, 100, 100, 300, 300, TOPMOST_FLAGS);
            Timer.Elapsed += OnTimedEvent;
            Timer.Start();
        }

        private void SystemParameters_StaticPropertyChanged(object sender, System.ComponentModel.PropertyChangedEventArgs e)
        {
            //if (e.PropertyName == "WindowGlassBrush")
            //{
            //    this.SetBackgroundColor();
            //}
        }

        // Handle the UI exceptions by showing a dialog box, and asking the user whether
        // or not they wish to abort execution.
        private static void Form1_UIThreadException(object sender, ThreadExceptionEventArgs t)
        {
            if (System.Windows.Forms.MessageBox.Show(
                    t.Exception.Message + "\nRestart the application?",
                    "An error occurred", MessageBoxButtons.YesNo
                )
                == System.Windows.Forms.DialogResult.Yes)
            {
                System.Windows.Forms.Application.Restart();
            }

            Environment.Exit(1);
        }

        #endregion

        #region Items Mouse Move & Leave events

        private void OnTimedEvent(object sender, ElapsedEventArgs e)
        {
            this.Dispatcher.Invoke(new Action(() =>
            {
                #region Cursor position check
                var mousePos = System.Windows.Forms.Cursor.Position;
                var posX = (int)mousePos.X;
                var posY = (int)mousePos.Y;

                Screen currentScreen = Screen.FromPoint(
                    new System.Drawing.Point(posX, posY)
                );

                if ((currentScreen != null) && (posX >= currentScreen.Bounds.Width - 98))
                {

                    if ((posY == 0) || (posY == currentScreen.Bounds.Height))
                    {

                        if (this.Height != currentScreen.Bounds.Height)
                        {
                            this.Height = currentScreen.Bounds.Height;
                        }

                        if (this.Opacity != 1.0)
                        {
                            this.Opacity = 1.0;
                        }

                        if (CharmsClock.Opacity != 1.0)
                        {
                            CharmsClock.Opacity = 1.0;
                        }

                        Background = (Brush)converter.ConvertFromString("#111111");

                        SearchBtn.UseInactiveMode(false);
                        ShareBtn.UseInactiveMode(false);
                        StartBtn.UseInactiveMode(false);
                        DevicesBtn.UseInactiveMode(false);
                        SettingsBtn.UseInactiveMode(false);
                    }
                }
                else
                {
                    this.Opacity = 0.0;
                    CharmsClock.Opacity = 0.0;
                }
                #endregion

                #region Keyboard check
                /// TODO
                #endregion
            }));
        }

        private void Charms_MouseLeave(object sender, System.EventArgs e)
        {
            HideEverythingAndShowInactiveIcons();
        }

        #endregion

        // These callbacks WON'T hide the bar (and the clock if it's visible)
        // Only do that on Charms_MouseLeave (usable for touch devices).
        #region Default buttons click

        private void Search_Down(object sender, RoutedEventArgs e)
        {
            WinPlusKey(Key.S);
        }

        private void Share_Down(object sender, RoutedEventArgs e)
        {
            var hWnd = new WindowInteropHelper(this).Handle;
            IDataTransferManagerInterop interop = DataTransferManager.As<IDataTransferManagerInterop>();

            var dataTransferManager = WinRT.MarshalInterface
                <Windows.ApplicationModel.DataTransfer.DataTransferManager>.FromAbi(interop.GetForWindow(hWnd, _dtm_iid));

            dataTransferManager.DataRequested += (sender, args) =>
            {
                args.Request.Data.Properties.Title = " ";
                args.Request.Data.SetText("WinRT.Interop.WindowNative.GetWindowHandle(this)");
                args.Request.Data.RequestedOperation =
                    Windows.ApplicationModel.DataTransfer.DataPackageOperation.Copy;
            };

            if (this.IsActive)
            {
                interop.ShowShareUIForWindow(hWnd);
            }
        }

        private void Start_Down(object sender, RoutedEventArgs e)
        {
            PressAKey(Key.LWin);
        }

        private void Devices_Down(object sender, RoutedEventArgs e)
        {
            WinPlusKey(Key.K);
        }

        private void Settings_Down(object sender, RoutedEventArgs e)
        {
            WinPlusKey(Key.I);
        }

        #endregion
    }
}
