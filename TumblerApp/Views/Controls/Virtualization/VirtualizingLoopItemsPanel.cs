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

        private bool IsVirtualizedAt(int index)
        {
            GeneratorPosition pos = ItemContainerGenerator.GeneratorPositionFromIndex(index);
            return pos.Offset != 0;
        }


        private void VirtualizeAt(int index)
        {
            Log.d($"About to remove index {index}");
            if (IsVirtualizedAt(index)) return;

            GeneratorPosition pos = ItemContainerGenerator.GeneratorPositionFromIndex(index);
            Children.RemoveAt(pos.Index);
            ItemContainerGenerator.Remove(pos, 1);
        }

        private void RealizeAt(int index)
        {
            Log.d($"About to restore index {index}");

            GeneratorPosition pos = ItemContainerGenerator.GeneratorPositionFromIndex(index);
            ItemContainerGenerator.StartAt(pos, GeneratorDirection.Forward, true);

            DependencyObject child = ItemContainerGenerator.GenerateNext(out bool isNewlyRealized);
            Log.d("isNewlyRealized = " + isNewlyRealized);
            ItemContainerGenerator.PrepareItemContainer(child);

            ItemContainerGenerator.Stop();
            Children.Add(child as UIElement);
        }




        private Tuple<int, int> VirtualizationRange
        {
            get
            {
                double realizationHeight = ActualHeight * 3;
                double realizationRangeTopOffset    = OffsetFromInitialPosition + realizationHeight / 2;
                double realizationRangeBottomOffset = OffsetFromInitialPosition - realizationHeight / 2;

                return DetermineIndiceRangeFromOffsetRange(
                    realizationRangeTopOffset,
                    realizationRangeBottomOffset);
            }
        }

        private Tuple<int, int> DetermineIndiceRangeFromOffsetRange(
            double topOffset,
            double bottomOffset)
        {
            var childTopBottoms = 
                from child in Children
                select (TranslateTransform)child.RenderTransform into transform
                select transform.Y into childTop
                let childBottom = childTop + ChildHeight
                select new Tuple<double, double>(childTop, childBottom);

            int[] indicesInRange = DetermineIndicesInRange(childTopBottoms.ToList(), topOffset, bottomOffset);
            int firstIndexInRange = indicesInRange.Min();
            int lastIndexInRange  = indicesInRange.Max();

            return new Tuple<int, int>(firstIndexInRange, lastIndexInRange);
        }

        private static int[] DetermineIndicesInRange(
            IList<Tuple<double, double>> itemStartEnds, 
            double rangeStart,
            double rangeEnd)
        {
            var indicesInRange = new List<int>();

            for (var i = 0; i < itemStartEnds.Count; ++i)
            {
                double start = itemStartEnds[i].Item1;
                double end   = itemStartEnds[i].Item2;

                bool isInRange = start < rangeStart && end > rangeStart
                              || start > rangeStart && end < rangeEnd
                              || start < rangeEnd   && end > rangeEnd
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
                Log.d($"Item index={i} " +
                      $"Generator position: \t" +
                          $"index={position.Index}, \t" +
                          $"offset={position.Offset}");
            }

            Log.d("\n");
        }
    }
}