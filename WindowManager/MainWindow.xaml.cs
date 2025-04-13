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
using MessageBox = System.Windows.Forms.MessageBox;
using System.Threading;

namespace WindowManager
{
    public partial class MainWindow : Window
    {
        private System.Windows.Forms.NotifyIcon _notifyIcon;
        private KeyboardHook _keyboardHook;
        
        // Define a delegate for the EnumWindows callback
        private delegate bool EnumWindowsProc(IntPtr hWnd, IntPtr lParam);
        
        // P/Invoke declaration for EnumWindows
        [DllImport("user32.dll")]
        [return: MarshalAs(UnmanagedType.Bool)]
        private static extern bool EnumWindows(EnumWindowsProc lpEnumFunc, IntPtr lParam);
        
        // explicit window activation before centering:
        [DllImport("user32.dll")]
        private static extern bool SetForegroundWindow(IntPtr hWnd);
        
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
        
        //TESTING
        private void TestKeyboardHooks()
        {
            // Check if keyboard hook is initialized
            if (_keyboardHook == null)
            {
                MessageBox.Show("Keyboard hook is not initialized. Initializing now...");
        
                // Initialize keyboard hook if it's null
                _keyboardHook = new KeyboardHook();
                _keyboardHook.KeyDown += KeyboardHook_KeyDown;
                _keyboardHook.Install();
            }
            else
            {
                MessageBox.Show("Keyboard hook is already initialized and should be working.\n" +
                                "Try pressing Ctrl+Alt+C, Ctrl+Shift+F11, or Alt+F10 to center a window.");
            }
        }
        
        public MainWindow()
        {
            InitializeComponent();
            
            // Create and configure the tray icon
            CreateTrayIcon();
            
            // Test window enumeration
            TestWindowEnumeration();
            
            // Explicitly initialize keyboard hook here
            if (_keyboardHook == null)
            {
                _keyboardHook = new KeyboardHook();
                _keyboardHook.KeyDown += KeyboardHook_KeyDown;
                _keyboardHook.Install();
                Console.WriteLine("Keyboard hook initialized in constructor");
                Debug.WriteLine("Keyboard hook initialized in constructor");
            }

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
            
            // Skip if window has no title or is our application window
            IntPtr ourWindowHandle = new WindowInteropHelper(this).Handle;
            if (string.IsNullOrEmpty(title.ToString()) || hWnd == ourWindowHandle)
            {
                Console.WriteLine("Skipping window with no title or our own window");
                Debug.WriteLine("Skipping window with no title or our own window");
                return;
            }

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
            
            Thread.Sleep(100); // Small delay to ensure window is ready


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
            
            //TESTING
            _notifyIcon.ContextMenuStrip.Items.Add("Test Keyboard Hooks", null, (s, e) => TestKeyboardHooks());
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
            Console.WriteLine($"Window handle: {handle}");
            Debug.WriteLine($"Window handle: {handle}");

            // Initialize keyboard hook if not already initialized
            if (_keyboardHook == null)
            {
                _keyboardHook = new KeyboardHook();
                _keyboardHook.KeyDown += KeyboardHook_KeyDown;
                _keyboardHook.Install();
        
                Console.WriteLine("Keyboard hook initialized in OnSourceInitialized");
                Debug.WriteLine("Keyboard hook initialized in OnSourceInitialized");
            }
            else
            {
                Console.WriteLine("Keyboard hook already initialized");
                Debug.WriteLine("Keyboard hook already initialized");
            }
        }

        // Handle keyboard hook events
        private void KeyboardHook_KeyDown(object sender, System.Windows.Forms.KeyEventArgs e)
        {
            Console.WriteLine($"Keyboard hook triggered: {e.KeyData}");
            Debug.WriteLine($"Keyboard hook triggered: {e.KeyData}");
            
            // Check for Ctrl+Alt+C
            if (e.KeyData == (System.Windows.Forms.Keys.C | System.Windows.Forms.Keys.Control | System.Windows.Forms.Keys.Alt))
            {
                Console.WriteLine("Ctrl+Alt+C detected, centering window");
                Debug.WriteLine("Ctrl+Alt+C detected, centering window");
                CenterActiveWindow();
            }
            // Check for Ctrl+Shift+F11
            else if (e.KeyData == (System.Windows.Forms.Keys.F11 | System.Windows.Forms.Keys.Control | System.Windows.Forms.Keys.Shift))
            {
                Console.WriteLine("Ctrl+Shift+F11 detected, centering window");
                Debug.WriteLine("Ctrl+Shift+F11 detected, centering window");
                CenterActiveWindow();
            }
            // Check for Alt+F10
            else if (e.KeyData == (System.Windows.Forms.Keys.F10 | System.Windows.Forms.Keys.Alt))
            {
                Console.WriteLine("Alt+F10 detected, centering window");
                Debug.WriteLine("Alt+F10 detected, centering window");
                CenterActiveWindow();
            }
        }
        
        protected override void OnClosed(EventArgs e)
        {
            // Clean up keyboard hook
            if (_keyboardHook != null)
            {
                _keyboardHook.Dispose();
                _keyboardHook = null;
                Console.WriteLine("Keyboard hook disposed");
                Debug.WriteLine("Keyboard hook disposed");
            }

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