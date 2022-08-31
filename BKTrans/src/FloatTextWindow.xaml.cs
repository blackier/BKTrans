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
            Trans
        };

        private Action<ButtonType> _onButtonClick;
        public FloatTextWindow(Action<ButtonType> OnButtonClick = null)
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
                _onButtonClick(ButtonType.Capture);
        }

        private void btn_trans_Click(object sender, RoutedEventArgs e)
        {
            if (_onButtonClick != null)
                _onButtonClick(ButtonType.Trans);
        }

        private void btn_hide_Click(object sender, RoutedEventArgs e)
        {
            grid_textbox.Visibility = grid_textbox.Visibility == Visibility.Visible ? Visibility.Hidden : Visibility.Visible;
        }
    }

}
