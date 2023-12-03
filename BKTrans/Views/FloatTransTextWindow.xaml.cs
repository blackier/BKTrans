using BKTrans.Kernel;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Interop;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace BKTrans.Views;

/// <summary>
/// Interaction logic for FloatTransTextWindow.xaml
/// </summary>
public partial class FloatTransTextWindow : Window
{
    public FloatTransTextWindow()
    {
        InitializeComponent();

        ShowInTaskbar = false;
    }

    public void ShowWindow()
    {
        Topmost = false;
        Show();
        Activate();
        Dispatcher.InvokeAsync(() => { Topmost = true; Activate(); });
    }

    public void HideWindow()
    {
        Hide();
    }

    public void MoveWindow(Rectangle pos)
    {
        WindowInteropHelper wih = new(this);
        IntPtr hWnd = wih.Handle;
        if (!pos.IsEmpty)
        {
            BKWindowsAPI.MoveWindow(hWnd, pos.Left, pos.Top, pos.Width, pos.Height, false);
        }
    }

    public void ChangeContent(UIElement content, bool addOrRemove)
    {
        if (addOrRemove)
            grid_root.Children.Add(content);
        else
            grid_root.Children.Remove(content);
    }
}
