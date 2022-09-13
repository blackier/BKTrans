using BKAssembly;
using System;
using System.ComponentModel;
using System.Drawing;
using System.Linq;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Forms;
using System.Windows.Interop;
using System.Windows.Threading;

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

        private Rectangle _captureRect;
        private Bitmap _captureBmp;

        private FloatTextWindow _floatTextWindow;

        private Settings.Options _options;

        private BKOCRBaidu _ocrBaidu;
        private BKTransBase _transHandle;
        private BKSetting _transSetting;

        private DispatcherTimer _autoCaptrueTransTimer;
        private int _autoCaptrueTransCountdown;
        private bool _autoCaptrueTransStart;

        private bool _combox_updating = false;
        private string _ocr_replace_splite_string = "\n+++===+++===+++\n";
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

            // 设置支持语言列表
            _combox_updating = true;
            combobox_trans_type.ItemsSource = BKTransMap.TransType.Values.ToList();
            combobox_trans_type.SelectedItem = BKTransMap.TransType[_options.trans_type];
            _combox_updating = false;
            RestoreLanguageTypeMap();

            // 设置OCR文本替换
            _combox_updating = true;
            combobox_ocr_replace.ItemsSource = _options.ocr_replace.Keys.ToList();
            combobox_ocr_replace.SelectedItem = _options.ocr_replace_select;
            _combox_updating = false;
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
            { Checked = _options.auto_captrue_trans_open });
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

            _floatTextWindow = new FloatTextWindow((FloatTextWindow.ButtonType btntype) =>
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
                    default:
                        break;
                }

            });
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
            text = text.Split(_ocr_replace_splite_string)[0];
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
            string transType = BKTransMap.TransType.ElementAt(combobox_trans_type.SelectedIndex).Key;

            combobox_src_type.ItemsSource = BKTransMap.GetOCRLanguageTypeName(transType);
            combobox_target_type.ItemsSource = BKTransMap.GetTransLanguageTypeName(transType);

            string src_type = "";
            string target_type = "";
            if (transType == BKTransMap.TransType.ElementAt(0).Key)
            {
                src_type = _options.trans_baidu.from;
                target_type = _options.trans_baidu.to;
            }
            else if (transType == BKTransMap.TransType.ElementAt(1).Key)
            {
                src_type = _options.trans_caiyun.from;
                target_type = _options.trans_caiyun.to;
            }

            if (!string.IsNullOrEmpty(src_type))
            {
                combobox_src_type.SelectedItem = BKTransMap.GetOCRLanguageTypeName(transType, src_type);
            }
            if (!string.IsNullOrEmpty(target_type))
            {
                combobox_target_type.SelectedItem = BKTransMap.GetTransLanguageTypeName(transType, target_type);
            }
        }

        private void SaveLanguageTypeMap()
        {
            string transType = BKTransMap.TransType.ElementAt(combobox_trans_type.SelectedIndex).Key;
            _options.trans_type = transType;

            string lantype = "";
            string src_type = "";
            string target_type = "";
            BKTransMap.GetLanguageType(transType, combobox_src_type.SelectedIndex, combobox_target_type.SelectedIndex,
                ref lantype, ref src_type, ref target_type);
            _options.ocr_baidu.language_type = lantype;

            if (transType == BKTransMap.TransType.ElementAt(0).Key)
            {
                _options.trans_baidu.from = src_type;
                _options.trans_baidu.to = target_type;

                _transSetting = _options.trans_baidu;
                _transHandle = new BKTransBaidu();
            }
            else if (transType == BKTransMap.TransType.ElementAt(1).Key)
            {
                _options.trans_caiyun.from = src_type;
                _options.trans_caiyun.to = target_type;

                _transSetting = _options.trans_caiyun;
                _transHandle = new BKTransCaiyun();
            }
            Settings.SaveSettings();
        }

        private async void DoCaptureOCR(bool captrueLast = false, bool showFloatWindow = false, bool showMainWindow = true)
        {
            SaveLanguageTypeMap();
            BKScreenCapture.DataStruct capturedata;

            if (captrueLast)
                capturedata = new BKScreenCapture().CaptureLastRegion();
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

                if (_ocrBaidu == null)
                    _ocrBaidu = new BKOCRBaidu();

                string ocrResultText = "";
                string ocrResult = await Task.Run(() => _ocrBaidu.OCR(_options.ocr_baidu, bmpData));
                try
                {
                    JsonDocument jdocOcrResult = JsonDocument.Parse(ocrResult);
                    var rootElement = jdocOcrResult.RootElement;
                    if (!rootElement.TryGetProperty("words_result", out JsonElement wordsResult))
                    {
                        ocrResultText = ocrResult;
                    }
                    else
                    {
                        foreach (JsonElement words_elem in wordsResult.EnumerateArray())
                            ocrResultText += words_elem.GetProperty("words").GetString();
                    }
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

                    ocrResultText = replacetext + _ocr_replace_splite_string + ocrResultText;
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
            string transResultText = await Task.Run(() => _transHandle.Trans(_transSetting, srctext));

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
            if (obj.Checked)
            {
                obj.Checked = false;
                _autoCaptrueTransTimer.Stop();
                _options.auto_captrue_trans_open = false;
            }
            else
            {
                obj.Checked = true;
                _autoCaptrueTransTimer.Start();
                _options.auto_captrue_trans_open = true;
            }
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

                var newcaptruebmp = new BKScreenCapture().CaptureLastRegion().captureBmp;
                if (newcaptruebmp == null)
                    break;

                float similarity = BKMisc.BitmapDHashCompare(newcaptruebmp, _captureBmp);
                if (similarity < _options.auto_captrue_trans_similarity)
                {
                    // 发生变化
                    _captureBmp?.Dispose();
                    _captureBmp = newcaptruebmp;
                    _autoCaptrueTransStart = true;
                    break;
                }
                newcaptruebmp.Dispose();

                if (_autoCaptrueTransStart)
                {
                    _autoCaptrueTransStart = false;
                    Dispatcher.InvokeAsync(() => DoCaptureOCR(true, true, false));
                }
            } while (false);

            _autoCaptrueTransCountdown = _options.auto_captrue_trans_countdown;
        }

        private void combobox_trans_type_SelectionChanged(object sender, System.Windows.Controls.SelectionChangedEventArgs e)
        {
            if (_combox_updating)
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

            _combox_updating = true;
            combobox_ocr_replace.ItemsSource = _options.ocr_replace.Keys.ToList();
            combobox_ocr_replace.SelectedItem = _options.ocr_replace_select;
            _combox_updating = false;
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
            if (_combox_updating)
                return;
            _options.ocr_replace_select = (string)combobox_ocr_replace.SelectedItem;
        }
    }
}
