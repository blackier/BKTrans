using BKAssembly;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.IO;
using System.Text.Json;
using System.Threading;
using System.Windows;
using System.Windows.Forms;
using System.Windows.Interop;

namespace BKTrans
{
    public partial class MainWindow : Window
    {
        #region 成员变量定义

        private string settings_path_;

        private string api_key_;
        private string secret_key_;
        private string language_type_;

        private string app_id_;
        private string app_secret_key_;
        private string from_type_;
        private string to_type_;

        private BKScreenCapture screen_capture_;

        private NotifyIcon notify_icon_;
        private bool notify_close_;

        private Rectangle capture_rect_;
        private Rectangle text_wnd_rect_;

        private BKTextWindow target_text_window_;

        // 热键详见：Win32API: RegisterHotKey function
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
        CaptureType cap_type;
        #endregion 成员变量定义

        #region 公有成员函数定义

        public MainWindow()
        {
            settings_path_ = @".\settings.json";
            target_text_window_ = new BKTextWindow();
            cap_type = CaptureType.button;

            InitializeComponent();
            // 设置托盘图标
            notify_close_ = false;
            var notify_icon_cms = new ContextMenuStrip();
            notify_icon_cms.Items.Add(new ToolStripMenuItem("打开", null, new EventHandler(NotifyIcon_Open)));
            notify_icon_cms.Items.Add(new ToolStripMenuItem("截取", null, new EventHandler(NotifyIcon_Capture)));
            notify_icon_cms.Items.Add(new ToolStripMenuItem("翻译", null, new EventHandler(NotifyIcon_Trans)));
            notify_icon_cms.Items.Add("-");
            notify_icon_cms.Items.Add(new ToolStripMenuItem("隐藏", null, new EventHandler(NotifyIcon_Hide)));
            notify_icon_cms.Items.Add("-");
            notify_icon_cms.Items.Add(new ToolStripMenuItem("退出", null, new EventHandler(NotifyIcon_Close)));
            notify_icon_ = new NotifyIcon
            {
                Visible = true,
                Icon = System.Drawing.Icon.ExtractAssociatedIcon(System.Windows.Forms.Application.ExecutablePath),
                ContextMenuStrip = notify_icon_cms
            };
            notify_icon_.Click += new EventHandler(NotifyIcon_Open);

            // 关联关闭函数，设置为最小化到托盘
            this.Closing += new CancelEventHandler(Window_Closing);

            // 设置
            GetSettings();
        }

        #endregion 公有成员函数定义

        #region 保护成员函数定义

        protected override void OnSourceInitialized(EventArgs e)
        {
            // 热键注册
            base.OnSourceInitialized(e);
            IntPtr handle = new WindowInteropHelper(this).Handle;
            bool is_succeess = false;
            is_succeess = BKHotKey.Register(handle, (int)HotKeyId.capture, (uint)(BKHotKey.Modifiers.norepeat), (uint)Keys.F2);
            is_succeess = BKHotKey.Register(handle, (int)HotKeyId.trans, (uint)(BKHotKey.Modifiers.norepeat), (uint)Keys.NumPad0);
            is_succeess = BKHotKey.Register(handle, (int)HotKeyId.hide, (uint)(BKHotKey.Modifiers.shift | BKHotKey.Modifiers.alt | BKHotKey.Modifiers.norepeat), (uint)Keys.C);
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

        #endregion 保护成员函数定义

        #region 私有成员函数定义

        private void GetSettings()
        {
            do
            {
                if (!File.Exists(settings_path_))
                {
                    using (var stream = File.Create(settings_path_))
                    {
                        var json_options = new JsonWriterOptions
                        {
                            Indented = true
                        };
                        using (var json_writer = new Utf8JsonWriter(stream, json_options))
                        {
                            json_writer.WriteStartObject();
                            json_writer.WriteStartObject("OCR");
                            json_writer.WriteString("api_key", "");
                            json_writer.WriteString("secret_key", "");
                            json_writer.WriteString("language_type", "");
                            json_writer.WriteEndObject();
                            json_writer.WriteStartObject("Trans");
                            json_writer.WriteString("app_id", "");
                            json_writer.WriteString("app_secret_key", "");
                            json_writer.WriteString("from_type", "");
                            json_writer.WriteString("to_type", "");
                            json_writer.WriteEndObject();
                            json_writer.WriteEndObject();
                        }
                    }
                    SetSourceText("settings.json不存在，已自动生成，前往程序目录下配置，下次启动生效。");
                    break;
                }
                using (var steam = File.OpenRead(settings_path_))
                {
                    using (var jdoc_settings = JsonDocument.Parse(steam))
                    {
                        var root_element = jdoc_settings.RootElement;
                        if (!root_element.TryGetProperty("OCR", out var ocr_settings) || !root_element.TryGetProperty("Trans", out var trans_settings))
                        {
                            SetSourceText("settings.json文件出错，请将文件删除后重启应程序。");
                        }
                        else
                        {
                            try
                            {
                                api_key_ = ocr_settings.GetProperty("api_key").GetString();
                                secret_key_ = ocr_settings.GetProperty("secret_key").GetString();
                                language_type_ = ocr_settings.GetProperty("language_type").GetString();

                                app_id_ = trans_settings.GetProperty("app_id").GetString();
                                app_secret_key_ = trans_settings.GetProperty("app_secret_key").GetString();
                                from_type_ = trans_settings.GetProperty("from_type").GetString();
                                to_type_ = trans_settings.GetProperty("to_type").GetString();
                            }
                            catch (KeyNotFoundException e)
                            {
                                SetSourceText(String.Format("{0}", e));
                            }
                        }
                    }
                }
            } while (false);

        }

        private void SetSettings()
        {

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
            return new Rectangle { X = (int)(capture_rect_.X * p), Y = (int)(capture_rect_.Y * p), Width = (int)(capture_rect_.Width * p), Height = (int)(capture_rect_.Height * p) };
        }

        private void DoCaptureOCR(bool only_trans = false)
        {
            if (screen_capture_ == null)
            {
                screen_capture_ = new BKScreenCapture((cb_data) =>
                {
                    if (cap_type == CaptureType.button)
                    {
                        ShowWnd();
                    }
                    var bmp_data = (Bitmap)cb_data.capture_bmp.Clone();
                    capture_rect_ = cb_data.capture_rect;
                    text_wnd_rect_ = GetWndRect();
                    do
                    {
                        if (bmp_data.Height < 15 || bmp_data.Width < 15)
                        {
                            SetSourceText("截取完成，截取最短边要求至少15px...");
                            break;
                        }
                        else if (bmp_data.Height > 4096 || bmp_data.Width > 4096)
                        {
                            SetSourceText("截取完成，截取最长边要求不超过4096px...");
                            break;
                        }

                        SetSourceText("截取完成，等待OCR翻译...");
                        var ocr_request = new BKBaiduOCR(api_key_, secret_key_);
                        ocr_request.DoOCR((string ocr_result) =>
                        {
                            string ocr_result_text = "";
                            using (JsonDocument jdoc_ocr_result = JsonDocument.Parse(ocr_result))
                            {
                                var root_element = jdoc_ocr_result.RootElement;
                                if (!root_element.TryGetProperty("words_result", out JsonElement words_result))
                                {
                                    ocr_result_text = ocr_result;
                                }
                                else
                                {
                                    foreach (JsonElement words_elem in words_result.EnumerateArray())
                                    {
                                        ocr_result_text += words_elem.GetProperty("words").GetString();
                                    }
                                    ocr_result_text = ocr_result_text.Replace("!", "。");
                                    ocr_result_text = ocr_result_text.Replace("!?", "。");
                                    ocr_result_text = ocr_result_text.Replace("~ト", "~");
                                    ocr_result_text = ocr_result_text.Replace("~?", "~");
                                }
                            }
                            SetSourceText(ocr_result_text);
                            DoTextTrans();
                        }, bmp_data, language_type_);
                    } while (false);

                });
            }
            if (cap_type == CaptureType.hotkey && !text_wnd_rect_.Size.IsEmpty && only_trans)
            {
                screen_capture_.StartCapture(capture_rect_);
            }
            else
            {
                screen_capture_.StartCapture(false);
            }
        }

        private void DoTextTrans()
        {
            SetTargetText("文本翻译中...");
            var trans_request = new BKBaiduFanyi(app_id_, app_secret_key_, "");
            trans_request.DoFanyi((string trans_result) =>
            {
                string trans_result_text = "";
                using (JsonDocument jdoc_ocr_result = JsonDocument.Parse(trans_result))
                {
                    do
                    {
                        var root_element = jdoc_ocr_result.RootElement;
                        if (!root_element.TryGetProperty("trans_result", out JsonElement trasn_result))
                        {
                            trans_result_text = trans_result;
                            break;
                        }
                        foreach (JsonElement dst_elem in trasn_result.EnumerateArray())
                        {
                            trans_result_text += dst_elem.GetProperty("dst").GetString();
                        }
                    } while (false);
                }
                SetTargetText(trans_result_text);
                if (cap_type == CaptureType.hotkey)
                {
                    target_text_window_.SetRect(text_wnd_rect_);
                    target_text_window_.SetText(trans_result_text);
                    target_text_window_.ShowWnd();
                }
            }, GetSourceText(), from_type_, to_type_);
        }

        #region 界面按钮事件处理
        private void Button_Click_Capture(object sender, RoutedEventArgs e)
        {
            HideWnd();
            target_text_window_.HideWnd();
            cap_type = CaptureType.button;
            // 需要沉睡一小段时间，让窗口完全隐藏
            Thread.Sleep(170);
            DoCaptureOCR();
        }

        private void Button_Click_Trans(object sender, RoutedEventArgs e)
        {
            DoTextTrans();
        }

        private void Button_Click_Setting(object sender, RoutedEventArgs e)
        {

        }
        #endregion 界面按钮事件处理

        #region 系统托盘按钮事件处理
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
            notify_icon_.Visible = false;
            notify_close_ = true;
            this.Close();
        }

        private void NotifyIcon_Capture(object sender, EventArgs e)
        {
            target_text_window_.HideWnd();
            // 需要沉睡一小段时间，让窗口完全隐藏
            Thread.Sleep(100);
            cap_type = CaptureType.hotkey;
            DoCaptureOCR();
        }

        private void NotifyIcon_Trans(object sender, EventArgs e)
        {
            cap_type = CaptureType.hotkey;
            DoCaptureOCR(true);
        }

        private void NotifyIcon_Hide(object sender, EventArgs e)
        {
            this.Hide();
            target_text_window_.Hide();
        }

        #endregion 系统托盘按钮事件处理

        #region 窗口事件处理
        private void Window_Closing(object sender, CancelEventArgs e)
        {
            if (notify_close_)
            {
                target_text_window_.Close();
                e.Cancel = false;
            }
            else
            {
                this.Hide();
                target_text_window_.Hide();
                e.Cancel = true;
            }
        }

        public IntPtr HotKey_Hook(IntPtr hwnd, int msg, IntPtr wParam, IntPtr lParam, ref bool handled)
        {
            if (wParam.ToInt64() == (int)HotKeyId.capture)
            {
                target_text_window_.HideWnd();
                // 需要沉睡一小段时间，让窗口完全隐藏
                Thread.Sleep(100);
                cap_type = CaptureType.hotkey;
                DoCaptureOCR();
            }
            else if (wParam.ToInt64() == (int)HotKeyId.trans)
            {
                cap_type = CaptureType.hotkey;
                DoCaptureOCR(true);
            }
            else if (wParam.ToInt64() == (int)HotKeyId.hide)
            {
                this.Hide();
                target_text_window_.Hide();
            }
            return IntPtr.Zero;
        }

        #endregion 窗口事件处理

        #endregion 私成员函数定义


    }
}
