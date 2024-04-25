using Newtonsoft.Json;
using System.IO;

namespace MigotoLauncher {
    internal struct Settings {
        private static readonly string settingsPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "MigotoLauncher", "settings.json");
        public string migotoPath { get; set; }
        public int comboBoxRegionSelectedItemIndex { get; set; }
        public string selfLocation { get; set; }
        public int DetectionDelayMs { get; set; }

        private Settings(string migotoPath, int comboBoxRegionSelectedItemIndex, string selfLocation, int DetectionDelayMs) {
            this.migotoPath = migotoPath;
            this.selfLocation = selfLocation;
            this.comboBoxRegionSelectedItemIndex = comboBoxRegionSelectedItemIndex;
            this.DetectionDelayMs = DetectionDelayMs;
        }


        public static Settings LoadSettings() {
            try {
                var rawJson = File.ReadAllText(settingsPath);
                var settings = JsonConvert.DeserializeObject<Settings>(rawJson);
                if (settings.DetectionDelayMs == 0) settings.DetectionDelayMs = 8000;
                return settings;
            } catch {
                Directory.CreateDirectory(settingsPath.Substring(0, settingsPath.LastIndexOf(@"\")));
            }

            return new Settings("", 0, "", 8000);
        }
        public void Save() {
            try {
                var rawJson = JsonConvert.SerializeObject(this);
                File.WriteAllText(settingsPath, rawJson);
                return;
            } catch {
                Directory.CreateDirectory(settingsPath.Substring(0, settingsPath.LastIndexOf(@"\")));
            }
        }
    }
}
