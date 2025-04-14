using System;
using System.IO;
using System.Text.Json;
using System.Windows.Forms;

namespace WindowManager
{
    public class Settings
    {
        // Default keyboard shortcuts
        public Keys CenterWindowKey1 { get; set; } = Keys.C | Keys.Control | Keys.Alt;
        public Keys CenterWindowKey2 { get; set; } = Keys.F11 | Keys.Control | Keys.Shift;
        public Keys CenterWindowKey3 { get; set; } = Keys.F10 | Keys.Alt;
        
        // File path for settings
        private static readonly string SettingsFilePath = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
            "WindowManager",
            "settings.json");
            
        // Load settings from file
        public static Settings Load()
        {
            try
            {
                // Create directory if it doesn't exist
                Directory.CreateDirectory(Path.GetDirectoryName(SettingsFilePath));
                
                // If file exists, load it
                if (File.Exists(SettingsFilePath))
                {
                    string json = File.ReadAllText(SettingsFilePath);
                    return JsonSerializer.Deserialize<Settings>(json) ?? new Settings();
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error loading settings: {ex.Message}");
                System.Diagnostics.Debug.WriteLine($"Error loading settings: {ex.Message}");
            }
            
            // Return default settings if loading fails
            return new Settings();
        }
        
        // Save settings to file
        public void Save()
        {
            try
            {
                // Create directory if it doesn't exist
                Directory.CreateDirectory(Path.GetDirectoryName(SettingsFilePath));
                
                // Serialize and save
                string json = JsonSerializer.Serialize(this, new JsonSerializerOptions { WriteIndented = true });
                File.WriteAllText(SettingsFilePath, json);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error saving settings: {ex.Message}");
                System.Diagnostics.Debug.WriteLine($"Error saving settings: {ex.Message}");
            }
        }
    }
}