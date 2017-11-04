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
//            Log.d($"--------- Moved by {movedBySinceLastCalculation}");

            if (movedBySinceLastCalculation < ChildHeight) return;
            Log.d($"----- Moved by more than half the actual height");

            IndexRange oldRealizationRange = RealizationRange;
            IndexRange currentRealizationRange = CalculateCurrentRealizationRange();

            bool isScrollingDown = currentRealizationRange.Start < oldRealizationRange.Start;

            Log.d($"----- OldRange: {oldRealizationRange}, NewRange: {currentRealizationRange}");
            Log.d($"----- isScrollingDown = {isScrollingDown}");

            if (oldRealizationRange.Equals(currentRealizationRange)) return;


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

            PrintValues();

            for (var i = 0; i < count; ++i)
            {
                if (pos.Offset == 0) continue;

                DependencyObject child = ItemContainerGenerator.GenerateNext(out bool isNewlyRealized);
                Log.d("isNewlyRealized = " + isNewlyRealized);
                ItemContainerGenerator.PrepareItemContainer(child);

                Log.d($"Trying to insert:");
                PrintValue(child);

                int insertionIndex = pos.Index + pos.Offset;
                Children.Insert(insertionIndex, child as UIElement);
                pos = ItemContainerGenerator.GeneratorPositionFromIndex(startIndex + i);
            }

            ItemContainerGenerator.Stop();

            Log.d($"Just realized {count} items starting at index {startIndex}");
            DumpGeneratorContent();

            PrintValues();
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
            var c = (ListBoxItem)child;
            var dc = (ViewModels.Data)c.DataContext;
            var val = dc.Title;

            Log.d(
                i == int.MinValue
                    ? $"----- UI child has a value of {val}"
                    : $"----- UI child at UI index {i} has a value of {val}");
        }




        private IndexRange CalculateCurrentRealizationRange()
        {
            double realizationHeight = ActualHeight;

            // When you move the panel up (into Q1), where the offsets are positive, the 
            // offset from initial position is negative.  Thus the *-1
            double realizationRangeTopOffset    = -1 * OffsetFromInitialPosition + realizationHeight / 2;
            double realizationRangeBottomOffset = -1 * OffsetFromInitialPosition - realizationHeight / 2;

            Log.d($"realizationHeight = {realizationHeight}");
            Log.d($"OffsetFromInitialPosition = {OffsetFromInitialPosition}");

            return CalculateIndexRangeFromOffsetRange(
                realizationRangeTopOffset,
                realizationRangeBottomOffset);
        }

        private IndexRange CalculateIndexRangeFromOffsetRange(
            double topOffset,
            double bottomOffset)
        {
//            Log.d($"CalculateIndexRangeFromOffsetRange");

            var childTopBottoms = GetItemsTopAndBottomOffsets();
            //Log.d($"|\tchildTopBottoms = {childTopBottoms.ToList().ToDebugString()}");

            int[] indicesInRange = CalculateIndicesInRange(childTopBottoms.ToList(), topOffset, bottomOffset);
            int firstIndexInRange = indicesInRange.Min();
            int lastIndexInRange = indicesInRange.Max();

            var r = new IndexRange(firstIndexInRange, lastIndexInRange);

            Log.d($"|\tFrom range [{topOffset}:{bottomOffset}] we got an index range of {r}");
            return r;
        }

        private static int[] CalculateIndicesInRange(
            IList<Tuple<double, double>> itemStartEnds,
            double rangeTop,
            double rangeBottom)
        {
            Log.d($"CalculateIndicesInRange(topBottoms (size {itemStartEnds.Count}), {rangeTop}, {rangeBottom}");
            var indicesInRange = new List<int>();

            for (var i = 0; i < itemStartEnds.Count; ++i)
            {
                double top    = itemStartEnds[i].Item1;
                double bottom = itemStartEnds[i].Item2;

                // Cartesian coordinate plane - up is positive, down is negative
                bool isInRange = top > rangeTop    && bottom < rangeTop      // Straddles top border
                              || top < rangeTop    && bottom > rangeBottom   // Somewhere in the middle
                              || top > rangeBottom && bottom < rangeBottom   // Straddles the bottom border
                              || top > rangeTop    && bottom < rangeBottom;  // Overlaps the entire range

                Log.d($"| \tItem from [{(int)top}:{(int)bottom}]\t at index {i} is {(isInRange ? "" : "NOT")} in range");

                if (isInRange) indicesInRange.Add(i);
            }

            return indicesInRange.ToArray();
        }


        // We don't really need the bottom offsets, because they're clearly repeated
        private IList<Tuple<double, double>> GetItemsTopAndBottomOffsets()
        {
            int firstRealizedChildIndex = GetFirstRealizedIndex();
            UIElement firstRealizedChild = Children[0];
            Rect firstRealizedChildBounds = GetChildBoundsInThisPanel(firstRealizedChild);
            double firstRealizedChildTop = firstRealizedChildBounds.Top;

            double topOfFirstChild = firstRealizedChildTop - firstRealizedChildIndex * ChildHeight;
            var childTopBottoms = new List<Tuple<double, double>>
            {
                Tuple.Create(topOfFirstChild, topOfFirstChild + ChildHeight)
            };

            // Virtualized items and realized items
            int allItemsCount = GetParentItemsControl().Items.Count;
            for (var i = 1; i < allItemsCount; ++i)
            {
                double bottomOfLastChild = childTopBottoms[i - 1].Item2;
                childTopBottoms.Add(Tuple.Create(bottomOfLastChild, bottomOfLastChild + ChildHeight));
            }

            return childTopBottoms;


//            var childTopBottoms = 
//                from child in Children
//                select GetChildBoundsInThisPanel(child) into bounds
//                select bounds.Top into childTop
//                let childbottom = childTop + ChildHeight
//                select new Tuple<double, double>(childTop, childbottom);
//
//            return childTopBottoms.ToList();
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

            public int Length => End - Start;

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