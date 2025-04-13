using System;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Windows.Forms;

namespace WindowManager
{
    public class KeyboardHook : IDisposable
    {
        private const int WH_KEYBOARD_LL = 13;
        private const int WM_KEYDOWN = 0x0100;
        private const int WM_KEYUP = 0x0101;
        private const int WM_SYSKEYDOWN = 0x0104;
        private const int WM_SYSKEYUP = 0x0105;
        private bool _isCtrlPressed = false;
        private bool _isAltPressed = false;
        private bool _isShiftPressed = false;
        
        
        [DllImport("user32.dll", CharSet = CharSet.Auto, SetLastError = true)]
        private static extern IntPtr SetWindowsHookEx(int idHook, LowLevelKeyboardProc lpfn, IntPtr hMod, uint dwThreadId);

        [DllImport("user32.dll", CharSet = CharSet.Auto, SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        private static extern bool UnhookWindowsHookEx(IntPtr hhk);

        [DllImport("user32.dll", CharSet = CharSet.Auto, SetLastError = true)]
        private static extern IntPtr CallNextHookEx(IntPtr hhk, int nCode, IntPtr wParam, IntPtr lParam);

        [DllImport("kernel32.dll", CharSet = CharSet.Auto, SetLastError = true)]
        private static extern IntPtr GetModuleHandle(string lpModuleName);

        private delegate IntPtr LowLevelKeyboardProc(int nCode, IntPtr wParam, IntPtr lParam);

        private LowLevelKeyboardProc _proc;
        private IntPtr _hookId = IntPtr.Zero;

        public event EventHandler<KeyEventArgs> KeyDown;
        public event EventHandler<KeyEventArgs> KeyUp;

        public KeyboardHook()
        {
            _proc = HookCallback;
        }

        public void Install()
        {
            _hookId = SetHook(_proc);
            Console.WriteLine($"Keyboard hook installed: {_hookId}");
            Debug.WriteLine($"Keyboard hook installed: {_hookId}");
        }

        private IntPtr SetHook(LowLevelKeyboardProc proc)
        {
            using (Process curProcess = Process.GetCurrentProcess())
            using (ProcessModule curModule = curProcess.MainModule)
            {
                return SetWindowsHookEx(WH_KEYBOARD_LL, proc, GetModuleHandle(curModule.ModuleName), 0);
            }
        }

// In KeyboardHook.cs
private IntPtr HookCallback(int nCode, IntPtr wParam, IntPtr lParam)
{
    if (nCode >= 0)
    {
        int vkCode = Marshal.ReadInt32(lParam);
        Keys key = (Keys)vkCode;
        
        // Log every key event
        Console.WriteLine($"Key event: {key}, wParam: {wParam}");
        Debug.WriteLine($"Key event: {key}, wParam: {wParam}");

        // Check if it's a key down or key up event
        if (wParam == (IntPtr)WM_KEYDOWN || wParam == (IntPtr)WM_SYSKEYDOWN)
        {
            // Check for key combinations
            bool control = (Control.ModifierKeys & Keys.Control) != 0;
            bool shift = (Control.ModifierKeys & Keys.Shift) != 0;
            bool alt = (Control.ModifierKeys & Keys.Alt) != 0;
            
            Console.WriteLine($"Modifiers - Ctrl: {control}, Shift: {shift}, Alt: {alt}");
            Debug.WriteLine($"Modifiers - Ctrl: {control}, Shift: {shift}, Alt: {alt}");

            // Handle Ctrl+Alt+C
            if (control && alt && key == Keys.C)
            {
                Console.WriteLine("Keyboard hook detected Ctrl+Alt+C");
                Debug.WriteLine("Keyboard hook detected Ctrl+Alt+C");
                OnKeyDown(new KeyEventArgs(Keys.C | Keys.Control | Keys.Alt));
                return (IntPtr)1; // Mark as handled
            }

            // Handle Ctrl+Shift+F11
            if (control && shift && key == Keys.F11)
            {
                Console.WriteLine("Keyboard hook detected Ctrl+Shift+F11");
                Debug.WriteLine("Keyboard hook detected Ctrl+Shift+F11");
                OnKeyDown(new KeyEventArgs(Keys.F11 | Keys.Control | Keys.Shift));
                return (IntPtr)1; // Mark as handled
            }

            // Handle Alt+F10
            if (alt && !control && !shift && key == Keys.F10)
            {
                Console.WriteLine("Keyboard hook detected Alt+F10");
                Debug.WriteLine("Keyboard hook detected Alt+F10");
                OnKeyDown(new KeyEventArgs(Keys.F10 | Keys.Alt));
                return (IntPtr)1; // Mark as handled
            }
        }
        else if (wParam == (IntPtr)WM_KEYUP || wParam == (IntPtr)WM_SYSKEYUP)
        {
            // Similar code for key up events if needed
            OnKeyUp(new KeyEventArgs((Keys)vkCode));
        }
    }

    return CallNextHookEx(_hookId, nCode, wParam, lParam);
}

    return CallNextHookEx(_hookId, nCode, wParam, lParam);
}

        protected virtual void OnKeyDown(KeyEventArgs e)
        {
            KeyDown?.Invoke(this, e);
        }

        protected virtual void OnKeyUp(KeyEventArgs e)
        {
            KeyUp?.Invoke(this, e);
        }

        public void Dispose()
        {
            if (_hookId != IntPtr.Zero)
            {
                UnhookWindowsHookEx(_hookId);
                _hookId = IntPtr.Zero;
            }
        }
    }
}