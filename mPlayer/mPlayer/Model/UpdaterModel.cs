using System;
using System.Diagnostics;
using System.IO;
using System.Net;
using System.Reflection;
using System.Windows;
using System.Windows.Threading;

namespace mPlayer.Model
{
    public class UpdaterModel : BindableObject
    {
        #region Constants

        private const string CHECK_URL = "http://imadrian.net/files/mplayer_latest.txt";
        private const string CHANGELOG_URL = "http://imadrian.net/files/mplayer_changelog.txt";
        private const string SETUP_URL = "http://imadrian.net/files/mplayer_setup.exe";
        private const string TEMP_NAME = "mplayer_setup.exe";

        #endregion

        #region Properties

        private bool isChecking;
        public bool IsChecking
        {
            get { return isChecking; }
            set
            {
                isChecking = value;
                OnPropertyChanged("IsChecking");
            }
        }

        private bool isUpdateAvailable;
        public bool IsUpdateAvailable
        {
            get { return isUpdateAvailable; }
            set
            {
                isUpdateAvailable = value;
                OnPropertyChanged("IsUpdateAvailable");
            }
        }

        private bool isDownloading;
        public bool IsDownloading
        {
            get { return isDownloading; }
            set
            {
                isDownloading = value;
                OnPropertyChanged("IsDownloading");
            }
        }

        private int progress;
        public int Progress
        {
            get { return progress; }
            set
            {
                progress = value;
                OnPropertyChanged("Progress");
            }
        }

        private string status;
        public string Status
        {
            get { return status; }
            set
            {
                status = value;
                OnPropertyChanged("Status");
            }
        }

        private string changelog;
        public string Changelog
        {
            get { return changelog; }
            set
            {
                changelog = value;
                OnPropertyChanged("Changelog");
            }
        }

        #endregion

        public async void CheckForUpdate(bool showMessage = false)
        {
            IsChecking = true;
            Status = "Checking for updates...";

            var version = await LastFMWrapper.DownloadString(CHECK_URL);
            if (version != null)
            {
                IsUpdateAvailable = IsVersionHigher(version);
                if (!IsUpdateAvailable) Status = "There is no update available";
                else
                {
                    Changelog = await LastFMWrapper.DownloadString(CHANGELOG_URL);

                    if (showMessage)
                    {
                        if (MessageBox.Show("Do you want to download it and then install it?", "A new update is available", MessageBoxButton.YesNo) == MessageBoxResult.Yes)
                        {
                            DownloadUpdate();
                        }
                    }
                }
            }
            else
            {
                Status = "Could not check for updates";
            }

            IsChecking = false;
        }

        public void DownloadUpdate()
        {
            Progress = 0;
            IsDownloading = true;

            var client = new WebClient();
            client.DownloadProgressChanged += client_DownloadProgressChanged;
            client.DownloadFileCompleted += client_DownloadFileCompleted;
            client.DownloadFileAsync(new Uri(SETUP_URL), Path.Combine(Path.GetTempPath(), TEMP_NAME));
        }

        private void client_DownloadFileCompleted(object sender, System.ComponentModel.AsyncCompletedEventArgs e)
        {
            IsDownloading = false;

            var path = Path.Combine(Path.GetTempPath(), TEMP_NAME);
            if (File.Exists(path))
            {
                try
                {
                    Application.Current.Dispatcher.BeginInvoke(new Action(() =>
                    {
                        Process.Start(path);
                        Application.Current.Shutdown();
                    }), DispatcherPriority.Normal);
                }
                catch
                {
                    IsUpdateAvailable = false;
                    Status = "The setup file could not be opened";
                }
            }
        }

        private void client_DownloadProgressChanged(object sender, DownloadProgressChangedEventArgs e)
        {
            Progress = e.ProgressPercentage;
        }

        private bool IsVersionHigher(string version)
        {
            double ver1 = 0, ver2 = 0;

            double.TryParse(version.Replace('.', ','), out ver1);
            double.TryParse(Version.Replace('.', ','), out ver2);

            return ver1 > ver2;
        }

        public string Version
        {
            get
            {
                var major = Assembly.GetExecutingAssembly().GetName().Version.Major.ToString();
                var minor = Assembly.GetExecutingAssembly().GetName().Version.Minor.ToString();
                var rev = Assembly.GetExecutingAssembly().GetName().Version.MinorRevision.ToString();
                if (rev == "0") return major + "." + minor;
                return major + "." + minor + rev;
            }
        }
    }
}
