using System;
using System.Diagnostics;
using System.Windows;
using System.Windows.Forms; // For NotifyIcon
using System.Runtime.InteropServices; // For P/Invoke
using System.Drawing; // For System.Drawing.Icon
using System.Windows.Interop;
using System.Collections.Generic;
using System.Text;
using System.ComponentModel;

namespace WindowManager
{
    public partial class MainWindow : Window
    {
        private System.Windows.Forms.NotifyIcon _notifyIcon;
        
        // Define a delegate for the EnumWindows callback
        private delegate bool EnumWindowsProc(IntPtr hWnd, IntPtr lParam);
        
        // P/Invoke declaration for EnumWindows
        [DllImport("user32.dll")]
        [return: MarshalAs(UnmanagedType.Bool)]
        private static extern bool EnumWindows(EnumWindowsProc lpEnumFunc, IntPtr lParam);
        
        // Get window text
        [DllImport("user32.dll", CharSet = CharSet.Auto, SetLastError = true)]
        private static extern int GetWindowText(IntPtr hWnd, StringBuilder lpString, int nMaxCount);
        
        // Get window text length
        [DllImport("user32.dll", SetLastError = true, CharSet = CharSet.Auto)]
        private static extern int GetWindowTextLength(IntPtr hWnd);
        
        // Check if window is visible
        [DllImport("user32.dll")]
        [return: MarshalAs(UnmanagedType.Bool)]
        private static extern bool IsWindowVisible(IntPtr hWnd);
        
        // Get window styles
        [DllImport("user32.dll", SetLastError = true)]
        private static extern int GetWindowLong(IntPtr hWnd, int nIndex);
        
        // Get window thread process id
        [DllImport("user32.dll", SetLastError = true)]
        private static extern uint GetWindowThreadProcessId(IntPtr hWnd, out uint lpdwProcessId);
        
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

        // Constants for hotkey
        private const int WM_HOTKEY = 0x0312;
        private const int MOD_ALT = 0x0001;
        private const int MOD_CONTROL = 0x0002;
        private const int MOD_SHIFT = 0x0004;
        private const int MOD_WIN = 0x0008;
        
        // Constants for monitor
        private const uint MONITOR_DEFAULTTONEAREST = 0x00000002;
        
        // Constants for GetWindowLong
        private const int GWL_STYLE = -16;
        private const int WS_VISIBLE = 0x10000000;
        private const int WS_CAPTION = 0x00C00000;

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
            
            // Test window enumeration
            TestWindowEnumeration();
            
            // Hide the main window but keep the application running
            this.ShowInTaskbar = false;
            this.Visibility = Visibility.Hidden;
        }

        private void CreateTrayIcon()
        {
            _notifyIcon = new System.Windows.Forms.NotifyIcon
            {
                Icon = System.Drawing.SystemIcons.Application,
                Visible = true,
                Text = "Window Manager"
            };
    
            // Create initial context menu
            _notifyIcon.ContextMenuStrip = new System.Windows.Forms.ContextMenuStrip();
    
            // Initialize the context menu with windows
            UpdateContextMenuWithWindows();
    
            // Optional: Show settings on double-click
            _notifyIcon.DoubleClick += (s, e) => ShowSettings();
        }
        
        private void OnCenterWindow(object sender, EventArgs e)
        {
            CenterActiveWindow();
        }
        
        private void OnListWindows(object sender, EventArgs e)
        {
            TestWindowEnumeration();
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
            Debug.WriteLine("CenterActiveWindow: Function has been called.");
            
            // Get handle to foreground window
            IntPtr hWnd = GetForegroundWindow();
            if (hWnd == IntPtr.Zero)
            {
                Console.WriteLine("No foreground window found");
                Debug.WriteLine("No foreground window found");
                return;
            }
            
            Console.WriteLine($"Foreground window handle: {hWnd}");
            Debug.WriteLine($"Foreground window handle: {hWnd}");
            
            // Get window title
            int length = GetWindowTextLength(hWnd);
            StringBuilder title = new StringBuilder(length + 1);
            GetWindowText(hWnd, title, title.Capacity);
            
            Console.WriteLine($"Window title: {title}");
            Debug.WriteLine($"Window title: {title}");

            // Get window dimensions
            RECT windowRect;
            if (!GetWindowRect(hWnd, out windowRect))
            {
                Console.WriteLine("Failed to get window rect");
                Debug.WriteLine("Failed to get window rect");
                return;
            }

            int windowWidth = windowRect.Right - windowRect.Left;
            int windowHeight = windowRect.Bottom - windowRect.Top;
            
            Console.WriteLine($"Window dimensions: {windowWidth}x{windowHeight}");
            Debug.WriteLine($"Window dimensions: {windowWidth}x{windowHeight}");

            // Get the monitor the window is currently on
            IntPtr hMonitor = MonitorFromWindow(hWnd, MONITOR_DEFAULTTONEAREST);
            
            Console.WriteLine($"Monitor handle: {hMonitor}");
            Debug.WriteLine($"Monitor handle: {hMonitor}");
            
            // Get monitor info
            MONITORINFO monitorInfo = new MONITORINFO();
            monitorInfo.cbSize = Marshal.SizeOf(typeof(MONITORINFO));
            if (!GetMonitorInfo(hMonitor, ref monitorInfo))
            {
                Console.WriteLine("Failed to get monitor info");
                Debug.WriteLine("Failed to get monitor info");
                return;
            }
            
            Console.WriteLine($"Monitor work area: {monitorInfo.rcWork.Left},{monitorInfo.rcWork.Top} to {monitorInfo.rcWork.Right},{monitorInfo.rcWork.Bottom}");
            Debug.WriteLine($"Monitor work area: {monitorInfo.rcWork.Left},{monitorInfo.rcWork.Top} to {monitorInfo.rcWork.Right},{monitorInfo.rcWork.Bottom}");

            // Calculate center position
            int centerX = monitorInfo.rcWork.Left + (monitorInfo.rcWork.Right - monitorInfo.rcWork.Left - windowWidth) / 2;
            int centerY = monitorInfo.rcWork.Top + (monitorInfo.rcWork.Bottom - monitorInfo.rcWork.Top - windowHeight) / 2;
            
            Console.WriteLine($"Calculated center position: {centerX},{centerY}");
            Debug.WriteLine($"Calculated center position: {centerX},{centerY}");

            // Move window to center
            bool moveResult = MoveWindow(hWnd, centerX, centerY, windowWidth, windowHeight, true);
            Console.WriteLine($"MoveWindow result: {moveResult}");
            Debug.WriteLine($"MoveWindow result: {moveResult}");
        }
        
        private void CenterSpecificWindow(IntPtr hWnd)
        {
            Console.WriteLine($"Attempting to center specific window: {hWnd}");
            Debug.WriteLine($"Attempting to center specific window: {hWnd}");
            
            // Get window title
            int length = GetWindowTextLength(hWnd);
            StringBuilder title = new StringBuilder(length + 1);
            GetWindowText(hWnd, title, title.Capacity);
            
            Console.WriteLine($"Window title: {title}");
            Debug.WriteLine($"Window title: {title}");
            
            // Get window dimensions
            RECT windowRect;
            if (!GetWindowRect(hWnd, out windowRect))
            {
                Console.WriteLine("Failed to get window rect");
                Debug.WriteLine("Failed to get window rect");
                return;
            }

            int windowWidth = windowRect.Right - windowRect.Left;
            int windowHeight = windowRect.Bottom - windowRect.Top;
            
            Console.WriteLine($"Window dimensions: {windowWidth}x{windowHeight}");
            Debug.WriteLine($"Window dimensions: {windowWidth}x{windowHeight}");

            // Get the monitor the window is currently on
            IntPtr hMonitor = MonitorFromWindow(hWnd, MONITOR_DEFAULTTONEAREST);
            
            Console.WriteLine($"Monitor handle: {hMonitor}");
            Debug.WriteLine($"Monitor handle: {hMonitor}");
            
            // Get monitor info
            MONITORINFO monitorInfo = new MONITORINFO();
            monitorInfo.cbSize = Marshal.SizeOf(typeof(MONITORINFO));
            if (!GetMonitorInfo(hMonitor, ref monitorInfo))
            {
                Console.WriteLine("Failed to get monitor info");
                Debug.WriteLine("Failed to get monitor info");
                return;
            }
            
            Console.WriteLine($"Monitor work area: {monitorInfo.rcWork.Left},{monitorInfo.rcWork.Top} to {monitorInfo.rcWork.Right},{monitorInfo.rcWork.Bottom}");
            Debug.WriteLine($"Monitor work area: {monitorInfo.rcWork.Left},{monitorInfo.rcWork.Top} to {monitorInfo.rcWork.Right},{monitorInfo.rcWork.Bottom}");

            // Calculate center position
            int centerX = monitorInfo.rcWork.Left + (monitorInfo.rcWork.Right - monitorInfo.rcWork.Left - windowWidth) / 2;
            int centerY = monitorInfo.rcWork.Top + (monitorInfo.rcWork.Bottom - monitorInfo.rcWork.Top - windowHeight) / 2;
            
            Console.WriteLine($"Calculated center position: {centerX},{centerY}");
            Debug.WriteLine($"Calculated center position: {centerX},{centerY}");

            // Move window to center
            bool moveResult = MoveWindow(hWnd, centerX, centerY, windowWidth, windowHeight, true);
            Console.WriteLine($"MoveWindow result: {moveResult}");
            Debug.WriteLine($"MoveWindow result: {moveResult}");
        }
        
        private bool RegisterHotkeyWithErrorCheck(IntPtr hwnd, int id, int modifiers, int key)
        {
            Console.WriteLine($"Attempting to register hotkey - ID: {id}, Modifiers: {modifiers}, Key: {key}");
            Debug.WriteLine($"Attempting to register hotkey - ID: {id}, Modifiers: {modifiers}, Key: {key}");
            
            if (!RegisterHotKey(hwnd, id, modifiers, key))
            {
                int errorCode = Marshal.GetLastWin32Error();
                string errorMessage = new Win32Exception(errorCode).Message;
                
                Console.WriteLine($"Failed to register hotkey. Error code: {errorCode}, Message: {errorMessage}");
                Debug.WriteLine($"Failed to register hotkey. Error code: {errorCode}, Message: {errorMessage}");
                
                return false;
            }
            
            Console.WriteLine($"Successfully registered hotkey - ID: {id}, Modifiers: {modifiers}, Key: {key}");
            Debug.WriteLine($"Successfully registered hotkey - ID: {id}, Modifiers: {modifiers}, Key: {key}");
            return true;
        }
        
        /// <summary>
        /// Get all visible windows with titles
        /// </summary>
        public List<WindowInfo> GetAllWindows()
        {
            List<WindowInfo> windows = new List<WindowInfo>();
            
            EnumWindows((hWnd, lParam) => 
            {
                // Check if window is visible
                if (!IsWindowVisible(hWnd))
                    return true; // Continue enumeration
                
                // Check if window has a title
                int length = GetWindowTextLength(hWnd);
                if (length == 0)
                    return true; // Continue enumeration
                
                // Get window style
                int style = GetWindowLong(hWnd, GWL_STYLE);
                
                // Check if it's a normal application window (visible with caption)
                if ((style & WS_VISIBLE) != 0 && (style & WS_CAPTION) != 0)
                {
                    StringBuilder title = new StringBuilder(length + 1);
                    GetWindowText(hWnd, title, title.Capacity);
                    
                    // Get process ID
                    uint processId = 0;
                    GetWindowThreadProcessId(hWnd, out processId);
                    
                    // Get process name
                    string processName = string.Empty;
                    try
                    {
                        Process process = Process.GetProcessById((int)processId);
                        processName = process.ProcessName;
                    }
                    catch
                    {
                        // Process might have exited
                        processName = "Unknown";
                    }
                    
                    // Get window rect
                    RECT rect;
                    if (GetWindowRect(hWnd, out rect))
                    {
                        WindowInfo window = new WindowInfo
                        {
                            Handle = hWnd,
                            Title = title.ToString(),
                            ProcessId = (int)processId,
                            ProcessName = processName,
                            Width = rect.Right - rect.Left,
                            Height = rect.Bottom - rect.Top,
                            Left = rect.Left,
                            Top = rect.Top
                        };
                        
                        windows.Add(window);
                        
                        Console.WriteLine($"Window: {window.Title}, Process: {window.ProcessName}, Size: {window.Width}x{window.Height}");
                        Debug.WriteLine($"Window: {window.Title}, Process: {window.ProcessName}, Size: {window.Width}x{window.Height}");
                    }
                }
                
                return true; // Continue enumeration
            }, IntPtr.Zero);
            
            return windows;
        }
        
        // Test the window enumeration
        private void TestWindowEnumeration()
        {
            Console.WriteLine("Enumerating all windows...");
            Debug.WriteLine("Enumerating all windows...");
            
            var windows = GetAllWindows();
            
            Console.WriteLine($"Found {windows.Count} windows");
            Debug.WriteLine($"Found {windows.Count} windows");
        }
        
        private void UpdateContextMenuWithWindows()
        {
            // Get all windows
            var windows = GetAllWindows();
    
            // Clear existing menu items
            _notifyIcon.ContextMenuStrip.Items.Clear();
    
            // Add standard menu items
            _notifyIcon.ContextMenuStrip.Items.Add("Center Active Window", null, OnCenterWindow);
            _notifyIcon.ContextMenuStrip.Items.Add("List All Windows", null, OnListWindows);
            _notifyIcon.ContextMenuStrip.Items.Add("Refresh Window List", null, OnRefreshWindowList);
            _notifyIcon.ContextMenuStrip.Items.Add("-"); // Separator
    
            // Add windows to the context menu (limited to 10 to avoid huge menus)
            var maxWindowsToShow = Math.Min(windows.Count, 10);
            for (int i = 0; i < maxWindowsToShow; i++)
            {
                var window = windows[i];
                string menuText = $"Center: {window.Title.Substring(0, Math.Min(window.Title.Length, 40))}";
                if (window.Title.Length > 40) menuText += "...";
        
                var menuItem = new ToolStripMenuItem(menuText);
                IntPtr windowHandle = window.Handle; // Capture the handle
                menuItem.Click += (sender, e) => CenterSpecificWindow(windowHandle);
                _notifyIcon.ContextMenuStrip.Items.Add(menuItem);
            }
    
            // Add exit item
            _notifyIcon.ContextMenuStrip.Items.Add("-"); // Separator
            _notifyIcon.ContextMenuStrip.Items.Add("Exit", null, OnExit);
        }
        
        private void OnRefreshWindowList(object sender, EventArgs e)
        {
            UpdateContextMenuWithWindows();
        }
        
        protected override void OnSourceInitialized(EventArgs e)
        {
            base.OnSourceInitialized(e);
    
            // Get the window handle
            IntPtr handle = new WindowInteropHelper(this).Handle;
    
            // Add a hook to the window procedure
            HwndSource source = HwndSource.FromHwnd(handle);
            source.AddHook(WndProc);
            
            // Register hotkeys now that the window is initialized
            Console.WriteLine($"Window handle: {handle}");
            Debug.WriteLine($"Window handle: {handle}");
            
            // Try Ctrl+Alt+C
            bool ctrlAltCRegistered = RegisterHotkeyWithErrorCheck(handle, 1, MOD_CONTROL | MOD_ALT, (int)Keys.C);
            
            // If Ctrl+Alt+C fails, try Alt+C
            if (!ctrlAltCRegistered)
            {
                bool altCRegistered = RegisterHotkeyWithErrorCheck(handle, 2, MOD_ALT, (int)Keys.C);
                
                // If Alt+C fails, try Shift+C
                if (!altCRegistered)
                {
                    RegisterHotkeyWithErrorCheck(handle, 3, MOD_SHIFT, (int)Keys.C);
                }
            }
        }

        private IntPtr WndProc(IntPtr hwnd, int msg, IntPtr wParam, IntPtr lParam, ref bool handled)
        {
            // Log all window messages for debugging
            Debug.WriteLine($"WndProc message: 0x{msg:X4}, wParam: {wParam.ToInt32()}, lParam: {lParam.ToInt64()}");
            
            // Check if the message is our hotkey
            if (msg == WM_HOTKEY)
            {
                int hotkeyId = wParam.ToInt32();
                Console.WriteLine($"HOTKEY DETECTED - ID: {hotkeyId}");
                Debug.WriteLine($"HOTKEY DETECTED - ID: {hotkeyId}");
                
                switch (hotkeyId)
                {
                    case 1:
                        Console.WriteLine("Ctrl+Alt+C hotkey pressed");
                        Debug.WriteLine("Ctrl+Alt+C hotkey pressed");
                        CenterActiveWindow();
                        break;
                        
                    case 2:
                        Console.WriteLine("Alt+C hotkey pressed");
                        Debug.WriteLine("Alt+C hotkey pressed");
                        CenterActiveWindow();
                        break;
                        
                    case 3:
                        Console.WriteLine("Shift+C hotkey pressed");
                        Debug.WriteLine("Shift+C hotkey pressed");
                        CenterActiveWindow();
                        break;
                        
                    default:
                        Console.WriteLine($"Unknown hotkey ID: {hotkeyId}");
                        Debug.WriteLine($"Unknown hotkey ID: {hotkeyId}");
                        break;
                }
                
                handled = true;
            }
            
            return IntPtr.Zero;
        }
        
        protected override void OnClosed(EventArgs e)
        {
            IntPtr handle = new WindowInteropHelper(this).Handle;
            
            // Unregister all hotkeys
            UnregisterHotKey(handle, 1); // Ctrl+Alt+C
            UnregisterHotKey(handle, 2); // Alt+C
            UnregisterHotKey(handle, 3); // Shift+C
            
            Console.WriteLine("Hotkeys unregistered");
            Debug.WriteLine("Hotkeys unregistered");
    
            // Clean up the tray icon when window is closed
            _notifyIcon.Visible = false;
            _notifyIcon.Dispose();
            base.OnClosed(e);
        }
    }
    
    // Class to hold window information
    public class WindowInfo
    {
        public IntPtr Handle { get; set; }
        public string Title { get; set; }
        public int ProcessId { get; set; }
        public string ProcessName { get; set; }
        public int Width { get; set; }
        public int Height { get; set; }
        public int Left { get; set; }
        public int Top { get; set; }
    }
}