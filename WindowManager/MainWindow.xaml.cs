using System;
using System.Diagnostics;
using System.Windows;
using System.Windows.Forms; // For NotifyIcon
using System.Runtime.InteropServices; // For P/Invoke
using System.Drawing; // For System.Drawing.Icon
using System.Windows.Interop;

namespace WindowManager
{
    public partial class MainWindow : Window
    {
        
        private System.Windows.Forms.NotifyIcon _notifyIcon;
        
        
        // P/Invoke declarations for hotkey registration
        [DllImport("user32.dll")]
        private static extern bool RegisterHotKey(IntPtr hWnd, int id, int fsModifiers, int vk);
        
        [DllImport("user32.dll")]
        private static extern bool UnregisterHotKey(IntPtr hWnd, int id);
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
        
        // Constants for hotkey
        private const int WM_HOTKEY = 0x0312;
        private const int MOD_ALT = 0x0001;
        private const int MOD_CONTROL = 0x0002;
        private const int MOD_SHIFT = 0x0004;
        private const int MOD_WIN = 0x0008;
        
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
    
    // Register global hotkey (Alt+C for "Center")
    // 'C' key has virtual key code 67
    if (!RegisterHotKey(new WindowInteropHelper(this).Handle, 1, MOD_ALT, 67))
    {
        Console.WriteLine("Failed to register hotkey");
        Debug.WriteLine("Failed to register hotkey");
    }
    else
    {
        Console.WriteLine("Hotkey registered successfully");
        Debug.WriteLine("Hotkey registered successfully");
    }
    
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
            Console.WriteLine("CenterActiveWindow: Function has been called.");
            
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