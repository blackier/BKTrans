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
            this.WindowStartupLocation = WindowStartupLocation.Manual;
            this.ShowInTaskbar = false;
            mfOnButtonClick = OnButtonClick;
        }
        public void SetRect(Rectangle rect)
        {
            this.Dispatcher.Invoke(() =>
            {
                this.Left = rect.X;
                this.Top = rect.Y;
                this.Width = rect.Width + 32;
                this.Height = rect.Height * 2;
            });

        }
        public void SetText(string t)
        {
            this.Dispatcher.Invoke(() =>
            {
                this.tb_main.Text = t;
            });

        }
        public void ShowWnd()
        {
            this.Dispatcher.Invoke(() =>
            {
                this.Show();
                this.Activate();
                this.Topmost = true;
            });
        }
        public void HideWnd()
        {
            this.Dispatcher.Invoke(() =>
            {
                this.Hide();
            });
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
