﻿using System;
using System.Linq;
using Windows.Foundation;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Media.Animation;
using TumblerApp.Util;

namespace TumblerApp.Views.Controls
{
    public class LoopItemsPanel : Panel
    {
        /// <summary>The time it takes to move to a new element</summary>
        private const double AnimationDurationInMillis = 200;
        private static readonly TimeSpan AnimationDuration = TimeSpan.FromMilliseconds(AnimationDurationInMillis);

        #region Members
        /// <summary>Slider control used to make animating easier</summary>
        private readonly Slider _sliderVertical;

        /// <summary>
        ///     Indicates that the list is currently scrolled past the last item
        /// </summary>
        private bool _hasScrolledPastEnd;
        /// <summary>
        ///     Indicates that for the current manipulation, whether or not we
        ///     have received a motion delta that is both past the end of the list
        ///     and generated by inertia, and not tracking the users finger.
        /// 
        ///     It's purpose is to allow us to ignore inertia after the user has
        ///     gone beyond the end of the list.
        /// </summary>
        private bool _isFirstInertialCall;

        /// <summary>
        ///     The initial position is in the exact middle of the list, where this
        ///     offset is zero.  If the user scrolls above the middle, this offset
        ///     is positive, and if they scroll below the middle, this offset is
        ///     negative.
        /// </summary>
        protected double OffsetFromInitialPosition;

        /// <summary>True when ArrangeOverride has run, false before</summary>
        private bool _templateApplied;
        #endregion Members

        public LoopItemsPanel()
        {
            ManipulationMode = (ManipulationModes.TranslateY | ManipulationModes.TranslateInertia);
            ManipulationDelta += OnManipulationDelta;
            ManipulationCompleted += OnManipulationCompleted;
            Tapped += OnTapped;

            _sliderVertical = new Slider
            {
                SmallChange = 0.0000000001,
                Minimum = double.MinValue,
                Maximum = double.MaxValue,
                StepFrequency = 0.0000000001
            };
            _sliderVertical.ValueChanged += AnimationSliderValueChanged;
        }

        #region Properties
        private bool IsMovingUp => OffsetFromInitialPosition > 0;
        private bool IsMovingDown => OffsetFromInitialPosition < 0;

        /// <summary>
        ///     Allow the user to scroll a small amount past the end of the items, so that scrolling 
        ///     back to the end item looks natural instead of only moving a few pixels.
        /// </summary>
        private double AllowedDistancePastEnd => ChildHeight;

        /// <summary>
        ///     Height of an arbitrary child.  This assumes that all children
        ///     have the same height. Must be 1d to fire first ArrangeOverride
        /// </summary>
        public double ChildHeight { get; set; } = 1d;

        public double AboveCenterChild => ActualHeight / 2 - ChildHeight / 2;
        public double BelowCenterChild => ActualHeight / 2 + ChildHeight / 2;

        /// <summary>The number of children this panel currently has</summary>
        private int ChildCount => Children?.Count ?? 0;
        private int ShownChildCount => (ChildrenToShow < 1) ? ChildCount : ChildrenToShow;
        #endregion Properties

        #region Layout
        /// <summary>
        ///     Call each child's Measure() method to update its DesiredSize
        /// </summary>
        protected override Size MeasureOverride(Size availableSize)
        {
            //Log.d($"MeasureOverride: available size is {availableSize}");

            Size baseSize = base.MeasureOverride(availableSize);

            var infinity = new Size(double.PositiveInfinity, double.PositiveInfinity);
            foreach (UIElement child in Children) child.Measure(infinity);

            Size firstChildSize = Children[0]?.DesiredSize ?? new Size(0,0);
            ChildHeight = firstChildSize.Height;
            Clip = new RectangleGeometry { Rect = new Rect(0, 0, baseSize.Width, baseSize.Height) };

            return firstChildSize;
        }

        /// <summary>
        ///     Arrange all items.  Called after MeasureOverride.
        /// 
        ///     This method is where each child is explicitly given a TranslateTransform
        ///     so that they can be vertically moved.
        /// </summary>
        protected override Size ArrangeOverride(Size finalSize)
        {
            //Log.d($"ArrangeOverride: final size is {finalSize}");

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

            _templateApplied = true;
            return finalSize;
        }

        /// <summary>
        ///     Get the top offset of the first child so that all of the children collectively
        ///     will be properly vertically aligned in this panel (Top, Center, Bottom).
        /// </summary>
        /// <param name="parentSize">The current size of this panel</param>
        private double GetFirstChildTopOffset(Size parentSize)
        {
            if (VerticalContentAlignment != VerticalAlignment.Center &&
                VerticalContentAlignment != VerticalAlignment.Bottom) return 0;

            double totalChildHeight = Children.Sum(child => child.DesiredSize.Height);

            return VerticalContentAlignment == VerticalAlignment.Center
                ? parentSize.Height / 2 - totalChildHeight / 2
                : parentSize.Height - totalChildHeight;
        }

        /// <summary>
        ///     Get the left offset of any child based on the current value of 
        ///     HorizontalContentAlignment (Left, Center, Right)
        /// </summary>
        /// <param name="parentSize">The current size of this panel</param>
        /// <param name="childSize">The current size of the child to be aligned</param>
        /// <returns>The proper left offset to correctly align the child in question</returns>
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
                        nameof(HorizontalContentAlignment), 
                        HorizontalContentAlignment, 
                        null);
            }
        }
        #endregion Layout

        #region Movement
        protected virtual void OnTapped(object sender, TappedRoutedEventArgs args)
        {
            if (Children == null || Children.Count == 0) return;

            double tappedPointY = args.GetPosition(this).Y;
            //Log.d($"OnTapped: tapped at Y position {tappedPointY}");

            foreach (UIElement child in Children)
            {
                Rect childBoundsInThisPanel = GetChildBoundsInThisPanel(child);
                double childBottom = childBoundsInThisPanel.Y;
                double childTop = childBoundsInThisPanel.Y + childBoundsInThisPanel.Height;

                bool childVerticallyIntersectsTapPoint = 
                    childBottom  <= tappedPointY && 
                    tappedPointY <= childTop;

                if (childVerticallyIntersectsTapPoint)
                {
                    ScrollToItem(child, childBoundsInThisPanel);
                    break;
                }
            }
        }

        /// <summary>
        ///     Get the childs bounding rectangle relaive to this panel
        /// </summary>
        protected Rect GetChildBoundsInThisPanel(UIElement child)
        {
            GeneralTransform toThisPanelCoordPlaneMap = child.TransformToVisual(this);
            var childDesiredBounds = new Rect(0, 0, child.DesiredSize.Width, child.DesiredSize.Height);
            return toThisPanelCoordPlaneMap.TransformBounds(childDesiredBounds);
        }


        public void ScrollToIndex(int index, double duration = AnimationDurationInMillis)
        {

            if (SelectedIndex != index)
            {
                // Setting this property will cause another invocation of this method
                SelectedIndex = index;
                return;
            }

            //Log.d($"ScrollToIndex: at index {index} in {duration} millis");

            if (!_templateApplied || index >= ChildCount || index < 0) return;
            UIElement selectedItem = Children[index];
            if (selectedItem == null) return;

            ScrollToItem(selectedItem, GetChildBoundsInThisPanel(selectedItem), duration);
        }

        /// <summary>
        ///     Scroll to a particular child item
        /// </summary>
        /// <param name="selectedItem">The child to be scrolled to</param>
        /// <param name="childBoundsInThisPanel">
        ///     The rectangular bounds of selectedItem, relative to this panel.
        /// </param>
        /// <param name="duration">The amount of time in milliseconds to take to animate to the passed item</param>
        protected void ScrollToItem(UIElement selectedItem, Rect childBoundsInThisPanel, double duration = AnimationDurationInMillis)
        {
            if (!_templateApplied) return;

            // TranslateTransform was explicitly set in ArrangeOverride
            var transform = (TranslateTransform)selectedItem.RenderTransform;
            if (transform == null) return;

            double centerTopOffset = (ActualHeight / 2d) - (ChildHeight) / 2d;
            double deltaOffset = centerTopOffset - childBoundsInThisPanel.Y;

            UpdatePositionsWithAnimation(
                transform.Y,
                transform.Y + deltaOffset, 
                AnimationDuration);
        }

        /// <summary>
        ///     Update the current positions of all elements to a certain new offset
        ///     with a nice animation.
        /// 
        ///     This method is used only when the user is not dragging.  For example, 
        ///     when the user taps on on element or after they drag and do not exactly
        ///     center one child so we "snap" to the closest child.
        /// </summary>
        protected void UpdatePositionsWithAnimation(double fromOffset, double toOffset, TimeSpan duration)
        {
            var storyboard = new Storyboard();
            var animationSnap = new DoubleAnimation
            {
                EnableDependentAnimation = true,
                From = fromOffset,
                To = toOffset,
                Duration = duration,
                //EasingFunction = new ExponentialEase { EasingMode = EasingMode.EaseInOut }
            };

            storyboard.Children.Add(animationSnap);

            Storyboard.SetTarget(animationSnap, _sliderVertical);
            Storyboard.SetTargetProperty(animationSnap, "Value");

            _sliderVertical.ValueChanged -= AnimationSliderValueChanged;
            _sliderVertical.Value = fromOffset;
            _sliderVertical.ValueChanged += AnimationSliderValueChanged;

            storyboard.Begin();
            //Log.d($"Started animation from {fromOffset} to {toOffset}");
        }

        /// <summary>
        ///     This event is attached to the _sliderVertical object so that we can
        ///     mimic its animation to make ours look natural
        /// </summary>
        private void AnimationSliderValueChanged(object sender, RangeBaseValueChangedEventArgs e)
        {
            UpdatePositions(e.NewValue - e.OldValue);
        }

        /// <summary>
        ///     The user has moved their finger some tiny amount.
        ///     This method will be called a large number of times while
        ///     the user is in the middle of their swipe.
        /// </summary>
        protected virtual void OnManipulationDelta(
            object sender, 
            ManipulationDeltaRoutedEventArgs e)
        {
            if (e == null) return;

            Point translation = e.Delta.Translation;
            double offsetDelta = translation.Y / 2;

            /* The absolute top or bottom of the list.  An offset greater than this will
             * scroll beyond all items */
            double offsetToEndOfList = ChildHeight * (ChildCount - 1) / 2;

            // Prevent scrolling too far past end of items
            double currentOffset = Math.Abs(OffsetFromInitialPosition + offsetDelta);
            double maxAllowedOffset = offsetToEndOfList + AllowedDistancePastEnd;

            //Log.d($"\t offsetDelta = {offsetDelta}");
            //Log.d($"\t OffsetFromInitialPosition = {_offsetFromInitialPosition}");
            //Log.d($"\t maxAllowedOffset = {maxAllowedOffset}");

            if (ShouldLoopChildren)
            {
                /* If we're looping, the behavior is somewhat simpler, just move 
                 * everything by the amount the users finger has moved */
                UpdatePositions(offsetDelta);
                return;
            }

            if (_hasScrolledPastEnd)
            {
                /* Inertia at the end of the list leads to a very slow feeling.
                 * It takes quite awhile for the inertia to finish, so even though
                 * the snap back is quick, it gives a laggy feeling to the control.
                 * To fix this, we'll ignore inertia after we've gone past the end
                 * of the list */
                if (e.IsInertial)
                {
                    if (_isFirstInertialCall)
                    {
                        SnapBackFromDragPastEnd();
                        _isFirstInertialCall = false;
                    }
                    return;
                }

                /* If we've scrolled past the end, we want to allow them to 
                 * continue to scroll, but as they scroll farther and farther, 
                 * the list should be slower to respond to their drag, as if 
                 * they're dragging through molasses. */
                const double viscosityConstant = 2;
                double amountPastMaxAllowedOffset = currentOffset - maxAllowedOffset;

                UpdatePositions(offsetDelta / amountPastMaxAllowedOffset * viscosityConstant);
                return;
            }

            _hasScrolledPastEnd = currentOffset > maxAllowedOffset;
            UpdatePositions(offsetDelta);
            //Log.d($"\thas scrolled past end => {currentOffset} > {maxAllowedOffset} => {_hasScrolledPastEnd}");
        }

        /// <summary>
        ///     Increments the current offset by offsetDelta and updates the positions of all children
        ///     to this new offset.
        ///     This method contains the logic for "looping" the children from one
        ///     end of the container to the other if ShouldLoopChildren is true.
        /// </summary>
        protected void UpdatePositions(double offsetDelta)
        {
            //Log.d($"UpdatePositions: moving by {offsetDelta}");

            double maxLogicalHeight = ShownChildCount * ChildHeight;
            OffsetFromInitialPosition = (OffsetFromInitialPosition + offsetDelta) % maxLogicalHeight;

            if (!ShouldLoopChildren)
            {
                // Update all items to new offset
                UpdatePositionsForIndices(0, ChildCount, OffsetFromInitialPosition);
                return;
            }

            // Get the correct number item
            var itemNumberSeparator = (int)(Math.Abs(OffsetFromInitialPosition) / ChildHeight);

            int indexToMove = IsMovingUp
                ? ChildCount - itemNumberSeparator - 1
                : itemNumberSeparator;

            double offsetBefore = IsMovingUp
                ? OffsetFromInitialPosition - maxLogicalHeight
                : OffsetFromInitialPosition;

            double offsetAfter = IsMovingUp
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
             * Address this by checking for an offset that is a multiple of the child height and 
             * properly adjusting the indexToMove.  This will only happen on the final call because 
             * the first call will already have some movement happening (a small offset).
             */
            if (OffsetFromInitialPosition > 0 && OffsetFromInitialPosition % ChildHeight == 0)
            {
                indexToMove++;
            }

            // Items that must be before
            UpdatePositionsForIndices(indexToMove, ChildCount, offsetBefore);

            // Items that must be after
            UpdatePositionsForIndices(0, indexToMove, offsetAfter);
        }

        /// <summary>
        ///     Translate items to a new offset
        /// </summary>
        private void UpdatePositionsForIndices(int startIndex, int endIndex, double offset)
        {
            for (int i = startIndex; i < endIndex; ++i)
            {
                UIElement child = Children[i];
                var transform = (TranslateTransform)child.RenderTransform;

                if (transform == null) continue;
                transform.Y = offset;
            }
        }



        protected virtual void OnManipulationCompleted(object sender, ManipulationCompletedRoutedEventArgs args)
        {
            //Log.d($"------- Manipulation completed");
            if (_hasScrolledPastEnd) SnapBackFromDragPastEnd();
            else if (SnapToItem) SanpToClosestItem();

            _isFirstInertialCall = true;
        }

        /// <summary>
        ///     When a user drag completes, if SnapToItem is true, we need to snap to
        ///     the child which is currently closest to the center of the container.
        /// </summary>
        private void SanpToClosestItem()
        {
            int closestIndex = -1;
            var closestTopToCenter = double.MaxValue;

            for (int i = 0; i < ChildCount; ++i)
            {
                UIElement child = Children[i];
                Rect childBoundsInThisPanel = GetChildBoundsInThisPanel(child);
                double childDistanceFromCenter = Math.Abs(AboveCenterChild - childBoundsInThisPanel.Y);
                //Log.d($"Child at index {i} is {childDistanceFromCenter} units away from center");

                if (childDistanceFromCenter < closestTopToCenter)
                {
                    closestTopToCenter = childDistanceFromCenter;
                    closestIndex = i;
                    //Log.d($"Index {i} is the closest so far with a value of ???");
                }
            }

            //Log.d($"Scrolling to closest index {closestIndex} with a value of ???");
            SelectedIndex = closestIndex;
        }

        private void SnapBackFromDragPastEnd()
        {
            if (!_hasScrolledPastEnd) return;

            //            double startOffset = _offsetFromInitialPosition;
            //            double endOffset = _offsetFromInitialPosition + AllowedDistancePastEnd;
            //            Log.d($"Scrilling back to end => from {startOffset} to {endOffset}");
            //
            //            UpdatePositionsWithAnimation(startOffset, endOffset, AnimationDuration);

            int index = IsMovingUp ? 0 : ChildCount - 1;
            ScrollToIndex(index, AnimationDurationInMillis);

            _hasScrolledPastEnd = false;
        }
        #endregion Movement

        #region Dependency Properties
        public HorizontalAlignment HorizontalContentAlignment
        {
            get { return (HorizontalAlignment)GetValue(HorizontalContentAlignmentProperty); }
            set { SetValue(HorizontalContentAlignmentProperty, value); }
        }
        public static readonly DependencyProperty HorizontalContentAlignmentProperty = DependencyProperty.Register(
            "HorizontalContentAlignment", 
            typeof(HorizontalAlignment), 
            typeof(LoopItemsPanel),
            new PropertyMetadata(HorizontalAlignment.Center));


        public VerticalAlignment VerticalContentAlignment
        {
            get { return (VerticalAlignment)GetValue(VerticalContentAlignmentProperty); }
            set { SetValue(VerticalContentAlignmentProperty, value); }
        }
        public static readonly DependencyProperty VerticalContentAlignmentProperty = DependencyProperty.Register(
            "VerticalContentAlignment", 
            typeof(VerticalAlignment), 
            typeof(LoopItemsPanel),
            new PropertyMetadata(HorizontalAlignment.Center));

        public int ChildrenToShow
        {
            get { return (int)GetValue(ChildrenToShowProperty); }
            set { SetValue(ChildrenToShowProperty, value); }
        }
        public static readonly DependencyProperty ChildrenToShowProperty = DependencyProperty.Register(
            "ChildrenToShow", 
            typeof(int), 
            typeof(LoopItemsPanel),
            new PropertyMetadata(-1));

        /// <summary>
        /// Whether or not to loop the children from one end of the container to the other for an infinite scroll.
        /// </summary>
        public bool ShouldLoopChildren
        {
            get { return (bool)GetValue(ShouldLoopChildrenProperty); }
            set { SetValue(ShouldLoopChildrenProperty, value); }
        }
        public static readonly DependencyProperty ShouldLoopChildrenProperty = DependencyProperty.Register(
            "ShouldLoopChildren",
            typeof(bool), 
            typeof(LoopItemsPanel),
            new PropertyMetadata(false));

        /// <summary>
        /// The child that is currently selected, in the center of this panel.
        /// </summary>
        public int SelectedIndex
        {
            get { return (int)GetValue(SelectedIndexProperty); }
            set { SetValue(SelectedIndexProperty, value); }
        }
        public static readonly DependencyProperty SelectedIndexProperty = DependencyProperty.Register(
            "SelectedIndex", 
            typeof(int), 
            typeof(LoopItemsPanel),
            new PropertyMetadata(0, (sender, args) =>
            {
                var panel = (LoopItemsPanel) sender;
                panel.ScrollToIndex(panel.SelectedIndex);
            }));

        /// <summary>
        /// Whether or not the panel should snap so that when a user's drag stops some item is 
        /// always directly in the center of (selected in) this panel.
        /// </summary>
        public bool SnapToItem
        {
            get { return (bool)GetValue(SnapToItemProperty); }
            set { SetValue(SnapToItemProperty, value); }
        }
        public static readonly DependencyProperty SnapToItemProperty = DependencyProperty.Register(
            "SnapToItem", 
            typeof(bool), 
            typeof(LoopItemsPanel),
            new PropertyMetadata(true));
        #endregion Dependency Properties
    }
}
