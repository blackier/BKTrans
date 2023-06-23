using BKTrans.ViewModels.Pages;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Input;
using Wpf.Ui.Controls.Navigation;

namespace BKTrans.Views.Pages;

/// <summary>
/// Interaction logic for Settings.xaml
/// </summary>
public partial class AboutPage : INavigableView<AboutViewModel>
{
    private AboutViewModel _viewModel;

    public AboutViewModel ViewModel { get { return _viewModel; } }

    public AboutPage(AboutViewModel viewModel)
    {
        _viewModel = viewModel;
        DataContext = this;
        InitializeComponent();
    }
}
