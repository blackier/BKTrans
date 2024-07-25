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
public partial class SettingsAutoTransPage : INavigableView<SettingsAutoTransViewModel>
{
    public object TextBoxUpdateSource
    {
        set { BindingOperations.GetBindingExpression(value as TextBox, TextBox.TextProperty).UpdateSource(); }
    }

    private SettingsAutoTransViewModel _viewModel;

    public SettingsAutoTransViewModel ViewModel { get; }

    public SettingsAutoTransPage(SettingsAutoTransViewModel viewModel)
    {
        ViewModel = viewModel;
        DataContext = this;
        InitializeComponent();
    }

    protected void btn_save_Click(object sender, RoutedEventArgs e)
    {
        SettingsModel.SaveSettings();
    }

    protected void btn_cancle_Click(object sender, RoutedEventArgs e)
    {
        App.NavigateGoBack();
    }
}
