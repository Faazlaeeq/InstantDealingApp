using System;
using System.IO;
using System.IO.Compression;
using System.Diagnostics;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Drawing;

namespace InstantDealingAppInstaller
{
    public class SetupForm : Form
    {
        private ProgressBar progressBar;
        private Label statusLabel;
        private Label titleLabel;

        public SetupForm()
        {
            this.Text = "Instant Dealing - Setup & Update";
            this.Size = new Size(450, 220);
            this.FormBorderStyle = FormBorderStyle.FixedDialog;
            this.MaximizeBox = false;
            this.MinimizeBox = false;
            this.StartPosition = FormStartPosition.CenterScreen;
            this.BackColor = Color.White;

            titleLabel = new Label
            {
                Text = "Installing & Updating Instant Dealing...",
                Font = new Font("Segoe UI", 13, FontStyle.Bold),
                ForeColor = Color.FromArgb(2, 132, 199),
                Location = new Point(25, 25),
                AutoSize = true
            };

            statusLabel = new Label
            {
                Text = "Preparing installation files...",
                Font = new Font("Segoe UI", 10, FontStyle.Regular),
                ForeColor = Color.FromArgb(71, 85, 105),
                Location = new Point(25, 65),
                Size = new Size(380, 25)
            };

            progressBar = new ProgressBar
            {
                Location = new Point(25, 95),
                Size = new Size(380, 25),
                Style = ProgressBarStyle.Marquee,
                MarqueeAnimationSpeed = 30
            };

            this.Controls.Add(titleLabel);
            this.Controls.Add(statusLabel);
            this.Controls.Add(progressBar);

            this.Shown += SetupForm_Shown;
        }

        private async void SetupForm_Shown(object sender, EventArgs e)
        {
            await Task.Run(() => PerformInstallation());
        }

        private void PerformInstallation()
        {
            try
            {
                UpdateStatus("Closing running application instances...");
                foreach (var proc in Process.GetProcessesByName("InstantDealingApp"))
                {
                    try { proc.Kill(); proc.WaitForExit(3000); } catch { }
                }

                string localAppData = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
                string installDir = Path.Combine(localAppData, "InstantDealingApp");

                if (!Directory.Exists(installDir))
                {
                    Directory.CreateDirectory(installDir);
                }

                UpdateStatus("Extracting updated files...");
                var assembly = typeof(SetupForm).Assembly;
                string resourceName = "Installer.PublishPayload.zip";

                using (Stream stream = assembly.GetManifestResourceStream(resourceName))
                {
                    if (stream == null)
                    {
                        throw new Exception("Embedded payload not found.");
                    }

                    string tempZip = Path.Combine(Path.GetTempPath(), "InstantDealingPayload_" + Guid.NewGuid().ToString("N") + ".zip");
                    using (FileStream fs = new FileStream(tempZip, FileMode.Create, FileAccess.Write))
                    {
                        stream.CopyTo(fs);
                    }

                    using (ZipArchive archive = ZipFile.OpenRead(tempZip))
                    {
                        foreach (ZipArchiveEntry entry in archive.Entries)
                        {
                            string destPath = Path.Combine(installDir, entry.FullName);
                            string dir = Path.GetDirectoryName(destPath);
                            if (!string.IsNullOrEmpty(dir) && !Directory.Exists(dir))
                            {
                                Directory.CreateDirectory(dir);
                            }

                            if (!string.IsNullOrEmpty(entry.Name))
                            {
                                entry.ExtractToFile(destPath, overwrite: true);
                            }
                        }
                    }

                    try { File.Delete(tempZip); } catch { }
                }

                UpdateStatus("Creating desktop and start menu shortcuts...");
                string targetExe = Path.Combine(installDir, "InstantDealingApp.exe");

                string desktopPath = Environment.GetFolderPath(Environment.SpecialFolder.DesktopDirectory);
                CreateShortcut(Path.Combine(desktopPath, "Instant Dealing.lnk"), targetExe, installDir);

                string startMenuPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.StartMenu), "Programs");
                CreateShortcut(Path.Combine(startMenuPath, "Instant Dealing.lnk"), targetExe, installDir);

                UpdateStatus("Launching Instant Dealing...");
                Process.Start(new ProcessStartInfo
                {
                    FileName = targetExe,
                    WorkingDirectory = installDir
                });

                this.Invoke(new Action(() =>
                {
                    this.Hide();
                    MessageBox.Show("Instant Dealing has been updated and installed successfully!", "Setup Complete", MessageBoxButtons.OK, MessageBoxIcon.Information);
                    this.Close();
                }));
            }
            catch (Exception ex)
            {
                this.Invoke(new Action(() =>
                {
                    MessageBox.Show("Installation Error: " + ex.Message, "Setup Failed", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    this.Close();
                }));
            }
        }

        private void UpdateStatus(string message)
        {
            if (this.InvokeRequired)
            {
                this.Invoke(new Action(() => statusLabel.Text = message));
            }
            else
            {
                statusLabel.Text = message;
            }
        }

        private void CreateShortcut(string shortcutPath, string targetPath, string workingDir)
        {
            try
            {
                Type shellType = Type.GetTypeFromProgID("WScript.Shell");
                if (shellType != null)
                {
                    dynamic shell = Activator.CreateInstance(shellType);
                    dynamic shortcut = shell.CreateShortcut(shortcutPath);
                    shortcut.TargetPath = targetPath;
                    shortcut.WorkingDirectory = workingDir;
                    shortcut.Description = "Instant Dealing Application";
                    shortcut.Save();
                }
            }
            catch { }
        }
    }

    static class Program
    {
        [STAThread]
        static void Main()
        {
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);
            Application.Run(new SetupForm());
        }
    }
}
