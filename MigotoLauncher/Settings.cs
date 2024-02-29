using Newtonsoft.Json;
using System.IO;

namespace MigotoLauncher {
    internal struct Settings {
        private static readonly string settingsPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "MigotoLauncher", "settings.json");
        public string migotoPath { get; set; }
        public int comboBoxRegionSelectedItemIndex { get; set; }
        public string selfLocation { get; set; }

        private Settings(string migotoPath, int comboBoxRegionSelectedItemIndex, string selfLocation) {
            this.migotoPath = migotoPath;
            this.selfLocation = selfLocation;
            this.comboBoxRegionSelectedItemIndex = comboBoxRegionSelectedItemIndex;
        }


        public static Settings LoadSettings() {
            try {
                var rawJson = File.ReadAllText(settingsPath);
                return JsonConvert.DeserializeObject<Settings>(rawJson);
            } catch {
                Directory.CreateDirectory(settingsPath.Substring(0, settingsPath.LastIndexOf(@"\")));
            }

            return new Settings("", 0, "");
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
