using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using BKTrans.Models;

namespace BKTrans.ViewModels.Pages;

public partial class AboutViewModel : ObservableObject
{
    [ObservableProperty]
    private string _version;

    private readonly SettingsModel.Settings _settings;

    public AboutViewModel()
    {
        _settings = SettingsModel.LoadSettings();
        Version = "版本 v" + Application.ResourceAssembly.GetName().Version.ToString();
    }
}
