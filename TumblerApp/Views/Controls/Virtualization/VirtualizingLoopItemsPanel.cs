using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Linq;
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

        public VirtualizingLoopItemsPanel() { Loaded += OnLoaded; }

        private void OnLoaded(object sender, RoutedEventArgs e)
        {
            RealizationRange = CalculateCurrentRealizationRange();

            // Virtualize all items that come before the realization range
            VirtualizeRange(0, RealizationRange.Start);

            // Virtualize all items that come after the realization range
            ItemsControl owner = ItemsControl.GetItemsOwner(this);
            int numItemsToVirtualizeAtEnd = owner.Items.Count - RealizationRange.End;
            VirtualizeRange(RealizationRange.End + 1, numItemsToVirtualizeAtEnd);
        }


        protected override void OnScrolled(double movedBy)
        {
            base.OnScrolled(movedBy);

            double movedBySinceLastCalculation = Math.Abs(OffsetFromInitialPosition - _lastCalculatedRealizationRangeAtOffset);
            if (movedBySinceLastCalculation < ActualHeight / 2) return;

            IndexRange currentRealizationRange = CalculateCurrentRealizationRange();

            IndexRange toBeRealized;
            IndexRange toBeVirtualized;

            bool isScrollingDown = currentRealizationRange.Start < RealizationRange.Start;
            if (isScrollingDown)
            {
                // New items need to be added at the top
                toBeRealized = new IndexRange(
                    currentRealizationRange.Start, 
                    RealizationRange.Start - 1);
                toBeVirtualized = new IndexRange(
                    currentRealizationRange.End + 1,
                    RealizationRange.End);
            }
            else
            {
                // New items need to be added at the bottom
                toBeRealized = new IndexRange(
                    RealizationRange.End + 1,
                    currentRealizationRange.End);
                toBeVirtualized = new IndexRange(
                    RealizationRange.Start,
                    currentRealizationRange.Start - 1);
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
            Log.d($"About to virtualize {count} items starting at index {startIndex}");

            GeneratorPosition pos = ItemContainerGenerator.GeneratorPositionFromIndex(startIndex);
            for (int i = pos.Index; i < count; ++i)
            {
                Children.RemoveAt(i);
            }

            ItemContainerGenerator.Remove(pos, count);
        }

        private void RealizeRange(int startIndex, int count)
        {
            Log.d($"About to realize {count} items starting at index {startIndex}");

            GeneratorPosition pos = ItemContainerGenerator.GeneratorPositionFromIndex(startIndex);
            ItemContainerGenerator.StartAt(pos, GeneratorDirection.Forward, true);

            for (var i = 0; i < count; ++i)
            {
                DependencyObject child = ItemContainerGenerator.GenerateNext(out bool isNewlyRealized);
                Log.d("isNewlyRealized = " + isNewlyRealized);
                ItemContainerGenerator.PrepareItemContainer(child);

                ItemContainerGenerator.Stop();
                Children.Add(child as UIElement);
            }
        }




        private IndexRange CalculateCurrentRealizationRange()
        {
            double realizationHeight = ActualHeight * 3;
            double realizationRangeTopOffset = OffsetFromInitialPosition + realizationHeight / 2;
            double realizationRangeBottomOffset = OffsetFromInitialPosition - realizationHeight / 2;

            return CalculateIndexRangeFromOffsetRange(
                realizationRangeTopOffset,
                realizationRangeBottomOffset);
        }

        private IndexRange CalculateIndexRangeFromOffsetRange(
            double topOffset,
            double bottomOffset)
        {
            var childTopBottoms =
                from child in Children
                select (TranslateTransform)child.RenderTransform
                into transform
                select transform.Y
                into childTop
                let childBottom = childTop + ChildHeight
                select new Tuple<double, double>(childTop, childBottom);

            int[] indicesInRange = CalculateIndicesInRange(childTopBottoms.ToList(), topOffset, bottomOffset);
            int firstIndexInRange = indicesInRange.Min();
            int lastIndexInRange = indicesInRange.Max();

            return new IndexRange(firstIndexInRange, lastIndexInRange);
        }

        private static int[] CalculateIndicesInRange(
            IList<Tuple<double, double>> itemStartEnds,
            double rangeStart,
            double rangeEnd)
        {
            var indicesInRange = new List<int>();

            for (var i = 0; i < itemStartEnds.Count; ++i)
            {
                double start = itemStartEnds[i].Item1;
                double end = itemStartEnds[i].Item2;

                bool isInRange = start < rangeStart && end > rangeStart
                                 || start > rangeStart && end < rangeEnd
                                 || start < rangeEnd && end > rangeEnd
                                 || start < rangeStart && end > rangeEnd;

                if (isInRange) indicesInRange.Add(i);
            }

            return indicesInRange.ToArray();
        }















        private void DumpGeneratorContent()
        {
            ItemContainerGenerator generator = this.ItemContainerGenerator;
            ItemsControl itemsControl = ItemsControl.GetItemsOwner(this);

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
        }
    }
}