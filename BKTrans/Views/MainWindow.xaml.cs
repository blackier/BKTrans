﻿using BKTrans.Misc;
using BKTrans.Models;
using BKTrans.Services.Contracts;
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
using Wpf.Ui.Controls.IconElements;
using Wpf.Ui.Controls.Navigation;

namespace BKTrans.Views;

public partial class MainWindow : IWindow
{
    private bool _notifyClose = false;

    private MainWindowViewModel _viewModel;
    public MainWindowViewModel ViewModel { get { return _viewModel; } }
    public MainWindow(MainWindowViewModel viewModel, INavigationService navigationService,
        IServiceProvider serviceProvider, ISnackbarService snackbarService, IContentDialogService contentDialogService)
    {
        Wpf.Ui.Appearance.Watcher.Watch(this);

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
        NavigationView.Loaded += (_, _) => NavigationView.Navigate(typeof(DashboardPage));
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
        capture = 0xB001,
        trans = 0xB002,
        hide = 0xB003
    }
    private void Window_Loaded(object sender, RoutedEventArgs e)
    {
        // native窗体事件hook
        if (DesignerHelper.IsInDesignMode)
            return;

        // 热键注册
        IntPtr handle = new WindowInteropHelper(this).EnsureHandle();
        bool is_succeess = false;
        is_succeess = BKHotKey.Register(handle, (int)HotKeyId.capture, (uint)BKHotKey.Modifiers.norepeat, (uint)Keys.F2);
        is_succeess = BKHotKey.Register(handle, (int)HotKeyId.trans, (uint)(BKHotKey.Modifiers.shift | BKHotKey.Modifiers.alt | BKHotKey.Modifiers.norepeat), (uint)Keys.X);
        is_succeess = BKHotKey.Register(handle, (int)HotKeyId.hide, (uint)(BKHotKey.Modifiers.shift | BKHotKey.Modifiers.alt | BKHotKey.Modifiers.norepeat), (uint)Keys.Z);
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

    public enum WM
    {
        NCHITTEST = 0x0084,
        NCMOUSELEAVE = 0x02A2,
        NCLBUTTONDOWN = 0x00A1,
        NCLBUTTONUP = 0x00A2,
    }
    private bool _isThemeBtnClickedDown;
    private IntPtr HwndSourceHook(IntPtr hwnd, int msg, IntPtr wParam, IntPtr lParam, ref bool handled)
    {
        // 快捷键处理
        if (wParam.ToInt64() == (int)HotKeyId.capture)
        {
            App.GetService<DashboardPage>()?.CaptureTrans();
        }
        else if (wParam.ToInt64() == (int)HotKeyId.trans)
        {
            App.GetService<DashboardPage>()?.CaptureTransLast();
        }
        else if (wParam.ToInt64() == (int)HotKeyId.hide)
        {
            App.GetService<FloatCaptureRectWindow>()?.HideWindow();
        }

        // 因为wpfui的titlebar会自动hook系统事件，导致布局到titlebar上面的
        // 主题切换按钮点击时是没法获取到事件无法点击的，所以这里先一步hook处理。
        // TODO: 后面可以完善成比较标准的button
        var message = (WM)msg;
        if (message is not (WM.NCLBUTTONDOWN or WM.NCLBUTTONUP))
            return IntPtr.Zero;

        switch (message)
        {
            case WM.NCLBUTTONDOWN when IsMouseOverElement(btn_toggle_theme, lParam):
                _isThemeBtnClickedDown = true;
                handled = true;
                return IntPtr.Zero;

            case WM.NCLBUTTONUP when _isThemeBtnClickedDown && IsMouseOverElement(btn_toggle_theme, lParam): // Left button clicked up
                btn_toggle_theme.RaiseEvent(new RoutedEventArgs(System.Windows.Controls.Button.ClickEvent));
                _isThemeBtnClickedDown = false;
                handled = true;
                return IntPtr.Zero;

            default:
                break;
        }

        return IntPtr.Zero;
    }

    public bool IsMouseOverElement(UIElement element, IntPtr lParam)
    {
        if (lParam == IntPtr.Zero)
            return false;

        var mousePosScreen = new System.Windows.Point(Get_X_LParam(lParam), Get_Y_LParam(lParam));
        var bounds = new Rect(new System.Windows.Point(), element.RenderSize);
        var mousePosRelative = element.PointFromScreen(mousePosScreen);
        return bounds.Contains(mousePosRelative);
    }

    private static int Get_X_LParam(IntPtr lParam)
    {
        return (short)(lParam.ToInt32() & 0xFFFF);
    }

    private static int Get_Y_LParam(IntPtr lParam)
    {
        return (short)(lParam.ToInt32() >> 16);
    }

    private void btn_toggle_theme_Click(object sender, RoutedEventArgs e)
    {
        var currentTheme = Wpf.Ui.Appearance.Theme.GetAppTheme();

        Wpf.Ui.Appearance.Theme.Apply(currentTheme == Wpf.Ui.Appearance.ThemeType.Light ? Wpf.Ui.Appearance.ThemeType.Dark : Wpf.Ui.Appearance.ThemeType.Light);
    }

    private void tray_MenuItem_Click(object sender, RoutedEventArgs e)
    {
        var trayType = (MainWindowViewModel.TrayType)Enum.Parse(typeof(MainWindowViewModel.TrayType), (sender as MenuItem).Tag as string);
        switch (trayType)
        {
            case MainWindowViewModel.TrayType.Capture:
                App.GetService<DashboardPage>()?.CaptureTrans();
                break;
            case MainWindowViewModel.TrayType.Trans:
                App.GetService<DashboardPage>()?.CaptureTransLast();
                break;
            case MainWindowViewModel.TrayType.ShowFloatWindow:
                App.GetService<FloatCaptureRectWindow>()?.ShowWindow();
                break;
            case MainWindowViewModel.TrayType.HideFloatWindow:
                App.GetService<FloatCaptureRectWindow>()?.HideWindow();
                break;
            case MainWindowViewModel.TrayType.Exit:
                _notifyClose = true;
                Close();
                break;
            default:
                break;
        }
    }
}
