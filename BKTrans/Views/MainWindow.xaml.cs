using BKTrans.Core;
using BKTrans.Models;
using BKTrans.ViewModels;
using BKTrans.Views.Pages;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Drawing;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Forms;
using System.Windows.Interop;
using System.Windows.Media;
using System.Windows.Threading;
using WinRT;

namespace BKTrans.Views;

public partial class MainWindow
{
    private bool _notifyClose = false;

    private MainWindowViewModel _viewModel;
    public MainWindowViewModel ViewModel { get { return _viewModel; } }
    public MainWindow(MainWindowViewModel viewModel, INavigationService navigationService,
        IServiceProvider serviceProvider, ISnackbarService snackbarService, IContentDialogService contentDialogService)
    {
        Wpf.Ui.Appearance.SystemThemeWatcher.Watch(this);

        _viewModel = viewModel;
        DataContext = this;

        InitializeComponent();
        // 加载时才需要处理的内容
        Loaded += new RoutedEventHandler(Window_Loaded);
        // 关联关闭函数，设置为最小化到托盘
        Closing += new CancelEventHandler(Window_Closing);

        snackbarService.SetSnackbarPresenter(SnackbarPresenter);
        navigationService.SetNavigationControl(NavigationView);
        contentDialogService.SetContentPresenter(RootContentDialog);

        NavigationView.SetServiceProvider(serviceProvider);
        NavigationView.Loaded += (_, _) => NavigationView.Navigate(typeof(MainPage));
    }

    public void BringToForeground()
    {
        if (WindowState == WindowState.Minimized || Visibility == Visibility.Hidden)
        {
            Show();
            WindowState = WindowState.Normal;
        }

        Activate();
        Topmost = true;
        Topmost = false;
        Focus();
    }

    // Win32API: RegisterHotKey function
    private enum HotKeyId
    {
        Capture = 0xB001,
        ShowFloatWindow = 0xB002,
        HideFloatWindow = 0xB003
    }
    private void Window_Loaded(object sender, RoutedEventArgs e)
    {
        // 热键注册
        IntPtr handle = new WindowInteropHelper(this).EnsureHandle();
        bool is_succeess = false;
        is_succeess = BKHotKey.Register(handle, (int)HotKeyId.Capture, (uint)BKHotKey.Modifiers.norepeat, (uint)Keys.F2);
        is_succeess = BKHotKey.Register(handle, (int)HotKeyId.ShowFloatWindow, (uint)(BKHotKey.Modifiers.shift | BKHotKey.Modifiers.alt | BKHotKey.Modifiers.norepeat), (uint)Keys.X);
        is_succeess = BKHotKey.Register(handle, (int)HotKeyId.HideFloatWindow, (uint)(BKHotKey.Modifiers.shift | BKHotKey.Modifiers.alt | BKHotKey.Modifiers.norepeat), (uint)Keys.Z);
        if (!is_succeess)
        {
            Dispatcher.InvokeAsync(() => App.SnackbarError("热键注册失败"));
        }

        var windowSource = HwndSource.FromHwnd(handle) ?? throw new ArgumentNullException("Window source is null");
        windowSource.AddHook(HwndSourceHook);
    }

    private void Window_Closing(object sender, CancelEventArgs e)
    {
        Hide();
        e.Cancel = !_notifyClose;
        SettingsModel.SaveSettings();
    }

    private IntPtr HwndSourceHook(IntPtr hwnd, int msg, IntPtr wParam, IntPtr lParam, ref bool handled)
    {
        // 快捷键处理
        if (wParam.ToInt64() == (int)HotKeyId.Capture)
        {
            App.GetRequiredService<MainPage>()?.CaptureTrans();
        }
        else if (wParam.ToInt64() == (int)HotKeyId.ShowFloatWindow)
        {
            App.GetRequiredService<MainPage>()?.CaptureTransLast();
        }
        else if (wParam.ToInt64() == (int)HotKeyId.HideFloatWindow)
        {
            App.GetRequiredService<FloatCaptureRectWindow>()?.HideWindow();
        }

        return IntPtr.Zero;
    }

    private void btn_toggle_theme_Click(object sender, RoutedEventArgs e)
    {
        var currentTheme = Wpf.Ui.Appearance.ApplicationThemeManager.GetAppTheme();

        Wpf.Ui.Appearance.ApplicationThemeManager.Apply(currentTheme == Wpf.Ui.Appearance.ApplicationTheme.Light ? Wpf.Ui.Appearance.ApplicationTheme.Dark : Wpf.Ui.Appearance.ApplicationTheme.Light);
        App.GetRequiredService<MainPage>()?.OnSwitchTheme();
    }

    private void tray_MenuItem_Click(object sender, RoutedEventArgs e)
    {
        var trayType = EnumExtensions.TryParse((sender as MenuItem).Tag as string, MainWindowViewModel.TrayType.Trans);
        switch (trayType)
        {
            case MainWindowViewModel.TrayType.Capture:
                App.GetRequiredService<MainPage>()?.CaptureTrans();
                break;
            case MainWindowViewModel.TrayType.Trans:
                App.GetRequiredService<MainPage>()?.CaptureTransLast();
                break;
            case MainWindowViewModel.TrayType.ShowFloatWindow:
                App.GetRequiredService<FloatCaptureRectWindow>()?.ShowWindow();
                break;
            case MainWindowViewModel.TrayType.HideFloatWindow:
                App.GetRequiredService<FloatCaptureRectWindow>()?.HideWindow();
                break;
            case MainWindowViewModel.TrayType.Exit:
                // 不能设置这两个窗体为子窗体，obs捕捉不到子窗体，手动关闭
                App.GetRequiredService<FloatCaptureRectWindow>()?.Close();
                App.GetRequiredService<FloatTransTextWindow>()?.Close();
                _notifyClose = true;
                Close();
                break;
            default:
                break;
        }
    }
}
