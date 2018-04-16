﻿using System;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Windows.Forms;

namespace WorktipsWallet
{
    public partial class ImportWalletPrompt : WorktipsWalletForm
    {
        public string ImportWalletPath { get; set; }

        public string ImportWalletPassword { get; set; }

        public ImportWalletPrompt()
        {
            InitializeComponent();
            this.Text = Application.ProductName;
        }

        protected override void OnFormClosing(FormClosingEventArgs e)
        {
            Utilities.CloseProgram(e);
        }

        private void CancelButton_Click(object sender, EventArgs e)
        {
            DialogResult dialogResult = MessageBox.Show("Are you sure you want to cancel your Worktips Wallet import?", "Cancel wallet import?", MessageBoxButtons.YesNo);
            if (dialogResult == DialogResult.Yes)
            {
                Utilities.SetDialogResult(this, DialogResult.Cancel);
                Utilities.Close(this);
            }
        }

        private void ImportWalletButton_Click(object sender, EventArgs e)
        {
            ImportWallet();
        }

        private void WalletNameText_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.KeyCode == Keys.Enter)
            {
                ImportWallet();
            }
        }

        private void ImportWallet()
        {
            var curDir = System.IO.Path.GetDirectoryName(System.Reflection.Assembly.GetExecutingAssembly().Location);
            var _walletFile = System.IO.Path.Combine(curDir, walletNameText.Text + ".wallet");

            if (walletNameText.Text == "")
            {
                MessageBox.Show("Please enter a valid wallet name", "Worktips Wallet Import");
                return;
            }
            else if (walletNameText.Text.Any(c => !Char.IsLetterOrDigit(c)))
            {
                MessageBox.Show("Wallet name cannot contain special characters", "Worktips Wallet Import");
                return;
            }
            else if (System.IO.File.Exists(_walletFile))
            {
                MessageBox.Show("A wallet with that name already exists! Choose a different name or choose the \"Select Existing Wallet\" option instead.", "Worktips Wallet Import");
                return;
            }

            if (passwordText.Text == "")
            {
                MessageBox.Show("Please enter a valid password", "Worktips Wallet Import");
                return;
            }
            else if (passwordText.Text.Length < 6)
            {
                MessageBox.Show("Please enter a password that is larger than 6 characters", "Worktips Wallet Import");
                return;
            }
            else if (passwordText.Text.Length > 150)
            {
                MessageBox.Show("Passwords cannot be longer than 150 characters!", "Worktips Wallet Import");
                return;
            }

            if (passwordText.Text != passwordConfirmText.Text)
            {
                MessageBox.Show("Passwords do not match", "Worktips Wallet Import");
                return;
            }

            if (viewSecretKeyText.Text.Length != 64 || spendSecretKeyText.Text.Length != 64)
            {
                MessageBox.Show("View key or spend key is incorrect length! Should be 64 characters long.", "Worktips Wallet Import");
                return;
            }

            var walletdexe = System.IO.Path.Combine(curDir, "walletd.exe");

            if (IsRunningOnMono())
            {
                walletdexe = System.IO.Path.Combine(curDir, "walletd");
            }

            if (!System.IO.File.Exists(walletdexe))
            {
                MessageBox.Show("The 'walletd' daemon is missing from the folder the wallet is currently running from! Please place 'walletd' next to your wallet exe and run again!", "Worktips Wallet Import", MessageBoxButtons.OK, MessageBoxIcon.Error);
                Utilities.SetDialogResult(this, DialogResult.Abort);
                Utilities.Close(this);
            }

            importProgressbar.Visible = true;
            StringBuilder tmpstdout = new StringBuilder();
            try
            {
                Process p = new Process();
                p.StartInfo.UseShellExecute = false;
                p.StartInfo.RedirectStandardOutput = true;
                p.StartInfo.CreateNoWindow = true;
                p.StartInfo.WindowStyle = ProcessWindowStyle.Hidden;
                p.StartInfo.FileName = walletdexe;

                p.StartInfo.Arguments = CLIEncoder.Encode(new string[]
                    {"-w", _walletFile, "-p", passwordText.Text, "--view-key",
                     viewSecretKeyText.Text, "--spend-key", spendSecretKeyText.Text,
                     "-g"});

                p.OutputDataReceived += (sender, args) => tmpstdout.AppendLine(args.Data);
                p.Start();
                p.BeginOutputReadLine();
                p.WaitForExit(10000);
                p.CancelOutputRead();

                if (!System.IO.File.Exists(_walletFile))
                {
                    MessageBox.Show("Wallet failed to import after communicating with daemon. Please ensure your secret keys are correct, and open walletd.log for more information on what went wrong, if it exists.", "Worktips Wallet Import", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    Utilities.SetDialogResult(this, DialogResult.Abort);
                    Utilities.Close(this);
                }
                else
                {
                    ImportWalletPath = _walletFile;
                    ImportWalletPassword = passwordText.Text;
                    MessageBox.Show("Wallet successfully imported at: " + Environment.NewLine + _walletFile, "Worktips Wallet Import", MessageBoxButtons.OK, MessageBoxIcon.Information);
                    Utilities.SetDialogResult(this, DialogResult.OK);
                    Utilities.Close(this);
                }

            }
            catch (Exception ex)
            {
                MessageBox.Show("An exception occured while attempting to import the wallet." + Environment.NewLine + "Error:" + Environment.NewLine + ex.Message, "Worktips Wallet Import", MessageBoxButtons.OK, MessageBoxIcon.Error);
                Utilities.SetDialogResult(this, DialogResult.Abort);
                Utilities.Close(this);
            }
        }

        public static bool IsRunningOnMono()
        {
            return Type.GetType("Mono.Runtime") != null;
        }

        private void PasswordText_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.KeyCode == Keys.Enter)
            {
                ImportWallet();
            }
        }

        private void PasswordConfirmText_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.KeyCode == Keys.Enter)
            {
                ImportWallet();
            }
        }
    }
}
