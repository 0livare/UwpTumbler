using Windows.UI.Xaml.Controls;
using TumblerApp.ViewModels;

namespace TumblerApp.Views.Samples
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
