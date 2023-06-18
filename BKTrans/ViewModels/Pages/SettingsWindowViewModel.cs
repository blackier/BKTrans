using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static BKTrans.SettingsModel;

namespace BKTrans
{
    public partial class SettingsViewModel : ObservableObject
    {
        private readonly SettingsModel.Settings _settings;

        #region api
        public string OcrBaiduClientID
        {
            get => _settings.ocr_baidu.client_id;
            set
            {
                SetProperty(_settings.ocr_baidu.client_id, value, _settings, (s, v) => s.ocr_baidu.client_id = v);
            }
        }

        public string OcrBaiduClientSecret
        {
            get => _settings.ocr_baidu.client_secret;
            set
            {
                SetProperty(_settings.ocr_baidu.client_secret, value, _settings, (s, v) => s.ocr_baidu.client_secret = v);
            }
        }

        public string TransBaiduAppID
        {
            get => _settings.trans_baidu.appid;
            set
            {
                SetProperty(_settings.trans_baidu.appid, value, _settings, (s, v) => s.trans_baidu.appid = v);
            }
        }

        public string TransBaiduSecretKey
        {
            get => _settings.trans_baidu.secretkey;
            set
            {
                SetProperty(_settings.trans_baidu.secretkey, value, _settings, (s, v) => s.trans_baidu.secretkey = v);
            }
        }

        public string TransBaiduSalt
        {
            get => _settings.trans_baidu.salt;
            set
            {
                SetProperty(_settings.trans_baidu.salt, value, _settings, (s, v) => s.trans_baidu.salt = v);
            }
        }

        public string TransCaiyunToken
        {
            get => _settings.trans_caiyun.token;
            set
            {
                SetProperty(_settings.trans_caiyun.token, value, _settings, (s, v) => s.trans_caiyun.token = v);
            }
        }

        public string TransCaiyunReqeustID
        {
            get => _settings.trans_caiyun.request_id;
            set
            {
                SetProperty(_settings.trans_caiyun.request_id, value, _settings, (s, v) => s.trans_caiyun.request_id = v);
            }
        }

        public string TransGoogleApiKey
        {
            get => _settings.trans_google.api_key;
            set
            {
                SetProperty(_settings.trans_google.api_key, value, _settings, (s, v) => s.trans_google.api_key = v);
            }
        }

        #endregion api

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

        public List<OCRReplace> OcrReplaceItems
        {
            get => _settings.ocr_replace[_settings.ocr_replace_select].ToList();
            set
            {
                SetProperty(_settings.ocr_replace[_settings.ocr_replace_select], value, _settings, (s, v) => s.ocr_replace[_settings.ocr_replace_select] = v);
            }
        }

        #endregion ocr替换

        #region 自动翻译

        public string AutoCaptrueTransInterval
        {
            get => _settings.auto_captrue_trans_interval.ToString();
            set
            {
                int.TryParse(value, out int auto_captrue_trans_interval);
                SetProperty(_settings.auto_captrue_trans_interval, auto_captrue_trans_interval, _settings, (s, v) => s.auto_captrue_trans_interval = v);
            }
        }

        public string AutoCaptrueTransCountdown
        {
            get => _settings.auto_captrue_trans_countdown.ToString();
            set
            {
                int.TryParse(value, out int auto_captrue_trans_countdown);
                SetProperty(_settings.auto_captrue_trans_countdown, auto_captrue_trans_countdown, _settings, (s, v) => s.auto_captrue_trans_countdown = v);
            }
        }

        public string AutoCaptrueTransSimilarity
        {
            get => _settings.auto_captrue_trans_similarity.ToString();
            set
            {
                float.TryParse(value, out float auto_captrue_trans_similarity);
                SetProperty(_settings.auto_captrue_trans_similarity, auto_captrue_trans_similarity, _settings, (s, v) => s.auto_captrue_trans_similarity = v);
            }
        }

        #endregion 自动翻译
        public SettingsViewModel()
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
}
