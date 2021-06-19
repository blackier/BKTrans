using System;
using System.Drawing;
using System.Drawing.Imaging;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Windows.Threading;

namespace BKAssembly
{
    public class BKScreenCapture : Form
    {

        public struct DataStruct
        {
            public Bitmap captureBmp;
            public Rectangle captureRect;
        }

        private Bitmap mScreenBmp;
        private Bitmap mScreenBmpBrush;

        private bool mIsMouseDown;
        private Point mMouseDownPoint;

        private Rectangle mTargetRect;

        private readonly object mLockObj;

        private readonly Action<DataStruct> mResultCallback;

        private bool mNeedEnter;

        private Dispatcher mDispatcher;

        public BKScreenCapture(Action<DataStruct> callback)
        {
            mIsMouseDown = false;
            mResultCallback = callback;
            mLockObj = new object();
            mNeedEnter = true;
            mDispatcher = Dispatcher.CurrentDispatcher;

            // 双重缓存，解决图片闪烁
            this.SetStyle(ControlStyles.UserPaint, true);
            this.SetStyle(ControlStyles.AllPaintingInWmPaint, true);
            this.SetStyle(ControlStyles.OptimizedDoubleBuffer, true);
            this.UpdateStyles();

            // 鼠标动作绑定
            this.MouseClick += new MouseEventHandler(this.GetCapture_MouseClick);
            this.MouseDown += new MouseEventHandler(this.GetCapture_MouseDown);
            this.MouseMove += new MouseEventHandler(this.GetCapture_MouseMove);
            this.MouseUp += new MouseEventHandler(this.GetCapture_Mouseup);

            // 按键动作绑定
            this.KeyDown += new KeyEventHandler(this.GetCapture_KeyDown);

            // 最大化
            this.WindowState = FormWindowState.Maximized;

            // 不在任务栏显示
            this.ShowInTaskbar = false;
            //this.TopMost = true;

            // 无边框
            this.FormBorderStyle = FormBorderStyle.None;
        }

        public void StartCapture(bool needEnter = true)
        {
            mNeedEnter = needEnter;
            mScreenBmp = GetScreenCapture();
            mScreenBmpBrush = (Bitmap)mScreenBmp.Clone();
            // 绘制蒙板
            Graphics g = Graphics.FromImage(mScreenBmpBrush);
            g.FillRectangle(new SolidBrush(Color.FromArgb(100, 0, 0, 0)), 0, 0, mScreenBmpBrush.Width, mScreenBmpBrush.Height);
            g.Dispose();

            this.BackgroundImage = mScreenBmpBrush;

            ShowWnd();
        }

        public void StartCapture(Rectangle captureRect)
        {
            mTargetRect = captureRect;
            mScreenBmp = GetScreenCapture();
            DoResultCallback();
        }

        private void ShowWnd()
        {
            mDispatcher.Invoke(() =>
            {
                this.Show();
                this.Activate();
            });
        }

        private void HideWnd()
        {
            mDispatcher.Invoke(() =>
            {
                this.Hide();
            });
        }

        private Bitmap GetScreenCapture()
        {
            Rectangle screenRect = new Rectangle(0, 0, Screen.PrimaryScreen.Bounds.Width, Screen.PrimaryScreen.Bounds.Height);
            Bitmap screenBmp = new Bitmap(screenRect.Width, screenRect.Height);
            Graphics gp = Graphics.FromImage(screenBmp);
            gp.CopyFromScreen(0, 0, 0, 0, screenRect.Size);
            return screenBmp;
        }

        private async void DoResultCallback()
        {
            await Task.Run(() =>
            {
                if (mTargetRect.Width == 0)
                {
                    mTargetRect.Width = 1;
                }
                if (mTargetRect.Height == 0)
                {
                    mTargetRect.Height = 1;
                }
                mResultCallback(new DataStruct { captureBmp = mScreenBmp.Clone(mTargetRect, PixelFormat.Format32bppArgb), captureRect = mTargetRect });
                HideWnd();
            });
        }

        private void GetCapture_MouseClick(object sender, MouseEventArgs e)
        {

        }

        private void GetCapture_MouseDown(object sender, MouseEventArgs e)
        {
            mMouseDownPoint.X = e.X;
            mMouseDownPoint.Y = e.Y;
            mIsMouseDown = true;
        }

        private void GetCapture_MouseMove(object sender, MouseEventArgs e)
        {
            if (mIsMouseDown)
            {

                lock (mLockObj)
                {
                    Bitmap screenBrushClone = (Bitmap)mScreenBmpBrush.Clone();
                    Graphics brushGph = Graphics.FromImage(screenBrushClone);

                    // 截图区域
                    Point newRectPoint = mMouseDownPoint;
                    if (e.X < mMouseDownPoint.X)
                    {
                        newRectPoint.X = e.X;
                    }
                    if (e.Y < mMouseDownPoint.Y)
                    {
                        newRectPoint.Y = e.Y;
                    }

                    Rectangle newRect = new Rectangle(newRectPoint, new Size(Math.Abs(e.X - mMouseDownPoint.X), Math.Abs(e.Y - mMouseDownPoint.Y)));
                    brushGph.DrawImage(mScreenBmp, newRect, newRect, GraphicsUnit.Pixel);

                    // 画框
                    brushGph.DrawRectangle(
                        new Pen(Color.Blue, 1),
                        new Rectangle(newRect.X, newRect.Y, newRect.Width - 1, newRect.Height - 1));

                    // 要重新绘制的区域
                    Rectangle brushRect = mTargetRect;
                    if (mTargetRect.X > newRect.X)
                    {
                        brushRect.X = newRect.X;
                    }
                    if (mTargetRect.Y > newRect.Y)
                    {
                        brushRect.Y = newRect.Y;
                    }

                    Point brushRectBot = new Point(mTargetRect.Right, mTargetRect.Bottom);

                    if (newRect.Right > mTargetRect.Right)
                    {
                        brushRectBot.X = newRect.Right;
                    }
                    if (newRect.Bottom > mTargetRect.Bottom)
                    {
                        brushRectBot.Y = newRect.Bottom;
                    }

                    brushRect.Width = brushRectBot.X - brushRect.X;
                    brushRect.Height = brushRectBot.Y - brushRect.Y;
                    // 绘制
                    Graphics thiGph = this.CreateGraphics();
                    thiGph.DrawImage(screenBrushClone, brushRect, brushRect, GraphicsUnit.Pixel);

                    mTargetRect = newRect;

                    brushGph.Dispose();
                    screenBrushClone.Dispose();
                }

            }

        }

        private void GetCapture_Mouseup(object sender, MouseEventArgs e)
        {
            mIsMouseDown = false;
            if (!mNeedEnter)
            {
                DoResultCallback();
            }
        }

        private void GetCapture_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.KeyCode == Keys.Escape || e.KeyCode == Keys.Enter)
            {
                DoResultCallback();
            }
        }

    }
}
