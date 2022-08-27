using BKAssembly;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.Drawing.Imaging;
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

        int cou11nt = 0;
        public MainWindow()
        {
            InitializeComponent();

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
            foreach (var ele in BKTransMap.TransType)
            {
                combobox_trans_type.Items.Add(ele.Value);
            }
            combobox_trans_type.SelectedItem = BKTransMap.TransType[_options.trans_type];
            RestoreLanguageTypeMap();

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
            _notifyIcon.Click += new EventHandler(NotifyIcon_Open);

            // 关联关闭函数，设置为最小化到托盘
            Closing += new CancelEventHandler(Window_Closing);

            _floatTextWindow = new FloatTextWindow((FloatTextWindow.ButtonType btntype) =>
            {
                switch (btntype)
                {
                    case FloatTextWindow.ButtonType.Capture:
                        _floatTextWindow.HideWnd();
                        Dispatcher.Invoke(() => DoCaptureOCR(false, true, false));
                        break;
                    case FloatTextWindow.ButtonType.Trans:
                        DoCaptureOCR(true, true, false);
                        break;
                    default:
                        break;
                }

            });
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
            return Dispatcher.Invoke(() =>
            {
                return textbox_source_text.Text;
            });
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

            List<string> ocrlantypes = BKTransMap.GetOCRLanguageTypeName(transType);
            combobox_src_type.Items.Clear();
            foreach (var ele in ocrlantypes)
            {
                combobox_src_type.Items.Add(ele);
            }

            List<string> translantypes = BKTransMap.GetTransLanguageTypeName(transType);
            combobox_target_type.Items.Clear();
            foreach (var ele in translantypes)
            {
                combobox_target_type.Items.Add(ele);
            }

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

            if (showMainWindow)
                ShowWnd();

            _captureRect = capturedata.captureRect;
            do
            {
                if (capturedata.captureBmp == null)
                    break;

                var bmpData = (Bitmap)capturedata.captureBmp.Clone();
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
                SetSourceText(ocrResultText);
                Dispatcher.Invoke(() => DoTextTrans(showFloatWindow));
            } while (false);
        }

        private async void DoTextTrans(bool showFloatWindow = false)
        {
            SaveLanguageTypeMap();

            SetTargetText("文本翻译中...");
            string transResultText = await Task.Run(() => _transHandle.Trans(_transSetting, GetSourceText()));
            SetTargetText(transResultText);

            _floatTextWindow.SetTextRect(_captureRect);
            _floatTextWindow.SetText(transResultText);
            if (showFloatWindow)
            {
                _floatTextWindow.ShowWnd();
            }
        }

        private void NotifyIcon_Open(object sender, EventArgs e)
        {
            do
            {
                if (e.GetType().Name == "MouseEventArgs")
                {
                    var mouse_event_args = (MouseEventArgs)e;
                    if (mouse_event_args.Button != MouseButtons.Left)
                    {
                        break;
                    }
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
            } while (false);
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
            Dispatcher.Invoke(() => DoCaptureOCR(false, true, false));
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
                Dispatcher.Invoke(() => DoCaptureOCR(false, true, false));
            }
            else if (wParam.ToInt64() == (int)HotKeyId.trans)
            {
                Dispatcher.Invoke(() => DoCaptureOCR(true, true, false));
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
                var newcaptruebmp = new BKScreenCapture().CaptureLastRegion().captureBmp;
                if (newcaptruebmp == null)
                    break;


                float similarity = BKMisc.BitmapDHashCompare(newcaptruebmp, _captureBmp);
                if (similarity < _options.auto_captrue_trans_similarity)
                {
                    newcaptruebmp.Save("p1_" + cou11nt.ToString(), ImageFormat.Bmp);
                    _captureBmp.Save("p2_" + cou11nt.ToString(), ImageFormat.Bmp);
                    _captureBmp = newcaptruebmp;
                    _autoCaptrueTransStart = true;
                    break;
                }

                _autoCaptrueTransCountdown--;
                if (_autoCaptrueTransCountdown > 0)
                    return;
                if (_autoCaptrueTransStart)
                {
                    Dispatcher.Invoke(() => DoCaptureOCR(true, true, false));
                    _autoCaptrueTransStart = false;
                }
            } while (false);

            _autoCaptrueTransCountdown = _options.auto_captrue_trans_countdown;
        }

        private void combobox_trans_type_SelectionChanged(object sender, System.Windows.Controls.SelectionChangedEventArgs e)
        {
            RestoreLanguageTypeMap();
        }

        private void btn_setting_Click(object sender, RoutedEventArgs e)
        {
            var settingsWindow = new Settings
            {
                Owner = this
            };
            settingsWindow.ShowDialog();
        }

        private void btn_capture_Click(object sender, RoutedEventArgs e)
        {
            HideWnd();
            Dispatcher.Invoke(() => DoCaptureOCR());
        }

        private void btn_trans_Click(object sender, RoutedEventArgs e)
        {
            Dispatcher.Invoke(() => DoTextTrans());
        }
    }
}
