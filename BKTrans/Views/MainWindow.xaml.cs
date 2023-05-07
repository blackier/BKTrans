using BKTrans.Misc;
using System;
using System.Collections.Generic;
using System.ComponentModel;
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
using static BKTrans.Misc.BKOCRMicrosoft;

namespace BKTrans
{
    public partial class MainWindow : Window
    {
        // Win32API: RegisterHotKey function
        private enum HotKeyId
        {
            capture = 0xB001,
            trans = 0xB002,
            hide = 0xB003
        }

        private NotifyIcon _notifyIcon;
        private bool _notifyClose;

        private RectangleF _captureRect;
        private Bitmap _captureBmp;

        private FloatCaptureRectWindow _floatTextWindow;

        private DispatcherTimer _autoCaptrueTransTimer;
        private int _autoCaptrueTransCountdown;
        private bool _autoCaptrueTransStart;

        private bool _comboxUpdating = false;
        private string _textSpliteString = "\n+++===+++===+++\n";

        private string _tsmenuitemAutotransName = "tsmenuitem_autotrans";

        static int _autoCaptrueTransDebugNum = 0;

        private MainWindowViewModel _viewModel;

        public MainWindow()
        {
            _viewModel = new MainWindowViewModel();
            this.DataContext = _viewModel;

            InitializeComponent();

            // 异常处理
            Dispatcher.UnhandledException += Dispatcher_UnhandledException;
            AppDomain.CurrentDomain.UnhandledException += CurrentDomain_UnhandledException;
            TaskScheduler.UnobservedTaskException += TaskScheduler_UnobservedTaskException;

            // 自动截图翻译
            _autoCaptrueTransCountdown = _viewModel.AutoCaptrueTransCountdown;
            _autoCaptrueTransStart = false;
            _autoCaptrueTransTimer = new();
            _autoCaptrueTransTimer.Tick += _catprueTransTimer_Tick;
            _autoCaptrueTransTimer.Interval = new TimeSpan(0, 0, 0, 0, _viewModel.AutoCaptrueTransInterval);
            if (_viewModel.AutoCaptrueTransOpen)
                _autoCaptrueTransTimer.Start();

            // 设置支持语言列表
            combobox_ocr_type_CheckClick(null, null);
            combobox_trans_type_CheckClick(null, null);

            // 加载时才需要处理的内容
            Loaded += new RoutedEventHandler(Window_Loaded);
            // 关联关闭函数，设置为最小化到托盘
            Closing += new CancelEventHandler(Window_Closing);

            // 翻译浮窗设置
            _floatTextWindow = new FloatCaptureRectWindow((FloatCaptureRectWindow.ButtonType btntype, object arg) =>
            {
                switch (btntype)
                {
                    case FloatCaptureRectWindow.ButtonType.Capture:
                        _floatTextWindow.HideWindow();
                        Dispatcher.InvokeAsync(() => DoCaptureOCR(false, true, false));
                        break;
                    case FloatCaptureRectWindow.ButtonType.Trans:
                        Dispatcher.InvokeAsync(() => DoCaptureOCR(true, true, false));
                        break;
                    case FloatCaptureRectWindow.ButtonType.AutoTrans:
                        Dispatcher.InvokeAsync(() => AutoTransSetting((bool)arg));
                        break;
                    default:
                        break;
                }

            });
            _floatTextWindow.SetAutoTransStatus(_viewModel.AutoCaptrueTransOpen);
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

        private void TaskScheduler_UnobservedTaskException(object sender, UnobservedTaskExceptionEventArgs e)
        {
            ExceptionHandler("TaskScheduler_UnobservedTaskException", e.Exception);
        }

        private void CurrentDomain_UnhandledException(object sender, UnhandledExceptionEventArgs e)
        {
            ExceptionHandler("CurrentDomain_UnhandledException", e.ExceptionObject as Exception);
        }

        private void Dispatcher_UnhandledException(object sender, DispatcherUnhandledExceptionEventArgs e)
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
            SetTargetText(error_msg);
        }

        protected override void OnSourceInitialized(EventArgs e)
        {
            // 热键注册
            base.OnSourceInitialized(e);
            IntPtr handle = new WindowInteropHelper(this).Handle;
            bool is_succeess = false;
            is_succeess = BKHotKey.Register(handle, (int)HotKeyId.capture, (uint)BKHotKey.Modifiers.norepeat, (uint)Keys.F2);
            is_succeess = BKHotKey.Register(handle, (int)HotKeyId.trans, (uint)(BKHotKey.Modifiers.shift | BKHotKey.Modifiers.alt | BKHotKey.Modifiers.norepeat), (uint)Keys.X);
            is_succeess = BKHotKey.Register(handle, (int)HotKeyId.hide, (uint)(BKHotKey.Modifiers.shift | BKHotKey.Modifiers.alt | BKHotKey.Modifiers.norepeat), (uint)Keys.Z);
            if (!is_succeess)
            {
                SetSourceText("热键注册失败");
            }
            else
            {
                HwndSource hwnd_source = PresentationSource.FromVisual(this) as HwndSource;
                hwnd_source.AddHook(HotKey_Hook);
            }

        }

        private void SetSourceText(string src_text)
        {
            Dispatcher.Invoke(() =>
            {
                textbox_source_text.Text = src_text;
            });
        }

        private void SetTargetText(string target_text)
        {
            Dispatcher.Invoke(() =>
            {
                textbox_target_text.Text = target_text;
            });
        }

        private string GetSourceText()
        {
            string text = Dispatcher.Invoke(() =>
            {
                return textbox_source_text.Text;
            });
            text = text.Split(_textSpliteString)[0];
            return text;
        }

        private string GetTargetText()
        {
            return Dispatcher.Invoke(() =>
            {
                return textbox_target_text.Text;
            });
        }

        private void ShowWindow()
        {
            Show();
        }

        private void HideWindow()
        {
            Hide();
            _floatTextWindow.HideWindow();
            Thread.Sleep(250);
        }

        private void AutoTransSetting(bool start)
        {
            if (start)
            {
                _autoCaptrueTransTimer.Start();
                _viewModel.AutoCaptrueTransOpen = true;
            }
            else
            {
                _autoCaptrueTransTimer.Stop();
                _viewModel.AutoCaptrueTransOpen = false;
            }
            _floatTextWindow.SetAutoTransStatus(_viewModel.AutoCaptrueTransOpen);
            if (_notifyIcon.ContextMenuStrip.Items[_tsmenuitemAutotransName] != null)
                (_notifyIcon.ContextMenuStrip.Items[_tsmenuitemAutotransName] as ToolStripMenuItem).Checked = _viewModel.AutoCaptrueTransOpen;
        }

        private async void DoCaptureOCR(bool captrueLast = false, bool showFloatWindow = false, bool showMainWindow = true)
        {
            BKScreenCapture.DataStruct capturedata;

            if (captrueLast)
                capturedata = new BKScreenCapture().CaptureCustomRegion(_floatTextWindow.GetTextRect());
            else
                capturedata = new BKScreenCapture().CaptureRegion();

            _captureRect = capturedata.captureRect;
            _floatTextWindow.SetTextRect(_captureRect);

            if (showMainWindow)
                ShowWindow();
            if (showFloatWindow)
                _floatTextWindow.ShowWindow();
            do
            {
                if (capturedata.captureBmp == null)
                    break;

                var bmpData = capturedata.captureBmp;
                if (bmpData.Height < 15 || bmpData.Width < 15)
                {
                    SetSourceText("截取完成，截取最短边要求至少15px...");
                    break;
                }
                else if (bmpData.Height > 4096 || bmpData.Width > 4096)
                {
                    SetSourceText("截取完成，截取最长边要求不超过4096px...");
                    break;
                }

                _captureBmp?.Dispose();
                _captureBmp = bmpData;
                SetSourceText("截取完成，等待OCR翻译...");

                string ocrResultText = await _viewModel.OCR(bmpData);
                if (string.IsNullOrEmpty(ocrResultText))
                {
                    SetSourceText("");
                    SetTargetText("");
                    break;
                }

                ocrResultText = _viewModel.OCRRepalce(ocrResultText) + _textSpliteString + ocrResultText;
                SetSourceText(ocrResultText);

                Dispatcher.Invoke(() => DoTextTrans());
            } while (false);
        }

        private async void DoTextTrans()
        {
            string srctext = GetSourceText();
            if (string.IsNullOrEmpty(srctext))
                return;

            SetTargetText("文本翻译中...");

            string transResultText = "";
            foreach (var result in await _viewModel.TransText(srctext))
            {
                transResultText += result;
                transResultText += _textSpliteString;
            }
            if (!string.IsNullOrEmpty(transResultText))
                transResultText = transResultText.Remove(transResultText.Length - _textSpliteString.Length);

            SetTargetText(transResultText);
            _floatTextWindow.SetText(transResultText);
        }

        #region 事件处理
        public IntPtr HotKey_Hook(IntPtr hwnd, int msg, IntPtr wParam, IntPtr lParam, ref bool handled)
        {
            if (wParam.ToInt64() == (int)HotKeyId.capture)
            {
                _floatTextWindow.HideWindow();
                Dispatcher.InvokeAsync(() => DoCaptureOCR(false, true, false));
            }
            else if (wParam.ToInt64() == (int)HotKeyId.trans)
            {
                Dispatcher.InvokeAsync(() => DoCaptureOCR(true, true, false));
            }
            else if (wParam.ToInt64() == (int)HotKeyId.hide)
            {
                _floatTextWindow.HideWindow();
            }
            return IntPtr.Zero;
        }

        #region 托盘事件
        private void NotifyIcon_Click(object sender, EventArgs e)
        {
            if (e.GetType().Name == "MouseEventArgs")
            {
                if (((MouseEventArgs)e).Button != MouseButtons.Left)
                    return;
            }
            if (WindowState == WindowState.Minimized)
            {
                WindowState = WindowState.Normal;
            }
            if (!IsVisible)
            {
                ShowWindow();
            }
            Activate();
        }

        private void NotifyIcon_Close(object sender, EventArgs e)
        {
            _notifyIcon.Visible = false;
            _notifyClose = true;
            Close();
        }

        private void NotifyIcon_Capture(object sender, EventArgs e)
        {
            _floatTextWindow.HideWindow();
            Dispatcher.InvokeAsync(() => DoCaptureOCR(false, true, false));
        }

        private void NotifyIcon_Trans(object sender, EventArgs e)
        {
            DoCaptureOCR(true, true, false);
        }

        private void NotifyIcon_Open(object sender, EventArgs e)
        {
            _floatTextWindow.ShowWindow();
        }

        private void NotifyIcon_Hide(object sender, EventArgs e)
        {
            _floatTextWindow.HideWindow();
        }

        private void NotifyIcon_AutoCaptrueTrans(object sender, EventArgs e)
        {
            var obj = (ToolStripMenuItem)sender;
            obj.Checked = !obj.Checked;
            Dispatcher.InvokeAsync(() => AutoTransSetting(obj.Checked));
        }
        #endregion

        #region 窗体事件
        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            // 设置托盘图标
            _notifyClose = false;
            var notifyIconCms = new ContextMenuStrip();
            notifyIconCms.Items.Add(new ToolStripMenuItem("截取", null, new EventHandler(NotifyIcon_Capture)));
            notifyIconCms.Items.Add(new ToolStripMenuItem("翻译", null, new EventHandler(NotifyIcon_Trans)));
            notifyIconCms.Items.Add("-");
            notifyIconCms.Items.Add(new ToolStripMenuItem("显示浮窗", null, new EventHandler(NotifyIcon_Open)));
            notifyIconCms.Items.Add(new ToolStripMenuItem("隐藏浮窗", null, new EventHandler(NotifyIcon_Hide)));
            notifyIconCms.Items.Add("-");
            notifyIconCms.Items.Add(new ToolStripMenuItem("自动翻译", null, new EventHandler(NotifyIcon_AutoCaptrueTrans))
            {
                Checked = _viewModel.AutoCaptrueTransOpen,
                Name = _tsmenuitemAutotransName
            });
            notifyIconCms.Items.Add("-");
            notifyIconCms.Items.Add(new ToolStripMenuItem("退出", null, new EventHandler(NotifyIcon_Close)));
            _notifyIcon = new NotifyIcon
            {
                Visible = true,
                Icon = System.Drawing.Icon.ExtractAssociatedIcon(System.Windows.Forms.Application.ExecutablePath),
                ContextMenuStrip = notifyIconCms
            };
            _notifyIcon.Click += new EventHandler(NotifyIcon_Click);
        }

        private void Window_Closing(object sender, CancelEventArgs e)
        {
            if (_notifyClose)
            {
                _floatTextWindow.Close();
                e.Cancel = false;
            }
            else
            {
                Hide();
                _floatTextWindow.HideWindow();
                e.Cancel = true;
            }
            SettingsModel.SaveSettings();
        }
        #endregion 窗体事件

        #region 控件事件
        private void _catprueTransTimer_Tick(object sender, EventArgs e)
        {
            do
            {
                if (!_floatTextWindow.IsVisible)
                    break;
                if (_captureBmp == null)
                    break;

                if (_autoCaptrueTransStart)
                {
                    // 倒数
                    _autoCaptrueTransCountdown--;
                    if (_autoCaptrueTransCountdown > 0)
                        return;
                    // 再次对比
                }

                var t = new BKScreenCapture().CaptureCustomRegion(_floatTextWindow.GetTextRect());
                var newcaptruebmp = t.captureBmp;
                if (newcaptruebmp == null)
                    break;

                float similarity = BKMisc.BitmapDHashCompare(newcaptruebmp, (Bitmap)_captureBmp.Clone());

                _autoCaptrueTransDebugNum++;
                if (similarity < _viewModel.AutoCaptrueTransSimilarity)
                {
                    // 发生变化
                    //newcaptruebmp.Save($"{_autoCaptrueTransDebugNum}_{similarity}_1.png", ImageFormat.Png);
                    //_captureBmp.Save($"{_autoCaptrueTransDebugNum}_{similarity}_2.png", ImageFormat.Png);

                    _captureBmp?.Dispose();
                    _captureBmp = newcaptruebmp;
                    _autoCaptrueTransStart = true;
                    break;
                }
                newcaptruebmp.Dispose();

                if (_autoCaptrueTransStart)
                    Dispatcher.InvokeAsync(() => DoCaptureOCR(true, true, false));
                _autoCaptrueTransStart = false;
            } while (false);

            _autoCaptrueTransCountdown = _viewModel.AutoCaptrueTransCountdown;
        }

        private void combobox_ocr_type_CheckClick(object sender, RoutedEventArgs e)
        {
            _viewModel.UpdateLanguageTypeMap();
        }

        private void combobox_trans_type_CheckClick(object sender, RoutedEventArgs e)
        {
            _viewModel.UpdateLanguageTypeMap();
        }

        private void btn_setting_Click(object sender, RoutedEventArgs e)
        {
            var settingsWindow = new SettingsWindow
            {
                Owner = this
            };
            settingsWindow.ShowDialog();

            _viewModel.OcrReplaceIsChanged = true;
        }

        private void btn_capture_Click(object sender, RoutedEventArgs e)
        {
            bool floatwindowVisible = _floatTextWindow.IsVisible;
            HideWindow();
            Dispatcher.InvokeAsync(() => DoCaptureOCR(false, floatwindowVisible));
        }

        private void btn_trans_Click(object sender, RoutedEventArgs e)
        {
            Dispatcher.InvokeAsync(() => DoTextTrans());
        }

        #endregion 用户控件

        #endregion 事件处理
    }
}
