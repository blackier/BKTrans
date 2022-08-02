﻿#region License Information (GPL v3)

/*
    ShareX - A program that allows you to take screenshots and share any file type
    Copyright (c) 2007-2022 ShareX Team

    This program is free software; you can redistribute it and/or
    modify it under the terms of the GNU General Public License
    as published by the Free Software Foundation; either version 2
    of the License, or (at your option) any later version.

    This program is distributed in the hope that it will be useful,
    but WITHOUT ANY WARRANTY; without even the implied warranty of
    MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
    GNU General Public License for more details.

    You should have received a copy of the GNU General Public License
    along with this program; if not, write to the Free Software
    Foundation, Inc., 51 Franklin Street, Fifth Floor, Boston, MA  02110-1301, USA.

    Optionally you can also view the license at <http://www.gnu.org/licenses/>.
*/

#endregion License Information (GPL v3)

using ShareX.HelpersLib;
using ShareX.ScreenCaptureLib.Properties;
using System;
using System.Diagnostics;
using System.Drawing;
using System.Threading;
using System.Windows.Forms;

namespace ShareX.ScreenCaptureLib
{
    public partial class ScreenRecordForm : Form
    {
        public event Action StopRequested;

        public bool IsWorking { get; private set; }
        public bool IsRecording { get; private set; }
        public bool IsCountdown { get; set; }
        public TimeSpan Countdown { get; set; }
        public Stopwatch Timer { get; private set; }
        public ManualResetEvent RecordResetEvent { get; set; }
        public bool IsStopRequested { get; private set; }
        public bool IsAbortRequested { get; private set; }

        public bool ActivateWindow { get; set; } = true;
        public float Duration { get; set; } = 0;
        public bool AskConfirmationOnAbort { get; set; } = false;

        private Color borderColor = Color.Red;
        private Rectangle borderRectangle;
        private Rectangle borderRectangle0Based;
        private static int lastIconStatus = -1;

        public ScreenRecordForm(Rectangle regionRectangle)
        {
            InitializeComponent();
            Icon = ShareXResources.Icon;
            niTray.Icon = ShareXResources.Icon;

            borderRectangle = regionRectangle.Offset(1);
            borderRectangle0Based = new Rectangle(0, 0, borderRectangle.Width, borderRectangle.Height);

            Location = borderRectangle.Location;
            int windowWidth = Math.Max(borderRectangle.Width, pInfo.Width);
            Size = new Size(windowWidth, borderRectangle.Height + pInfo.Height + 1);
            pInfo.Location = new Point(0, borderRectangle.Height + 1);

            Region region = new Region(ClientRectangle);
            region.Exclude(borderRectangle0Based.Offset(-1));
            region.Exclude(new Rectangle(0, borderRectangle.Height, windowWidth, 1));
            if (borderRectangle.Width < pInfo.Width)
            {
                region.Exclude(new Rectangle(borderRectangle.Width, 0, pInfo.Width - borderRectangle.Width, borderRectangle.Height));
            }
            else if (borderRectangle.Width > pInfo.Width)
            {
                region.Exclude(new Rectangle(pInfo.Width, borderRectangle.Height + 1, borderRectangle.Width - pInfo.Width, pInfo.Height));
            }
            Region = region;

            Timer = new Stopwatch();
            UpdateTimer();

            RecordResetEvent = new ManualResetEvent(false);

            ChangeState(ScreenRecordState.Waiting);
        }

        protected override bool ShowWithoutActivation
        {
            get
            {
                return !ActivateWindow;
            }
        }

        protected override CreateParams CreateParams
        {
            get
            {
                CreateParams createParams = base.CreateParams;
                createParams.ExStyle |= (int)(WindowStyles.WS_EX_TOPMOST | WindowStyles.WS_EX_TOOLWINDOW);
                return createParams;
            }
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                if (components != null)
                {
                    components.Dispose();
                }

                if (RecordResetEvent != null)
                {
                    RecordResetEvent.Dispose();
                }
            }

            base.Dispose(disposing);
        }

        private void ScreenRegionForm_Shown(object sender, EventArgs e)
        {
            if (ActivateWindow)
            {
                this.ForceActivate();
            }
        }

        private void ScreenRecordForm_FormClosed(object sender, FormClosedEventArgs e)
        {
            if (!IsStopRequested)
            {
                AbortRecording();
            }
        }

        protected void OnStopRequested()
        {
            StopRequested?.Invoke();
        }

        public void StartCountdown(int milliseconds)
        {
            IsCountdown = true;
            Countdown = TimeSpan.FromMilliseconds(milliseconds);

            lblTimer.ForeColor = Color.Yellow;

            Timer.Start();
            timerRefresh.Start();
            UpdateTimer();
        }

        public void StartRecordingTimer()
        {
            IsCountdown = Duration > 0;
            Countdown = TimeSpan.FromSeconds(Duration);

            lblTimer.ForeColor = Color.White;
            borderColor = Color.FromArgb(0, 255, 0);
            Refresh();

            Timer.Reset();
            Timer.Start();
            timerRefresh.Start();
            UpdateTimer();
        }

        private void UpdateTimer()
        {
            if (!IsDisposed)
            {
                TimeSpan timer;

                if (IsCountdown)
                {
                    timer = Countdown - Timer.Elapsed;
                    if (timer.Ticks < 0) timer = TimeSpan.Zero;
                }
                else
                {
                    timer = Timer.Elapsed;
                }

                lblTimer.Text = timer.ToString("mm\\:ss\\:ff");
            }
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            using (Pen pen1 = new Pen(Color.Black) { DashPattern = new float[] { 5, 5 } })
            using (Pen pen2 = new Pen(borderColor) { DashPattern = new float[] { 5, 5 }, DashOffset = 5 })
            {
                e.Graphics.DrawRectangleProper(pen1, borderRectangle0Based);
                e.Graphics.DrawRectangleProper(pen2, borderRectangle0Based);
            }

            base.OnPaint(e);
        }

        private void timerRefresh_Tick(object sender, EventArgs e)
        {
            UpdateTimer();
        }

        private void btnStart_MouseClick(object sender, MouseEventArgs e)
        {
            if (e.Button == MouseButtons.Left)
            {
                StartStopRecording();
            }
        }

        private void btnAbort_MouseClick(object sender, MouseEventArgs e)
        {
            if (e.Button == MouseButtons.Left)
            {
                if (!AskConfirmationOnAbort || MessageBox.Show(Resources.ScreenRecordForm_ConfirmCancel, "ShareX", MessageBoxButtons.YesNo,
                    MessageBoxIcon.Warning) == DialogResult.Yes)
                {
                    AbortRecording();
                }
            }
        }

        public void StartStopRecording()
        {
            if (IsWorking)
            {
                IsStopRequested = true;

                if (!IsRecording)
                {
                    IsAbortRequested = true;
                }

                OnStopRequested();
            }
            else if (RecordResetEvent != null)
            {
                RecordResetEvent.Set();
            }
        }

        public void AbortRecording()
        {
            IsAbortRequested = true;
            StartStopRecording();
        }

        public void ChangeState(ScreenRecordState state)
        {
            this.InvokeSafe(() =>
            {
                switch (state)
                {
                    case ScreenRecordState.Waiting:
                        string trayTextWaiting = "ShareX - " + Resources.ScreenRecordForm_StartRecording_Waiting___;
                        niTray.Text = trayTextWaiting.Truncate(63);
                        niTray.Icon = Resources.control_record_yellow.ToIcon();
                        cmsMain.Enabled = false;
                        niTray.Visible = true;
                        break;
                    case ScreenRecordState.BeforeStart:
                        string trayTextBeforeStart = "ShareX - " + Resources.ScreenRecordForm_StartRecording_Click_tray_icon_to_start_recording_;
                        niTray.Text = trayTextBeforeStart.Truncate(63);
                        tsmiStart.Text = Resources.ScreenRecordForm_Start;
                        cmsMain.Enabled = true;
                        break;
                    case ScreenRecordState.AfterStart:
                        IsWorking = true;
                        string trayTextAfterStart = "ShareX - " + Resources.ScreenRecordForm_StartRecording_Click_tray_icon_to_stop_recording_;
                        niTray.Text = trayTextAfterStart.Truncate(63);
                        niTray.Icon = Resources.control_record.ToIcon();
                        tsmiStart.Text = Resources.ScreenRecordForm_Stop;
                        btnStart.Text = Resources.ScreenRecordForm_Stop;
                        break;
                    case ScreenRecordState.AfterRecordingStart:
                        IsRecording = true;
                        StartRecordingTimer();
                        break;
                    case ScreenRecordState.Encoding:
                        Hide();
                        string trayTextAfterStop = "ShareX - " + Resources.ScreenRecordForm_StartRecording_Encoding___;
                        niTray.Text = trayTextAfterStop.Truncate(63);
                        niTray.Icon = Resources.camcorder__pencil.ToIcon();
                        cmsMain.Enabled = false;
                        break;
                }
            });
        }

        public void ChangeStateProgress(int progress)
        {
            niTray.Text = $"ShareX - {Resources.ScreenRecordForm_StartRecording_Encoding___} {progress}%";

            if (niTray.Visible && lastIconStatus != progress)
            {
                Icon icon;

                if (progress >= 0)
                {
                    try
                    {
                        icon = Helpers.GetProgressIcon(progress, Color.FromArgb(140, 0, 36));
                    }
                    catch (Exception e)
                    {
                        DebugHelper.WriteException(e);
                        progress = -1;
                        if (lastIconStatus == progress) return;
                        icon = Resources.camcorder__pencil.ToIcon();
                    }
                }
                else
                {
                    icon = Resources.camcorder__pencil.ToIcon();
                }

                using (Icon oldIcon = niTray.Icon)
                {
                    niTray.Icon = icon;
                    oldIcon.DisposeHandle();
                }

                lastIconStatus = progress;
            }
        }
    }
}