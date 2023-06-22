using BKTrans.Misc;
using BKTrans.ViewModels.Pages;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.Linq;
using System.Reflection.Metadata;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Forms;
using System.Windows.Interop;
using System.Windows.Media;
using System.Windows.Threading;
using Wpf.Ui.Controls.Navigation;

namespace BKTrans.Views.Pages;

public partial class DashboardPage : INavigableView<DashboardViewModel>
{
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

    private DashboardViewModel _viewModel;

    public DashboardViewModel ViewModel { get { return _viewModel; } }

    public DashboardPage(DashboardViewModel viewModel, FloatCaptureRectWindow floatCaptureRectWindow)
    {
        _viewModel = viewModel;
        DataContext = _viewModel;

        InitializeComponent();

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

        // 翻译浮窗设置
        _floatTextWindow = floatCaptureRectWindow;
        _floatTextWindow.RegisterCallback((FloatCaptureRectWindow.ButtonType btntype, object arg) =>
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
            _viewModel.AutoCaptrueTransOpen = true;
        }
        else
        {
            _autoCaptrueTransTimer.Stop();
            _viewModel.AutoCaptrueTransOpen = false;
        }
        _floatTextWindow.SetAutoTransStatus(_viewModel.AutoCaptrueTransOpen);
    }

    public void CaptureTrans()
    {
        bool floatwindowVisible = _floatTextWindow.IsVisible;
        HideWindow();
        _ = Task.Delay(250).ContinueWith(t => { Dispatcher.InvokeAsync(() => DoCaptureOCR(false, floatwindowVisible)); });
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

            if(_viewModel.AutoTransOCRResult)
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

    #region 用户控件
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
        App.NavigateTo(typeof(SettingsPage));

        _viewModel.OcrReplaceIsChanged = true;
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
        _viewModel.AutoTransOCRResult = !_viewModel.AutoTransOCRResult;
    }

    #endregion 用户控件

    #endregion 事件处理
}
