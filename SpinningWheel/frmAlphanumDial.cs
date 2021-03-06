﻿using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using System.Linq;
using System.Text;
using System.Windows.Forms;

namespace SpinningWheel
{
    public partial class frmAlphanumDial : Form
    {
        GameWheel wheel;
        Random rand;      // random number generator
        string result = "";

        public frmAlphanumDial() {
            InitializeComponent();

            // Minimize flicker by painting the form ourselves - doesn't have much effect
            this.SetStyle(ControlStyles.UserPaint, true);
            this.SetStyle(ControlStyles.OptimizedDoubleBuffer, true);
            this.SetStyle(ControlStyles.AllPaintingInWmPaint, true);

            this.Shown += new EventHandler(frm_Shown);
            panel.Click += new EventHandler(panel_Click);
            timerRepaint.Tick += new EventHandler(timerRepaint_Tick);
            timerRepaint.Interval = 55;     // this is the highest resolution possible for form timers

            rand = new Random();
        }

        void frm_Shown(object sender, EventArgs e) {
            panel.Height = this.ClientSize.Height;
            panel.Width = panel.Height + 60;
            panel.Top = (this.ClientSize.Height - panel.Height) / 2;
            panel.Left = (this.ClientSize.Width - panel.Width) / 2;
            Point panelCenter = new Point(panel.Width / 2, panel.Height / 2);

            int radius = (this.ClientSize.Height - 20) / 2;
            string[] values = new String[] {"A1", "B2", "C1", "D2", "E1", "F2", "G1"};
            wheel = new GameWheel(panelCenter, radius, values);

            wheel.SetGraphics(panel.CreateGraphics());
            wheel.Draw();
        }

        void panel_Click(object sender, EventArgs e) {
            if (wheel.state == GameWheel.STATE_NOT_STARTED) {
                // the game can only be played once
                timerRepaint.Start();
                result = wheel.Spin(ref rand);
            }
        }

        private void timerRepaint_Tick(object sender, EventArgs e) {
            wheel.Refresh(true);
        }

    }
}
