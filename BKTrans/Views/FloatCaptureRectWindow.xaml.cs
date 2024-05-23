using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Interop;
using System.Windows.Threading;
using BKTrans.Controls;
using BKTrans.Core;

namespace BKTrans.Views;

public partial class FloatCaptureRectWindow : Window
{
    public enum ButtonType
    {
        Capture,
        Trans,
        AutoTrans,
        GragMove
    };

    private Stack<RectangleF> _historyRect;
    private Action<ButtonType, object> _onButtonClick;
    private TransResultTextControl _transResultTextControl;
    private FloatTransTextWindow _floatTransTextWindow;

    public FloatCaptureRectWindow(FloatTransTextWindow floatTransTextWindow)
    {
        InitializeComponent();

        ShowInTaskbar = false;
        _onButtonClick = null;
        _historyRect = new();

        _transResultTextControl = new TransResultTextControl();
        _transResultTextControl.TextButtonMouseWheel += checkbox_text_MouseWheel;
        _transResultTextControl.TextButtonClick += checkbox_text_Click;
        _transResultTextControl.DragButtonPreviewMouseLeftButtonDown += checkbox_text_PreviewMouseLeftButtonDown;
        content_transresult.Content = _transResultTextControl;

        _floatTransTextWindow = floatTransTextWindow;

        Closing += new CancelEventHandler(Window_Closing);
        Loaded += new RoutedEventHandler(Window_Loaded);
    }

    public void RegisterCallback(Action<ButtonType, object> OnButtonClick = null)
    {
        _onButtonClick = OnButtonClick;
    }

    private void AddHistoryRect()
    {
        var currentRect = new RectangleF((float)Left, (float)Top, (float)Width, (float)Height);
        if (_historyRect.Count > 0 && currentRect == _historyRect.Peek())
            return;
        _historyRect.Push(currentRect);
    }

    public void SetTextRect(RectangleF rect)
    {
        Dispatcher.Invoke(() =>
        {
            var p = WindowExtensions.ScreenScaling();
            Left = rect.X / p;
            Top = rect.Y / p;
            Width = rect.Width / p + gridcolumn_btn.Width.Value;
            Height = (rect.Height / p) * 2;
            AddHistoryRect();
        });
    }

    public RectangleF GetTextRect()
    {
        var p = WindowExtensions.ScreenScaling();
        return new RectangleF(
            (float)(Left * p),
            (float)(Top * p),
            (float)((Width - gridcolumn_btn.Width.Value) * p),
            (float)((Height / 2 * p))
        );
    }

    public void SetText(string t)
    {
        Dispatcher.Invoke(() =>
        {
            _transResultTextControl.Text = t;
        });
    }

    public void SetAutoTransStatus(bool start)
    {
        Dispatcher.Invoke(() =>
        {
            checkbox_autotrans.IsChecked = start;
        });
    }

    public void ShowWindow()
    {
        Dispatcher.Invoke(() =>
        {
            if (_transResultTextControl.TextButtonIsChecked)
                _floatTransTextWindow.ShowWindow();
            Topmost = true;
            Show();
            Activate();
        });
    }

    public void HideWindow()
    {
        Dispatcher.Invoke(() =>
        {
            Hide();
            _floatTransTextWindow.HideWindow();
        });
    }

    private void Window_Loaded(object sender, RoutedEventArgs e)
    {
        // 设置拖拽
        AdornerLayer adornerLayer = AdornerLayer.GetAdornerLayer(rectangele_capture);
        adornerLayer.Add(
            new BKDragAdorner(
                rectangele_capture,
                (RectangleF change) =>
                {
                    //Left += change.X;
                    //Top += change.Y;
                    //Width += change.Width;
                    //Height += change.Height;

                    var p = 1;
                    var newPosition = new Rectangle(
                        (int)(Left * p + change.X),
                        (int)(Top * p + change.Y),
                        (int)(Width * p + change.Width),
                        (int)(Height * p + change.Height)
                    );

                    WindowInteropHelper wih = new(this);
                    IntPtr hWnd = wih.Handle;
                    if (!newPosition.IsEmpty)
                    {
                        // 偶然测试出来，直接move不用担心数据出错
                        _ = BKWindowsAPI.MoveWindow(
                            hWnd,
                            newPosition.Left,
                            newPosition.Top,
                            newPosition.Width,
                            newPosition.Height,
                            false
                        );
                    }
                    //AddHistoryRect();
                }
            )
        );
    }

    private void Window_Closing(object sender, CancelEventArgs e)
    {
        _floatTransTextWindow.Close();
    }

    private void btn_capture_Click(object sender, RoutedEventArgs e)
    {
        if (_onButtonClick != null)
            _onButtonClick(ButtonType.Capture, null);
    }

    private void btn_trans_Click(object sender, RoutedEventArgs e)
    {
        if (_onButtonClick != null)
            _onButtonClick(ButtonType.Trans, null);
    }

    private void btn_hide_Click(object sender, RoutedEventArgs e)
    {
        grid_textbox.Visibility =
            grid_textbox.Visibility == Visibility.Visible ? Visibility.Hidden : Visibility.Visible;
        if (!_transResultTextControl.TextButtonIsChecked)
            _transResultTextControl.TextVisibility = grid_textbox.Visibility;

        AdornerLayer adornerLayer = AdornerLayer.GetAdornerLayer(rectangele_capture);
        Adorner[] adorners = adornerLayer.GetAdorners(rectangele_capture);
        for (int i = adorners.Length - 1; i >= 0; i--)
        {
            adorners[i].Visibility = grid_textbox.Visibility;
        }
    }

    private void checkbox_autotrans_Click(object sender, RoutedEventArgs e)
    {
        checkbox_autotrans.IsChecked = !checkbox_autotrans.IsChecked;
        if (_onButtonClick != null)
            _onButtonClick(ButtonType.AutoTrans, checkbox_autotrans.IsChecked);
    }

    private void btn_drag_PreviewMouseLeftButtonDown(object sender, System.Windows.Input.MouseButtonEventArgs e)
    {
        bool? autotrans_checked = checkbox_autotrans.IsChecked;
        if (_onButtonClick != null)
            _onButtonClick(ButtonType.AutoTrans, false);

        DragMove();
        AddHistoryRect();

        if (_onButtonClick != null)
        {
            _onButtonClick(ButtonType.GragMove, null);
            _onButtonClick(ButtonType.AutoTrans, autotrans_checked);
        }
    }

    private void btn_undo_Click(object sender, RoutedEventArgs e)
    {
        if (_historyRect.Count > 0)
        {
            RectangleF preRect;
            // 丢弃当前位置
            if (_historyRect.Count > 1)
                _historyRect.Pop();
            preRect = _historyRect.Peek();
            Top = preRect.Top;
            Left = preRect.Left;
            Width = preRect.Width;
            Height = preRect.Height;

            System.Windows.Point position = btn_undo.PointToScreen(new System.Windows.Point(0d, 0d));
            BKWindowsAPI.SetCursorPos((int)(position.X + btn_undo.Width / 2), (int)(position.Y + btn_undo.Height / 2));
        }
    }

    private void checkbox_text_MouseWheel(object sender, System.Windows.Input.MouseWheelEventArgs e)
    {
        if (e.Delta == 0)
            return;
        double unit = 1;
        if (e.Delta < 0)
            unit = -unit;
        double fontSize = _transResultTextControl.FontSize;
        if (fontSize + unit < 12)
            return;
        _transResultTextControl.FontSize = fontSize + unit;
    }

    private void checkbox_text_Click(object sender, RoutedEventArgs e)
    {
        _transResultTextControl.TextButtonIsChecked = !_transResultTextControl.TextButtonIsChecked;
        bool isCheck = _transResultTextControl.TextButtonIsChecked;

        _transResultTextControl.ButtonsVisibility = isCheck ? Visibility.Visible : Visibility.Hidden;
        if (isCheck)
        {
            content_transresult.Content = null;
            _floatTransTextWindow.ChangeContent(_transResultTextControl, isCheck);
            _floatTransTextWindow.MoveWindow(
                new Rectangle(
                    content_transresult.PointToScreen(new()).ToDrawingPoint(),
                    content_transresult.RenderSize.ToDrawingSize()
                )
            );
            _floatTransTextWindow.ShowWindow();
            _floatTransTextWindow.MoveWindow(
                new Rectangle(
                    content_transresult.PointToScreen(new()).ToDrawingPoint(),
                    content_transresult.RenderSize.ToDrawingSize()
                )
            );

            _transResultTextControl.InitDragAdorner(
                (RectangleF change) =>
                {
                    var p = 1;
                    var newPosition = new Rectangle(
                        (int)(_floatTransTextWindow.Left * p + change.X),
                        (int)(_floatTransTextWindow.Top * p + change.Y),
                        (int)(_floatTransTextWindow.Width * p + change.Width),
                        (int)(_floatTransTextWindow.Height * p + change.Height)
                    );
                    _floatTransTextWindow.MoveWindow(newPosition);
                }
            );
        }
        else
        {
            _floatTransTextWindow.HideWindow();
            _floatTransTextWindow.ChangeContent(_transResultTextControl, isCheck);
            content_transresult.Content = _transResultTextControl;
        }
    }

    private void checkbox_text_PreviewMouseLeftButtonDown(object sender, System.Windows.Input.MouseButtonEventArgs e)
    {
        _floatTransTextWindow.DragMove();
    }
}
