using System;
using Windows.UI;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Shapes;
using TumblerApp.Util;

namespace TumblerApp.Views
{
    public sealed partial class MainPage : Page
    {
        public MainPage()
        {
            InitializeComponent();
            Loaded += OnLoaded;
        }

        private void OnLoaded(object sender, RoutedEventArgs routedEventArgs)
        {
            double totalHeight = 700;
            CreateRulerLines(totalHeight);
        }

        private void CreateRulerLines(double totalHeight)
        {
            for (double offset = 0; offset <= totalHeight; offset += 50)
            {
                AddRulerLineAtOffset(offset, totalHeight);
            }

            // Always add line for zero
            AddRulerLineAtOffset(0, totalHeight);

            AddRulerLineAtOffset(-189, totalHeight);
            AddRulerLineAtOffset(-343, totalHeight);
        }

        private void AddRulerLineAtOffset(double naturalOffset, double totalHeight)
        {
            Relative.Children.Add(CreateRulerLineAtOffset(naturalOffset, totalHeight));
        }

        private UIElement CreateRulerLineAtOffset(double naturalOffset, double totalHeight)
        {
            double offsetFromBottom = naturalOffset - totalHeight;
            string label = naturalOffset.ToString();

            return CreateRulerLine(offsetFromBottom, totalHeight, label);
        }

        private UIElement CreateRulerLine(double offset, double totalHeight, string label)
        {
            var line = new Line
            {
                X2 = 500,
                Stroke = new SolidColorBrush(Colors.Blue),
                StrokeThickness = 2,
                VerticalAlignment = VerticalAlignment.Bottom,
            };

            var text = new TextBlock
            {
                Text = label,
                Margin = new Thickness(0, 0, 10, 0),
                VerticalAlignment = VerticalAlignment.Bottom,
            };

            var sp = new StackPanel
            {
                Orientation = Orientation.Horizontal,
                RenderTransform = new TranslateTransform { Y = offset },
                VerticalAlignment = VerticalAlignment.Bottom,
            };

            sp.Children.Add(line);
            sp.Children.Add(text);
            RelativePanel.SetAlignBottomWith(sp, LoopItemsSample);

            return sp;
        }
    }
}
