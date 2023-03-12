using BKTrans.Misc;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Documents;
using System.Windows.Forms;
using System.Windows.Interop;
using System.Windows.Threading;

namespace BKTrans
{
    public partial class FloatTextWindow : Window
    {
        public enum ButtonType
        {
            Capture,
            Trans,
            AutoTrans,
            GragMove
        };

        private Stack<RectangleF> _historyRect;
        private Action<ButtonType, object> _onButtonClick;
        public FloatTextWindow(Action<ButtonType, object> OnButtonClick = null)
        {
            InitializeComponent();
            ShowInTaskbar = false;
            _onButtonClick = OnButtonClick;
            _historyRect = new();

            Loaded += new RoutedEventHandler(Window_Loaded);
        }

        private void AddHistoryRect()
        {
            var currentRect = new RectangleF((float)Left, (float)Top, (float)Width, (float)Height);
            if (_historyRect.Count > 0 && currentRect == _historyRect.Peek())
                return;
            _historyRect.Push(currentRect);
        }

        public void SetTextRect(RectangleF rect)
        {
            Dispatcher.Invoke(() =>
            {
                var p = BKMisc.ScreenScaling();
                Left = rect.X / p;
                Top = rect.Y / p;
                Width = rect.Width / p + gridcolumn_btn.Width.Value;
                Height = (rect.Height / p) * 2;
                AddHistoryRect();
            });
        }

        public RectangleF GetTextRect()
        {
            var p = BKMisc.ScreenScaling();
            return new RectangleF((float)(Left * p), (float)(Top * p), (float)((Width - gridcolumn_btn.Width.Value) * p), (float)((Height / 2 * p)));
        }
        public void SetText(string t)
        {
            Dispatcher.Invoke(() =>
            {
                textbox_transtext.Text = t;
            });

        }

        public void SetAutoTransStatus(bool start)
        {
            Dispatcher.Invoke(() =>
            {
                checkbox_autotrans.IsChecked = start;
            });
        }

        public void ShowWnd()
        {
            Dispatcher.Invoke(() =>
            {
                Topmost = true;
                Show();
                Activate();
            });
        }

        public void HideWnd()
        {
            Dispatcher.Invoke(() => Hide());
        }

        #region 事件处理

        #region 窗体事件
        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            // 设置拖拽
            AdornerLayer adornerLayer = AdornerLayer.GetAdornerLayer(grid_textbox);
            adornerLayer.Add(new BKDragAdorner(grid_textbox, (RectangleF change) =>
            {
                //Left += change.X;
                //Top += change.Y;
                //Width += change.Width;
                //Height += change.Height;

                var p = 1;
                var Position = new Rectangle((int)(Left * p + change.X), (int)(Top * p + change.Y), (int)(Width * p + change.Width), (int)(Height * p + change.Height));

                WindowInteropHelper wih = new(this);
                IntPtr hWnd = wih.Handle;
                if (!Position.IsEmpty)
                {
                    _ = BKWindowsAPI.MoveWindow(hWnd, Position.Left, Position.Top, Position.Width, Position.Height, false);
                }
                //AddHistoryRect();
            }));

        }
        #endregion 窗体事件

        #region 控件事件
        private void btn_capture_Click(object sender, RoutedEventArgs e)
        {
            if (_onButtonClick != null)
                _onButtonClick(ButtonType.Capture, null);
        }

        private void btn_trans_Click(object sender, RoutedEventArgs e)
        {
            if (_onButtonClick != null)
                _onButtonClick(ButtonType.Trans, null);
        }

        private void btn_hide_Click(object sender, RoutedEventArgs e)
        {
            grid_textbox.Visibility = grid_textbox.Visibility == Visibility.Visible ? Visibility.Hidden : Visibility.Visible;

            AdornerLayer adornerLayer = AdornerLayer.GetAdornerLayer(grid_textbox);
            Adorner[] adorners = adornerLayer.GetAdorners(grid_textbox);
            for (int i = adorners.Length - 1; i >= 0; i--)
            {
                adorners[i].Visibility = grid_textbox.Visibility;
            }
        }

        private void checkbox_autotrans_Click(object sender, RoutedEventArgs e)
        {
            checkbox_autotrans.IsChecked = !checkbox_autotrans.IsChecked;
            if (_onButtonClick != null)
                _onButtonClick(ButtonType.AutoTrans, checkbox_autotrans.IsChecked);
        }

        private void btn_drag_PreviewMouseLeftButtonDown(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            bool? autotrans_checked = checkbox_autotrans.IsChecked;
            if (_onButtonClick != null)
                _onButtonClick(ButtonType.AutoTrans, false);

            DragMove();
            AddHistoryRect();

            if (_onButtonClick != null)
            {
                _onButtonClick(ButtonType.GragMove, null);
                _onButtonClick(ButtonType.AutoTrans, autotrans_checked);
            }
        }

        private void btn_undo_Click(object sender, RoutedEventArgs e)
        {
            if (_historyRect.Count > 0)
            {
                RectangleF preRect;
                // 丢弃当前位置
                if (_historyRect.Count > 1)
                    _historyRect.Pop();
                preRect = _historyRect.Peek();
                Top = preRect.Top;
                Left = preRect.Left;
                Width = preRect.Width;
                Height = preRect.Height;
            }
        }

        private void btn_drag_MouseWheel(object sender, System.Windows.Input.MouseWheelEventArgs e)
        {
            if (e.Delta == 0)
                return;
            double unit = 1;
            if (e.Delta < 0)
                unit = -unit;
            double fontSize = textbox_transtext.FontSize;
            if (fontSize + unit < 12)
                return;
            textbox_transtext.FontSize = fontSize + unit;
        }
        #endregion 控件事件
        #endregion 事件处理
    }

}
