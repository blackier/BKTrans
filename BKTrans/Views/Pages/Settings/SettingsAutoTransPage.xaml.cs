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
public partial class SettingsAutoTransPage : INavigableView<SettingsAutoTransViewModel>
{

    public object TextBoxUpdateSource
    {
        set
        {
            BindingOperations.GetBindingExpression(value as TextBox, TextBox.TextProperty).UpdateSource();
        }
    }

    private SettingsAutoTransViewModel _viewModel;

    public SettingsAutoTransViewModel ViewModel { get { return _viewModel; } }

    public SettingsAutoTransPage(SettingsAutoTransViewModel viewModel)
    {
        _viewModel = viewModel;
        DataContext = this;
        InitializeComponent();
    }
    #region 事件处理
    protected void btn_save_Click(object sender, RoutedEventArgs e)
    {
        SettingsModel.SaveSettings();
    }

    protected void btn_cancle_Click(object sender, RoutedEventArgs e)
    {
        App.NavigateGoBack();
    }

    private void textbox_auto_captrue_trans_interval_MouseWheel(object sender, System.Windows.Input.MouseWheelEventArgs e)
    {
        if (e.Delta == 0)
            return;
        int unit = 10;
        if (e.Delta < 0)
            unit = -unit;
        int interval = int.Parse(textbox_auto_captrue_trans_interval.Text);
        if (interval + unit < 100)
            return;
        textbox_auto_captrue_trans_interval.Text = (interval + unit).ToString();
    }

    private void textbox_auto_captrue_trans_countdown_MouseWheel(object sender, System.Windows.Input.MouseWheelEventArgs e)
    {
        if (e.Delta == 0)
            return;
        int unit = 1;
        if (e.Delta < 0)
            unit = -unit;
        int interval = int.Parse(textbox_auto_captrue_trans_countdown.Text);
        if (interval + unit < 0)
            return;
        textbox_auto_captrue_trans_countdown.Text = (interval + unit).ToString();
    }

    private void textbox_auto_captrue_trans_similarity_MouseWheel(object sender, System.Windows.Input.MouseWheelEventArgs e)
    {
        if (e.Delta == 0)
            return;
        float unit = 0.01f;
        if (e.Delta < 0)
            unit = -unit;
        float interval = float.Parse(textbox_auto_captrue_trans_similarity.Text);
        if (interval + unit > 1)
            return;
        if (interval + unit < 0.5f)
            return;
        textbox_auto_captrue_trans_similarity.Text = (interval + unit).ToString("0.00");
    }

    #endregion 事件处理
}
