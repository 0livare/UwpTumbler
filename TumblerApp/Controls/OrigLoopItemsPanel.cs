using System;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using Windows.Foundation;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Media.Animation;

namespace TumblerApp.Controls
{
    public class OrigLoopItemsPanel : Panel, INotifyPropertyChanged
    {
        /// <summary>The time it takes to move to a new element</summary>
        private static readonly TimeSpan AnimationDuration = TimeSpan.FromMilliseconds(600);
        /// <summary>Slider control used to make animating easier</summary>
        private readonly Slider sliderVertical;
        /// <summary>Separating offset</summary>
        private double OffsetFromInitialPosition { get; set; }

        /// <summary>
        /// Height of an arbitrary child.  This assumes that all children 
        /// have the same height. Must be 1d to fire first ArrangeOverride
        /// </summary>
        private double _childHeight = 1d;
        public double ChildHeight
        {
            get { return _childHeight; }
            set
            {
                if (value - _childHeight == 0) return;
                _childHeight = value;
                OnPropertyChanged();
                OnPropertyChanged(nameof(AboveCenterChild));
                OnPropertyChanged(nameof(BelowCenterChild));
            }
        }

        public double AboveCenterChild => ActualHeight / 2 - ChildHeight / 2;
        public double BelowCenterChild => ActualHeight / 2 + ChildHeight / 2;

        /// <summary>True when ArrangeOverride has run, false before</summary>
        private bool TemplateApplied { get; set; }
        /// <summary>The number of children this panel currently has</summary>
        private int ChildCount => Children?.Count ?? 0;
        private int ShownChildCount => (ChildrenToShow < 1) ? ChildCount : ChildrenToShow;


        public OrigLoopItemsPanel()
        {
            ManipulationMode = (ManipulationModes.TranslateY | ManipulationModes.TranslateInertia);
            ManipulationDelta += OnManipulationDelta;
            ManipulationCompleted += OnManipulationCompleted;
            Tapped += OnTapped;

            sliderVertical = new Slider
            {
                SmallChange = 0.0000000001,
                Minimum = double.MinValue,
                Maximum = double.MaxValue,
                StepFrequency = 0.0000000001
            };
            sliderVertical.ValueChanged += OnVerticalOffsetChanged;
        }

        /// <summary>
        /// Arrange all items
        /// </summary>
        protected override Size ArrangeOverride(Size finalSize)
        {
            // Clip to ensure items dont override container
            Clip = new RectangleGeometry { Rect = new Rect(0, 0, finalSize.Width, finalSize.Height) };
            double positionTop = GetFirstChildTopOffset(finalSize);

            // Must Create looping items count
            foreach (UIElement child in Children)
            {
                if (child == null) continue;

                Size childDesiredSize = child.DesiredSize;
                if (double.IsNaN(childDesiredSize.Width)
                    || double.IsNaN(childDesiredSize.Height)) continue;

                var childsDesiredBounds = new Rect(
                    GetChildLeftOffset(finalSize, childDesiredSize),
                    positionTop,
                    childDesiredSize.Width,
                    childDesiredSize.Height);

                child.Arrange(childsDesiredBounds);

                // Explicitly set internal RenderTransform to TranslateTransform 
                // to handle vertical movement
                child.RenderTransform = new TranslateTransform();
                positionTop += childDesiredSize.Height;
            }

            TemplateApplied = true;
            return finalSize;
        }

        private double GetFirstChildTopOffset(Size parentSize)
        {
            if (VerticalContentAlignment != VerticalAlignment.Center &&
                VerticalContentAlignment != VerticalAlignment.Bottom) return 0;

            double totalChildHeight = Children.Sum(child => child.DesiredSize.Height);

            return VerticalContentAlignment == VerticalAlignment.Center
                ? parentSize.Height / 2 - totalChildHeight / 2
                : parentSize.Height - totalChildHeight;
        }

        private double GetChildLeftOffset(Size parentSize, Size childSize)
        {
            switch (HorizontalContentAlignment)
            {
                case HorizontalAlignment.Center: return parentSize.Width / 2 - childSize.Width / 2;
                case HorizontalAlignment.Right: return parentSize.Width - childSize.Width;
                case HorizontalAlignment.Left: return 0;
                case HorizontalAlignment.Stretch: return 0;
                default:
                    throw new ArgumentOutOfRangeException(
               nameof(HorizontalContentAlignment), HorizontalContentAlignment, null);
            }
        }

        /// <summary>
        /// Call each child's Measure() method to update its DesiredSize
        /// </summary>
        protected override Size MeasureOverride(Size availableSize)
        {
            Size baseSize = base.MeasureOverride(availableSize);

            var infinity = new Size(double.PositiveInfinity, double.PositiveInfinity);
            foreach (UIElement child in Children) child.Measure(infinity);

            if (Children[0] != null) ChildHeight = Children[0].DesiredSize.Height;

            double clipHeight = (ChildrenToShow < 1)
                ? baseSize.Height
                : Math.Min(ShownChildCount * ChildHeight, baseSize.Height);
            Clip = new RectangleGeometry { Rect = new Rect(0, 0, baseSize.Width, clipHeight) };

            return baseSize;
        }

        private void OnTapped(object sender, TappedRoutedEventArgs args)
        {
            if (Children == null || Children.Count == 0) return;

            double tappedPointY = args.GetPosition(this).Y;
            foreach (UIElement child in Children)
            {
                GeneralTransform toThisPanelCoordPlaneMap = child.TransformToVisual(this);
                var childDesiredSize = new Rect(0, 0, child.DesiredSize.Width, child.DesiredSize.Height);
                Rect childBoundsInThisPanel = toThisPanelCoordPlaneMap.TransformBounds(childDesiredSize);

                double childBottom = childBoundsInThisPanel.Y;
                double childTop = childBoundsInThisPanel.Y + childBoundsInThisPanel.Height;

                bool childVerticallyIntersectsTapPoint = childBottom <= tappedPointY && tappedPointY <= childTop;
                if (childVerticallyIntersectsTapPoint)
                {
                    ScrollToSelectedIndex(child, childBoundsInThisPanel);
                    break;
                }
            }
        }

        private void ScrollToSelectedIndex(UIElement selectedItem, Rect childBoundsInThisPanel)
        {
            if (!TemplateApplied) return;

            // TranslateTransform was explicitly set in ArrangeOverride
            var transform = (TranslateTransform)selectedItem.RenderTransform;
            if (transform == null) return;

            double centerTopOffset = (ActualHeight / 2d) - (ChildHeight) / 2d;
            double deltaOffset = centerTopOffset - childBoundsInThisPanel.Y;

            UpdatePositionsWithAnimation(transform.Y, transform.Y + deltaOffset);
        }

        /// <summary>
        /// Updating with an animation (after a tap)
        /// </summary>
        private void UpdatePositionsWithAnimation(double fromOffset, double toOffset)
        {
            var storyboard = new Storyboard();
            var animationSnap = new DoubleAnimation
            {
                EnableDependentAnimation = true,
                From = fromOffset,
                To = toOffset,
                Duration = AnimationDuration,
                EasingFunction = new ExponentialEase { EasingMode = EasingMode.EaseInOut }
            };

            storyboard.Children.Add(animationSnap);

            Storyboard.SetTarget(animationSnap, sliderVertical);
            Storyboard.SetTargetProperty(animationSnap, "Value");

            sliderVertical.ValueChanged -= OnVerticalOffsetChanged;
            sliderVertical.Value = fromOffset;
            sliderVertical.ValueChanged += OnVerticalOffsetChanged;

            storyboard.Begin();
        }

        private void OnVerticalOffsetChanged(object sender, RangeBaseValueChangedEventArgs e)
        {
            UpdatePositions(e.NewValue - e.OldValue);
        }

        /// <summary>
        /// On manipulation delta
        /// </summary>
        private void OnManipulationDelta(object sender, ManipulationDeltaRoutedEventArgs e)
        {
            if (e == null) return;
            Point translation = e.Delta.Translation;
            UpdatePositions(translation.Y / 2);
        }

        /// <summary>
        /// Updating position
        /// </summary>
        private void UpdatePositions(double offsetDelta)
        {
            double maxLogicalHeight = ShownChildCount * ChildHeight;

            // Reaffect correct offsetSeparator
            OffsetFromInitialPosition = (OffsetFromInitialPosition + offsetDelta) % maxLogicalHeight;

            // Get the correct number item
            var itemNumberSeparator = (int)(Math.Abs(OffsetFromInitialPosition) / ChildHeight);

            int indexToMove = (OffsetFromInitialPosition > 0)
                ? ChildCount - itemNumberSeparator - 1
                : itemNumberSeparator;

            double offsetBefore = (OffsetFromInitialPosition > 0)
                ? OffsetFromInitialPosition - maxLogicalHeight
                : OffsetFromInitialPosition;

            double offsetAfter = (OffsetFromInitialPosition > 0)
                ? OffsetFromInitialPosition
                : OffsetFromInitialPosition + maxLogicalHeight;

            /*
             * When downward motion is completing, the last call to this method will calculate 
             * itemNumberSeparator to have the next value, causing indexToMove to have the previous
             * value.  This is because on the final call when motion is complete, the items are in 
             * the next position.
             * 
             * This causes odd behavior where the items shift upward because the item that is supposed 
             * be displayed on the bottom instead moves above the top item.
             * 
             * Address this buy checking for an offset that is a multiple of the child height and 
             * properly adjusting the indexToMove.  This will only happen on the final call because 
             * the first call will already have some movement happening (a small offset).
             */
            if (OffsetFromInitialPosition > 0 && OffsetFromInitialPosition % ChildHeight == 0)
            {
                indexToMove++;
            }

            // Items that must be before
            UpdatePosition(indexToMove, ChildCount, offsetBefore);

            // Items that must be after
            UpdatePosition(0, indexToMove, offsetAfter);
        }

        /// <summary>
        /// Translate items to a new offset
        /// </summary>
        private void UpdatePosition(int startIndex, int endIndex, double offset)
        {
            for (int i = startIndex; i < endIndex; ++i)
            {
                UIElement child = Children[i];
                var transform = (TranslateTransform)child.RenderTransform;

                if (transform == null) continue;
                transform.Y = offset;
            }
        }


        private void OnManipulationCompleted(object sender, ManipulationCompletedRoutedEventArgs args)
        {

        }






        #region Dependency Properties

        public HorizontalAlignment HorizontalContentAlignment
        {
            get { return (HorizontalAlignment)GetValue(HorizontalContentAlignmentProperty); }
            set { SetValue(HorizontalContentAlignmentProperty, value); }
        }

        // Using a DependencyProperty as the backing store for HorizontalContentAlignment.  This enables animation, styling, binding, etc...
        public static readonly DependencyProperty HorizontalContentAlignmentProperty =
            DependencyProperty.Register("HorizontalContentAlignment", typeof(HorizontalAlignment), typeof(OrigLoopItemsPanel),
                new PropertyMetadata(HorizontalAlignment.Center));


        public VerticalAlignment VerticalContentAlignment
        {
            get { return (VerticalAlignment)GetValue(VerticalContentAlignmentProperty); }
            set { SetValue(VerticalContentAlignmentProperty, value); }
        }

        // Using a DependencyProperty as the backing store for VerticalContentAlignment.  This enables animation, styling, binding, etc...
        public static readonly DependencyProperty VerticalContentAlignmentProperty =
            DependencyProperty.Register("VerticalContentAlignment", typeof(VerticalAlignment), typeof(OrigLoopItemsPanel),
                new PropertyMetadata(HorizontalAlignment.Center));

        public int ChildrenToShow
        {
            get { return (int)GetValue(ChildrenToShowProperty); }
            set { SetValue(ChildrenToShowProperty, value); }
        }

        // Using a DependencyProperty as the backing store for ChildrenToShow.  This enables animation, styling, binding, etc...
        public static readonly DependencyProperty ChildrenToShowProperty =
            DependencyProperty.Register("ChildrenToShow", typeof(int), typeof(OrigLoopItemsPanel),
                new PropertyMetadata(-1));


        #endregion Dependency Properties

        public event PropertyChangedEventHandler PropertyChanged;

        protected virtual void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}
