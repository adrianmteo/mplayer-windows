using mPlayer.Properties;
using System;
using System.IO;
using System.Windows.Threading;
using Un4seen.Bass;
using Un4seen.Bass.AddOn.Fx;

namespace mPlayer.Model
{
    public class MainModel : BindableObject
    {
        public event EventHandler PositionChanged;

        private PlaylistModel playlist;
        public PlaylistModel Playlist
        {
            get { return playlist; }
            set
            {
                playlist = value;
                OnPropertyChanged("Playlist");
            }
        }

        private EqualizerModel equalizer;
        public EqualizerModel Equalizer
        {
            get { return equalizer; }
            set
            {
                equalizer = value;
                OnPropertyChanged("Equalizer");
            }
        }

        private ThemeModel theme;
        public ThemeModel Theme
        {
            get { return theme; }
            set
            {
                theme = value;
                OnPropertyChanged("Theme");
            }
        }

        private UpdaterModel updater;
        public UpdaterModel Updater
        {
            get { return updater; }
            set
            {
                updater = value;
                OnPropertyChanged("Updater");
            }
        }

        private SharingModel sharing;
        public SharingModel Sharing
        {
            get { return sharing; }
            set
            {
                sharing = value;
                OnPropertyChanged("Sharing");
            }
        }

        private SettingsModel _settings;
        public SettingsModel Settings
        {
            get { return _settings; }
            set
            {
                _settings = value;
                OnPropertyChanged("Settings");
            }
        }

        private double volume = 0.75d;
        public double Volume
        {
            get { return volume; }
            set
            {
                volume = value;
                Bass.BASS_ChannelSetAttribute(Stream, BASSAttribute.BASS_ATTRIB_VOL, (float)volume);
            }
        }

        private bool isPlaying;
        public bool IsPlaying
        {
            get { return isPlaying; }
            set
            {
                isPlaying = value;
                OnPropertyChanged("IsPlaying");

                if (isPlaying) SeekTimer.Start();
                else SeekTimer.Stop();
            }
        }

        public double Position
        {
            get { return Bass.BASS_ChannelBytes2Seconds(Stream, Bass.BASS_ChannelGetPosition(Stream)); }
            set { Bass.BASS_ChannelSetPosition(Stream, value); }
        }

        private double duration;
        public double Duration
        {
            get { return duration; }
            set
            {
                duration = value;
                OnPropertyChanged("Duration");
            }
        }

        private bool eqEnabled;
        public bool EQEnabled
        {
            get { return eqEnabled; }
            set
            {
                if (eqEnabled != value)
                {
                    eqEnabled = value;
                    OnPropertyChanged("EQEnabled");

                    if (eqEnabled) InitEQ();
                    else RemoveEQ();
                }
            }
        }

        private SongItem currentPlaying;
        public SongItem CurrentPlaying
        {
            get { return currentPlaying; }
            set
            {
                if (currentPlaying != value)
                {
                    currentPlaying = value;
                    Settings.LastPlayed = Playlist.Items.IndexOf(currentPlaying);
                    OnPropertyChanged("CurrentPlaying");
                }
            }
        }

        private int Stream, SyncStream, EQStream;
        private DispatcherTimer SeekTimer;
        private SYNCPROC SyncProc;

        public MainModel()
        {
            InitBASS();

            Settings = new SettingsModel(this);
            Playlist = new PlaylistModel();
            Equalizer = new EqualizerModel();
            Theme = new ThemeModel();
            Updater = new UpdaterModel();
            Sharing = new SharingModel(Settings.EnableSharing);

            EQEnabled = Settings.EQName != "Off";

            if (Settings.LastPlayed != -1 && Settings.LastPlayed < Playlist.Items.Count)
            {
                CurrentPlaying = Playlist.Items[Settings.LastPlayed];
            }

            SeekTimer = new DispatcherTimer(DispatcherPriority.Normal);
            SeekTimer.Interval = TimeSpan.FromMilliseconds(500);
            SeekTimer.Tick += SeekTimer_Tick;
        }

        private void InitBASS()
        {
            BassNet.Registration("adrianitech@gmail.com", "2X393121152222");
            Bass.BASS_Init(-1, 44100, BASSInit.BASS_DEVICE_DEFAULT, IntPtr.Zero);
            BassFx.LoadMe();

            SyncProc = new SYNCPROC(MediaEnded);
        }

        private void SeekTimer_Tick(object sender, EventArgs e)
        {
            if (PositionChanged != null) PositionChanged(null, null);
        }

        public void LoadFile(string path)
        {
            var item = new SongItem() { Path = path };
            item.LoadTags();
            LoadFile(item);
        }

        public void LoadFile(SongItem item, bool autoPlay = false)
        {
            Bass.BASS_StreamFree(Stream);
            Bass.BASS_ChannelRemoveSync(Stream, SyncStream);
            RemoveEQ();

            Stream = Bass.BASS_StreamCreateFile(item.Path, 0, 0, BASSFlag.BASS_SAMPLE_FLOAT);

            Bass.BASS_ChannelSetAttribute(Stream, BASSAttribute.BASS_ATTRIB_VOL, (float)this.Volume);
            SyncStream = Bass.BASS_ChannelSetSync(Stream, BASSSync.BASS_SYNC_END, 0, SyncProc, IntPtr.Zero);
            if (EQEnabled) InitEQ();

            Duration = Bass.BASS_ChannelBytes2Seconds(Stream, Bass.BASS_ChannelGetLength(Stream));
            if (PositionChanged != null) PositionChanged(null, null);

            CurrentPlaying = item;

            if (autoPlay) Bass.BASS_ChannelPlay(Stream, false);
        }

        private void MediaEnded(int a, int b, int c, IntPtr d)
        {
            App.Current.Dispatcher.BeginInvoke(new Action(() =>
            {
                this.IsPlaying = false;

                if (Settings.Repeat)
                {
                    LoadFile(CurrentPlaying);
                    Play();
                }
                else if (Settings.Shuffle)
                {
                    var rand = new Random();
                    var index = rand.Next(0, Playlist.Items.Count);
                    LoadFile(Playlist.Items[index]);
                    Play();
                }
                else
                {
                    var item = Playlist.GetNextItem(CurrentPlaying);
                    if (item != null)
                    {
                        LoadFile(item);
                        Play();
                    }
                }

            }), DispatcherPriority.Normal);
        }

        public void Play()
        {
            if (Bass.BASS_ChannelPlay(Stream, false))
            {
                IsPlaying = true;
            }
            else if(CurrentPlaying != null)
            {
                LoadFile(CurrentPlaying, true);
            }
        }

        public void Pause()
        {
            if (Bass.BASS_ChannelPause(Stream))
            {
                IsPlaying = false;
            }
        }

        public void TogglePlay()
        {
            if (IsPlaying) Pause();
            else Play();
        }

        public void PlayPrev()
        {
            var item = Playlist.GetPrevItem(CurrentPlaying);
            if (item != null)
            {
                LoadFile(item);
                Play();
            }
        }

        public void PlayNext()
        {
            var item = Playlist.GetNextItem(CurrentPlaying);
            if (item != null)
            {
                LoadFile(item);
                Play();
            }
        }

        private void InitEQ()
        {
            EQStream = Bass.BASS_ChannelSetFX(Stream, BASSFXType.BASS_FX_BFX_PEAKEQ, 0);

            var eq = new BASS_BFX_PEAKEQ()
            {
                fQ = 0f,
                fBandwidth = 2.5f,
                lChannel = BASSFXChan.BASS_BFX_CHANALL
            };

            for (int i = 0; i < EqualizerModel.EQ_FREQ.Length; i++)
            {
                eq.lBand = i;
                eq.fCenter = (float)EqualizerModel.EQ_FREQ[i];
                Bass.BASS_FXSetParameters(EQStream, eq);
                UpdateEQ(i, Settings.EQValues[i]);
            }
        }

        private void RemoveEQ()
        {
            Bass.BASS_ChannelRemoveFX(Stream, EQStream);
        }

        public void UpdateEQ(int band, float gain)
        {
            var eq = new BASS_BFX_PEAKEQ();
            eq.lBand = band;

            Bass.BASS_FXGetParameters(EQStream, eq);
            eq.fGain = gain;

            Bass.BASS_FXSetParameters(EQStream, eq);
        }

        public static string GetUserDataFolder
        {
            get
            {
                var root = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "mPlayer");
                if (!Directory.Exists(root)) Directory.CreateDirectory(root);
                return root;
            }
        }
    }
}
