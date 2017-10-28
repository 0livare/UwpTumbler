using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;

namespace jci.glas.device.ui.Views.Controls
{
    public sealed partial class Tumbler : UserControl
    {
        public Tumbler()
        {
            InitializeComponent();
            LayoutRoot.DataContext = this;
        }



        public object ItemsSource
        {
            get { return GetValue(ItemsSourceProperty); }
            set { SetValue(ItemsSourceProperty, value); }
        }

        public static readonly DependencyProperty ItemsSourceProperty =
            DependencyProperty.Register("ItemsSource", typeof(object), typeof(Tumbler),
                new PropertyMetadata(0));



        public DataTemplate ItemTemplate
        {
            get { return (DataTemplate)GetValue(ItemTemplateProperty); }
            set { SetValue(ItemTemplateProperty, value); }
        }

        public static readonly DependencyProperty ItemTemplateProperty =
            DependencyProperty.Register("ItemTemplate", typeof(DataTemplate), typeof(Tumbler),
                new PropertyMetadata(0));

        public int SelectedIndex
        {
            get { return (int)GetValue(SelectedIndexProperty); }
            set { SetValue(SelectedIndexProperty, value); }
        }

        public static readonly DependencyProperty SelectedIndexProperty =
            DependencyProperty.Register("SelectedIndex", typeof(int), typeof(Tumbler),
                new PropertyMetadata(0));



    }
}
