using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Input;
using BKTrans.Models;
using BKTrans.ViewModels.Pages;
using BKTrans.ViewModels.Pages.Settings;

namespace BKTrans.Views.Pages.Settings;

/// <summary>
/// Interaction logic for Settings.xaml
/// </summary>
public partial class SettingsTransPage : INavigableView<SettingsTransViewModel>
{
    public object TextBoxUpdateSource
    {
        set { BindingOperations.GetBindingExpression(value as TextBox, TextBox.TextProperty).UpdateSource(); }
    }

    public SettingsTransViewModel ViewModel { get; }

    public SettingsTransPage(SettingsTransViewModel viewModel)
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
