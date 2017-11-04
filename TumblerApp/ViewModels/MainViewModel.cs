using System;
using System.Collections.Generic;
using Windows.UI.Xaml.Media.Imaging;

namespace TumblerApp.ViewModels
{
    public class MainViewModel
    {
        public List<Data> Data { get; set; }

        public MainViewModel()
        {
            this.Data = new List<Data>();
            this.Data.Add(new Data { BitmapImage = new BitmapImage(new Uri("ms-appx:///Assets/pic01.jpg", UriKind.Absolute)), Title = "00" });
            this.Data.Add(new Data { BitmapImage = new BitmapImage(new Uri("ms-appx:///Assets/pic03.jpg", UriKind.Absolute)), Title = "01" });
            this.Data.Add(new Data { BitmapImage = new BitmapImage(new Uri("ms-appx:///Assets/pic05.jpg", UriKind.Absolute)), Title = "02" });
            this.Data.Add(new Data { BitmapImage = new BitmapImage(new Uri("ms-appx:///Assets/pic04.jpg", UriKind.Absolute)), Title = "03" });
            this.Data.Add(new Data { BitmapImage = new BitmapImage(new Uri("ms-appx:///Assets/pic02.jpg", UriKind.Absolute)), Title = "04" });
            this.Data.Add(new Data { BitmapImage = new BitmapImage(new Uri("ms-appx:///Assets/pic06.jpg", UriKind.Absolute)), Title = "05" });
            this.Data.Add(new Data { BitmapImage = new BitmapImage(new Uri("ms-appx:///Assets/pic01.jpg", UriKind.Absolute)), Title = "06" });
            this.Data.Add(new Data { BitmapImage = new BitmapImage(new Uri("ms-appx:///Assets/pic02.jpg", UriKind.Absolute)), Title = "07" });
            this.Data.Add(new Data { BitmapImage = new BitmapImage(new Uri("ms-appx:///Assets/pic03.jpg", UriKind.Absolute)), Title = "08" });
            this.Data.Add(new Data { BitmapImage = new BitmapImage(new Uri("ms-appx:///Assets/pic04.jpg", UriKind.Absolute)), Title = "09" });
            this.Data.Add(new Data { BitmapImage = new BitmapImage(new Uri("ms-appx:///Assets/pic05.jpg", UriKind.Absolute)), Title = "10" });
            this.Data.Add(new Data { BitmapImage = new BitmapImage(new Uri("ms-appx:///Assets/pic06.jpg", UriKind.Absolute)), Title = "11" });
            this.Data.Add(new Data { BitmapImage = new BitmapImage(new Uri("ms-appx:///Assets/pic01.jpg", UriKind.Absolute)), Title = "12" });
            this.Data.Add(new Data { BitmapImage = new BitmapImage(new Uri("ms-appx:///Assets/pic02.jpg", UriKind.Absolute)), Title = "13" });
            this.Data.Add(new Data { BitmapImage = new BitmapImage(new Uri("ms-appx:///Assets/pic03.jpg", UriKind.Absolute)), Title = "14" });
        }
    }

    public class Data
    {
        public BitmapImage BitmapImage { get; set; }
        public string Title { get; set; }
    }
}