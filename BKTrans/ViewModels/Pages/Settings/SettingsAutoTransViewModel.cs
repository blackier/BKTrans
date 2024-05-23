using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using BKTrans.Models;

namespace BKTrans.ViewModels.Pages;

public partial class SettingsAutoTransViewModel : ObservableObject
{
    private readonly SettingsModel.Settings _settings;

    public int AutoCaptrueTransInterval
    {
        get => _settings.auto_captrue_trans_interval;
        set
        {
            SetProperty(
                _settings.auto_captrue_trans_interval,
                value,
                _settings,
                (s, v) => s.auto_captrue_trans_interval = v
            );
        }
    }

    public int AutoCaptrueTransCountdown
    {
        get => _settings.auto_captrue_trans_countdown;
        set
        {
            SetProperty(
                _settings.auto_captrue_trans_countdown,
                value,
                _settings,
                (s, v) => s.auto_captrue_trans_countdown = v
            );
        }
    }

    public float AutoCaptrueTransSimilarity
    {
        get => _settings.auto_captrue_trans_similarity;
        set
        {
            SetProperty(
                _settings.auto_captrue_trans_similarity,
                value,
                _settings,
                (s, v) => s.auto_captrue_trans_similarity = v
            );
        }
    }

    public SettingsAutoTransViewModel()
    {
        _settings = SettingsModel.LoadSettings();
    }
}
