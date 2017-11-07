using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Linq;
using Windows.Foundation;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Media;
using TumblerApp.Util;

namespace TumblerApp.Views.Controls.Virtualization
{
    public class VirtualizingLoopItemsPanel : LoopItemsPanel
    {
        #region Virtualization Core

        private ItemContainerGenerator _generator;
        private double _initialTopOfFirstElement;

        public ItemContainerGenerator ItemContainerGenerator
        {
            get
            {
                if (_generator != null) return _generator;

                ItemsControl owner = ItemsControl.GetItemsOwner(this);
                if (owner == null)
                    throw new InvalidOperationException(
                        "VirtualizingPanels must be in the Template of an ItemsControl in order to generate items");

                _generator = owner.ItemContainerGenerator;
                _generator.ItemsChanged += OnItemsChangedInternal;
                return _generator;
            }
        }

        protected virtual void OnClearChildren() { }

        private void OnItemsChangedInternal(
            object sender,
            ItemsChangedEventArgs args)
        {
            InvalidateMeasure();
            if ((NotifyCollectionChangedAction)args.Action == NotifyCollectionChangedAction.Reset)
            {
                Children.Clear();
                ItemContainerGenerator.RemoveAll();
                OnClearChildren();
            }
        }

        #endregion Virtualization Core

        private double _lastCalculatedRealizationRangeAtOffset;
        private bool _isInitialVirtualizationComplete;

        private IndexRange _realizationRange;
        private IndexRange RealizationRange
        {
            get { return _realizationRange; }
            set
            {
                _realizationRange = value;
                _lastCalculatedRealizationRangeAtOffset = OffsetFromInitialPosition;
            }
        }

        public VirtualizingLoopItemsPanel()
        {
        }

        protected override Size ArrangeOverride(Size finalSize)
        {
            finalSize = base.ArrangeOverride(finalSize);
            if (_isInitialVirtualizationComplete || !Children.Any()) return finalSize;

            RealizationRange = CalculateCurrentRealizationRange();

            int allItemsCount = GetParentItemsControl().Items.Count;
            _initialTopOfFirstElement = -1 * allItemsCount * ChildHeight / 2;

            // Virtualize all items that come before the realization range
            VirtualizeRange(0, RealizationRange.Start);

            // Virtualize all items that come after the realization range
            ItemsControl owner = ItemsControl.GetItemsOwner(this);
            int numItemsToVirtualizeAtEnd = owner.Items.Count - RealizationRange.End - 1;
            VirtualizeRange(RealizationRange.End + 1, numItemsToVirtualizeAtEnd);

            _isInitialVirtualizationComplete = true;

            return finalSize;
        }


        protected override void OnScrolled(double movedBy)
        {
            base.OnScrolled(movedBy);

            double movedBySinceLastCalculation = Math.Abs(OffsetFromInitialPosition - _lastCalculatedRealizationRangeAtOffset);
            //Log.d($"--------- Moved by {movedBySinceLastCalculation} (waiting for {ChildHeight})");

            if (movedBySinceLastCalculation < ChildHeight / 2) return;
            Log.d($"----- Moved by more than half the actual height");

            IndexRange oldRealizationRange = RealizationRange;
            IndexRange currentRealizationRange = CalculateCurrentRealizationRange();

            Log.d($"----- OldRange: {oldRealizationRange}, NewRange: {currentRealizationRange}");
            if (oldRealizationRange.Equals(currentRealizationRange)) return;

            bool isScrollingDown = currentRealizationRange.Start < oldRealizationRange.Start;
            Log.d($"----- isScrollingDown = {isScrollingDown}");

            IndexRange toBeRealized;
            IndexRange toBeVirtualized;

            if (isScrollingDown)
            {
                // New items need to be added at the top
                toBeRealized = new IndexRange(
                    currentRealizationRange.Start,
                    oldRealizationRange.Start - 1);
                toBeVirtualized = new IndexRange(
                    currentRealizationRange.End + 1,
                    oldRealizationRange.End);
            }
            else
            {
                // New items need to be added at the bottom
                toBeRealized = new IndexRange(
                    oldRealizationRange.End + 1,
                    currentRealizationRange.End);
                toBeVirtualized = new IndexRange(
                    oldRealizationRange.Start,
                    currentRealizationRange.Start - 1);
            }


            int maxIndex = GetParentItemsControl().Items.Count;
            foreach (IndexRange range in new[] { toBeRealized, toBeVirtualized })
            {
                if (range.Start < 0) range.Start = 0;
                if (range.End > maxIndex) range.End = maxIndex;
            }

            RealizeRange(toBeRealized.Start, toBeRealized.Length);
            VirtualizeRange(toBeVirtualized.Start, toBeVirtualized.Length);

            RealizationRange = currentRealizationRange;
        }


        private bool IsVirtualizedAt(int index)
        {
            GeneratorPosition pos = ItemContainerGenerator.GeneratorPositionFromIndex(index);
            return pos.Offset != 0;
        }


        private void VirtualizeRange(int startIndex, int count)
        {
            if (count <= 0) return;
            GeneratorPosition pos = ItemContainerGenerator.GeneratorPositionFromIndex(startIndex);

            int startChildIndex = pos.Index;
            int endChildIndex = startChildIndex + count - 1;

            /* This will remove the actual UI elements from the screen.  Without this,
             * after the children are virtualized, there is a tiny orange square left on
             * the UI where the element used to be.
             * 
             * Loop backwards because otherwise after you remove the first one
             * the index of the last one changes on you */
            for (int i = endChildIndex; i >= startChildIndex; --i)
            {
                Children.RemoveAt(i);
            }

            // This will actually virtualize the children
            ItemContainerGenerator.Remove(pos, count);

            Log.d($"Just virtualized {count} items starting at index {startIndex}");
            DumpGeneratorContent();
        }

        private void RealizeRange(int startIndex, int count)
        {
            if (count <= 0) return;

            GeneratorPosition pos = ItemContainerGenerator.GeneratorPositionFromIndex(startIndex);
            ItemContainerGenerator.StartAt(pos, GeneratorDirection.Forward, true);

            for (var i = 0; i < count; ++i)
            {
                int itemsControlIndex = startIndex + i;
                pos = ItemContainerGenerator.GeneratorPositionFromIndex(itemsControlIndex);

                if (pos.Offset == 0)
                {
                    Log.e($"Position {(startIndex + i)} has an offset of ZERO!? -- skipping");
                    continue;
                }

                DependencyObject child = ItemContainerGenerator.GenerateNext(out bool isNewlyRealized);
                Log.d("isNewlyRealized = " + isNewlyRealized);
                ItemContainerGenerator.PrepareItemContainer(child);

                Log.d($"Trying to insert:");
                PrintValue(child);

                IList<double> topAndBottoms = GetItemsTopAndBottomOffsets();
                double topOffset = topAndBottoms[itemsControlIndex];

                var uielem = (UIElement)child;
                uielem.RenderTransform = new TranslateTransform { Y = 0 };

                int insertionIndex = pos.Index + pos.Offset;
                Children.Insert(insertionIndex, child as UIElement);
            }

            ItemContainerGenerator.Stop();

            Log.d($"Just realized {count} items starting at index {startIndex}");
            DumpGeneratorContent();
        }


        private void PrintValues()
        {
            for (var i=0; i<Children.Count; ++i)
            {
                PrintValue(Children[i], i);
            }
        }

        private void PrintValue(object child, int i = int.MinValue)
        {
            string val = GetValueFromChild(child);
            Log.d(
                i == int.MinValue
                    ? $"----- UI child has a value of {val}"
                    : $"----- UI child at UI index {i} has a value of {val}");
        }

        private string GetValueFromChild(object child)
        {
            var c = (ListBoxItem)child;
            var dc = (ViewModels.Data)c.DataContext;
            return dc.Title;
        }




        private IndexRange CalculateCurrentRealizationRange()
        {
            double realizationHeight = ActualHeight * 1;

            double realizationRangeTopOffset    = -realizationHeight / 2;
            double realizationRangeBottomOffset =  realizationHeight / 2;

            //Log.d($"realizationHeight = {realizationHeight}");
            //Log.d($"OffsetFromInitialPosition = {OffsetFromInitialPosition}");

            return CalculateIndexRangeFromOffsetRange(
                realizationRangeTopOffset,
                realizationRangeBottomOffset);
        }

        private IndexRange CalculateIndexRangeFromOffsetRange(
            double topOffset,
            double bottomOffset)
        {
//            Log.d($"CalculateIndexRangeFromOffsetRange");

            var childtops = GetItemsTopAndBottomOffsets();
            //Log.d($"|\tchildTopBottoms = {childTopBottoms.ToList().ToDebugString()}");

            int[] indicesInRange = CalculateIndicesInRange(childtops, topOffset, bottomOffset);
            int firstIndexInRange = indicesInRange.Min();
            int lastIndexInRange = indicesInRange.Max();

            var r = new IndexRange(firstIndexInRange, lastIndexInRange);

            Log.d($"|\tFrom range [{topOffset}:{bottomOffset}] we got an index range of {r}");
            return r;
        }

        private static int[] CalculateIndicesInRange(
            IList<double> itemTops,
            double rangeTop,
            double rangeBottom)
        {
            Log.d($"CalculateIndicesInRange(topBottoms (size {itemTops.Count}), {rangeTop}, {rangeBottom}");
            var indicesInRange = new List<int>();

            for (var i = 0; i < itemTops.Count - 1; ++i)
            {
                double top    = itemTops[i];
                double bottom = itemTops[i + 1];

                bool isInRange = top < rangeTop    && bottom > rangeTop      // Straddles top border
                              || top > rangeTop    && bottom < rangeBottom   // Somewhere in the middle
                              || top < rangeBottom && bottom > rangeBottom   // Straddles the bottom border
                              || top < rangeTop    && bottom > rangeBottom;  // Overlaps the entire range

                Log.d($"| \tItem from [{(int)top}:{(int)bottom}]\t at index {i} is {(isInRange ? "" : "NOT")} in range");

                if (isInRange) indicesInRange.Add(i);
            }

            return indicesInRange.ToArray();
        }


        // We don't really need the bottom offsets, because they're clearly repeated
        private IList<double> GetItemsTopAndBottomOffsets()
        {
//            int firstRealizedChildIndex = GetFirstRealizedIndex();
//            UIElement firstRealizedChild = Children[0];
//            Rect firstRealizedChildBounds = GetChildBoundsInThisPanel(firstRealizedChild);
//
//            double missingElementsHeight = firstRealizedChildIndex * ChildHeight;
//            double firstRealizedChildTop = firstRealizedChildBounds.Y - missingElementsHeight + OffsetFromInitialPosition;
//            double topOfFirstChild = firstRealizedChildTop - missingElementsHeight;

//            Log.d($"GetItemsTopAndBottomOffsets");
//            Log.d($"|\t OffsetFromInitialPosition = {OffsetFromInitialPosition}");
//            Log.d($"|\t ChildHeight = {ChildHeight}");
//            Log.d($"|\t firstRealizedChildIndex = {firstRealizedChildIndex}");
//            Log.d($"|\t firstRealizedChildBounds Y = {firstRealizedChildBounds.Y}");
//            Log.d($"|\t firstRealizedChildBounds top = {firstRealizedChildBounds.Top}");
//            Log.d($"|\t firstRealizedChildTop calcualted = {firstRealizedChildTop}");
//            Log.d($"|\t topOfFirstChild = {topOfFirstChild}");

            var childTopBottoms = new List<double> { _initialTopOfFirstElement };

            // Virtualized items and realized items
            int allItemsCount = GetParentItemsControl().Items.Count;
            for (var i = 1; i <= allItemsCount; ++i)
            {
                double topOfLastChild = childTopBottoms[i - 1];
                childTopBottoms.Add(topOfLastChild + ChildHeight);
            }

            return childTopBottoms;
        }





        private int GetFirstRealizedIndex()
        {
            ItemsControl itemsControl = GetParentItemsControl();

            for (var i = 0; i < itemsControl.Items.Count; ++i)
            {
                if (!IsVirtualizedAt(i)) return i;
            }

            return -1;
        }

        private ItemsControl GetParentItemsControl()
        {
            return ItemsControl.GetItemsOwner(this);
        }


        private void DumpGeneratorContent()
        {
            ItemsControl itemsControl = GetParentItemsControl();
            ItemContainerGenerator generator = ItemContainerGenerator;

            Log.d("Generator positions:");

            for (int i = 0; i < itemsControl.Items.Count; i++)
            {
                GeneratorPosition position = generator.GeneratorPositionFromIndex(i);
                Log.d(
                    $"Item index={i} " +
                    $"Generator position: \t" +
                    $"index={position.Index}, \t" +
                    $"offset={position.Offset}");
            }

            Log.d("\n");
        }







        private class IndexRange
        {
            public int Start  { get; set; }
            public int End { get; set; }

            public int Length => End - Start + 1;

            public IndexRange(int start, int end)
            {
                Start = start;
                End = end;
            }

            public override bool Equals(object obj)
            {
                var that = obj as IndexRange;
                if (that == null) return false;

                return this.Start == that.Start &&
                       this.End == that.End;
            }

            public override string ToString() { return $"[{Start}:{End}]"; }
        }
    }
}