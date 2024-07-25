using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Input;
using BKTrans.ViewModels.Pages;
using BKTrans.Views.Pages.Settings;

namespace BKTrans.Views.Pages;

/// <summary>
/// Interaction logic for Settings.xaml
/// </summary>
public partial class SettingsPage : INavigableView<SettingsViewModel>
{
    public object TextBoxUpdateSource
    {
        set { BindingOperations.GetBindingExpression(value as TextBox, TextBox.TextProperty).UpdateSource(); }
    }

    public SettingsViewModel ViewModel { get; }

    public SettingsPage(SettingsViewModel viewModel, INavigationViewPageProvider navigationViewPageProvider)
    {
        ViewModel = viewModel;
        DataContext = this;
        InitializeComponent();

        NavigationView.SetPageProviderService(navigationViewPageProvider);
        NavigationView.Loaded += (_, _) => NavigationView.Navigate(typeof(SettingsTransPage));
    }
}
