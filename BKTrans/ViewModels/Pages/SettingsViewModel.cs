using BKTrans.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BKTrans.ViewModels.Pages;

public partial class SettingsViewModel : ObservableObject
{
    private readonly SettingsModel.Settings _settings;

    public SettingsViewModel()
    {
        _settings = SettingsModel.LoadSettings();
    }
}
