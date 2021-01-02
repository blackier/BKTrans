using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Imaging;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Windows.Threading;

namespace BKAssembly
{
    public class BKScreenCapture : Form
    {
        #region 成员变量定义

        public struct DataStruct
        {
            public Bitmap capture_bmp;
            public Rectangle capture_rect;
        }

        private Bitmap screen_bmp_;
        private Bitmap screen_bmp_brush_;

        private bool is_mouse_down_;
        private Point mouse_down_point_;

        private Rectangle target_rect_;

        private readonly object lock_obj_;

        private readonly Action<DataStruct> result_callback_;

        private bool need_enter_;

        private Dispatcher dispatcher_;

        #endregion


        #region 公有成员函数定义

        public BKScreenCapture(Action<DataStruct> callback)
        {
            is_mouse_down_ = false;
            result_callback_ = callback;
            lock_obj_ = new object();
            need_enter_ = true;
            dispatcher_ = Dispatcher.CurrentDispatcher;

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

        public void StartCapture(bool need_enter = true)
        {
            need_enter_ = need_enter;
            screen_bmp_ = GetScreenCapture();
            screen_bmp_brush_ = (Bitmap)screen_bmp_.Clone();
            // 绘制蒙板
            Graphics g = Graphics.FromImage(screen_bmp_brush_);
            g.FillRectangle(new SolidBrush(Color.FromArgb(100, 0, 0, 0)), 0, 0, screen_bmp_brush_.Width, screen_bmp_brush_.Height);
            g.Dispose();

            this.BackgroundImage = screen_bmp_brush_;

            ShowWnd();
        }

        public void StartCapture(Rectangle capture_rect)
        {
            target_rect_ = capture_rect;
            screen_bmp_ = GetScreenCapture();
            DoResultCallback();
        }

        #endregion 公有成员函数定义

        #region 私有成员函数定义

        private void ShowWnd()
        {
            this.dispatcher_.Invoke(() =>
            {
                this.Show();
                this.Activate();
            });
        }

        private void HideWnd()
        {
            this.dispatcher_.Invoke(() =>
            {
                this.Hide();
            });
        }

        private Bitmap GetScreenCapture()
        {
            Rectangle tScreenRect = new Rectangle(0, 0, Screen.PrimaryScreen.Bounds.Width, Screen.PrimaryScreen.Bounds.Height);
            Bitmap tSrcBmp = new Bitmap(tScreenRect.Width, tScreenRect.Height);
            Graphics gp = Graphics.FromImage(tSrcBmp);
            gp.CopyFromScreen(0, 0, 0, 0, tScreenRect.Size);
            gp.DrawImage(tSrcBmp, 0, 0, tScreenRect, GraphicsUnit.Pixel);
            return tSrcBmp;
        }

        private async void DoResultCallback()
        {
            await Task.Run(() =>
            {
                if (target_rect_.Width == 0)
                {
                    target_rect_.Width = 1;
                }
                if (target_rect_.Height == 0)
                {
                    target_rect_.Height = 1;
                }
                result_callback_(new DataStruct { capture_bmp = screen_bmp_.Clone(target_rect_, PixelFormat.Format32bppArgb), capture_rect = target_rect_ });
                HideWnd();
            });
        }

        private void GetCapture_MouseClick(object sender, MouseEventArgs e)
        {

        }

        private void GetCapture_MouseDown(object sender, MouseEventArgs e)
        {
            mouse_down_point_.X = e.X;
            mouse_down_point_.Y = e.Y;
            is_mouse_down_ = true;
        }

        private void GetCapture_MouseMove(object sender, MouseEventArgs e)
        {
            if (is_mouse_down_)
            {

                lock (lock_obj_)
                {
                    Bitmap screen_brush_clone = (Bitmap)screen_bmp_brush_.Clone();
                    Graphics brush_gph = Graphics.FromImage(screen_brush_clone);

                    Size new_rect_size = new Size(Math.Abs(e.X - mouse_down_point_.X), Math.Abs(e.Y - mouse_down_point_.Y));

                    Point new_rect_point = mouse_down_point_;
                    if (e.X < mouse_down_point_.X)
                    {
                        new_rect_point.X = e.X;
                    }
                    if (e.Y < mouse_down_point_.Y)
                    {
                        new_rect_point.Y = e.Y;
                    }

                    Rectangle new_rect = new Rectangle(new_rect_point, new_rect_size);
                    brush_gph.DrawImage(screen_bmp_, new_rect, new_rect, GraphicsUnit.Pixel);
                    Pen brush_pen = new Pen(Color.Blue, 1);
                    Rectangle pen_rect = new_rect;
                    pen_rect.Width = pen_rect.Width - 1;
                    pen_rect.Height = pen_rect.Height - 1;
                    brush_gph.DrawRectangle(brush_pen, pen_rect);

                    Rectangle brush_rect = target_rect_;
                    if (target_rect_.X > new_rect.X)
                    {
                        brush_rect.X = new_rect.X;
                    }
                    if (target_rect_.Y > new_rect.Y)
                    {
                        brush_rect.Y = new_rect.Y;
                    }

                    Point pp1 = new Point(target_rect_.X + target_rect_.Width, target_rect_.Y + target_rect_.Height);
                    Point pp2 = new Point(new_rect.X + new_rect.Width, new_rect.Y + new_rect.Height);
                    Point pp3 = pp1;

                    if (pp2.X > pp1.X)
                    {
                        pp3.X = pp2.X;
                    }
                    if (pp2.Y > pp1.Y)
                    {
                        pp3.Y = pp2.Y;
                    }

                    brush_rect.Width = pp3.X - brush_rect.X;
                    brush_rect.Height = pp3.Y - brush_rect.Y;

                    Graphics this_gph = this.CreateGraphics();
                    this_gph.DrawImage(screen_brush_clone, brush_rect, brush_rect, GraphicsUnit.Pixel);

                    target_rect_ = new_rect;

                    this_gph.Dispose();
                    brush_gph.Dispose();
                    brush_pen.Dispose();
                    screen_brush_clone.Dispose();
                }

            }

        }

        private void GetCapture_Mouseup(object sender, MouseEventArgs e)
        {
            is_mouse_down_ = false;
            if (!need_enter_)
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

        #endregion 私有成员函数定义
    }
}
