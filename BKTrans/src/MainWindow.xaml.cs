using BKAssembly;
using System;
using System.ComponentModel;
using System.Drawing;
using System.Text.Json;
using System.Threading;
using System.Windows;
using System.Windows.Forms;
using System.Windows.Interop;

namespace BKTrans
{
    public partial class MainWindow : Window
    {

        private BKScreenCapture mScreenCapture;

        private NotifyIcon mNotifyIcon;
        private bool mNotifyClose;

        private Rectangle mCaptureRect;
        private Rectangle mTextWndRect;

        private FloatTextWindow mTargetTextWindow;

        private Settings.Options mOptions;

        // Win32API: RegisterHotKey function
        private enum HotKeyId
        {
            capture = 0xB001,
            trans = 0xB002,
            hide = 0xB003
        }

        private enum CaptureType
        {
            button,
            hotkey
        }
        CaptureType mCapType;

        public MainWindow()
        {
            InitializeComponent();

            mCapType = CaptureType.button;
            mOptions = Settings.LoadSetting();
            RestoreLanguageTypeMap();

            mTargetTextWindow = new FloatTextWindow();

            // 设置托盘图标
            mNotifyClose = false;
            var notifyIconCms = new ContextMenuStrip();
            notifyIconCms.Items.Add(new ToolStripMenuItem("打开", null, new EventHandler(NotifyIcon_Open)));
            notifyIconCms.Items.Add(new ToolStripMenuItem("截取", null, new EventHandler(NotifyIcon_Capture)));
            notifyIconCms.Items.Add(new ToolStripMenuItem("翻译", null, new EventHandler(NotifyIcon_Trans)));
            notifyIconCms.Items.Add("-");
            notifyIconCms.Items.Add(new ToolStripMenuItem("隐藏", null, new EventHandler(NotifyIcon_Hide)));
            notifyIconCms.Items.Add("-");
            notifyIconCms.Items.Add(new ToolStripMenuItem("退出", null, new EventHandler(NotifyIcon_Close)));
            mNotifyIcon = new NotifyIcon
            {
                Visible = true,
                Icon = System.Drawing.Icon.ExtractAssociatedIcon(System.Windows.Forms.Application.ExecutablePath),
                ContextMenuStrip = notifyIconCms
            };
            mNotifyIcon.Click += new EventHandler(NotifyIcon_Open);

            // 关联关闭函数，设置为最小化到托盘
            this.Closing += new CancelEventHandler(Window_Closing);
        }

        protected override void OnSourceInitialized(EventArgs e)
        {
            // 热键注册
            base.OnSourceInitialized(e);
            IntPtr handle = new WindowInteropHelper(this).Handle;
            bool is_succeess = false;
            is_succeess = BKHotKey.Register(handle, (int)HotKeyId.capture, (uint)BKHotKey.Modifiers.norepeat, (uint)Keys.F2);
            is_succeess = BKHotKey.Register(handle, (int)HotKeyId.trans, (uint)BKHotKey.Modifiers.norepeat, (uint)Keys.NumPad0);
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
            this.Dispatcher.Invoke(() =>
            {
                this.tb_source_text.Text = src_text;
            });
        }

        private void SetTargetText(string target_text)
        {
            this.Dispatcher.Invoke(() =>
            {
                this.tb_target_text.Text = target_text;
            });
        }

        private string GetSourceText()
        {
            return this.Dispatcher.Invoke(() =>
            {
                return this.tb_source_text.Text;
            });
        }

        private string GetTargetText()
        {
            return this.Dispatcher.Invoke(() =>
            {
                return this.tb_target_text.Text;
            });
        }

        private void ShowWnd()
        {
            this.Dispatcher.Invoke(() =>
            {
                this.Show();
            });
        }

        private void HideWnd()
        {
            this.Dispatcher.Invoke(() =>
            {
                this.Hide();
            });
        }

        private Rectangle GetWndRect()
        {
            var w_1 = (double)Screen.PrimaryScreen.Bounds.Width;
            var w_2 = (double)SystemParameters.PrimaryScreenWidth;
            var p = w_2 / w_1;
            return new Rectangle { X = (int)(mCaptureRect.X * p), Y = (int)(mCaptureRect.Y * p), Width = (int)(mCaptureRect.Width * p), Height = (int)(mCaptureRect.Height * p) };
        }

        private void RestoreLanguageTypeMap()
        {
            if (mOptions.language_type != null && mOptions.language_type.Length != 0)
            {
                comboBox_src_text.SelectedIndex = BKBaiduOCR.LanguageType.FindIndex(type => type == mOptions.language_type);
            }
            if (mOptions.to != null && mOptions.to.Length != 0)
            {
                comboBox_target_text.SelectedIndex = BKBaiduFanyi.mLanguageType.FindIndex(type => type == mOptions.to);
            }
        }

        private void DoLanguageTypeMap()
        {
            this.Dispatcher.Invoke(() =>
            {
                mOptions.language_type = BKBaiduOCR.LanguageType[comboBox_src_text.SelectedIndex];
                mOptions.from = BKBaiduFanyi.mLanguageType[comboBox_src_text.SelectedIndex];
                mOptions.to = BKBaiduFanyi.mLanguageType[comboBox_target_text.SelectedIndex];
            });
        }

        private void DoCaptureOCR(bool onlyTrans = false)
        {
            DoLanguageTypeMap();
            if (mScreenCapture == null)
            {
                mScreenCapture = new BKScreenCapture((cb_data) =>
                {
                    if (mCapType == CaptureType.button)
                    {
                        ShowWnd();
                    }
                    var bmpData = (Bitmap)cb_data.captureBmp.Clone();
                    mCaptureRect = cb_data.captureRect;
                    mTextWndRect = GetWndRect();
                    do
                    {
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

                        SetSourceText("截取完成，等待OCR翻译...");
                        var ocrRequest = new BKBaiduOCR(mOptions.client_id, mOptions.client_secret);
                        ocrRequest.DoOCR((string ocrResult) =>
                        {
                            string ocrResultText = "";
                            using (JsonDocument jdocOcrResult = JsonDocument.Parse(ocrResult))
                            {
                                var rootElement = jdocOcrResult.RootElement;
                                if (!rootElement.TryGetProperty("words_result", out JsonElement wordsResult))
                                {
                                    ocrResultText = ocrResult;
                                }
                                else
                                {
                                    foreach (JsonElement words_elem in wordsResult.EnumerateArray())
                                    {
                                        ocrResultText += words_elem.GetProperty("words").GetString();
                                    }
                                    ocrResultText = ocrResultText.Replace("!", "。");
                                    ocrResultText = ocrResultText.Replace("!?", "。");
                                    ocrResultText = ocrResultText.Replace("~ト", "~");
                                    ocrResultText = ocrResultText.Replace("~?", "~");
                                }
                            }
                            SetSourceText(ocrResultText);
                            DoTextTrans();
                        }, bmpData, mOptions.language_type);
                    } while (false);

                });
            }
            if (mCapType == CaptureType.hotkey && !mTextWndRect.Size.IsEmpty && onlyTrans)
            {
                mScreenCapture.StartCapture(mCaptureRect);
            }
            else
            {
                mScreenCapture.StartCapture(false);
            }
        }

        private void DoTextTrans()
        {
            DoLanguageTypeMap();
            SetTargetText("文本翻译中...");
            var transTequest = new BKBaiduFanyi(mOptions.appid, mOptions.secretkey, "");
            transTequest.DoFanyi((string transResult) =>
            {
                string transResultText = "";
                using (JsonDocument jdocOcrResult = JsonDocument.Parse(transResult))
                {
                    do
                    {
                        var root_element = jdocOcrResult.RootElement;
                        if (!root_element.TryGetProperty("trans_result", out JsonElement trasn_result))
                        {
                            transResultText = transResult;
                            break;
                        }
                        foreach (JsonElement dstElem in trasn_result.EnumerateArray())
                        {
                            transResultText += dstElem.GetProperty("dst").GetString();
                        }
                    } while (false);
                }
                SetTargetText(transResultText);
                if (mCapType == CaptureType.hotkey)
                {
                    mTargetTextWindow.SetRect(mTextWndRect);
                    mTargetTextWindow.SetText(transResultText);
                    mTargetTextWindow.ShowWnd();
                }
            }, GetSourceText(), mOptions.from, mOptions.to);
        }

        private void Button_Click_Capture(object sender, RoutedEventArgs e)
        {
            HideWnd();
            mTargetTextWindow.HideWnd();
            mCapType = CaptureType.button;
            // 需要沉睡一小段时间，让窗口完全隐藏
            Thread.Sleep(100);
            DoCaptureOCR();
        }

        private void Button_Click_Trans(object sender, RoutedEventArgs e)
        {
            DoTextTrans();
        }

        private void Button_Click_Setting(object sender, RoutedEventArgs e)
        {
            var settingsWindow = new Settings
            {
                Owner = this
            };
            settingsWindow.ShowDialog();
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
                if (this.WindowState == WindowState.Minimized)
                {
                    this.WindowState = WindowState.Normal;
                }
                if (!this.IsVisible)
                {
                    this.Show();
                }
                this.Activate();
            } while (false);
        }

        private void NotifyIcon_Close(object sender, EventArgs e)
        {
            mNotifyIcon.Visible = false;
            mNotifyClose = true;
            this.Close();
        }

        private void NotifyIcon_Capture(object sender, EventArgs e)
        {
            mTargetTextWindow.HideWnd();
            // 需要沉睡一小段时间，让窗口完全隐藏
            Thread.Sleep(100);
            mCapType = CaptureType.hotkey;
            DoCaptureOCR();
        }

        private void NotifyIcon_Trans(object sender, EventArgs e)
        {
            mCapType = CaptureType.hotkey;
            DoCaptureOCR(true);
        }

        private void NotifyIcon_Hide(object sender, EventArgs e)
        {
            this.Hide();
            mTargetTextWindow.Hide();
        }

        private void Window_Closing(object sender, CancelEventArgs e)
        {
            if (mNotifyClose)
            {
                Settings.SaveSettings();
                mTargetTextWindow.Close();
                e.Cancel = false;
            }
            else
            {
                this.Hide();
                mTargetTextWindow.Hide();
                e.Cancel = true;
            }
        }

        public IntPtr HotKey_Hook(IntPtr hwnd, int msg, IntPtr wParam, IntPtr lParam, ref bool handled)
        {
            if (wParam.ToInt64() == (int)HotKeyId.capture)
            {
                mTargetTextWindow.HideWnd();
                // 需要沉睡一小段时间，让窗口完全隐藏
                Thread.Sleep(100);
                mCapType = CaptureType.hotkey;
                DoCaptureOCR();
            }
            else if (wParam.ToInt64() == (int)HotKeyId.trans)
            {
                mCapType = CaptureType.hotkey;
                DoCaptureOCR(true);
            }
            else if (wParam.ToInt64() == (int)HotKeyId.hide)
            {
                this.Hide();
                mTargetTextWindow.Hide();
            }
            return IntPtr.Zero;
        }

    }
}
