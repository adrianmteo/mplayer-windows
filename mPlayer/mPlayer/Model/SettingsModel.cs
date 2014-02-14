using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System.IO;
using System.Windows;

namespace mPlayer.Model
{
    public class SettingsModel : BindableObject
    {
        #region Variables

        private MainModel Model;

        #endregion

        #region Constants

        private const string SETTINGS_FILENAME = "settings";

        #endregion

        #region Properties

        private string _theme = "#3b83e6";
        public string Theme
        {
            get { return _theme; }
            set
            {
                _theme = value;
                OnPropertyChanged("Theme");
                SaveSettings();
            }
        }

        private bool _downloadArts = true;
        public bool DownloadArts
        {
            get { return _downloadArts; }
            set
            {
                _downloadArts = value;
                OnPropertyChanged("DownloadArts");
                SaveSettings();
            }
        }

        private bool _downloadCover = true;
        public bool DownloadCover
        {
            get { return _downloadCover; }
            set
            {
                _downloadCover = value;
                OnPropertyChanged("DownloadCover");
                SaveSettings();
            }
        }

        private bool _autoCheckUpdates = true;
        public bool AutoCheckUpdates
        {
            get { return _autoCheckUpdates; }
            set
            {
                _autoCheckUpdates = value;
                OnPropertyChanged("AutoCheckUpdates");
                SaveSettings();
            }
        }

        private bool _enableSharing = false;
        public bool EnableSharing
        {
            get { return _enableSharing; }
            set
            {
                _enableSharing = value;
                MessageBox.Show("Please restart app in order to enable/disable the Home sharing function");
                SaveSettings();
            }
        }

        private bool _shuffle;
        public bool Shuffle
        {
            get { return _shuffle; }
            set
            {
                _shuffle = value;
                OnPropertyChanged("Shuffle");
                SaveSettings();
            }
        }

        private bool _repeat;
        public bool Repeat
        {
            get { return _repeat; }
            set
            {
                _repeat = value;
                OnPropertyChanged("Repeat");
                SaveSettings();
            }
        }

        private double _volume = 0.75;
        public double Volume
        {
            get { return _volume; }
            set
            {
                _volume = value;
                Model.Volume = _volume;
                OnPropertyChanged("Volume");
            }
        }

        private float[] _eqValues = new float[] { 0, 0, 0, 0, 0, 0, 0, 0, 0, 0 };
        public float[] EQValues
        {
            get { return _eqValues; }
            set
            {
                _eqValues = value;
                OnPropertyChanged("EQValues");
            }
        }

        private string _eqName = "Off";
        public string EQName
        {
            get { return _eqName; }
            set
            {
                _eqName = value;
                OnPropertyChanged("EQName");
            }
        }

        private int _lastPlayed = -1;
        public int LastPlayed
        {
            get { return _lastPlayed; }
            set
            {
                _lastPlayed = value;
                OnPropertyChanged("LastPlayed");
                SaveSettings();
            }
        }

        private Point _windowPosition;
        public Point WindowPosition
        {
            get { return _windowPosition; }
            set
            {
                _windowPosition = value;
                OnPropertyChanged("WindowPosition");
            }
        }

        private Size _windowSize;
        public Size WindowSize
        {
            get { return _windowSize; }
            set
            {
                _windowSize = value;
                OnPropertyChanged("WindowSize");
            }
        }

        private bool _windowMaximized;
        public bool WindowMaximized
        {
            get { return _windowMaximized; }
            set
            {
                _windowMaximized = value;
                OnPropertyChanged("WindowMaximized");
            }
        }

        #endregion

        public SettingsModel(MainModel model)
        {
            Model = model;
            LoadSettings();
        }

        public void SaveSettings()
        {
            var json = JsonConvert.SerializeObject(this);
            var path = Path.Combine(MainModel.GetUserDataFolder, SETTINGS_FILENAME);
            File.WriteAllText(path, json);
        }

        public void LoadSettings()
        {
            var path = Path.Combine(MainModel.GetUserDataFolder, SETTINGS_FILENAME);
            if (File.Exists(path))
            {
                var data = File.ReadAllText(path);
                var json = JsonConvert.DeserializeObject(data) as JContainer;

                var jVal = json["Theme"];
                if (jVal != null) _theme = (string)jVal;

                jVal = json["DownloadArts"];
                if (jVal != null) _downloadArts = (bool)jVal;

                jVal = json["DownloadCover"];
                if (jVal != null) _downloadCover = (bool)jVal;

                jVal = json["AutoCheckUpdates"];
                if (jVal != null) _autoCheckUpdates = (bool)jVal;

                jVal = json["EnableSharing"];
                if (jVal != null) _enableSharing = (bool)jVal;

                jVal = json["Shuffle"];
                if (jVal != null) _shuffle = (bool)jVal;

                jVal = json["Repeat"];
                if (jVal != null) _repeat = (bool)jVal;

                jVal = json["Volume"];
                if (jVal != null) _volume = (double)jVal;

                jVal = json["LastPlayed"];
                if (jVal != null) _lastPlayed = (int)jVal;

                jVal = json["WindowPosition"];
                if (jVal != null) _windowPosition = Point.Parse((string)jVal);

                jVal = json["WindowSize"];
                if (jVal != null) _windowSize = Size.Parse((string)jVal);

                jVal = json["WindowMaximized"];
                if (jVal != null) _windowMaximized = (bool)jVal;

                jVal = json["EQValues"];
                if (jVal != null)
                {
                    var array = jVal as JArray;
                    if (array != null)
                    {
                        _eqValues = new float[10];
                        for (int i = 0; i < array.Count && i < _eqValues.Length; i++)
                        {
                            _eqValues[i] = array[i].Value<float>();
                        }
                    }
                }

                jVal = json["EQName"];
                if (jVal != null) _eqName = (string)jVal;
            }
        }
    }
}
