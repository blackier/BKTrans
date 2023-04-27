using System;
using System.Threading;
using System.Windows;

namespace BKTrans
{
    /// <summary>
    /// Interaction logic for App.xaml
    /// </summary>
    public partial class App : Application
    {
        private const string _uniqueEventName = "{GUID}BKTransSingleInstanceEvent";
        private const string _uniqueMutexName = "{GUID}BKTransSingleInstanceMutex";
        private EventWaitHandle _eventWaitHandle;
        private Mutex _mutex;

        private void AppOnStartup(object sender, StartupEventArgs e)
        {
            SingleInstance();
        }

        private void SingleInstance()
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
                return;
            }

            // Notify other instance so it could bring itself to foreground.
            _eventWaitHandle.Set();

            // Terminate this instance.
            Shutdown();
        }
    }
}
