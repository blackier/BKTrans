using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using BKTrans.Models;

namespace BKTrans.ViewModels.Pages.Settings;

public partial class SettingsTransViewModel : ObservableObject
{
    private readonly SettingsModel.Settings _settings;

    public string OcrBaiduClientID
    {
        get => _settings.ocr_baidu.client_id;
        set { SetProperty(_settings.ocr_baidu.client_id, value, _settings, (s, v) => s.ocr_baidu.client_id = v); }
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
        set { SetProperty(_settings.trans_baidu.appid, value, _settings, (s, v) => s.trans_baidu.appid = v); }
    }

    public string TransBaiduSecretKey
    {
        get => _settings.trans_baidu.secretkey;
        set { SetProperty(_settings.trans_baidu.secretkey, value, _settings, (s, v) => s.trans_baidu.secretkey = v); }
    }

    public string TransBaiduSalt
    {
        get => _settings.trans_baidu.salt;
        set { SetProperty(_settings.trans_baidu.salt, value, _settings, (s, v) => s.trans_baidu.salt = v); }
    }

    public string TransCaiyunToken
    {
        get => _settings.trans_caiyun.token;
        set { SetProperty(_settings.trans_caiyun.token, value, _settings, (s, v) => s.trans_caiyun.token = v); }
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
        set { SetProperty(_settings.trans_google.api_key, value, _settings, (s, v) => s.trans_google.api_key = v); }
    }

    public SettingsTransViewModel()
    {
        _settings = SettingsModel.LoadSettings();
    }
}
