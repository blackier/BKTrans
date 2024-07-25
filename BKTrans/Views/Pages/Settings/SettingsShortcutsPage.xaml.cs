using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Input;
using BKTrans.Models;
using BKTrans.ViewModels.Pages;

namespace BKTrans.Views.Pages.Settings;

/// <summary>
/// Interaction logic for Settings.xaml
/// </summary>
public partial class SettingsShortcutsPage : INavigableView<SettingsShortcutsViewModel>
{
    public SettingsShortcutsViewModel ViewModel { get; }

    public SettingsShortcutsPage(SettingsShortcutsViewModel viewModel)
    {
        ViewModel = viewModel;
        DataContext = this;
        InitializeComponent();
    }
}
