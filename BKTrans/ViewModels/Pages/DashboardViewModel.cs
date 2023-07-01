﻿using BKTrans.Misc;
using BKTrans.Models;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Documents;
using System.Windows.Forms.VisualStyles;
using static BKTrans.ViewModels.Pages.DashboardViewModel.TransResult;

namespace BKTrans.ViewModels.Pages;

public partial class DashboardViewModel : ObservableObject
{
    private readonly SettingsModel.Settings _settings;

    #region 翻译属性
    [ObservableProperty]
    private List<string> _fromTypes;
    [ObservableProperty]
    private string _fromTypesSelectedItem;
    [ObservableProperty]
    private List<string> _toTypes;
    [ObservableProperty]
    private string _toTypesSelectedItem;
    #endregion 翻译属性

    #region ocr替换
    public List<string> OcrReplace
    {
        get => _settings.ocr_replace.Keys.ToList();
        set { }
    }
    public string OcrReplaceSelectedItem
    {
        get => _settings.ocr_replace_select;
        set
        {
            SetProperty(_settings.ocr_replace_select, value, _settings, (s, v) => s.ocr_replace_select = v);
        }
    }
    public bool OcrReplaceIsChanged
    {
        set
        {
            OnPropertyChanged(nameof(OcrReplace));
            OnPropertyChanged(nameof(OcrReplaceSelectedItem));
        }
    }

    #endregion ocr替换

    #region 类型选择
    /// <summary>
    /// 支持的ocr类型
    /// </summary>
    public BindingList<SettingsModel.CheckBoxItem> OcrTypes
    {
        get => new(_settings.ocr_types);
        set
        {
            SetProperty(_settings.ocr_types, value.ToList(), _settings, (s, v) => s.ocr_types = v);
            OnPropertyChanged(nameof(OcrTypesSelectedText));
        }
    }
    public string OcrTypesSelectedText
    {
        get
        {
            string selectType = "";
            foreach (var type in OcrTypes)
            {
                if (type.IsChecked)
                {
                    selectType += type.Text.Substring(0, 1) + ";";
                }
            }
            return selectType.ToUpper();
        }
        set { }
    }
    /// <summary>
    /// 支持的翻译类型
    /// </summary>
    public BindingList<SettingsModel.CheckBoxItem> TransTypes
    {
        get => new(_settings.trans_types);
        set
        {
            SetProperty(_settings.trans_types, value.ToList(), _settings, (s, v) => s.trans_types = v);
            OnPropertyChanged(nameof(TransTypesSelectedText));
        }
    }
    public string TransTypesSelectedText
    {
        get
        {
            string selectType = "";
            foreach (var type in TransTypes)
            {
                if (type.IsChecked)
                {
                    selectType += type.Text.Substring(0, 1) + ";";
                }
            }
            return selectType.ToUpper();
        }
        set { }
    }
    #endregion 类型选择

    #region 自动截图翻译配置
    public bool AutoCaptrueTransOpen
    {
        get => _settings.auto_captrue_trans_open;
        set
        {
            SetProperty(_settings.auto_captrue_trans_open, value, _settings, (s, v) => s.auto_captrue_trans_open = v);
        }
    }
    public int AutoCaptrueTransCountdown
    {
        get => _settings.auto_captrue_trans_countdown;
        set
        {
            SetProperty(_settings.auto_captrue_trans_countdown, value, _settings, (s, v) => s.auto_captrue_trans_countdown = v);
        }
    }
    public int AutoCaptrueTransInterval
    {
        get => _settings.auto_captrue_trans_interval;
        set
        {
            SetProperty(_settings.auto_captrue_trans_interval, value, _settings, (s, v) => s.auto_captrue_trans_interval = v);
        }
    }
    public float AutoCaptrueTransSimilarity
    {
        get => _settings.auto_captrue_trans_similarity;
        set
        {
            SetProperty(_settings.auto_captrue_trans_similarity, value, _settings, (s, v) => s.auto_captrue_trans_similarity = v);
        }
    }
    #endregion

    // ocr结果是否自动翻译
    public bool AutoTransOCRResult
    {
        get => _settings.auto_trans_ocr_result;
        set
        {
            SetProperty(_settings.auto_trans_ocr_result, value, _settings, (s, v) => s.auto_trans_ocr_result = v);
        }
    }

    // 翻译记录
    public record TransResult
    {
        public record TransResultItem
        {
            public string tool { get; set; }
            public string result { get; set; }
        }
        public List<TransResultItem> ocr_result { get; set; }
        public List<TransResultItem> trans_result { get; set; }
    };

    public DashboardViewModel()
    {
        _settings = SettingsModel.LoadSettings();
        UpdateLanguageTypeMap();
    }

    public void UpdateLanguageTypeMap()
    {
        // 计算ocr和文本翻译交集
        // ocr只支持选择一个
        // 文本翻译支持选择好几个
        int ocrSelect = 0;
        foreach (var type in _settings.ocr_types)
        {
            if (type.IsChecked)
                break;
            ocrSelect++;
        }
        if (ocrSelect >= _settings.ocr_types.Count)
        {
            ocrSelect = 0;
            _settings.ocr_types[0].IsChecked = true;
        }

        string ocrType = BKTransMap.OCRType[ocrSelect];
        List<string> ocrItemsSource = BKTransMap.CreateBKOCRClient(ocrType).GetLangType();

        List<string> transItemsSource = new();
        foreach (var type in _settings.trans_types)
        {
            if (type.IsChecked)
            {
                if (transItemsSource.Count == 0)
                    transItemsSource = BKTransMap.CreateBKTransClient(type.Text).GetLangType();
                else
                    transItemsSource = transItemsSource.Intersect(BKTransMap.CreateBKTransClient(type.Text).GetLangType()).ToList();
            }
        }
        if (transItemsSource.Count == 0)
        {
            transItemsSource = BKTransMap.CreateBKTransClient(BKTransMap.TransType[0]).GetLangType();
            _settings.trans_types[0].IsChecked = true;
        }
        transItemsSource = transItemsSource.Intersect(ocrItemsSource).ToList();

        FromTypes = transItemsSource;
        ToTypes = transItemsSource;

        // 如果文本翻译选了好几个
        // 那么统一设置成和第一个相同的
        string from = "";
        string to = "";
        foreach (var type in _settings.trans_types)
        {
            if (type.IsChecked)
            {
                if (string.IsNullOrEmpty(from) || string.IsNullOrEmpty(to))
                {
                    from = _settings.GetTransSetting(type.Text).from;
                    to = _settings.GetTransSetting(type.Text).to;
                }
                else
                {
                    _settings.UpdateTransSetting(type.Text, from, to);
                }
            }
        }
        FromTypesSelectedItem = from;
        ToTypesSelectedItem = to;

        OcrTypes = new(_settings.ocr_types);
        TransTypes = new(_settings.trans_types);
    }

    public async Task<TransResult> OCR(Bitmap img)
    {
        TransResultItem ocr_result_item = new();
        BKOCRBase ocrClient = null;
        BKSetting ocrSetting = null;
        foreach (var type in OcrTypes)
        {
            if (type.IsChecked)
            {
                _settings.GetOCRSetting(type.Text).language = FromTypesSelectedItem;

                ocr_result_item.tool = type.Text;
                ocrClient = BKTransMap.CreateBKOCRClient(type.Text);
                ocrSetting = _settings.GetOCRSetting(type.Text);
                break;
            }
        }

        string ocrResult = "";
        await Task.Run(() => ocrClient.OCR(ocrSetting, img, out ocrResult));
        try
        {
            ocr_result_item.result = ocrClient.ParseResult(ocrResult);
        }
        catch
        {
            ocr_result_item.result = "OCR翻译失败，打开程序界面查看原因。";

        }
        return new TransResult() { ocr_result = new() { ocr_result_item } };
    }

    public record ReplaceTextItem
    {
        public string src { get; set; }
        public string dst { get; set; }
    }
    public List<ReplaceTextItem> OCRRepalce(string srcText)
    {
        // 量少，简单的替换
        // key是源文本，value是替换后的文本
        List<ReplaceTextItem> replaceTextArr = new() { new() { src = srcText } };
        foreach (var replaceMap in _settings.ocr_replace[_settings.ocr_replace_select])
        {
            if (string.IsNullOrEmpty(replaceMap.replace_src))
                continue;

            List<ReplaceTextItem> replaceTextArrTemp = new();
            foreach (var replaceText in replaceTextArr)
            {
                // 已经替换过的跳过
                if (!string.IsNullOrEmpty(replaceText.dst))
                {
                    replaceTextArrTemp.Add(replaceText);
                    continue;
                }

                string[] splitArr = replaceText.src.Split(replaceMap.replace_src);
                if (splitArr.Length == 1)
                {
                    replaceTextArrTemp.Add(replaceText);
                    continue;
                }
                foreach (var t in splitArr)
                {
                    // 插入替换的文本
                    replaceTextArrTemp.Add(new() { src = t });
                    replaceTextArrTemp.Add(new() { src = replaceMap.replace_src, dst = replaceMap.replace_dst });
                }
                // 删掉多余的一个插入
                replaceTextArrTemp.RemoveAt(replaceTextArrTemp.Count - 1);

            }
            replaceTextArr = replaceTextArrTemp;
        }

        return replaceTextArr;
    }

    public async Task<TransResult> TransText(string text)
    {
        List<Task> transTasks = new();
        TransResult result = new TransResult() { trans_result = new() };

        foreach (var type in TransTypes)
        {
            if (type.IsChecked)
            {
                TransResultItem trans_result_item = new();
                trans_result_item.tool = type.Text;
                _settings.UpdateTransSetting(type.Text, FromTypesSelectedItem, ToTypesSelectedItem);
                transTasks.Add(Task.Run(() =>
                {
                    trans_result_item.result = BKTransMap.CreateBKTransClient(type.Text).Trans(_settings.GetTransSetting(type.Text), text);
                    result.trans_result.Add(trans_result_item);
                }));
            }
        }

        var transtasks = Task.WhenAll(transTasks);
        await Task.Run(() => transtasks.Wait());
        return result;
    }

    public void AddOcrReplaceItem(string replace_src, string replace_dst)
    {
        if (string.IsNullOrEmpty(replace_src))
            return;

        _settings.ocr_replace[_settings.ocr_replace_select].Add(new() { replace_src = replace_src, replace_dst = replace_dst });
    }
}
