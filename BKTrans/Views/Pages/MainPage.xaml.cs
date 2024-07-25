using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Threading;
using BKTrans.Controls;
using BKTrans.Core;
using BKTrans.ViewModels.Pages;
using Serilog;
using static BKTrans.ViewModels.Pages.MainPageViewModel;

namespace BKTrans.Views.Pages;

public partial class MainPage : INavigableView<MainPageViewModel>
{
    private RectangleF _captureRect;
    private Bitmap _captureBmp;

    private FloatCaptureRectWindow _floatTextWindow;

    private DispatcherTimer _autoCaptrueTransTimer;
    private int _autoCaptrueTransCountdown;
    private bool _autoCaptrueTransStart;

    private const string _textSpliteString = "\n+++===+++===+++\n";

    static int _autoCaptrueTransDebugNum = 0;

    private ILogger _transLogger;

    public MainPageViewModel ViewModel { get; }

    public MainPage(MainPageViewModel viewModel, FloatCaptureRectWindow floatCaptureRectWindow)
    {
        ViewModel = viewModel;
        DataContext = this;

        InitializeComponent();

        // 复制粘贴时，格式化ocr翻译的文本
        richtextbox_source_text.CommandBindings.Add(
            new CommandBinding(ApplicationCommands.Copy, richtextbox_source_text_CopyCommand)
        );
        DataObject.AddCopyingHandler(richtextbox_source_text, richtextbox_source_text_Copying);
        DataObject.AddPastingHandler(richtextbox_source_text, richtextbox_source_text_Pasting);

        // 翻译日志
        _transLogger = new LoggerConfiguration()
            .WriteTo.File("logs/trans_.log", rollingInterval: RollingInterval.Day)
            .CreateLogger();
        _transLogger.Information("logger init");

        // 自动截图翻译
        _autoCaptrueTransCountdown = ViewModel.AutoCaptrueTransCountdown;
        _autoCaptrueTransStart = false;
        _autoCaptrueTransTimer = new();
        _autoCaptrueTransTimer.Tick += _catprueTransTimer_Tick;
        _autoCaptrueTransTimer.Interval = new TimeSpan(0, 0, 0, 0, ViewModel.AutoCaptrueTransInterval);
        if (ViewModel.AutoCaptrueTransOpen)
            _autoCaptrueTransTimer.Start();

        // 设置支持语言列表
        combobox_ocr_type_CheckClick(null, null);
        combobox_trans_type_CheckClick(null, null);

        // 翻译浮窗设置
        _floatTextWindow = floatCaptureRectWindow;
        _floatTextWindow.OnButtonClick = (FloatCaptureRectWindow.ButtonType btntype, object arg) =>
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
        };
        _floatTextWindow.SetAutoTransStatus(ViewModel.AutoCaptrueTransOpen);

        // 页面加载时
        Loaded += OnLoaded;
    }

    private void OnLoaded(object sender, RoutedEventArgs e)
    {
        // 加载时重新读取一遍ocr替换
        ViewModel.OcrReplaceIsChanged = true;
    }

    public void OnSwitchTheme()
    {
        // 主题切换有bug，字体颜色只支持白色的，需要切换主题时重新设置颜色
        richtextbox_source_text.Document.Foreground = textbox_target_text.Foreground.Clone();
    }

    private void SetSourceText(string srcText, List<ReplaceTextItem> replaceText = null)
    {
        Paragraph paragraph_source_text = new Paragraph();
        if (replaceText != null && replaceText.Count > 0)
        {
            foreach (var item in replaceText)
            {
                if (string.IsNullOrEmpty(item.dst))
                    paragraph_source_text.Inlines.Add(new Run(item.src));
                else
                    paragraph_source_text.Inlines.Add(new OCRReplaceItem(item.src, item.dst));
            }
            paragraph_source_text.Inlines.Add(_textSpliteString);
        }
        paragraph_source_text.Inlines.Add(srcText);

        richtextbox_source_text.Document.Blocks.Clear();
        richtextbox_source_text.Document.Blocks.Add(paragraph_source_text);
    }

    private void SetTargetText(string targetText)
    {
        textbox_target_text.Text = targetText;
    }

    private string GetSourceText()
    {
        string text = Dispatcher.Invoke(() =>
        {
            StringBuilder sb = new StringBuilder();
            foreach (Block b in richtextbox_source_text.Document.Blocks)
            {
                if (b is Paragraph)
                {
                    foreach (Inline inline in ((Paragraph)b).Inlines)
                    {
                        if (inline is OCRReplaceItem i)
                            sb.Append(i.Text);
                        else if (inline is Run r)
                            sb.Append(r.Text);
                    }
                }
            }
            return sb.ToString();
        });
        text = text.Split(_textSpliteString)[0];
        return text;
    }

    private void ShowWindow()
    {
        App.ShowWindow();
    }

    private void HideWindow()
    {
        App.HideWindow();
        _floatTextWindow.HideWindow();
    }

    private void AutoTransSetting(bool start)
    {
        if (start)
        {
            _autoCaptrueTransTimer.Start();
            ViewModel.AutoCaptrueTransOpen = true;
        }
        else
        {
            _autoCaptrueTransTimer.Stop();
            ViewModel.AutoCaptrueTransOpen = false;
        }
        _floatTextWindow.SetAutoTransStatus(ViewModel.AutoCaptrueTransOpen);
    }

    public void CaptureTrans()
    {
        bool floatwindowVisible = _floatTextWindow.IsVisible;
        HideWindow();
        // 需要延迟下，否则界面未完全隐藏
        _ = Task.Delay(250)
            .ContinueWith(
                _ => DoCaptureOCR(false, floatwindowVisible),
                TaskScheduler.FromCurrentSynchronizationContext()
            );
    }

    public void CaptureTransLast()
    {
        DoCaptureOCR(true, true, false);
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
            if (bmpData.Height < 15 || bmpData.Width < 15 || bmpData.Height > 4096 || bmpData.Width > 4096)
            {
                SetSourceText("截取失败，最短边至少15px，最长边不超过4096px");
                break;
            }

            _captureBmp?.Dispose();
            _captureBmp = bmpData;
            SetSourceText("截取完成，等待OCR翻译...");

            TransResult ocrResult = await ViewModel.OCR(bmpData);
            if (ocrResult.ocr_result.Count == 0 || string.IsNullOrEmpty(ocrResult.ocr_result[0].result))
            {
                SetSourceText("");
                SetTargetText("");
                break;
            }
            string ocrResultText = ocrResult.ocr_result[0].result;

            SetSourceText(ocrResultText, ViewModel.OCRRepalce(ocrResultText));

            if (ViewModel.AutoTransOCRResult)
                DoTextTrans(ocrResult);
            else
                _transLogger.Information($"Only OCR:\n{BKMisc.JsonSerialize(ocrResult)}");
        } while (false);
    }

    private async void DoTextTrans(TransResult ocrResult = null)
    {
        string srctext = GetSourceText();
        if (string.IsNullOrEmpty(srctext))
            return;

        SetTargetText("文本翻译中...");

        string transResultText = "";
        TransResult transResult = await ViewModel.TransText(srctext);
        foreach (var result in transResult.trans_result)
        {
            transResultText += result.result;
            transResultText += _textSpliteString;
        }
        if (!string.IsNullOrEmpty(transResultText))
            transResultText = transResultText.Remove(transResultText.Length - _textSpliteString.Length);

        SetTargetText(transResultText);
        _floatTextWindow.SetText(transResultText);

        transResult.trans_result.Add(new TransResult.TransResultItem() { tool = "text", result = srctext });
        if (ocrResult != null)
        {
            transResult.ocr_result = ocrResult.ocr_result;
            _transLogger.Information($"OCR Trans: \n{BKMisc.JsonSerialize(transResult)}");
        }
        else
        {
            _transLogger.Information($"Only Trans: \n{BKMisc.JsonSerialize(transResult)}");
        }
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

            float similarity = newcaptruebmp.DHashCompare((Bitmap)_captureBmp.Clone());

            _autoCaptrueTransDebugNum++;
            if (similarity < ViewModel.AutoCaptrueTransSimilarity)
            {
                // 发生变化
                //newcaptruebmp.Save($"{_autoCaptrueTransDebugNum}_{similarity}_1.png", ImageFormat.Png);
                //_captureBmp.Save($"{_autoCaptrueTransDebugNum}_{similarity}_2.png", ImageFormat.Png);

                _captureBmp?.Dispose();
                _captureBmp = newcaptruebmp;
                _autoCaptrueTransStart = true;
                break;
            }
            newcaptruebmp?.Dispose();

            if (_autoCaptrueTransStart)
                Dispatcher.InvokeAsync(() => DoCaptureOCR(true, true, false));
            _autoCaptrueTransStart = false;
        } while (false);

        _autoCaptrueTransCountdown = ViewModel.AutoCaptrueTransCountdown;
    }

    private void combobox_ocr_type_CheckClick(object sender, RoutedEventArgs e)
    {
        ViewModel.UpdateLanguageTypeMap();
    }

    private void combobox_trans_type_CheckClick(object sender, RoutedEventArgs e)
    {
        ViewModel.UpdateLanguageTypeMap();
    }

    private void btn_setting_Click(object sender, RoutedEventArgs e)
    {
        App.NavigateTo(typeof(SettingsPage));
    }

    private void btn_capture_Click(object sender, RoutedEventArgs e)
    {
        CaptureTrans();
    }

    private void btn_trans_Click(object sender, RoutedEventArgs e)
    {
        DoTextTrans();
    }

    private void checkbox_auto_trans_ocr_result_Click(object sender, RoutedEventArgs e)
    {
        ViewModel.AutoTransOCRResult = !ViewModel.AutoTransOCRResult;
    }

    private string GetInlineText(RichTextBox richTextBox)
    {
        StringBuilder sb = new StringBuilder();
        foreach (Block b in richTextBox.Document.Blocks)
        {
            if (b is Paragraph p)
            {
                foreach (Inline inline in p.Inlines)
                {
                    if (inline is OCRReplaceItem i)
                    {
                        if (richTextBox.Selection.Contains(i.ContentStart))
                            sb.Append(i.Text);
                    }
                    else if (inline is Run r)
                    {
                        if (richTextBox.Selection.Contains(r.ContentStart))
                        {
                            if (richTextBox.Selection.Contains(r.ContentEnd))
                            {
                                sb.Append(r.Text);
                            }
                            else
                            {
                                sb.Append(new TextRange(r.ContentStart, richTextBox.Selection.End).Text);
                                break;
                            }
                        }
                        else if (
                            r.ContentStart.CompareTo(richTextBox.Selection.Start) < 0
                            && r.ContentEnd.CompareTo(richTextBox.Selection.Start) > 0
                        )
                        {
                            if (r.ContentEnd.CompareTo(richTextBox.Selection.End) > 0)
                            {
                                sb.Append(new TextRange(richTextBox.Selection.Start, richTextBox.Selection.End).Text);
                                break;
                            }
                            else
                            {
                                sb.Append(new TextRange(richTextBox.Selection.Start, r.ContentEnd).Text);
                            }
                        }
                    }
                }
            }
        }
        return sb.ToString();
    }

    private void richtextbox_source_text_CopyCommand(object sender, ExecutedRoutedEventArgs e)
    {
        copyText = GetInlineText(richtextbox_source_text);
        Clipboard.SetText(copyText);
    }

    private string copyText = "";
    private bool dropCopy = false;

    private void richtextbox_source_text_Copying(object sender, DataObjectCopyingEventArgs e)
    {
        dropCopy = e.IsDragDrop;
        copyText = GetInlineText(richtextbox_source_text);
        Clipboard.SetText(copyText);
    }

    private void richtextbox_source_text_Pasting(object sender, DataObjectPastingEventArgs e)
    {
        string text;

        if (!string.IsNullOrEmpty(copyText) && dropCopy)
        {
            text = copyText;
            copyText = "";
            dropCopy = false;
        }
        else
        {
            text = e.DataObject.GetData(DataFormats.Text) as string ?? string.Empty;
        }

        e.DataObject = new DataObject(DataFormats.Text, text);
    }

    private void richtextbox_source_text_MenuItem_Click(object sender, RoutedEventArgs e)
    {
        string selectText = GetInlineText(richtextbox_source_text);
        textbox_ocr_replace_src.Text = selectText;
        textbox_ocr_replace_dst.Text = "";
        flyout_add_ocr_replace.IsOpen = true;
    }

    private void richtextbox_source_text_ContextMenuOpening(object sender, ContextMenuEventArgs e)
    {
        string selectText = GetInlineText(richtextbox_source_text);
        List<string> similarChars = ViewModel.GetSimilarChars(selectText);
        menuitem_similar_char.Items.Clear();
        if (similarChars is not null && similarChars.Count > 0)
        {
            foreach (string schar in similarChars)
            {
                var i = new MenuItem() { Header = schar };
                i.Click += (object sender, RoutedEventArgs e) =>
                {
                    Clipboard.SetText(schar);
                    richtextbox_source_text.Paste();
                };
                menuitem_similar_char.Items.Add(i);
            }
            menuitem_similar_char.IsEnabled = true;
        }
        else
        {
            menuitem_similar_char.IsEnabled = false;
        }
    }

    private void btn_add_ocr_replace_Click(object sender, RoutedEventArgs e)
    {
        ViewModel.AddOcrReplaceItem(textbox_ocr_replace_src.Text, textbox_ocr_replace_dst.Text);
        flyout_add_ocr_replace.IsOpen = false;
    }
}
