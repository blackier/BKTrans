using BKTrans.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BKTrans.ViewModels.Pages;

public partial class SettingsAutoTransViewModel : ObservableObject
{
    private readonly SettingsModel.Settings _settings;

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
    public SettingsAutoTransViewModel()
    {
        _settings = SettingsModel.LoadSettings();
    }
}
