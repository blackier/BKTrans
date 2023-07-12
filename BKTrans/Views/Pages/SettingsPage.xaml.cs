using BKTrans.ViewModels.Pages;
using BKTrans.Views.Pages.Settings;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Input;

namespace BKTrans.Views.Pages;

/// <summary>
/// Interaction logic for Settings.xaml
/// </summary>
public partial class SettingsPage : wpfui.INavigableView<SettingsViewModel>
{

    public object TextBoxUpdateSource
    {
        set
        {
            BindingOperations.GetBindingExpression(value as TextBox, TextBox.TextProperty).UpdateSource();
        }
    }

    private SettingsViewModel _viewModel;

    public SettingsViewModel ViewModel { get { return _viewModel; } }

    public SettingsPage(SettingsViewModel viewModel, IServiceProvider serviceProvider)
    {
        _viewModel = viewModel;
        DataContext = this;
        InitializeComponent();

        NavigationView.SetServiceProvider(serviceProvider);
        NavigationView.Loaded += (_, _) => NavigationView.Navigate(typeof(SettingsTransPage));
    }
    #region 事件处理
    #endregion 事件处理
}
