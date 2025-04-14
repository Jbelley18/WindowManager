using System;
using System.Windows;
using System.Windows.Forms;
using System.Windows.Input;
using MessageBox = System.Windows.MessageBox;

namespace WindowManager
{
    public partial class SettingWindow : Window
    {
        private Settings _settings;
        private System.Windows.Forms.Keys _currentShortcut1;
        private System.Windows.Forms.Keys _currentShortcut2;
        private System.Windows.Forms.Keys _currentShortcut3;
        
        private bool _isRecordingShortcut1 = false;
        private bool _isRecordingShortcut2 = false;
        private bool _isRecordingShortcut3 = false;
        
        public SettingWindow()
        {
            InitializeComponent();
            
            // Load settings
            _settings = Settings.Load();
            
            // Initialize current shortcuts
            _currentShortcut1 = _settings.CenterWindowKey1;
            _currentShortcut2 = _settings.CenterWindowKey2;
            _currentShortcut3 = _settings.CenterWindowKey3;
            
            // Display current shortcuts
            UpdateShortcutDisplay();
            
            // Connect event handlers (since they're not in the XAML)
            ChangeShortcut1.Click += ChangeShortcut1_Click;
            ChangeShortcut2.Click += ChangeShortcut2_Click;
            ChangeShortcut3.Click += ChangeShortcut3_Click;
            SaveButton.Click += SaveButton_Click;
            CancelButton.Click += CancelButton_Click;
            
            // Handle keyboard events
            this.PreviewKeyDown += SettingWindow_PreviewKeyDown;
        }
        
        private void UpdateShortcutDisplay()
        {
            ShortcutText1.Text = GetKeyDescription(_currentShortcut1);
            ShortcutText2.Text = GetKeyDescription(_currentShortcut2);
            ShortcutText3.Text = GetKeyDescription(_currentShortcut3);
        }
        
        private string GetKeyDescription(System.Windows.Forms.Keys key)
        {
            string description = "";
            
            // Check for modifiers
            if ((key & System.Windows.Forms.Keys.Control) == System.Windows.Forms.Keys.Control)
                description += "Ctrl + ";
                
            if ((key & System.Windows.Forms.Keys.Shift) == System.Windows.Forms.Keys.Shift)
                description += "Shift + ";
                
            if ((key & System.Windows.Forms.Keys.Alt) == System.Windows.Forms.Keys.Alt)
                description += "Alt + ";
            
            // Get the main key (without modifiers)
            System.Windows.Forms.Keys mainKey = key & ~(System.Windows.Forms.Keys.Control | System.Windows.Forms.Keys.Shift | System.Windows.Forms.Keys.Alt);
            description += mainKey.ToString();
            
            return description;
        }
        
        private void ChangeShortcut1_Click(object sender, RoutedEventArgs e)
        {
            ResetRecordingState();
            _isRecordingShortcut1 = true;
            ShortcutText1.Text = "Press key combination...";
            ChangeShortcut1.Content = "Cancel";
            ChangeShortcut1.Click -= ChangeShortcut1_Click;
            ChangeShortcut1.Click += CancelShortcut1_Click;
        }
        
        private void CancelShortcut1_Click(object sender, RoutedEventArgs e)
        {
            ResetRecordingState();
            UpdateShortcutDisplay();
        }
        
        private void ChangeShortcut2_Click(object sender, RoutedEventArgs e)
        {
            ResetRecordingState();
            _isRecordingShortcut2 = true;
            ShortcutText2.Text = "Press key combination...";
            ChangeShortcut2.Content = "Cancel";
            ChangeShortcut2.Click -= ChangeShortcut2_Click;
            ChangeShortcut2.Click += CancelShortcut2_Click;
        }
        
        private void CancelShortcut2_Click(object sender, RoutedEventArgs e)
        {
            ResetRecordingState();
            UpdateShortcutDisplay();
        }
        
        private void ChangeShortcut3_Click(object sender, RoutedEventArgs e)
        {
            ResetRecordingState();
            _isRecordingShortcut3 = true;
            ShortcutText3.Text = "Press key combination...";
            ChangeShortcut3.Content = "Cancel";
            ChangeShortcut3.Click -= ChangeShortcut3_Click;
            ChangeShortcut3.Click += CancelShortcut3_Click;
        }
        
        private void CancelShortcut3_Click(object sender, RoutedEventArgs e)
        {
            ResetRecordingState();
            UpdateShortcutDisplay();
        }
        
        private void ResetRecordingState()
        {
            _isRecordingShortcut1 = false;
            _isRecordingShortcut2 = false;
            _isRecordingShortcut3 = false;
            
            ChangeShortcut1.Content = "Change";
            ChangeShortcut1.Click -= CancelShortcut1_Click;
            ChangeShortcut1.Click += ChangeShortcut1_Click;
            
            ChangeShortcut2.Content = "Change";
            ChangeShortcut2.Click -= CancelShortcut2_Click;
            ChangeShortcut2.Click += ChangeShortcut2_Click;
            
            ChangeShortcut3.Content = "Change";
            ChangeShortcut3.Click -= CancelShortcut3_Click;
            ChangeShortcut3.Click += ChangeShortcut3_Click;
        }
        
        private void SettingWindow_PreviewKeyDown(object sender, System.Windows.Input.KeyEventArgs e)
        {
            // If we're recording a shortcut
            if (_isRecordingShortcut1 || _isRecordingShortcut2 || _isRecordingShortcut3)
            {
                // Cancel if Escape is pressed
                if (e.Key == System.Windows.Input.Key.Escape)
                {
                    ResetRecordingState();
                    UpdateShortcutDisplay();
                    e.Handled = true;
                    return;
                }
                
                // Get the key
                System.Windows.Forms.Keys key = (System.Windows.Forms.Keys)KeyInterop.VirtualKeyFromKey(e.Key);
                
                // Add modifiers
                if (Keyboard.IsKeyDown(System.Windows.Input.Key.LeftCtrl) || Keyboard.IsKeyDown(System.Windows.Input.Key.RightCtrl))
                    key |= System.Windows.Forms.Keys.Control;
                    
                if (Keyboard.IsKeyDown(System.Windows.Input.Key.LeftShift) || Keyboard.IsKeyDown(System.Windows.Input.Key.RightShift))
                    key |= System.Windows.Forms.Keys.Shift;
                    
                if (Keyboard.IsKeyDown(System.Windows.Input.Key.LeftAlt) || Keyboard.IsKeyDown(System.Windows.Input.Key.RightAlt))
                    key |= System.Windows.Forms.Keys.Alt;
                
                // Ensure it has at least one modifier
                if ((key & (System.Windows.Forms.Keys.Control | System.Windows.Forms.Keys.Shift | System.Windows.Forms.Keys.Alt)) == 0)
                {
                    MessageBox.Show("Please include at least one modifier key (Ctrl, Shift, or Alt).",
                                   "Invalid Shortcut", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }
                
                // Update the current shortcut
                if (_isRecordingShortcut1)
                    _currentShortcut1 = key;
                else if (_isRecordingShortcut2)
                    _currentShortcut2 = key;
                else if (_isRecordingShortcut3)
                    _currentShortcut3 = key;
                
                // Reset recording state and update display
                ResetRecordingState();
                UpdateShortcutDisplay();
                
                e.Handled = true;
            }
        }
        
        private void SaveButton_Click(object sender, RoutedEventArgs e)
        {
            // Update settings
            _settings.CenterWindowKey1 = _currentShortcut1;
            _settings.CenterWindowKey2 = _currentShortcut2;
            _settings.CenterWindowKey3 = _currentShortcut3;
            
            // Save settings
            _settings.Save();
            
            // Close dialog
            DialogResult = true;
        }
        
        private void CancelButton_Click(object sender, RoutedEventArgs e)
        {
            // Close dialog without saving
            DialogResult = false;
        }
    }
}