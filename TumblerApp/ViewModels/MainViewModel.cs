using System;
using System.Collections.Generic;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Media.Imaging;

namespace TumblerApp.ViewModels
{
    public class MainViewModel
    {
        public List<Data> Data { get; set; }

        public MainViewModel()
        {
            Data = new List<Data>
            {
                new Data { BitmapImage = CreateImage(1), Title = "00" },
                new Data { BitmapImage = CreateImage(2), Title = "01" },
                new Data { BitmapImage = CreateImage(3), Title = "02" },
                new Data { BitmapImage = CreateImage(4), Title = "03" },
                new Data { BitmapImage = CreateImage(5), Title = "04" },
                //new Data { BitmapImage = CreateImage(6), Title = "05" },
                //new Data { BitmapImage = CreateImage(1), Title = "06" },
                //new Data { BitmapImage = CreateImage(2), Title = "07" },
                //new Data { BitmapImage = CreateImage(3), Title = "08" },
                //new Data { BitmapImage = CreateImage(4), Title = "09" },
                //new Data { BitmapImage = CreateImage(5), Title = "10" },
                //new Data { BitmapImage = CreateImage(6), Title = "11" },
                //new Data { BitmapImage = CreateImage(1), Title = "12" },
                //new Data { BitmapImage = CreateImage(2), Title = "13" },
                //new Data { BitmapImage = CreateImage(3), Title = "14" }
            };
        }

        private static ImageSource CreateImage(int imageIndex)
        {
            return new BitmapImage(
                new Uri(
                    $"ms-appx:///Assets/pic0{imageIndex}.jpg", 
                    UriKind.Absolute));
        }
    }

    public class Data
    {
        public ImageSource BitmapImage { get; set; }
        public string Title { get; set; }
    }
}