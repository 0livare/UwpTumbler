using Windows.UI.Xaml.Controls;
using TumblerApp.ViewModels;

namespace TumblerApp.Samples
{
    public sealed partial class LoopItemsSample : UserControl
    {
        public LoopItemsSample()
        {
            InitializeComponent();

            Loaded += (sender, args) =>
            {
                DataContext = new MainViewModel();
            };
        }
    }
}
