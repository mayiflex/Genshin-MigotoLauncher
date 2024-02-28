using Microsoft.Win32;
using System.Diagnostics;
using System.IO;
using System.Security.Principal;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;

namespace MigotoLauncher {
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window {
        private NotifyIcon notifyIcon = new NotifyIcon();
        private string settingsPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "MigotoLauncher", "settings.txt");
        private string autostartPath = System.IO.Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "Microsoft", "Windows", "Start Menu", "Programs", "Startup", "MitogotoLauncher.url");
        private string migotoPath;
        private bool isAdmin;
        public MainWindow() {
            InitializeComponent();
            IsAdministrator();
            IsAutostarting();
            SetupNotifyIcon();
            if (TryLoadMigotoPath() && CheckBoxAutostart.IsChecked == true) {
                this.Hide();
                notifyIcon.Visible = true;
            } 

            StartGenshinLauncherDetectionTask();
        }

        public bool TryLoadMigotoPath() {
            try {
                migotoPath = File.ReadAllLines(settingsPath)[0];
                TextBoxMigotoPath.Text = migotoPath;
                return true;
            } catch {
                Directory.CreateDirectory(settingsPath.Substring(0, settingsPath.LastIndexOf(@"\")));
                if (!File.Exists(settingsPath)) {
                    File.Create(settingsPath);
                    return true;
                }
                return false;
            }
        }


        public Task StartGenshinLauncherDetectionTask() {
            return Task.Run(() => {
                while (true) {
                    Thread.Sleep(2500);
                    var procs = Process.GetProcessesByName("launcher");
                    if (procs.Length == 0) continue;
                    if (isAdmin) { //run as admin to prevent lauches for processes with ambiguous names
                        foreach (var proc in procs) {
                            try { //accessing MainModule requires admin
                                if (proc.MainModule.FileVersionInfo.FileDescription != "Genshin Impact") continue;
                            } catch { }
                        }
                    }
                    LaunchMigoto();
                    AwaitGenshinStartOrLauncherClose();
                }
            });
        }

        public void AwaitGenshinStartOrLauncherClose() {
            while(true) {
                Thread.Sleep(10000);
                var launcherProcs = Process.GetProcessesByName("launcher");
                if (launcherProcs.Length == 0) return;

                var genshinProcs = Process.GetProcessesByName("GenshinImpact");
                if (genshinProcs.Length > 1) return;
            }
        }

        public void LaunchMigoto() {
            ProcessStartInfo startInfo = new ProcessStartInfo();
            startInfo.FileName = migotoPath;
            startInfo.WorkingDirectory = migotoPath.Substring(0, migotoPath.LastIndexOf(@"\"));
            Process.Start(startInfo);
        }

        public void IsAdministrator() {
            var identity = WindowsIdentity.GetCurrent();
            var principal = new WindowsPrincipal(identity);
            isAdmin = principal.IsInRole(WindowsBuiltInRole.Administrator);

            if(isAdmin) {
                TextBlockAdmin.Text = "MigotoLauncher is running with admin privileges.";
            } else {
                TextBlockAdmin.Text = "MigotoLauncher is running without admin privileges.";
            }
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

        private void CheckBoxAutostart_Clicked(object sender, RoutedEventArgs e) {
            if (CheckBoxAutostart.IsChecked == true) {
                try {
                    using (StreamWriter writer = new StreamWriter(Path.Combine(autostartPath))) {
                        string app = System.Diagnostics.Process.GetCurrentProcess().MainModule.FileName;
                        writer.WriteLine("[InternetShortcut]");
                        writer.WriteLine("URL=file:///" + app);
                        writer.WriteLine("IconIndex=0");
                        string icon = app.Replace('\\', '/');
                        writer.WriteLine("IconFile=" + icon);
                    }
                } catch { }
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
                migotoPath = dialog.FileName;
                TextBoxMigotoPath.Text = migotoPath;
                try {
                    File.WriteAllText(settingsPath, migotoPath);
                } catch { }
            }
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
    }
}