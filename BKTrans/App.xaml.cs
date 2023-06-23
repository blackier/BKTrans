using BKTrans.Services;
using BKTrans.Services.Contracts;
using BKTrans.ViewModels;
using BKTrans.ViewModels.Pages;
using BKTrans.ViewModels.Pages.Settings;
using BKTrans.Views;
using BKTrans.Views.Pages;
using BKTrans.Views.Pages.Settings;
using System;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Threading;
using Wpf.Ui.Services;

namespace BKTrans;

/// <summary>
/// Interaction logic for App.xaml
/// </summary>
public partial class App : Application
{
    private const string _uniqueEventName = "{GUID}BKTransSingleInstanceEvent";
    private const string _uniqueMutexName = "{GUID}BKTransSingleInstanceMutex";
    private EventWaitHandle _eventWaitHandle;
    private Mutex _mutex;

    private static readonly IHost _host = Host
        .CreateDefaultBuilder()
        .ConfigureAppConfiguration(c => { c.SetBasePath(AppContext.BaseDirectory); })
        .ConfigureServices((context, services) =>
        {
            // App Host
            services.AddHostedService<BKTrans.Services.ApplicationHostService>();

            // Main window container with navigation
            services.AddSingleton<IWindow, MainWindow>();
            services.AddSingleton<MainWindowViewModel>();
            services.AddSingleton<INavigationService, NavigationService>();
            services.AddSingleton<ISnackbarService, SnackbarService>();
            services.AddSingleton<IContentDialogService, ContentDialogService>();
            services.AddSingleton<WindowsProviderService>();

            // Top-level pages
            services.AddSingleton<DashboardPage>();
            services.AddSingleton<DashboardViewModel>();
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

            // Windows
            services.AddSingleton<FloatCaptureRectWindow>();
            services.AddSingleton<FloatTransTextWindow>();
        }).Build();

    public static T? GetService<T>() where T : class
    {
        return _host.Services.GetService(typeof(T)) as T ?? null;
    }
    private async void OnStartup(object sender, StartupEventArgs e)
    {
        if (SingleInstance())
            return;

        Dispatcher.UnhandledException += OnDispatcherUnhandledException;
        AppDomain.CurrentDomain.UnhandledException += OnCurrentDomainUnhandledException;
        TaskScheduler.UnobservedTaskException += OnTaskSchedulerUnobservedTaskException;

        await _host.StartAsync();
    }

    private async void OnExit(object sender, ExitEventArgs e)
    {
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
        string error_msg = "";
        do
        {
            if (exception_type != null)
                error_msg += exception_type + ": ";
            if (e == null)
                break;
            error_msg += e.Message + "\n";
            error_msg += e.StackTrace + "\n\n";
            if (e.InnerException == null)
                break;
            error_msg += e.InnerException.Message + "\n";
            error_msg += e.InnerException.StackTrace + "\n\n";
        } while (false);
        //SetTargetText(error_msg);
    }

    private bool SingleInstance()
    {
        //https://stackoverflow.com/questions/14506406/wpf-single-instance-best-practices
        bool isOwned;
        _mutex = new Mutex(true, _uniqueMutexName, out isOwned);
        _eventWaitHandle = new EventWaitHandle(false, EventResetMode.AutoReset, _uniqueEventName);

        // So, R# would not give a warning that this variable is not used.
        GC.KeepAlive(_mutex);

        if (isOwned)
        {
            // Spawn a thread which will be waiting for our event
            var thread = new Thread(
                () =>
                {
                    while (_eventWaitHandle.WaitOne())
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
        _eventWaitHandle.Set();

        // Terminate this instance.
        Shutdown();

        return true;
    }

    public static bool NavigateTo(Type pageType)
    {
        var navigationService = GetService<INavigationService>();

        if (navigationService == null)
            return false;

        return navigationService.Navigate(pageType);
    }

    public static void NavigateGoBack()
    {
        var navigationService = GetService<INavigationService>();

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

    public static void SnackbarError(string message)
    {
        var snackbarService = GetService<ISnackbarService>();
        snackbarService.Show("", message, Wpf.Ui.Controls.ControlAppearance.Danger);
    }
}
