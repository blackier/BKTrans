using BKTrans.ViewModels.Pages;
using BKTrans.ViewModels.Pages.Settings;
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
public partial class SettingsTransPage : INavigableView<SettingsTransViewModel>
{

    public object TextBoxUpdateSource
    {
        set
        {
            BindingOperations.GetBindingExpression(value as TextBox, TextBox.TextProperty).UpdateSource();
        }
    }

    private SettingsTransViewModel _viewModel;

    public SettingsTransViewModel ViewModel { get { return _viewModel; } }

    public SettingsTransPage(SettingsTransViewModel viewModel)
    {
        _viewModel = viewModel;
        DataContext = this;
        InitializeComponent();
    }
    #region 事件处理
    protected void btn_save_Click(object sender, RoutedEventArgs e)
    {
    }

    protected void btn_cancle_Click(object sender, RoutedEventArgs e)
    {
        App.NavigateGoBack();
    }

    #endregion 事件处理
}
