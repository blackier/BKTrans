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

        private Action<ButtonType> mfOnButtonClick;
        public FloatTextWindow(Action<ButtonType> OnButtonClick = null)
        {
            InitializeComponent();
            WindowStartupLocation = WindowStartupLocation.Manual;
            ShowInTaskbar = false;
            mfOnButtonClick = OnButtonClick;

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
                tb_main.Text = t;
            });

        }
        public void ShowWnd()
        {
            Dispatcher.Invoke(() =>
            {
                Show();
                Activate();
                Topmost = true;
            });
        }

        public void HideWnd()
        {
            Hide();
        }

        private void Button_Click_Trans(object sender, RoutedEventArgs e)
        {
            if (mfOnButtonClick != null)
                mfOnButtonClick(ButtonType.Trans);
        }

        private void Button_Click_Capture(object sender, RoutedEventArgs e)
        {
            if (mfOnButtonClick != null)
                mfOnButtonClick(ButtonType.Capture);
        }

        private void Button_Click_Hide(object sender, RoutedEventArgs e)
        {
            mGridShowTrans.Visibility = mGridShowTrans.Visibility == Visibility.Visible ? Visibility.Hidden : Visibility.Visible;
        }
    }

}
