using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Input;

namespace BKTrans.Controls
{
    public class OCRReplaceItem : InlineUIContainer
    {
        private TextBlock _textBlock;
        private string _srcText;
        private string _replaceText;
        //private bool _isMouseDown = false;

        public string Text => _textBlock.Text;
        public OCRReplaceItem(string srcText, string replaceText) : base()
        {
            _srcText = srcText;
            _replaceText = replaceText;

            _textBlock = new TextBlock();
            _textBlock.Text = _replaceText;
            _textBlock.TextWrapping = TextWrapping.WrapWithOverflow;
            _textBlock.TextDecorations = System.Windows.TextDecorations.Underline;

            Child = _textBlock;
            //_textBlock.MouseLeave += OnMouseLeaveEvent;
            _textBlock.MouseLeftButtonDown += OnMouseLeftButtonDownEvent;
            //_textBlock.PreviewMouseLeftButtonUp += OnMouseLeftButtonUpEvent;
        }

        //private void OnMouseLeftButtonUpEvent(object sender, MouseButtonEventArgs e)
        //{
        //    if (_isMouseDown)
        //    {
        //        if (_textBlock.Text == _srcText)
        //            _textBlock.Text = _replaceText;
        //        else
        //            _textBlock.Text = _srcText;

        //        _isMouseDown = false;
        //    }
        //}

        private void OnMouseLeftButtonDownEvent(object sender, MouseButtonEventArgs e)
        {
            if (_textBlock.Text == _srcText)
                _textBlock.Text = _replaceText;
            else
                _textBlock.Text = _srcText;
        }

        //private void OnMouseLeaveEvent(object sender, MouseEventArgs e)
        //{
        //    _isMouseDown = false;
        //}
    }
}
