using Newtonsoft.Json;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Windows.Media.Imaging;
using Un4seen.Bass.AddOn.Tags;

namespace mPlayer.Model
{
    public class SongItem : BindableObject
    {
        private string _title;
        public string Title
        {
            get { return _title; }
            set
            {
                _title = value;
                OnPropertyChanged("Title");
            }
        }

        private string _artist;
        public string Artist
        {
            get { return _artist; }
            set
            {
                _artist = value;
                OnPropertyChanged("Artist");
            }
        }

        private string _album;
        public string Album
        {
            get { return _album; }
            set
            {
                _album = value;
                OnPropertyChanged("Album");
            }
        }

        private string _genre;
        public string Genre
        {
            get { return _genre; }
            set
            {
                _genre = value;
                OnPropertyChanged("Genre");
            }
        }

        private string _year;
        public string Year
        {
            get { return _year; }
            set
            {
                _year = value;
                OnPropertyChanged("Year");
            }
        }

        private double _duration;
        public double Duration
        {
            get { return _duration; }
            set
            {
                _duration = value;
                OnPropertyChanged("Duration");
            }
        }

        private string _path;
        public string Path
        {
            get { return _path; }
            set
            {
                _path = value;
                OnPropertyChanged("Path");
            }
        }

        private object _albumArt;
        [JsonIgnore()]
        public object AlbumArt
        {
            get { return _albumArt; }
            set
            {
                if (value is byte[])
                {
                    try
                    {
                        var bi = new BitmapImage();
                        bi.BeginInit();
                        bi.DecodePixelHeight = 180;
                        bi.StreamSource = new MemoryStream((byte[])value);
                        bi.EndInit();
                        _albumArt = bi;
                    }
                    catch { }
                }
                else
                {
                    _albumArt = value;
                }

                OnPropertyChanged("AlbumArt");
            }
        }

        public void LoadTags()
        {
            var tags = BassTags.BASS_TAG_GetFromFile(_path);

            if (tags != null)
            {
                Title = tags.title;
                if (string.IsNullOrEmpty(this.Title)) this.Title = System.IO.Path.GetFileNameWithoutExtension(Path);

                Artist = tags.artist;
                if (string.IsNullOrEmpty(this.Artist)) this.Artist = "Unknown artist";

                Album = tags.album;
                if (string.IsNullOrEmpty(this.Album)) this.Album = "Unknown album";

                Genre = tags.genre;
                if (string.IsNullOrEmpty(this.Genre)) this.Genre = "Unknown genre";

                Year = tags.year;
                if (string.IsNullOrEmpty(this.Year)) this.Year = "Unknown year";

                Duration = tags.duration;
            }
        }

        public void LoadAlbumArt()
        {
            if (AlbumArt == null)
            {
                var tags = BassTags.BASS_TAG_GetFromFile(_path);

                if (tags != null && tags.PictureCount > 0)
                {
                    AlbumArt = tags.PictureGet(0).Data;
                }
                else if (App.Model.Settings.DownloadArts)
                {
                    LoadExternalAlbumArt();
                }
            }
        }

        public async void LoadExternalAlbumArt()
        {
            var dir = System.IO.Path.Combine(MainModel.GetUserDataFolder, "Cache");
            if (!Directory.Exists(dir)) Directory.CreateDirectory(dir);

            var cacheFile = System.IO.Path.Combine(dir, System.IO.Path.GetFileNameWithoutExtension(this.Path) + ".cache");

            if (!System.IO.File.Exists(cacheFile))
            {
                var imgUrl = await LastFMWrapper.GetAlbumArt(this.Artist, this.Title);
                if (imgUrl != null)
                {
                    try
                    {
                        var client = new WebClient();
                        await client.DownloadFileTaskAsync(imgUrl, cacheFile);
                    }
                    catch { }
                }
            }

            if (System.IO.File.Exists(cacheFile))
            {
                AlbumArt = cacheFile;
            }
        }

        public bool Contains(string value)
        {
            if (Title != null && Title.ToLower().Contains(value)) return true;
            if (Artist != null && Artist.ToLower().Contains(value)) return true;
            if (Album != null && Album.ToLower().Contains(value)) return true;
            if (Path != null && Path.ToLower().Contains(value)) return true;
            return false;
        }

        private string _cover;
        [JsonIgnore()]
        public string Cover
        {
            get { return _cover; }
            set
            {
                _cover = value;
                OnPropertyChanged("Cover");
            }
        }

        private string _biography;
        [JsonIgnore()]
        public string Biography
        {
            get { return _biography; }
            set
            {
                _biography = value;
                if (string.IsNullOrWhiteSpace(_biography)) _biography = "No biography was found...";
                OnPropertyChanged("Biography");
            }
        }

        private bool _hasInfo;
        [JsonIgnore()]
        public bool HasInfo
        {
            get { return _hasInfo; }
            set
            {
                _hasInfo = value;
                OnPropertyChanged("HasInfo");
            }
        }

        private List<SimilarArtist> _similar;
        [JsonIgnore()]
        public List<SimilarArtist> Similar
        {
            get { return _similar; }
            set
            {
                _similar = value;
                OnPropertyChanged("Similar");
            }
        }
    }

    public class SimilarArtist : BindableObject
    {
        private string _name;
        public string Name
        {
            get { return _name; }
            set
            {
                _name = value;
                OnPropertyChanged("Name");
            }
        }

        private string _image;
        public string Image
        {
            get { return _image; }
            set
            {
                _image = value;
                OnPropertyChanged("Image");
            }
        }
    }
}
