using System;
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
            AutoTrans
        };


        private Action<ButtonType, object> _onButtonClick;
        public FloatTextWindow(Action<ButtonType, object> OnButtonClick = null)
        {
            InitializeComponent();
            ShowInTaskbar = false;
            _onButtonClick = OnButtonClick;
        }

        public void SetTextRect(Rectangle rect)
        {
            Dispatcher.Invoke(() =>
            {
                var w_1 = (double)System.Windows.Forms.Screen.PrimaryScreen.Bounds.Width;
                var w_2 = (double)SystemParameters.PrimaryScreenWidth;
                var p = w_2 / w_1;

                Left = rect.X * p;
                Top = rect.Y * p;
                Width = rect.Width * p + 32;
                Height = (rect.Height * p) * 2;
            });

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
    }

}
