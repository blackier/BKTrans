using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Interop;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace BKTrans.Controls;

/// <summary>
/// Interaction logic for TransResultTextControl.xaml
/// </summary>
public partial class TransResultTextControl : UserControl
{
    public String Text { get { return textbox_transtext.Text; } set { textbox_transtext.Text = value; } }
    public Visibility TextVisibility { get { return textbox_transtext.Visibility; } set { textbox_transtext.Visibility = value; } }
    public new double FontSize { get { return textbox_transtext.FontSize; } set { textbox_transtext.FontSize = value; } }
    public Visibility ButtonsVisibility { set { btn_drag.Visibility = value; btn_hide.Visibility = value; } }
    public bool TextButtonIsChecked { get { return checkbox_text.IsChecked ?? false; } set { checkbox_text.IsChecked = value; } }

    public event RoutedEventHandler TextButtonClick;
    public event MouseWheelEventHandler TextButtonMouseWheel;
    public event MouseButtonEventHandler DragButtonPreviewMouseLeftButtonDown;

    private BKDragAdorner _dragAdorner;

    public TransResultTextControl()
    {
        InitializeComponent();
        ButtonsVisibility = Visibility.Hidden;
    }

    public void InitDragAdorner(Action<RectangleF> dragAction)
    {
        // 设置拖拽
        _dragAdorner = new BKDragAdorner(textbox_transtext, dragAction);
        AdornerLayer adornerLayer = AdornerLayer.GetAdornerLayer(textbox_transtext);
        adornerLayer.Add(_dragAdorner);
    }

    private void checkbox_text_Click(object sender, RoutedEventArgs e)
    {
        if (TextButtonClick != null) { TextButtonClick(sender, e); }
    }

    private void checkbox_text_MouseWheel(object sender, MouseWheelEventArgs e)
    {
        if (TextButtonMouseWheel != null) { TextButtonMouseWheel(sender, e); }
    }

    private void btn_drag_PreviewMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
    {
        if (DragButtonPreviewMouseLeftButtonDown != null) { DragButtonPreviewMouseLeftButtonDown(sender, e); }
    }

    private void btn_hide_Click(object sender, RoutedEventArgs e)
    {
        _dragAdorner.Visibility = _dragAdorner.Visibility == Visibility.Visible ? Visibility.Hidden : Visibility.Visible;
    }
}
