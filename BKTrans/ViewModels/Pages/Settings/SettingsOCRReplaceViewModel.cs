using BKTrans.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BKTrans.ViewModels.Pages.Settings;

public partial class SettingsOCRReplaceViewModel : ObservableObject
{
    private readonly SettingsModel.Settings _settings;


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
            OnPropertyChanged(nameof(OcrReplaceItems));
        }
    }

    public List<SettingsModel.OCRReplace> OcrReplaceItems
    {
        get => _settings.ocr_replace[_settings.ocr_replace_select].ToList();
        set
        {
            SetProperty(_settings.ocr_replace[_settings.ocr_replace_select], value, _settings, (s, v) => s.ocr_replace[_settings.ocr_replace_select] = v);
        }
    }

    #endregion ocr替换

    public SettingsOCRReplaceViewModel()
    {
        _settings = SettingsModel.LoadSettings();

    }

    public void DeleteOcrSeletedItem()
    {
        if (!_settings.ocr_replace.ContainsKey(_settings.ocr_replace_select))
            return;
        // 删除旧的
        _settings.ocr_replace.Remove(_settings.ocr_replace_select);
        // 选择第一个
        if (_settings.ocr_replace.Count == 0)
        {
            _settings.ocr_replace_select = "";
            _settings.ocr_replace = new() { { "", new() } };
        }
        else
        {
            _settings.ocr_replace_select = _settings.ocr_replace.ElementAt(0).Key;
        }

        OnPropertyChanged(nameof(OcrReplaceSelectedItem));
        OnPropertyChanged(nameof(OcrReplace));
    }

    public void NewOcrReplace(string name)
    {
        if (string.IsNullOrEmpty(name))
            return;

        _settings.ocr_replace[name] = new();
        _settings.ocr_replace_select = name;

        OnPropertyChanged(nameof(OcrReplace));
        OnPropertyChanged(nameof(OcrReplaceSelectedItem));
    }
}
