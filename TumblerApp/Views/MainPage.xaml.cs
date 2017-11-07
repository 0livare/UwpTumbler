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
            for (var i = 0; i >= -totalHeight; i -= 100)
            {
                Relative.Children.Add(CreateRulerLine(i, totalHeight));
            }

            // Always add line for zero 
            Relative.Children.Add(CreateRulerLine((int)totalHeight / 2 * -1, totalHeight));
        }

        private UIElement CreateRulerLine(int offset, double totalHeight)
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
                Text = "" + (offset + totalHeight / 2),
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
