using Windows.UI.Xaml.Controls;
using TumblerApp.ViewModels;

namespace TumblerApp.Samples
{
    public sealed partial class CircleSample : UserControl
    {
        public CircleSample()
        {
            InitializeComponent();

            this.LayoutRoot.Loaded += (sender, args) =>
            {
                this.DataContext = new MainViewModel();
            };
        }
    }
}
