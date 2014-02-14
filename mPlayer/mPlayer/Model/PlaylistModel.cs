using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Data;
using System.Windows.Threading;

namespace mPlayer.Model
{
    public class PlaylistModel : BindableObject
    {
        #region Constants

        public static string[] MEDIA_FILE_EXT = new string[] { ".mp3", ".m4a", ".wav", ".aac" };
        private const string PLAYLIST_FILENAME = "playlist.plfile";
        private const string PLAYLISTS_DIR = "Playlists";
        private const string PLAYLIST_EXT = ".plfile";

        #endregion

        #region Properties

        private RangeObservableCollection<SongItem> items;
        public RangeObservableCollection<SongItem> Items
        {
            get { return items; }
            set
            {
                items = value;
                OnPropertyChanged("Items");
            }
        }

        private ObservableCollection<PlaylistItem> savedPlaylists;
        public ObservableCollection<PlaylistItem> SavedPlaylists
        {
            get { return savedPlaylists; }
            set
            {
                savedPlaylists = value;
                OnPropertyChanged("SavedPlaylists");
            }
        }

        private string _status;
        public string Status
        {
            get { return _status; }
            set
            {
                _status = value;
                OnPropertyChanged("Status");
            }
        }

        private bool _isBusy;
        public bool IsBusy
        {
            get { return _isBusy; }
            set
            {
                _isBusy = value;
                OnPropertyChanged("IsBusy");
            }
        }

        #endregion

        public PlaylistModel()
        {
            LoadPlaylist();
            LoadSavedPlaylists();
        }

        public async void AddFiles(string[] files, bool scanExt = true)
        {
            int progress = 0;
            IsBusy = true;

            var list = new List<SongItem>();

            await Task.Factory.StartNew(() =>
            {
                foreach (var file in files)
                {
                    var canAdd = !scanExt || MEDIA_FILE_EXT.Contains(Path.GetExtension(file).ToLower());
                    if (canAdd)
                    {
                        Status = string.Format("Getting tags... {0}%", (int)(100 * progress / (double)files.Length));

                        var item = new SongItem() { Path = file };
                        item.LoadTags();
                        list.Add(item);

                        progress++;
                    }
                }
            });

            Items.AddRange(list);
            SavePlaylist();

            Status = string.Empty;
            IsBusy = false;
        }

        public async void AddFolder(string folder)
        {
            IsBusy = true;
            Status = "Searching files...";

            var list = new List<string>();

            await Task.Factory.StartNew(() =>
            {
                ScanFolder(folder, list);
            });

            AddFiles(list.ToArray(), false);
        }

        public void ScanFolder(string folder, List<string> list)
        {
            string[] files;

            try { files = Directory.GetFiles(folder); }
            catch { return; }

            foreach (var file in files)
            {
                if (MEDIA_FILE_EXT.Contains(Path.GetExtension(file).ToLower()))
                {
                    list.Add(file);
                }
            }

            foreach (var dir in Directory.GetDirectories(folder))
            {
                ScanFolder(dir, list);
            }
        }

        public void RemoveItems(SongItem[] items)
        {
            foreach (var item in items)
            {
                Items.Remove(item);
            }

            SavePlaylist();
        }

        public void FilterItems(string query)
        {
            query = query.ToLower();

            var view = CollectionViewSource.GetDefaultView(this.Items);
            if (string.IsNullOrWhiteSpace(query))
            {
                view.Filter = null;
                return;
            }

            view.Filter = new Predicate<object>((item) =>
            {
                var song = item as SongItem;
                return song.Contains(query);
            });
        }

        public SongItem GetPrevItem(SongItem current)
        {
            var index = Items.IndexOf(current) - 1;
            if (index >= 0) return Items[index];
            return null;
        }

        public SongItem GetNextItem(SongItem current)
        {
            var index = Items.IndexOf(current) + 1;
            if (index < Items.Count) return Items[index];
            return null;
        }

        public string GetPlaylistPath
        {
            get
            {
                var dir = Path.Combine(MainModel.GetUserDataFolder, PLAYLIST_FILENAME);
                return dir;
            }
        }

        public string GetPlaylistsFolder
        {
            get
            {
                var dir = Path.Combine(MainModel.GetUserDataFolder, PLAYLISTS_DIR);
                if (!Directory.Exists(dir)) Directory.CreateDirectory(dir);
                return dir;
            }
        }

        public void SavePlaylist()
        {
            SavePlaylist(GetPlaylistPath);
        }

        public void SavePlaylist(string path)
        {
            try
            {
                var json = JsonConvert.SerializeObject(Items);
                File.WriteAllText(path, json);
            }
            catch { }
        }

        public void SaveLocalPlaylist(string name)
        {
            var newName = name;
            var path = Path.Combine(GetPlaylistsFolder, name + PLAYLIST_EXT);

            int index = 1;
            while (File.Exists(path))
            {
                newName = name + " (" + index + ")";
                path = Path.Combine(GetPlaylistsFolder, newName + PLAYLIST_EXT);
                index++;
            }

            SavePlaylist(path);

            var item = new PlaylistItem() { Name = newName, Path = path };
            SavedPlaylists.Add(item);
        }

        private void LoadPlaylist()
        {
            LoadPlaylist(GetPlaylistPath);
            if (Items == null) Items = new RangeObservableCollection<SongItem>();
        }

        public void LoadPlaylist(string path)
        {
            if (File.Exists(path))
            {
                var json = File.ReadAllText(path);
                Items = JsonConvert.DeserializeObject<RangeObservableCollection<SongItem>>(json);
            }
        }

        private void LoadSavedPlaylists()
        {
            SavedPlaylists = new ObservableCollection<PlaylistItem>();

            foreach (var file in Directory.GetFiles(GetPlaylistsFolder, "*" + PLAYLIST_EXT))
            {
                var item = new PlaylistItem()
                {
                    Name = Path.GetFileNameWithoutExtension(file),
                    Path = file
                };

                SavedPlaylists.Add(item);
            }
        }

        public void DeletePlaylist(PlaylistItem item)
        {
            if (File.Exists(item.Path))
            {
                try
                {
                    File.Delete(item.Path);
                    SavedPlaylists.Remove(item);
                }
                catch { }
            }
        }
    }
}
