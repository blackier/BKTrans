using BKTrans.Misc;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Windows;

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
                RectangleF pre_rect;
                // 丢弃当前位置
                if (_historyRect.Count > 1)
                    _historyRect.Pop();
                pre_rect = _historyRect.Peek();
                Top = pre_rect.Top;
                Left = pre_rect.Left;
                Width = pre_rect.Width;
                Height = pre_rect.Height;
            }
        }
    }

}
