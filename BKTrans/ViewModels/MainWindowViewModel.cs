using BKTrans.Kernel;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;

namespace BKTrans.ViewModels;

public partial class MainWindowViewModel : ObservableObject
{
    public enum TrayType
    {
        Capture,
        Trans,
        ShowFloatWindow,
        HideFloatWindow,
        Exit
    }

    private readonly IServiceProvider _serviceProvider;

    public MainWindowViewModel(IServiceProvider serviceProvider)
    {
        _serviceProvider = serviceProvider;
    }
}