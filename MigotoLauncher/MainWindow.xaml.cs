using System.Diagnostics;
using System.IO;
using System.Text.RegularExpressions;
using System.Windows;
using System.Windows.Input;

namespace MigotoLauncher {
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window {
        private static readonly string autostartPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "Microsoft", "Windows", "Start Menu", "Programs", "Startup", "MigotoLauncher.url");
        private Settings settings;
        private NotifyIcon notifyIcon = new NotifyIcon();
        private string genshinProcessName;
        private int lastGenshinPID;
        

        public MainWindow() {
            InitializeComponent();
            SetupNotifyIcon();

            settings = Settings.LoadSettings();
            IsAutostarting();
            ApplySettings();
            
            if (CheckBoxAutostart.IsChecked == true) {
                this.Hide();
                notifyIcon.Visible = true;
            }

            StartGenshinLauncherDetectionTask();
        }

        public Task StartGenshinLauncherDetectionTask() {
            return Task.Run(() => {
                while (true) {
                    Thread.Sleep(settings.DetectionDelayMs);
                    var genshinProcs = Process.GetProcessesByName(genshinProcessName);
                    if (genshinProcs.Length == 0) continue;
                    //prevent multiple 3d migoto starts caused by detecting genshin impact over and over
                    if (genshinProcs[0].Id == lastGenshinPID) continue;

                    lastGenshinPID = genshinProcs[0].Id;
                    LaunchMigoto();
                }
            });
        }

        public void LaunchMigoto() {
            ProcessStartInfo startInfo = new ProcessStartInfo();
            startInfo.FileName = settings.migotoPath;
            startInfo.WorkingDirectory = settings.migotoPath.Substring(0, settings.migotoPath.LastIndexOf(@"\"));
            Process.Start(startInfo);
        }

        public void IsAutostarting() {
            try {
                if (File.Exists(autostartPath)) {
                    CheckBoxAutostart.IsChecked = true;
                } else {
                    CheckBoxAutostart.IsChecked = false;
                }
            } catch {
                CheckBoxAutostart.IsChecked = false;
            }
        }

        public void ApplySettings() {
            TextBoxMigotoPath.Text = settings.migotoPath;
            ComboBoxRegion.SelectedIndex = settings.comboBoxRegionSelectedItemIndex;
            TextBoxDetectionDelay.Text = settings.DetectionDelayMs.ToString();
            UpdateGenshinProcessName();

            // update autostart shortcut if file was moved
            var location = Process.GetCurrentProcess().MainModule.FileName;
            if (CheckBoxAutostart.IsChecked == true && settings.selfLocation != location) { 
                settings.selfLocation = Process.GetCurrentProcess().MainModule.FileName;
                CreateAutostartShortcut();
            }
        }

        public void UpdateGenshinProcessName() {
            switch (ComboBoxRegion.SelectedIndex) {
                case 0:
                    genshinProcessName = "GenshinImpact";
                    break;
                case 1:
                    genshinProcessName = "YuanShen";
                    break;
            }
        }

        public void CreateAutostartShortcut() {
            try {
                using (StreamWriter writer = new StreamWriter(Path.Combine(autostartPath))) {
                    string app = Process.GetCurrentProcess().MainModule.FileName;
                    writer.WriteLine("[InternetShortcut]");
                    writer.WriteLine("URL=file:///" + app);
                    writer.WriteLine("IconIndex=0");
                    string icon = app.Replace('\\', '/');
                    writer.WriteLine("IconFile=" + icon);

                    settings.selfLocation = app;
                    settings.Save();
                }
            } catch { }
        }
        public void SetupNotifyIcon() {
            notifyIcon.Icon = Properties.Resources.migotolauncher;
            notifyIcon.Visible = false;
            notifyIcon.DoubleClick +=
                delegate (object sender, EventArgs args) {
                    this.Show();
                    this.WindowState = WindowState.Normal;
                };
        }

        protected override void OnStateChanged(EventArgs e) {
            if (WindowState == System.Windows.WindowState.Minimized) {
                this.Hide();
                notifyIcon.Visible = true;
            } else {
                notifyIcon.Visible = false;
            }

            base.OnStateChanged(e);
        }



        private void CheckBoxAutostart_Clicked(object sender, RoutedEventArgs e) {
            if (CheckBoxAutostart.IsChecked == true) {
                CreateAutostartShortcut();
            } else {
                try {
                    File.Delete(autostartPath);
                } catch { }
            }
        }

        private void Select_Click(object sender, RoutedEventArgs e) {
            Microsoft.Win32.OpenFileDialog dialog = new Microsoft.Win32.OpenFileDialog();
            dialog.Filter = "Executable (*.exe)|*.exe|All files (*.*)|*.*";
            dialog.InitialDirectory = Environment.GetFolderPath(Environment.SpecialFolder.Desktop);
            if (dialog.ShowDialog() == true) {
                settings.migotoPath = dialog.FileName;
                TextBoxMigotoPath.Text = settings.migotoPath;
                settings.Save();
            }
        }

        private void ComboBoxRegion_DropDownClosed(object sender, EventArgs e) {
            UpdateGenshinProcessName();
            settings.comboBoxRegionSelectedItemIndex = ComboBoxRegion.SelectedIndex;
            settings.Save();
        }

        private void NumberValidationTextBox(object sender, TextCompositionEventArgs e) {
            Regex regex = new Regex("[^0-9]+");
            e.Handled = regex.IsMatch(e.Text);
        }

        private void TextBoxDetectionDelay_TextChanged(object sender, System.Windows.Controls.TextChangedEventArgs e) {
            try {
                settings.DetectionDelayMs = Convert.ToInt32(TextBoxDetectionDelay.Text);
                settings.Save();
            } catch { }
        }
    }
}