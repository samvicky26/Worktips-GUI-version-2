using System;
using System.Windows.Forms;
using System.Diagnostics;

namespace WorktipsWallet
{
    class Utilities
    {
        /* Set to false every time we call hide() or close() so the whol
           program doesn't get shut down - but it does when we hit the x
           button. The issue is that microsoft are really helpful people
           and both close()/hide() and the x button give a close reason of
           "UserClosing" so we have to use an extra boolean flag so we can
           tell if the form is being closed or if we want to close the whole
           program. Thanks MS! */
        private static bool appClosing = true;

        public static void CloseProgram(FormClosingEventArgs e)
        {
            /* For some stupid reason if we programatically call close() on
               the form the closereason is still userclosing - so we need
               and extra flag */
            if (e.CloseReason == CloseReason.UserClosing && appClosing)
            {
                DialogResult dialogResult = MessageBox.Show("Are you sure you want to close Worktips Wallet?", "Worktips Wallet", MessageBoxButtons.YesNo);

                if (dialogResult != DialogResult.Yes)
                {
                    e.Cancel = true;
                }
                else
                {
                    /* Kill the launched walletd in the background */
                    var conflictingProcesses = Process.GetProcessesByName("walletd");

                    int numConflictingProcesses = conflictingProcesses.Length;

                    for (int i = 0; i < numConflictingProcesses; i++)
                    {
                        /* Need to kill all walletd and Worktipsd processes so
                           they don't lock the DB */
                        conflictingProcesses[i].Kill();
                    }

                    Environment.Exit(0);
                }
            }
        }

        public static void Hide(Form caller)
        {
            appClosing = false;
            caller.Hide();
            appClosing = true;
        }

        public static void Close(Form caller)
        {
            appClosing = false;
            caller.Close();
            appClosing = true;
        }

        public static void SetDialogResult(Form caller, DialogResult res)
        {
            appClosing = false;
            caller.DialogResult = res;
            appClosing = true;
        }

        public static void SetAppClosing(bool _appClosing)
        {
            appClosing = _appClosing;
        }
    }
}
