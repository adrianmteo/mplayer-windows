using mPlayer.Model;
using System;
using System.ComponentModel;
using System.Diagnostics;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Media.Animation;
using System.Windows.Media.Imaging;

namespace mPlayer.View
{
    public partial class SlideshowControl : UserControl
    {
        public static readonly DependencyProperty CurrentSongProperty = DependencyProperty.Register("CurrentSong", typeof(SongItem), typeof(SlideshowControl));

        public SongItem CurrentSong
        {
            get { return (SongItem)GetValue(CurrentSongProperty); }
            set { SetValue(CurrentSongProperty, value); }
        }

        private int CurrentPage = 1;
        private Storyboard FadeOutStory;

        public SlideshowControl()
        {
            InitializeComponent();

            FadeOutStory = this.Resources["FadeOutStory"] as Storyboard;
            FadeOutStory.Completed += FadeOutStory_Completed;

            var textDescr = DependencyPropertyDescriptor.FromProperty(SlideshowControl.CurrentSongProperty, typeof(SlideshowControl));
            if (textDescr != null)
            {
                textDescr.AddValueChanged(this, delegate
                {
                    UpdateImage(CurrentSong);
                });
            }
        }

        public async void UpdateImage(SongItem song)
        {
            if (song == null) return;

            CurrentPage = 1;
            var sb = this.Resources["ShowPage1Story"] as Storyboard;
            sb.Begin();

            if (!song.HasInfo)
            {
                var x = await LastFMWrapper.GetArtistInfo(song.Artist);

                song.Biography = x.Biography;
                song.Similar = x.Similar;
                song.Cover = x.Image;
                song.HasInfo = true;

                if (CurrentSong != song) return;
            }

            song.LoadAlbumArt();

            FadeOutStory.Begin();
        }

        private void FadeOutStory_Completed(object sender, EventArgs e)
        {
            if (!string.IsNullOrEmpty(CurrentSong.Cover))
            {
                var bi = new BitmapImage();
                bi.DownloadCompleted += (sender1, e1) =>
                {
                    Image.Source = bi;
                    var sb = this.Resources["FadeInStory"] as Storyboard;
                    sb.Begin();
                };
                bi.BeginInit();
                bi.UriSource = new Uri(CurrentSong.Cover);
                bi.EndInit();

                if (!bi.IsDownloading)
                {
                    Image.Source = bi;
                    var sb = this.Resources["FadeInStory"] as Storyboard;
                    sb.Begin();
                }
            }
        }

        private void TextBlockArtist_MouseDown(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            Process.Start("http://www.last.fm/music/" + (sender as TextBlock).Text);
        }

        private void ButtonPrevPage_Click(object sender, RoutedEventArgs e)
        {
            CurrentPage--;
            if (CurrentPage == 0) CurrentPage = 3;

            var sb = this.Resources["ShowPage" + CurrentPage + "Story"] as Storyboard;
            sb.Begin();
        }

        private void ButtonNextPage_Click(object sender, RoutedEventArgs e)
        {
            CurrentPage++;
            if (CurrentPage == 4) CurrentPage = 1;

            var sb = this.Resources["ShowPage" + CurrentPage + "Story"] as Storyboard;
            sb.Begin();
        }
    }
}
