using BKTrans.Misc;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Timers;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Shapes;

namespace BKTrans
{
    public class BKDragAdorner : Adorner
    {
        private VisualCollection _visuals;
        private ContentPresenter _contentPresenter;
        double _lastDrag;
        double _dragInterval;
        Action<System.Drawing.RectangleF> _onDrag;

        //必须生成构造函数
        public BKDragAdorner(UIElement adornedElement, Action<System.Drawing.RectangleF> onDrag = null) : base(adornedElement)
        {
            _onDrag = onDrag;

            _lastDrag = BKMisc.TimeNow().TotalMilliseconds;
            _dragInterval = 0;

            Thumb leftThumb = new Thumb();
            leftThumb.HorizontalAlignment = HorizontalAlignment.Left;
            leftThumb.VerticalAlignment = VerticalAlignment.Stretch;
            leftThumb.Cursor = Cursors.SizeWE;//获取双向水平（西/东）大小调整光标
            Thumb topThumb = new Thumb();
            topThumb.HorizontalAlignment = HorizontalAlignment.Stretch;
            topThumb.VerticalAlignment = VerticalAlignment.Top;
            topThumb.Cursor = Cursors.SizeNS;//获取双向垂直（北/南）大小调整光标
            Thumb rightThumb = new Thumb();
            rightThumb.HorizontalAlignment = HorizontalAlignment.Right;
            rightThumb.VerticalAlignment = VerticalAlignment.Stretch;
            rightThumb.Cursor = Cursors.SizeWE;//获取双向水平（西/东）大小调整光标
            Thumb bottomThumb = new Thumb();
            bottomThumb.HorizontalAlignment = HorizontalAlignment.Stretch;
            bottomThumb.VerticalAlignment = VerticalAlignment.Bottom;
            bottomThumb.Cursor = Cursors.SizeNS;//获取双向垂直（北/南）大小调整光标
            SetLineThumbStyle(leftThumb);
            SetLineThumbStyle(topThumb);
            SetLineThumbStyle(rightThumb);
            SetLineThumbStyle(bottomThumb);
            Thumb lefTopThumb = new Thumb();
            lefTopThumb.HorizontalAlignment = HorizontalAlignment.Left;
            lefTopThumb.VerticalAlignment = VerticalAlignment.Top;
            lefTopThumb.Cursor = Cursors.SizeNWSE;//获取双向对角线（西北/东南）大小调整光标
            Thumb rightTopThumb = new Thumb();
            rightTopThumb.HorizontalAlignment = HorizontalAlignment.Right;
            rightTopThumb.VerticalAlignment = VerticalAlignment.Top;
            rightTopThumb.Cursor = Cursors.SizeNESW;//获取双向对角线（东北/西南）大小调整光标
            Thumb rightBottomThumb = new Thumb();
            rightBottomThumb.HorizontalAlignment = HorizontalAlignment.Right;
            rightBottomThumb.VerticalAlignment = VerticalAlignment.Bottom;
            rightBottomThumb.Cursor = Cursors.SizeNWSE;//获取双向对角线（西北/东南）大小调整光标
            Thumb leftbottomThumb = new Thumb();
            leftbottomThumb.HorizontalAlignment = HorizontalAlignment.Left;
            leftbottomThumb.VerticalAlignment = VerticalAlignment.Bottom;
            leftbottomThumb.Cursor = Cursors.SizeNESW;//获取双向对角线（东北/西南）大小调整光标
            SetPointThumbStyle(lefTopThumb);
            SetPointThumbStyle(rightTopThumb);
            SetPointThumbStyle(rightBottomThumb);
            SetPointThumbStyle(leftbottomThumb);

            Grid grid = new Grid();
            grid.Children.Add(leftThumb);
            grid.Children.Add(topThumb);
            grid.Children.Add(rightThumb);
            grid.Children.Add(bottomThumb);
            grid.Children.Add(lefTopThumb);
            grid.Children.Add(rightTopThumb);
            grid.Children.Add(rightBottomThumb);
            grid.Children.Add(leftbottomThumb);

            _visuals = new VisualCollection(this);
            _contentPresenter = new ContentPresenter();
            _contentPresenter.Content = grid;

            _visuals.Add(_contentPresenter);
        }
        private void SetLineThumbStyle(Thumb thumb)
        {
            Thickness borderThickness = new Thickness(0, 0, 0, 0);
            double lineThickness = 3;
            double lineLength = 50;
            if (thumb.HorizontalAlignment == HorizontalAlignment.Left)
            {
                borderThickness.Left = lineThickness;
                thumb.Width = lineThickness;
            }
            else if (thumb.HorizontalAlignment == HorizontalAlignment.Right)
            {
                borderThickness.Right = lineThickness;
                thumb.Width = lineThickness;
            }
            else
            {
                thumb.Width = lineLength;
            }

            if (thumb.VerticalAlignment == VerticalAlignment.Top)
            {
                borderThickness.Top = lineThickness;
                thumb.Height = lineThickness;
            }
            else if (thumb.VerticalAlignment == VerticalAlignment.Bottom)
            {
                borderThickness.Bottom = lineThickness;
                thumb.Height = lineThickness;
            }
            else
            {
                thumb.Height = lineLength;
            }
            //thumb.Template = new ControlTemplate(typeof(Thumb))
            //{
            //    VisualTree = GetFactory(Brushes.LightSlateGray)
            //};
            thumb.BorderThickness = borderThickness;
            thumb.Background = Brushes.Black;
            thumb.BorderBrush = Brushes.Black;

            thumb.DragDelta += Thumb_DragDelta;
        }
        private void SetPointThumbStyle(Thumb thumb)
        {
            Thickness borderThickness = new Thickness(0, 0, 0, 0);
            double lineThickness = 3;
            double lineLength = 25;
            thumb.Width = lineLength;
            thumb.Height = lineLength;
            if (thumb.HorizontalAlignment == HorizontalAlignment.Left)
            {
                borderThickness.Left = lineThickness;
            }
            else if (thumb.HorizontalAlignment == HorizontalAlignment.Right)
            {
                borderThickness.Right = lineThickness;
            }

            if (thumb.VerticalAlignment == VerticalAlignment.Top)
            {
                borderThickness.Top = lineThickness;
            }
            else if (thumb.VerticalAlignment == VerticalAlignment.Bottom)
            {
                borderThickness.Bottom = lineThickness;
            }
            thumb.Template = new ControlTemplate(typeof(Thumb))
            {
                VisualTree = GetFactory(borderThickness)
            };

            thumb.DragDelta += Thumb_DragDelta;
        }
        private FrameworkElementFactory GetFactory(Thickness thickness)
        {
            FrameworkElementFactory fef = new FrameworkElementFactory(typeof(Border));
            fef.SetValue(Border.BorderThicknessProperty, thickness);
            fef.SetValue(Border.BorderBrushProperty, Brushes.Black);
            return fef;
        }

        private void Thumb_DragDelta(object sender, DragDeltaEventArgs e)
        {
            double currentTime = BKMisc.TimeNow().TotalMilliseconds;
            if (currentTime - _lastDrag < _dragInterval)
                return;
            _lastDrag = currentTime;

            if (sender is FrameworkElement thumb)
            {
                System.Drawing.RectangleF dragChange = new();
                if (thumb.HorizontalAlignment == HorizontalAlignment.Left)
                {
                    dragChange.X = (float)e.HorizontalChange;
                    dragChange.Width = -(float)e.HorizontalChange;
                }
                else if (thumb.HorizontalAlignment == HorizontalAlignment.Right)
                {
                    dragChange.Width = (float)e.HorizontalChange;
                }

                if (thumb.VerticalAlignment == VerticalAlignment.Top)
                {
                    dragChange.Y = (float)e.VerticalChange;
                    dragChange.Height = -(float)e.VerticalChange;
                }
                else if (thumb.VerticalAlignment == VerticalAlignment.Bottom)
                {
                    dragChange.Height = (float)e.VerticalChange;
                }

                if (_onDrag != null)
                    _onDrag(dragChange);
            }
        }

        protected override Visual GetVisualChild(int index)
        {
            return _visuals[index];
        }
        protected override int VisualChildrenCount
        {
            get { return _visuals.Count; }
        }
        protected override Size MeasureOverride(Size constraint)
        {
            _contentPresenter.Measure(constraint);
            return _contentPresenter.DesiredSize;
        }

        protected override Size ArrangeOverride(Size finalSize)
        {
            //布局位置和大小
            _contentPresenter.Arrange(new Rect(0, 0, this.AdornedElement.RenderSize.Width, this.AdornedElement.RenderSize.Height));
            return _contentPresenter.DesiredSize;
        }
    }


}
