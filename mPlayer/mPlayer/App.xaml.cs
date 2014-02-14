using mPlayer.Model;
using mPlayer.Properties;
using System;
using System.IO;
using System.Reflection;
using System.Windows;

namespace mPlayer
{
    public partial class App : Application
    {
        public static MainModel Model;

        public App()
        {
            Model = new MainModel();
        }

        protected override void OnExit(ExitEventArgs e)
        {
            Model.Playlist.SavePlaylist();
            Model.Settings.SaveSettings();
            Model.Sharing.Close();
        }
    }
}
