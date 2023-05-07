using Microsoft.Xaml.Behaviors;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Input;
using System.Windows.Media;

// https://stackoverflow.com/questions/10493450/drag-drop-row-behavior-on-wpf-datagrid
namespace BKTrans
{
    public static class UIHelpers
    {

        #region find parent

        /// <summary>
        /// Finds a parent of a given item on the visual tree.
        /// </summary>
        /// <typeparam name="T">The type of the queried item.</typeparam>
        /// <param name="child">A direct or indirect child of the
        /// queried item.</param>
        /// <returns>The first parent item that matches the submitted
        /// type parameter. If not matching item can be found, a null
        /// reference is being returned.</returns>
        public static T TryFindParent<T>(DependencyObject child)
          where T : DependencyObject
        {
            //get parent item
            DependencyObject parentObject = GetParentObject(child);

            //we've reached the end of the tree
            if (parentObject == null) return null;

            //check if the parent matches the type we're looking for
            T parent = parentObject as T;
            if (parent != null)
            {
                return parent;
            }
            else
            {
                //use recursion to proceed with next level
                return TryFindParent<T>(parentObject);
            }
        }


        /// <summary>
        /// This method is an alternative to WPF's
        /// <see cref="VisualTreeHelper.GetParent"/> method, which also
        /// supports content elements. Do note, that for content element,
        /// this method falls back to the logical tree of the element.
        /// </summary>
        /// <param name="child">The item to be processed.</param>
        /// <returns>The submitted item's parent, if available. Otherwise
        /// null.</returns>
        public static DependencyObject GetParentObject(DependencyObject child)
        {
            if (child == null) return null;
            ContentElement contentElement = child as ContentElement;

            if (contentElement != null)
            {
                DependencyObject parent = ContentOperations.GetParent(contentElement);
                if (parent != null) return parent;

                FrameworkContentElement fce = contentElement as FrameworkContentElement;
                return fce != null ? fce.Parent : null;
            }

            //if it's not a ContentElement, rely on VisualTreeHelper
            return VisualTreeHelper.GetParent(child);
        }

        #endregion


        #region update binding sources

        /// <summary>
        /// Recursively processes a given dependency object and all its
        /// children, and updates sources of all objects that use a
        /// binding expression on a given property.
        /// </summary>
        /// <param name="obj">The dependency object that marks a starting
        /// point. This could be a dialog window or a panel control that
        /// hosts bound controls.</param>
        /// <param name="properties">The properties to be updated if
        /// <paramref name="obj"/> or one of its childs provide it along
        /// with a binding expression.</param>
        public static void UpdateBindingSources(DependencyObject obj,
                                  params DependencyProperty[] properties)
        {
            foreach (DependencyProperty depProperty in properties)
            {
                //check whether the submitted object provides a bound property
                //that matches the property parameters
                BindingExpression be = BindingOperations.GetBindingExpression(obj, depProperty);
                if (be != null) be.UpdateSource();
            }

            int count = VisualTreeHelper.GetChildrenCount(obj);
            for (int i = 0; i < count; i++)
            {
                //process child items recursively
                DependencyObject childObject = VisualTreeHelper.GetChild(obj, i);
                UpdateBindingSources(childObject, properties);
            }
        }

        #endregion


        /// <summary>
        /// Tries to locate a given item within the visual tree,
        /// starting with the dependency object at a given position. 
        /// </summary>
        /// <typeparam name="T">The type of the element to be found
        /// on the visual tree of the element at the given location.</typeparam>
        /// <param name="reference">The main element which is used to perform
        /// hit testing.</param>
        /// <param name="point">The position to be evaluated on the origin.</param>
        public static T TryFindFromPoint<T>(UIElement reference, Point point)
          where T : DependencyObject
        {
            DependencyObject element = reference.InputHitTest(point)
                                         as DependencyObject;
            if (element == null) return null;
            else if (element is T) return (T)element;
            else return TryFindParent<T>(element);
        }
    }

    public class DataGridBehavior : Behavior<DataGrid>
    {
        private object draggedItem;
        private bool isEditing;
        private bool isDragging;

        public bool IsEditing => isEditing;

        #region DragEnded
        public static readonly RoutedEvent DragEndedEvent =
            EventManager.RegisterRoutedEvent("DragEnded", RoutingStrategy.Bubble, typeof(RoutedEventHandler), typeof(DataGridBehavior));
        public static void AddDragEndedHandler(DependencyObject d, RoutedEventHandler handler)
        {
            UIElement uie = d as UIElement;
            if (uie != null)
                uie.AddHandler(DataGridBehavior.DragEndedEvent, handler);
        }
        public static void RemoveDragEndedHandler(DependencyObject d, RoutedEventHandler handler)
        {
            UIElement uie = d as UIElement;
            if (uie != null)
                uie.RemoveHandler(DataGridBehavior.DragEndedEvent, handler);
        }

        private void RaiseDragEndedEvent()
        {
            var args = new RoutedEventArgs(DataGridBehavior.DragEndedEvent);
            AssociatedObject.RaiseEvent(args);
        }
        #endregion

        #region Popup
        public static readonly DependencyProperty PopupProperty =
            DependencyProperty.Register("Popup", typeof(System.Windows.Controls.Primitives.Popup), typeof(DataGridBehavior));
        public System.Windows.Controls.Primitives.Popup Popup
        {
            get { return (System.Windows.Controls.Primitives.Popup)GetValue(PopupProperty); }
            set { SetValue(PopupProperty, value); }
        }
        #endregion

        protected override void OnAttached()
        {
            base.OnAttached();

            AssociatedObject.BeginningEdit += OnBeginEdit;
            AssociatedObject.CellEditEnding += OnEndEdit;
            AssociatedObject.MouseLeftButtonUp += OnMouseLeftButtonUp;
            AssociatedObject.PreviewMouseLeftButtonDown += OnMouseLeftButtonDown;
            AssociatedObject.MouseMove += OnMouseMove;
        }

        protected override void OnDetaching()
        {
            base.OnDetaching();

            AssociatedObject.BeginningEdit -= OnBeginEdit;
            AssociatedObject.CellEditEnding -= OnEndEdit;
            AssociatedObject.MouseLeftButtonUp -= OnMouseLeftButtonUp;
            AssociatedObject.MouseLeftButtonDown -= OnMouseLeftButtonDown;
            AssociatedObject.MouseMove -= OnMouseMove;

            Popup = null;
            draggedItem = null;
            isEditing = false;
            isDragging = false;
        }

        private void OnBeginEdit(object sender, DataGridBeginningEditEventArgs e)
        {
            isEditing = true;
            //in case we are in the middle of a drag/drop operation, cancel it...
            if (isDragging) ResetDragDrop();
        }

        private void OnEndEdit(object sender, DataGridCellEditEndingEventArgs e)
        {
            isEditing = false;
        }

        private void OnMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            if (isEditing) return;

            var row = UIHelpers.TryFindFromPoint<DataGridRow>((UIElement)sender, e.GetPosition(AssociatedObject));
            if (row == null || row.IsEditing || row.DataContext == CollectionView.NewItemPlaceholder) return;

            //set flag that indicates we're capturing mouse movements
            isDragging = true;
            draggedItem = row.Item;
        }

        private void OnMouseLeftButtonUp(object sender, MouseButtonEventArgs e)
        {
            if (!isDragging || isEditing)
                return;

            //get the target item
            var targetItem = AssociatedObject.SelectedItem;

            if (targetItem == null || !ReferenceEquals(draggedItem, targetItem))
            {
                //get target index
                var targetIndex = ((AssociatedObject).ItemsSource as IList).IndexOf(targetItem);

                //remove the source from the list
                ((AssociatedObject).ItemsSource as IList).Remove(draggedItem);

                //move source at the target's location
                ((AssociatedObject).ItemsSource as IList).Insert(targetIndex, draggedItem);

                //select the dropped item
                AssociatedObject.SelectedItem = draggedItem;

                // refresh
                CollectionViewSource.GetDefaultView((AssociatedObject).ItemsSource).Refresh();

                RaiseDragEndedEvent();
            }

            //reset
            ResetDragDrop();
        }

        private void ResetDragDrop()
        {
            isDragging = false;
            Popup.IsOpen = false;
            AssociatedObject.IsReadOnly = false;
        }

        private void OnMouseMove(object sender, MouseEventArgs e)
        {
            if (!isDragging || e.LeftButton != MouseButtonState.Pressed)
                return;

            Popup.DataContext = draggedItem;
            //display the popup if it hasn't been opened yet
            if (!Popup.IsOpen)
            {
                //switch to read-only mode
                AssociatedObject.IsReadOnly = true;

                //make sure the popup is visible
                Popup.IsOpen = true;
            }

            var popupSize = new Size(Popup.ActualWidth, Popup.ActualHeight);
            Popup.PlacementRectangle = new Rect(e.GetPosition(AssociatedObject), popupSize);

            //make sure the row under the grid is being selected
            var position = e.GetPosition(AssociatedObject);
            var row = UIHelpers.TryFindFromPoint<DataGridRow>(AssociatedObject, position);
            if (row != null) AssociatedObject.SelectedItem = row.Item;
        }
    }
}
