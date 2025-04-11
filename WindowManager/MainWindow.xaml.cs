using System;
using System.Windows;
using System.Windows.Forms; // For NotifyIcon
using System.Runtime.InteropServices; // For P/Invoke
using System.Drawing; // For System.Drawing.Icon

namespace WindowManager
{
    public partial class MainWindow : Window
    {
        private System.Windows.Forms.NotifyIcon _notifyIcon;
        
        // P/Invoke declarations for Windows API
        [DllImport("user32.dll")]
        private static extern IntPtr GetForegroundWindow();

        [DllImport("user32.dll")]
        private static extern bool GetWindowRect(IntPtr hWnd, out RECT lpRect);

        [DllImport("user32.dll")]
        private static extern bool MoveWindow(IntPtr hWnd, int X, int Y, int nWidth, int nHeight, bool bRepaint);

        [DllImport("user32.dll")]
        private static extern IntPtr MonitorFromWindow(IntPtr hwnd, uint dwFlags);

        [DllImport("user32.dll")]
        private static extern bool GetMonitorInfo(IntPtr hMonitor, ref MONITORINFO lpmi);

        // Constants
        private const uint MONITOR_DEFAULTTONEAREST = 0x00000002;

        // Structures
        [StructLayout(LayoutKind.Sequential)]
        public struct RECT
        {
            public int Left;
            public int Top;
            public int Right;
            public int Bottom;
        }

        [StructLayout(LayoutKind.Sequential)]
        public struct MONITORINFO
        {
            public int cbSize;
            public RECT rcMonitor;
            public RECT rcWork;
            public uint dwFlags;
        }

        public MainWindow()
        {
            InitializeComponent();
            
            // Create and configure the tray icon
            CreateTrayIcon();
            
            // Hide the main window but keep the application running
            this.ShowInTaskbar = false;
            this.Visibility = Visibility.Hidden;
        }
        
        private void CreateTrayIcon()
        {
            _notifyIcon = new System.Windows.Forms.NotifyIcon
            {
                Icon = System.Drawing.SystemIcons.Application, // We'll use a system icon for now
                Visible = true,
                Text = "Window Manager"
            };
            
            // Create context menu
            var contextMenu = new System.Windows.Forms.ContextMenuStrip();
            contextMenu.Items.Add("Center Active Window", null, OnCenterWindow);
            contextMenu.Items.Add("-"); // Separator
            contextMenu.Items.Add("Exit", null, OnExit);
            
            _notifyIcon.ContextMenuStrip = contextMenu;
            
            // Optional: Show settings on double-click
            _notifyIcon.DoubleClick += (s, e) => ShowSettings();
        }
        
        private void OnCenterWindow(object sender, EventArgs e)
        {
            CenterActiveWindow();
        }
        
        private void OnExit(object sender, EventArgs e)
        {
            // Clean up and exit
            _notifyIcon.Visible = false;
            _notifyIcon.Dispose();
            System.Windows.Application.Current.Shutdown();
        }
        
        private void ShowSettings()
        {
            // For future use - show settings window
            this.Visibility = Visibility.Visible;
            this.WindowState = WindowState.Normal;
            this.Activate();
        }
        
        private void CenterActiveWindow()
        {
            // Get handle to foreground window
            IntPtr hWnd = GetForegroundWindow();
            if (hWnd == IntPtr.Zero)
                return;

            // Get window dimensions
            RECT windowRect;
            if (!GetWindowRect(hWnd, out windowRect))
                return;

            int windowWidth = windowRect.Right - windowRect.Left;
            int windowHeight = windowRect.Bottom - windowRect.Top;

            // Get the monitor the window is currently on
            IntPtr hMonitor = MonitorFromWindow(hWnd, MONITOR_DEFAULTTONEAREST);
            
            // Get monitor info
            MONITORINFO monitorInfo = new MONITORINFO();
            monitorInfo.cbSize = Marshal.SizeOf(typeof(MONITORINFO));
            if (!GetMonitorInfo(hMonitor, ref monitorInfo))
                return;

            // Calculate center position
            int centerX = monitorInfo.rcWork.Left + (monitorInfo.rcWork.Right - monitorInfo.rcWork.Left - windowWidth) / 2;
            int centerY = monitorInfo.rcWork.Top + (monitorInfo.rcWork.Bottom - monitorInfo.rcWork.Top - windowHeight) / 2;

            // Move window to center
            MoveWindow(hWnd, centerX, centerY, windowWidth, windowHeight, true);
        }
        
        protected override void OnClosed(EventArgs e)
        {
            // Clean up the tray icon when window is closed
            _notifyIcon.Visible = false;
            _notifyIcon.Dispose();
            base.OnClosed(e);
        }
    }
}