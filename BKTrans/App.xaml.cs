using System;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Threading;
using BKTrans.ViewModels;
using BKTrans.ViewModels.Pages;
using BKTrans.ViewModels.Pages.Settings;
using BKTrans.Views;
using BKTrans.Views.Pages;
using BKTrans.Views.Pages.Settings;
using Serilog;
using Wpf.Ui.DependencyInjection;

namespace BKTrans;

/// <summary>
/// Interaction logic for App.xaml
/// </summary>
public partial class App : Application
{
    private Mutex _singleInstanceMutex;

    private static ILogger _appLogger;

    private static readonly IHost _host = Host.CreateDefaultBuilder()
        .ConfigureAppConfiguration(c =>
        {
            c.SetBasePath(AppContext.BaseDirectory);
        })
        .ConfigureServices(
            (context, services) =>
            {
                _ = services.AddNavigationViewPageProvider();

                // App Host
                services.AddHostedService<Services.ApplicationHostService>();

                // Main window container with navigation
                services.AddSingleton<MainWindow>();
                services.AddSingleton<MainWindowViewModel>();
                services.AddSingleton<INavigationService, NavigationService>();
                services.AddSingleton<ISnackbarService, SnackbarService>();
                services.AddSingleton<IContentDialogService, ContentDialogService>();

                // Top-level pages
                services.AddSingleton<MainPage>();
                services.AddSingleton<MainPageViewModel>();
                services.AddTransient<SettingsPage>();
                services.AddTransient<SettingsViewModel>();
                services.AddTransient<AboutPage>();
                services.AddTransient<AboutViewModel>();

                // settings pages
                services.AddTransient<SettingsTransPage>();
                services.AddTransient<SettingsTransViewModel>();
                services.AddTransient<SettingsOCRReplacePage>();
                services.AddTransient<SettingsOCRReplaceViewModel>();
                services.AddTransient<SettingsAutoTransPage>();
                services.AddTransient<SettingsAutoTransViewModel>();
                services.AddTransient<SettingsShortcutsPage>();
                services.AddTransient<SettingsShortcutsViewModel>();

                // Windows
                services.AddSingleton<FloatCaptureRectWindow>();
                services.AddSingleton<FloatTransTextWindow>();
            }
        )
        .Build();

    public static T GetRequiredService<T>()
        where T : class
    {
        return _host.Services.GetRequiredService<T>();
    }

    private async void OnStartup(object sender, StartupEventArgs e)
    {
        if (SingleInstance())
            return;

        _appLogger = new LoggerConfiguration()
            .WriteTo.File("logs/app_.log", rollingInterval: RollingInterval.Day)
            .CreateLogger();
        _appLogger.Information("Application Startup");

        Dispatcher.UnhandledException += OnDispatcherUnhandledException;
        AppDomain.CurrentDomain.UnhandledException += OnCurrentDomainUnhandledException;
        TaskScheduler.UnobservedTaskException += OnTaskSchedulerUnobservedTaskException;

        await _host.StartAsync();
        GetRequiredService<MainWindow>().Show();
    }

    private async void OnExit(object sender, ExitEventArgs e)
    {
        BKPythonEngine.Shutdown();

        await _host.StopAsync();
        _host.Dispose();
    }

    private void OnTaskSchedulerUnobservedTaskException(object sender, UnobservedTaskExceptionEventArgs e)
    {
        ExceptionHandler("TaskScheduler_UnobservedTaskException", e.Exception);
    }

    private void OnCurrentDomainUnhandledException(object sender, UnhandledExceptionEventArgs e)
    {
        ExceptionHandler("CurrentDomain_UnhandledException", e.ExceptionObject as Exception);
    }

    private void OnDispatcherUnhandledException(object sender, DispatcherUnhandledExceptionEventArgs e)
    {
        ExceptionHandler("Dispatcher_UnhandledException", e.Exception);
        e.Handled = true;
    }

    private void ExceptionHandler(string exception_type, Exception e)
    {
        _appLogger.Error($"{exception_type}: {e}");
        SnackbarError("程序发生异常，详情查看logs");
    }

    private bool SingleInstance()
    {
        //https://stackoverflow.com/questions/14506406/wpf-single-instance-best-practices
        bool isOwned;
        const string uniqueEventName = "BKTransWake";
        const string uniqueMutexName = "BKTransMutex";
        _singleInstanceMutex = new Mutex(true, uniqueMutexName, out isOwned);
        EventWaitHandle eventWaitHandle = new EventWaitHandle(false, EventResetMode.AutoReset, uniqueEventName);

        // So, R# would not give a warning that this variable is not used.
        GC.KeepAlive(_singleInstanceMutex);

        if (isOwned)
        {
            // Spawn a thread which will be waiting for our event
            var thread = new Thread(() =>
            {
                while (eventWaitHandle.WaitOne())
                {
                    Current.Dispatcher.BeginInvoke(() => ((MainWindow)Current.MainWindow).BringToForeground());
                }
            });

            // It is important mark it as background otherwise it will prevent app from exiting.
            thread.IsBackground = true;

            thread.Start();
            return false;
        }

        // Notify other instance so it could bring itself to foreground.
        eventWaitHandle.Set();

        // Terminate this instance.
        Shutdown();

        return true;
    }

    public static bool NavigateTo(Type pageType)
    {
        var navigationService = GetRequiredService<INavigationService>();

        if (navigationService == null)
            return false;

        return navigationService.Navigate(pageType);
    }

    public static void NavigateGoBack()
    {
        var navigationService = GetRequiredService<INavigationService>();

        if (navigationService == null)
            return;

        navigationService.GoBack();
    }

    public static void ShowWindow()
    {
        Current.MainWindow.Show();
    }

    public static void HideWindow()
    {
        Current.MainWindow.Hide();
    }

    public static void SnackbarSuccess(string message)
    {
        var snackbarService = GetRequiredService<ISnackbarService>();
        snackbarService.Show("成功", message, WpfUi.ControlAppearance.Success);
    }

    public static void SnackbarError(string message)
    {
        var snackbarService = GetRequiredService<ISnackbarService>();
        snackbarService.Show(
            "失败",
            message,
            WpfUi.ControlAppearance.Danger,
            timeout: TimeSpan.FromSeconds(1.5)
        );
    }
}
