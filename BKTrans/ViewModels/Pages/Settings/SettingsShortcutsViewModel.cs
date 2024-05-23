using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using BKTrans.Models;

namespace BKTrans.ViewModels.Pages;

public partial class SettingsShortcutsViewModel : ObservableObject
{
    [ObservableProperty]
    private string _captureShortcut = "F2";

    [ObservableProperty]
    private string _showFloatWindow = "Shift+Alt+X";

    [ObservableProperty]
    private string _hideFloatWindow = "Shift+Alt+Z";

    private readonly SettingsModel.Settings _settings;

    public SettingsShortcutsViewModel()
    {
        _settings = SettingsModel.LoadSettings();
    }
}
