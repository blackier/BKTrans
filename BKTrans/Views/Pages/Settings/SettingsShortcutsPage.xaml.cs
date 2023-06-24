using BKTrans.Models;
using BKTrans.ViewModels.Pages;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Input;
using Wpf.Ui.Controls.Navigation;

namespace BKTrans.Views.Pages.Settings;

/// <summary>
/// Interaction logic for Settings.xaml
/// </summary>
public partial class SettingsShortcutsPage : INavigableView<SettingsShortcutsViewModel>
{

    private SettingsShortcutsViewModel _viewModel;

    public SettingsShortcutsViewModel ViewModel { get { return _viewModel; } }

    public SettingsShortcutsPage(SettingsShortcutsViewModel viewModel)
    {
        _viewModel = viewModel;
        DataContext = this;
        InitializeComponent();
    }
}
