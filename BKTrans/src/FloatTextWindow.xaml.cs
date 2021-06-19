using System.Drawing;
using System.Windows;

namespace BKTrans
{
    public partial class FloatTextWindow : Window
    {
        public FloatTextWindow()
        {
            InitializeComponent();
            this.WindowStartupLocation = WindowStartupLocation.Manual;
            this.ShowInTaskbar = false;
        }
        public void SetRect(Rectangle rect)
        {
            this.Dispatcher.Invoke(() =>
            {
                this.Left = rect.X;
                this.Top = rect.Y;
                this.Width = rect.Width;
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
    }

}
