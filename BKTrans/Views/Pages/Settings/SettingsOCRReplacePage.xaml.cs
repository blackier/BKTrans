using BKTrans.ViewModels.Pages;
using BKTrans.ViewModels.Pages.Settings;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Input;
using Wpf.Ui.Controls.Navigation;
using static BKTrans.Models.SettingsModel;

namespace BKTrans.Views.Pages.Settings;

/// <summary>
/// Interaction logic for Settings.xaml
/// </summary>
public partial class SettingsOCRReplacePage : INavigableView<SettingsOCRReplaceViewModel>
{

    public object TextBoxUpdateSource
    {
        set
        {
            BindingOperations.GetBindingExpression(value as TextBox, TextBox.TextProperty).UpdateSource();
        }
    }

    private SettingsOCRReplaceViewModel _viewModel;

    public SettingsOCRReplaceViewModel ViewModel { get { return _viewModel; } }

    public SettingsOCRReplacePage(SettingsOCRReplaceViewModel viewModel)
    {
        _viewModel = viewModel;
        DataContext = this;
        InitializeComponent();
    }
    #region 事件处理
    protected void btn_save_Click(object sender, RoutedEventArgs e)
    {
        BindingOperations.GetBindingExpression(datagrid_ocr_replace, DataGrid.ItemsSourceProperty).UpdateSource();
    }

    protected void btn_cancle_Click(object sender, RoutedEventArgs e)
    {
        App.NavigateGoBack();
    }

    private void btn_ocr_replace_new_Click(object sender, RoutedEventArgs e)
    {
        _viewModel.NewOcrReplace(textbox_ocr_replace_new.Text);
    }

    private async void btn_ocr_replace_delete_Click(object sender, RoutedEventArgs e)
    {
        var uiMessageBox = new Wpf.Ui.Controls.MessageBoxControl.MessageBox
        {
            Title = "警告",
            Content = "删除后不可恢复，是否删除？",
            PrimaryButtonText = "删除",
            PrimaryButtonAppearance = Wpf.Ui.Controls.ControlAppearance.Danger,
            CloseButtonText = "取消",
        };

        if (await uiMessageBox.ShowDialogAsync() != Wpf.Ui.Controls.MessageBoxControl.MessageBoxResult.Primary)
            return;

        _viewModel.DeleteOcrSeletedItem();
    }

    private void datagrid_ocr_replace_Sorting(object sender, System.Windows.Controls.DataGridSortingEventArgs e)
    {
        var ocrReplace = datagrid_ocr_replace.ItemsSource as List<OCRReplace>;

        if (e.Column.DisplayIndex == 0)
        {
            if (e.Column.SortDirection == System.ComponentModel.ListSortDirection.Descending)
                ocrReplace = ocrReplace.OrderByDescending(elem => elem.replace_src).ToList();
            else
                ocrReplace = ocrReplace.OrderBy(elem => elem.replace_src).ToList();

        }
        else if (e.Column.DisplayIndex == 1)
        {
            if (e.Column.SortDirection == System.ComponentModel.ListSortDirection.Descending)
                ocrReplace = ocrReplace.OrderByDescending(elem => elem.replace_dst).ToList();
            else
                ocrReplace = ocrReplace.OrderBy(elem => elem.replace_dst).ToList();
        }

        datagrid_ocr_replace.ItemsSource = ocrReplace;

        e.Handled = true;
    }

    private void datagrid_ocr_replace_PreviewKeyDown(object sender, System.Windows.Input.KeyEventArgs e)
    {
        if (e.Key == System.Windows.Input.Key.Enter && !datagrid_ocr_replace_behvior.IsEditing)
        {
            var selectIndex = datagrid_ocr_replace.SelectedIndex;
            var ocrReplace = datagrid_ocr_replace.ItemsSource as List<OCRReplace>;
            ocrReplace.Insert(ocrReplace.Count == selectIndex ? selectIndex : selectIndex + 1, new());

            CollectionViewSource.GetDefaultView(datagrid_ocr_replace.ItemsSource).Refresh();

            datagrid_ocr_replace.UpdateLayout();
            //datagrid_ocr_replace.ScrollIntoView(datagrid_ocr_replace.Items[selectIndex]);

            DataGridRow row = (DataGridRow)datagrid_ocr_replace.ItemContainerGenerator.ContainerFromIndex(selectIndex);
            //row?.MoveFocus(new TraversalRequest(FocusNavigationDirection.Next));
        }
    }

    #endregion 事件处理
}
