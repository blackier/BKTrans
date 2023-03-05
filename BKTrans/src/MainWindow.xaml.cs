using BKTrans.Misc;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Forms;
using System.Windows.Interop;
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

        private FloatTextWindow _floatTextWindow;

        private Settings.Options _options;

        private BKOCRBase _ocrClient;
        private BKSetting _ocrSetting;
        private BKTransBase _transClient;
        private BKSetting _transSetting;

        private DispatcherTimer _autoCaptrueTransTimer;
        private int _autoCaptrueTransCountdown;
        private bool _autoCaptrueTransStart;

        private bool _comboxUpdating = false;
        private string _textSpliteString = "\n+++===+++===+++\n";

        private string _tsmenuitemAutotransName = "tsmenuitem_autotrans";

        static int _autoCaptrueTransDebugNum = 0;
        public MainWindow()
        {
            InitializeComponent();

            // 异常处理
            Dispatcher.UnhandledException += Dispatcher_UnhandledException;
            AppDomain.CurrentDomain.UnhandledException += CurrentDomain_UnhandledException;
            TaskScheduler.UnobservedTaskException += TaskScheduler_UnobservedTaskException;

            _options = Settings.LoadSetting();

            // 自动截图翻译
            _autoCaptrueTransCountdown = _options.auto_captrue_trans_countdown;
            _autoCaptrueTransStart = false;
            _autoCaptrueTransTimer = new();
            _autoCaptrueTransTimer.Tick += _catprueTransTimer_Tick;
            _autoCaptrueTransTimer.Interval = new TimeSpan(0, 0, 0, 0, _options.auto_captrue_trans_interval);
            if (_options.auto_captrue_trans_open)
                _autoCaptrueTransTimer.Start();

            // 本地ocr
            checkbox_local_ocr.IsChecked = _options.ocr_microsoft_open;

            // 双翻译
            checkbox_both_trans.IsChecked = _options.trans_both;

            // 设置支持语言列表
            _comboxUpdating = true;
            combobox_trans_type.ItemsSource = BKTransMap.TransType;
            combobox_trans_type.SelectedItem = _options.trans_type;
            _comboxUpdating = false;
            RestoreLanguageTypeMap();

            // 设置OCR文本替换
            _comboxUpdating = true;
            combobox_ocr_replace.ItemsSource = _options.ocr_replace.Keys.ToList();
            combobox_ocr_replace.SelectedItem = _options.ocr_replace_select;
            _comboxUpdating = false;

            // 设置托盘图标
            _notifyClose = false;
            var notifyIconCms = new ContextMenuStrip();
            notifyIconCms.Items.Add(new ToolStripMenuItem("打开", null, new EventHandler(NotifyIcon_Open)));
            notifyIconCms.Items.Add(new ToolStripMenuItem("截取", null, new EventHandler(NotifyIcon_Capture)));
            notifyIconCms.Items.Add(new ToolStripMenuItem("翻译", null, new EventHandler(NotifyIcon_Trans)));
            notifyIconCms.Items.Add("-");
            notifyIconCms.Items.Add(new ToolStripMenuItem("隐藏", null, new EventHandler(NotifyIcon_Hide)));
            notifyIconCms.Items.Add("-");
            notifyIconCms.Items.Add(new ToolStripMenuItem("自动翻译", null, new EventHandler(NotifyIcon_AutoCaptrueTrans))
            {
                Checked = _options.auto_captrue_trans_open,
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

            // 关联关闭函数，设置为最小化到托盘
            Closing += new CancelEventHandler(Window_Closing);

            // 翻译浮窗设置
            _floatTextWindow = new FloatTextWindow((FloatTextWindow.ButtonType btntype, object arg) =>
            {
                switch (btntype)
                {
                    case FloatTextWindow.ButtonType.Capture:
                        _floatTextWindow.HideWnd();
                        Dispatcher.InvokeAsync(() => DoCaptureOCR(false, true, false));
                        break;
                    case FloatTextWindow.ButtonType.Trans:
                        Dispatcher.InvokeAsync(() => DoCaptureOCR(true, true, false));
                        break;
                    case FloatTextWindow.ButtonType.AutoTrans:
                        Dispatcher.InvokeAsync(() => AutoTransSetting((bool)arg));
                        break;
                    default:
                        break;
                }

            });
            _floatTextWindow.SetAutoTransStatus(_options.auto_captrue_trans_open);
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

        private void ShowWnd()
        {
            Show();
        }

        private void HideWnd()
        {
            Hide();
            _floatTextWindow.HideWnd();
            Thread.Sleep(250);
        }

        private void RestoreLanguageTypeMap()
        {
            // 计算ocr和文本翻译交集
            string ocrType = BKTransMap.OCRType[checkbox_local_ocr.IsChecked ?? false ? 1 : 0];
            List<string> ocrItemsSource = BKTransMap.CreateBKOCRClient(ocrType).GetLangType();

            string transType = BKTransMap.TransType[combobox_trans_type.SelectedIndex];
            List<string> transItemsSource = ocrItemsSource.Intersect(BKTransMap.CreateBKTransClient(transType).GetLangType()).ToList();
            if (checkbox_both_trans.IsChecked ?? false)
            {
                foreach (string type in BKTransMap.TransType)
                {
                    transItemsSource = transItemsSource.Intersect(BKTransMap.CreateBKTransClient(type).GetLangType()).ToList();
                }
            }

            combobox_src_type.ItemsSource = transItemsSource;
            combobox_target_type.ItemsSource = transItemsSource;

            string srcType = "";
            string targetType = "";
            if (transType == BKTransMap.TransType[0])
            {
                srcType = _options.trans_baidu.from;
                targetType = _options.trans_baidu.to;
            }
            else if (transType == BKTransMap.TransType[1])
            {
                srcType = _options.trans_caiyun.from;
                targetType = _options.trans_caiyun.to;
            }

            if (!transItemsSource.Contains(srcType))
            {
                srcType = "";
            }
            if (!transItemsSource.Contains(targetType))
            {
                targetType = "";
            }

            if (!string.IsNullOrEmpty(srcType))
            {
                combobox_src_type.SelectedItem = srcType;
            }
            if (!string.IsNullOrEmpty(targetType))
            {
                combobox_target_type.SelectedItem = targetType;
            }
        }

        private void AutoTransSetting(bool start)
        {
            if (start)
            {
                _autoCaptrueTransTimer.Start();
                _options.auto_captrue_trans_open = true;
            }
            else
            {
                _autoCaptrueTransTimer.Stop();
                _options.auto_captrue_trans_open = false;
            }
            _floatTextWindow.SetAutoTransStatus(_options.auto_captrue_trans_open);
            if (_notifyIcon.ContextMenuStrip.Items[_tsmenuitemAutotransName] != null)
                (_notifyIcon.ContextMenuStrip.Items[_tsmenuitemAutotransName] as ToolStripMenuItem).Checked = _options.auto_captrue_trans_open;
        }

        private void SaveLanguageTypeMap()
        {
            string transType = BKTransMap.TransType[combobox_trans_type.SelectedIndex];
            _options.trans_type = transType;
            string ocrType = BKTransMap.OCRType[_options.ocr_microsoft_open ? 1 : 0];

            string srcType = combobox_src_type.SelectedItem as string;
            string targetType = combobox_target_type.SelectedItem as string;

            // 文本翻译
            if (transType == BKTransMap.TransType[0])
            {
                _options.trans_baidu.from = srcType;
                _options.trans_baidu.to = targetType;

                _transSetting = _options.trans_baidu;
            }

            if (_options.trans_both)
                transType = BKTransMap.TransType[1];

            if (transType == BKTransMap.TransType[1])
            {
                _options.trans_caiyun.from = srcType;
                _options.trans_caiyun.to = targetType;

                _transSetting = _options.trans_caiyun;
            }
            _transClient = BKTransMap.CreateBKTransClient(transType);

            // ocr翻译
            _options.ocr_baidu.language_type = srcType;
            if (ocrType == BKTransMap.OCRType[1])
            {
                _ocrSetting = new SettingMiscrosoftOCR()
                {
                    language_tag = srcType
                };
            }
            else
            {
                _ocrSetting = _options.ocr_baidu;
            }
            _ocrClient = BKTransMap.CreateBKOCRClient(ocrType);
            Settings.SaveSettings();
        }

        private async void DoCaptureOCR(bool captrueLast = false, bool showFloatWindow = false, bool showMainWindow = true)
        {
            SaveLanguageTypeMap();
            BKScreenCapture.DataStruct capturedata;

            if (captrueLast)
                capturedata = new BKScreenCapture().CaptureCustomRegion(_floatTextWindow.GetTextRect());
            else
                capturedata = new BKScreenCapture().CaptureRegion();

            _captureRect = capturedata.captureRect;
            _floatTextWindow.SetTextRect(_captureRect);

            if (showMainWindow)
                ShowWnd();
            if (showFloatWindow)
                _floatTextWindow.ShowWnd();
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

                string ocrResultText = "";
                string ocrResult = "";
                _ = await Task.Run(() => _ocrClient.OCR(_ocrSetting, bmpData, out ocrResult));
                try
                {
                    ocrResultText = _ocrClient.ParseResult(ocrResult);
                }
                catch
                {
                    ocrResultText = ocrResult;
                    SetSourceText(ocrResultText);
                    if (showFloatWindow)
                    {
                        _floatTextWindow.SetText("OCR翻译失败，打开程序界面查看原因。");
                        _floatTextWindow.SetTextRect(_captureRect);
                        _floatTextWindow.ShowWnd();
                    }
                    break;
                }
                if (string.IsNullOrEmpty(ocrResultText))
                {
                    SetSourceText("");
                    SetTargetText("");
                    break;
                }
                if (_options.ocr_replace[_options.ocr_replace_select].Count > 0)
                {
                    string replacetext = (string)ocrResultText.Clone();
                    foreach (var replacemap in _options.ocr_replace[_options.ocr_replace_select])
                        if (!string.IsNullOrEmpty(replacemap.replace_src))
                            replacetext = replacetext.Replace(replacemap.replace_src, replacemap.replace_dst);

                    ocrResultText = replacetext + _textSpliteString + ocrResultText;
                }
                SetSourceText(ocrResultText);

                Dispatcher.Invoke(() => DoTextTrans());
            } while (false);
        }

        private async void DoTextTrans()
        {
            SaveLanguageTypeMap();

            string srctext = GetSourceText();
            if (string.IsNullOrEmpty(srctext))
                return;

            SetTargetText("文本翻译中...");

            string transResultText = "";
            if (_options.trans_both)
            {
                var transtasks = Task.WhenAll(new[] {
                    Task.Run(() => new BKTransBaidu().Trans(_options.trans_baidu, srctext))
                    , Task.Run(() => new BKTransCaiyun().Trans(_options.trans_caiyun, srctext)) });
                await Task.Run(() => transtasks.Wait());
                foreach (var result in transtasks.Result)
                {
                    transResultText += result;
                    transResultText += _textSpliteString;
                }
                if (!string.IsNullOrEmpty(transResultText))
                    transResultText = transResultText.Remove(transResultText.Length - _textSpliteString.Length);
            }
            else
            {
                transResultText = await Task.Run(() => _transClient.Trans(_transSetting, srctext));
            }


            SetTargetText(transResultText);
            _floatTextWindow.SetText(transResultText);
        }

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
                Show();
            }
            Activate();
        }
        private void NotifyIcon_Open(object sender, EventArgs e)
        {
            _floatTextWindow.ShowWnd();
        }

        private void NotifyIcon_Close(object sender, EventArgs e)
        {
            _notifyIcon.Visible = false;
            _notifyClose = true;
            Close();
        }

        private void NotifyIcon_Capture(object sender, EventArgs e)
        {
            _floatTextWindow.HideWnd();
            Dispatcher.InvokeAsync(() => DoCaptureOCR(false, true, false));
        }

        private void NotifyIcon_Trans(object sender, EventArgs e)
        {
            DoCaptureOCR(true, true, false);
        }

        private void NotifyIcon_Hide(object sender, EventArgs e)
        {
            _floatTextWindow.Hide();
        }

        private void NotifyIcon_AutoCaptrueTrans(object sender, EventArgs e)
        {
            var obj = (ToolStripMenuItem)sender;
            obj.Checked = !obj.Checked;
            Dispatcher.InvokeAsync(() => AutoTransSetting(obj.Checked));
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
                _floatTextWindow.Hide();
                e.Cancel = true;
            }
            Settings.SaveSettings();
        }

        public IntPtr HotKey_Hook(IntPtr hwnd, int msg, IntPtr wParam, IntPtr lParam, ref bool handled)
        {
            if (wParam.ToInt64() == (int)HotKeyId.capture)
            {
                _floatTextWindow.HideWnd();
                Dispatcher.InvokeAsync(() => DoCaptureOCR(false, true, false));
            }
            else if (wParam.ToInt64() == (int)HotKeyId.trans)
            {
                Dispatcher.InvokeAsync(() => DoCaptureOCR(true, true, false));
            }
            else if (wParam.ToInt64() == (int)HotKeyId.hide)
            {
                _floatTextWindow.Hide();
            }
            return IntPtr.Zero;
        }

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
                if (similarity < _options.auto_captrue_trans_similarity)
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

            _autoCaptrueTransCountdown = _options.auto_captrue_trans_countdown;
        }

        private void combobox_trans_type_SelectionChanged(object sender, System.Windows.Controls.SelectionChangedEventArgs e)
        {
            if (_comboxUpdating)
                return;
            RestoreLanguageTypeMap();
        }

        private void btn_setting_Click(object sender, RoutedEventArgs e)
        {
            var settingsWindow = new Settings
            {
                Owner = this
            };
            settingsWindow.ShowDialog();

            _comboxUpdating = true;
            combobox_ocr_replace.ItemsSource = _options.ocr_replace.Keys.ToList();
            combobox_ocr_replace.SelectedItem = _options.ocr_replace_select;
            _comboxUpdating = false;
        }

        private void btn_capture_Click(object sender, RoutedEventArgs e)
        {
            bool floatwindowVisible = _floatTextWindow.IsVisible;
            HideWnd();
            Dispatcher.InvokeAsync(() => DoCaptureOCR(false, floatwindowVisible));
        }

        private void btn_trans_Click(object sender, RoutedEventArgs e)
        {
            Dispatcher.InvokeAsync(() => DoTextTrans());
        }

        private void combobox_ocr_replace_SelectionChanged(object sender, System.Windows.Controls.SelectionChangedEventArgs e)
        {
            if (_comboxUpdating)
                return;
            _options.ocr_replace_select = (string)combobox_ocr_replace.SelectedItem;
        }

        private void checkbox_local_ocr_Click(object sender, RoutedEventArgs e)
        {
            _options.ocr_microsoft_open = checkbox_local_ocr.IsChecked ?? false ? true : false;
            RestoreLanguageTypeMap();

        }

        private void checkbox_both_trans_Click(object sender, RoutedEventArgs e)
        {
            _options.trans_both = (checkbox_both_trans.IsChecked ?? false) ? true : false;
            RestoreLanguageTypeMap();
        }
    }
}
