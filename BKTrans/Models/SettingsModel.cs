using BKTrans.Misc;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Text.Encodings.Web;
using System.Text.Unicode;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Input;

namespace BKTrans.Models;

public class SettingsModel
{
    [Serializable]
    public class CheckBoxItem
    {
        public bool IsChecked { get; set; }
        public string Text { get; set; }
        public CheckBoxItem()
        {
            IsChecked = false;
            Text = "";
        }
    }

    [Serializable]
    public class OCRReplace
    {
        public string replace_src { get; set; }
        public string replace_dst { get; set; }
        public OCRReplace()
        {
            replace_src = "";
            replace_dst = "";
        }
    }

    [Serializable]
    public class Settings
    {
        // 翻译源
        public List<CheckBoxItem> ocr_types { get; set; }
        public List<CheckBoxItem> trans_types { get; set; }
        // ocr参数
        public BKOCRBaidu.SettingBaiduOCR ocr_baidu { get; set; }
        public BKOCRMicrosoft.SettingMiscrosoftOCR ocr_microsoft { get; set; }
        // 翻译参数
        public BKTransBaidu.SettingBaiduTrans trans_baidu { get; set; }
        public BKTransCaiyun.SettingCaiyunTrans trans_caiyun { get; set; }
        public BKTransGoogle.SettingGoogleTrans trans_google { get; set; }

        // 截图翻译事件间隔
        public int auto_captrue_trans_interval { get; set; }
        public int auto_captrue_trans_countdown { get; set; }
        public bool auto_captrue_trans_open { get; set; }
        public float auto_captrue_trans_similarity { get; set; }

        // ocr结果是否自动翻译
        public bool auto_trans_ocr_result { get; set; }

        // ocr翻译替换
        public string ocr_replace_select { get; set; }
        public Dictionary<string, List<OCRReplace>> ocr_replace { get; set; }
        // 相似字
        public List<List<string>> similar_chars { get; set; }

        public Settings()
        {
            ocr_types = new();
            trans_types = new();

            ocr_baidu = new();
            ocr_microsoft = new();
            trans_baidu = new();
            trans_caiyun = new();
            trans_google = new();

            auto_captrue_trans_interval = 150;
            auto_captrue_trans_countdown = 5;
            auto_captrue_trans_open = false;
            auto_captrue_trans_similarity = 0.9f;

            auto_trans_ocr_result = true;

            ocr_replace_select = "";
            ocr_replace = new() { { "", new() } };

            similar_chars = new();
        }

        public void UpdateTransSetting(string trans_type, string from, string to)
        {
            GetTransSetting(trans_type).from = from;
            GetTransSetting(trans_type).to = to;
        }

        public BKTransSetting GetTransSetting(string tans_type)
        {
            if (tans_type == "baidu")
                return trans_baidu;

            if (tans_type == "caiyun")
                return trans_caiyun;

            if (tans_type == "google")
                return trans_google;

            return new();
        }

        public BKOCRSetting GetOCRSetting(string ocr_type)
        {
            if (ocr_type == "baidu")
                return ocr_baidu;

            if (ocr_type == "microsoft")
                return ocr_microsoft;

            return new();
        }
    }
    private static Settings _settings;

    private static string _settingsFilePath;

    public static Settings LoadSettings()
    {
        if (_settings == null)
        {
            _settings = new Settings();
            if (_settingsFilePath == null)
            {
                _settingsFilePath = Path.Combine(Directory.GetCurrentDirectory(), "BKTransSettings.json");
            }
            try
            {
                _settings = BKMisc.JsonDeserialize<Settings>(BKMisc.LoadTextFile(_settingsFilePath));
            }
            catch
            {
            }
            // 因为可能会修改支持的翻译类型
            // 做下比较，先做差值再合并
            List<string> newOcrType = _settings.ocr_types.Select(type => type.Text).Intersect(BKTransMap.OCRType).Union(BKTransMap.OCRType).ToList();
            List<string> newTransType = _settings.trans_types.Select(type => type.Text).Intersect(BKTransMap.TransType).Union(BKTransMap.TransType).ToList();

            _settings.ocr_types = newOcrType.Select(type => new CheckBoxItem() { IsChecked = _settings.ocr_types.Exists(t => t.Text == type && t.IsChecked), Text = type }).ToList();
            _settings.trans_types = newTransType.Select(type => new CheckBoxItem() { IsChecked = _settings.trans_types.Exists(t => t.Text == type && t.IsChecked), Text = type }).ToList();

            // 相似字是为了ocr识别一些字形出问题时，用于替换掉，内置日语字母的替换
            if (_settings.similar_chars.Count <= 0)
            {
                _settings.similar_chars = DefaultSimilarChars();
            }
        }
        return _settings;
    }
    public static void SaveSettings()
    {
        BKMisc.SaveTextFile(_settingsFilePath, BKMisc.JsonSerialize(_settings));
    }

    private static List<List<string>> DefaultSimilarChars()
    {
        return new() {
            new(){ "ア", "イ", "ウ", "エ", "オ", "ァ", "ィ", "ゥ", "ェ", "ォ"},
            new(){ "か", "カ", "が", "ガ", "ヵ" },
            new(){ "き", "キ", "ぎ", "ギ"},
            new(){ "く", "ク", "ぐ", "グ"},
            new(){ "け", "ケ", "げ", "ゲ", "ヶ"},
            new(){ "こ", "コ", "ご", "ゴ"},
            new(){ "さ", "サ", "ざ", "ザ"},
            new(){ "し", "シ", "じ", "ジ"},
            new(){ "す", "ス", "ず", "ズ"},
            new(){ "せ", "セ", "ぜ", "ゼ"},
            new(){ "そ", "ソ", "ぞ", "ゾ"},
            new(){ "た", "タ", "だ", "ダ"},
            new(){ "ち", "チ", "ぢ", "ヂ"},
            new(){ "つ", "ツ", "づ", "ヅ", "っ"},
            new(){ "て", "テ", "で", "デ"},
            new(){ "と", "ト", "ど", "ド"},
            new(){ "は", "ハ", "ば", "バ", "ぱ", "パ"},
            new(){ "ひ", "ヒ", "び", "ビ", "ぴ", "ピ"},
            new(){ "ふ", "フ", "ぶ", "ブ", "ぷ", "プ"},
            new(){ "へ", "べ", "ぺ"},
            new(){ "ほ", "ホ", "ぼ", "ボ", "ぽ", "ポ"},
            new(){ "や", "ヤ", "ゃ", "ャ" },
            new(){ "ゆ", "ユ", "ゅ", "ュ" },
            new(){ "よ", "ヨ", "ょ", "ョ" },
            new(){ "ワ", "ヮ" }
        };
    }

    public SettingsModel() { }
}
