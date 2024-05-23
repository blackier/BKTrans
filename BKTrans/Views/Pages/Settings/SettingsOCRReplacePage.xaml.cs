using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime;
using System.Text.Encodings.Web;
using System.Text.Unicode;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Data;
using System.Windows.Input;
using System.Windows.Media;
using BKTrans.Core;
using BKTrans.Models;
using BKTrans.ViewModels.Pages;
using BKTrans.ViewModels.Pages.Settings;
using Microsoft.Win32;
using static BKTrans.Models.SettingsModel;

namespace BKTrans.Views.Pages.Settings;

/// <summary>
/// Interaction logic for Settings.xaml
/// </summary>
public partial class SettingsOCRReplacePage : wpfui.INavigableView<SettingsOCRReplaceViewModel>
{
    public object TextBoxUpdateSource
    {
        set { BindingOperations.GetBindingExpression(value as TextBox, TextBox.TextProperty).UpdateSource(); }
    }

    private SettingsOCRReplaceViewModel _viewModel;

    public SettingsOCRReplaceViewModel ViewModel
    {
        get { return _viewModel; }
    }

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
        SettingsModel.SaveSettings();
    }

    protected void btn_cancle_Click(object sender, RoutedEventArgs e)
    {
        App.NavigateGoBack();
    }

    private void btn_ocr_replace_new_Click(object sender, RoutedEventArgs e)
    {
        _viewModel.NewOcrReplace(textbox_ocr_replace_new.Text);
        textbox_ocr_replace_new.Text = "";
    }

    private async void btn_ocr_replace_delete_Click(object sender, RoutedEventArgs e)
    {
        var uiMessageBox = new wpfui.MessageBox
        {
            Title = "警告",
            Content = "删除后不可恢复，是否删除？",
            PrimaryButtonText = "删除",
            PrimaryButtonAppearance = wpfui.ControlAppearance.Danger,
            CloseButtonText = "取消",
        };

        if (await uiMessageBox.ShowDialogAsync() != wpfui.MessageBoxResult.Primary)
            return;

        _viewModel.DeleteOcrSeletedItem();
    }

    private void btn_ocr_replace_import_Click(object sender, RoutedEventArgs e)
    {
        OpenFileDialog openFileDialog = new OpenFileDialog();
        openFileDialog.Filter = "Json(*.json)|*.json|Text(*.txt)|*txt";
        if (openFileDialog.ShowDialog() == true)
        {
            List<OCRReplace> importOcrRepace = _viewModel.LoadOCRReplace(openFileDialog.FileName);
            if (importOcrRepace != null)
            {
                var currentOcrReplace = datagrid_ocr_replace.ItemsSource as List<OCRReplace>;
                datagrid_ocr_replace.ItemsSource = currentOcrReplace
                    .UnionBy(importOcrRepace, r => r.replace_src)
                    .ToList();
                App.SnackbarSuccess("导入成功，去重合入。");
            }
            else
            {
                App.SnackbarError("导入失败，请检查文件格式是否正确。");
            }
        }
    }

    private void btn_ocr_replace_export_Click(object sender, RoutedEventArgs e)
    {
        SaveFileDialog saveFileDialog = new SaveFileDialog();
        saveFileDialog.Filter = "Json(*.json)|*.json|Text(*.txt)|*txt";
        if (saveFileDialog.ShowDialog() == true)
        {
            _viewModel.SaveOCRReplace(saveFileDialog.FileName, datagrid_ocr_replace.ItemsSource as List<OCRReplace>);
            App.SnackbarSuccess("导出成功，文件路径：" + saveFileDialog.FileName);
        }
    }

    private void datagrid_ocr_replace_menuitem_Click(object sender, RoutedEventArgs e)
    {
        var sortType = ((sender as MenuItem).DataContext as SortTypeInfo).sortType;
        var ocrReplace = datagrid_ocr_replace.ItemsSource as List<OCRReplace>;
        switch (sortType)
        {
            case SettingsOCRReplaceViewModel.SortType.ByChar:
                ocrReplace = ocrReplace.OrderBy(elem => elem.replace_src).ToList();
                break;
            case SettingsOCRReplaceViewModel.SortType.ByCharDesc:
                ocrReplace = ocrReplace.OrderByDescending(elem => elem.replace_src).ToList();
                break;
            case SettingsOCRReplaceViewModel.SortType.ByLength:
                ocrReplace = ocrReplace.OrderBy(elem => elem.replace_src.Length).ToList();
                break;
            case SettingsOCRReplaceViewModel.SortType.ByLengthDesc:
                ocrReplace = ocrReplace.OrderByDescending(elem => elem.replace_src.Length).ToList();
                break;
            default:
                break;
        }
        datagrid_ocr_replace.ItemsSource = ocrReplace;
    }

    public DataGridCell GetCell(DataGrid dataGrid, DataGridRow rowContainer, int column)
    {
        if (rowContainer != null)
        {
            DataGridCellsPresenter presenter = rowContainer.FindChild<DataGridCellsPresenter>();
            if (presenter == null)
            {
                /* if the row has been virtualized away, call its ApplyTemplate() method
                 * to build its visual tree in order for the DataGridCellsPresenter
                 * and the DataGridCells to be created */
                rowContainer.ApplyTemplate();
                presenter = rowContainer.FindChild<DataGridCellsPresenter>();
            }
            if (presenter != null)
            {
                DataGridCell cell = presenter.ItemContainerGenerator.ContainerFromIndex(column) as DataGridCell;
                if (cell == null)
                {
                    /* bring the column into view
                     * in case it has been virtualized away */
                    dataGrid.ScrollIntoView(rowContainer, dataGrid.Columns[column]);
                    cell = presenter.ItemContainerGenerator.ContainerFromIndex(column) as DataGridCell;
                }
                return cell;
            }
        }
        return null;
    }

    private void datagrid_ocr_replace_PreviewKeyDown(object sender, KeyEventArgs e)
    {
        if (e.Key == Key.Enter && !datagrid_ocr_replace_behvior.IsEditing)
        {
            var selectIndex = datagrid_ocr_replace.SelectedIndex;
            var ocrReplace = datagrid_ocr_replace.ItemsSource as List<OCRReplace>;
            var newItemIndex = ocrReplace.Count == selectIndex ? selectIndex : selectIndex + 1;
            ocrReplace.Insert(newItemIndex, new());

            CollectionViewSource.GetDefaultView(datagrid_ocr_replace.ItemsSource).Refresh();

            // https://blog.magnusmontin.net/2013/11/08/how-to-programmatically-select-and-focus-a-row-or-cell-in-a-datagrid-in-wpf/
            datagrid_ocr_replace.UpdateLayout();
            DataGridRow row =
                datagrid_ocr_replace.ItemContainerGenerator.ContainerFromIndex(selectIndex) as DataGridRow;
            if (row != null)
                GetCell(datagrid_ocr_replace, row, 1)?.Focus();
            e.Handled = true;
        }
    }

    private void menuitem_ocr_sort_IsVisibleChanged(object sender, DependencyPropertyChangedEventArgs e)
    {
        //https://stackoverflow.com/questions/22090490/context-menu-binding-with-itemsource-is-not-working
        //简单处理
        (sender as MenuItem).ItemsSource = ViewModel.SortTypeItems;
    }
    #endregion 事件处理
}
