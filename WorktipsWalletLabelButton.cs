﻿using System;
using System.Drawing;
using System.Windows.Forms;

namespace WorktipsWallet
{
    class WorktipsWalletLabelButton : Label
    {
        protected override void OnMouseLeave(EventArgs e)
        {
            this.BackColor = Color.FromArgb(52, 52, 52);
            this.ForeColor = Color.FromArgb(224, 224, 224);
        }

        protected override void OnMouseEnter(EventArgs e)
        {
            this.BackColor = Color.FromArgb(44, 44, 44);
            this.ForeColor = Color.FromArgb(39, 170, 107);
        }
    }
}
