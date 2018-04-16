using Newtonsoft.Json.Linq;
using System;
using System.ComponentModel;
using System.Net;
using System.Windows.Forms;

namespace WorktipsWallet
{
    public partial class UpdatePrompt : WorktipsWalletForm
    {
        public UpdatePrompt()
        {
            InitializeComponent();

            this.Text = Application.ProductName;
        }

        private void UpdatePrompt_Load(object sender, EventArgs e)
        {
            updateWorker.RunWorkerAsync();
        }

        protected override void OnFormClosing(FormClosingEventArgs e)
        {
            Utilities.CloseProgram(e);
        }

        private void UpdateRequest()
        {
            try
            {
                System.Net.ServicePointManager.ServerCertificateValidationCallback = delegate { return true; };
                string thisVersionString = System.Reflection.Assembly.GetExecutingAssembly().GetName().Version.ToString();
                bool needsUpdate = false;
                var builtURL = "https://api.github.com/repos/samvicky26/Worktips-GUI-version-2/releases/latest";

                var cli = new WebClient();
                cli.Headers[HttpRequestHeader.ContentType] = "application/json";
                cli.Headers[HttpRequestHeader.UserAgent] = "WorktipsCoin Wallet " + thisVersionString;
                string response = cli.DownloadString(new Uri(builtURL));

                var jobj = JObject.Parse(response);

                string gitVersionString = jobj["tag_name"].ToString();
               
                var gitVersion = new Version(gitVersionString);
                var thisVersion = new Version(thisVersionString);

                var result = gitVersion.CompareTo(thisVersion);
                if (result > 0)
                {
                    needsUpdate = true;
                }
                else
                {
                    needsUpdate = false;
                }

                if (needsUpdate)
                {
                    foreach (var item in jobj["assets"])
                    {
                        string name = item["name"].ToString();
                        if (name.Contains("WorktipsWallet.exe"))
                        {
                            DialogResult dialogResult = MessageBox.Show("A new version of WorktipsCoin Wallet is out. Download?", "WorktipsCoin Wallet", MessageBoxButtons.YesNo);
                            if (dialogResult == DialogResult.No)
                            {
                                return;
                            }
                            var dl = new WebClient();
                            dl.Headers[HttpRequestHeader.UserAgent] = "WorktipsCoin Wallet " + thisVersionString;
                            dl.DownloadFile(item["browser_download_url"].ToString(), "WorktipsWallet_update.exe");
                            System.Diagnostics.Process.Start("WorktipsWallet_update.exe");
                            Environment.Exit(0);
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("Failed to check for updates! " + ex.Message + Environment.NewLine + ex.InnerException, "WorktipsCoin Wallet");
            }
        }

        private void UpdateWorker_RunWorkerCompleted(object sender, RunWorkerCompletedEventArgs e)
        {
            Utilities.Close(this);
        }

        private void UpdateWorker_DoWork(object sender, DoWorkEventArgs e)
        {
            try
            {
                if (System.AppDomain.CurrentDomain.FriendlyName == "WorktipsWallet_update.exe")
                {
                    System.Threading.Thread.Sleep(500);
                    System.IO.File.Copy("WorktipsWallet_update.exe", "WorktipsWallet.exe", true);
                    System.Diagnostics.Process.Start("WorktipsWallet.exe");
                    Environment.Exit(0);
                }
                else if (System.AppDomain.CurrentDomain.FriendlyName == "WorktipsWallet.exe")
                {
                    if (System.IO.File.Exists("WorktipsWallet_update.exe"))
                    {
                        System.IO.File.Delete("WorktipsWallet_update.exe");
                    }
                }
            }
            catch
            {
                MessageBox.Show("Failed to check for updates!", "WorktipsCoin Wallet");
            }

            UpdateRequest();
        }
    }

}
