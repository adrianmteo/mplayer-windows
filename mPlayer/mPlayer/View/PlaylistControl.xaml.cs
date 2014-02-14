using Microsoft.Win32;
using mPlayer.Model;
using System;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Input;
using System.Windows.Media;

namespace mPlayer.View
{
    public partial class PlaylistControl : Popup
    {
        public PlaylistControl()
        {
            InitializeComponent();
            this.CustomPopupPlacementCallback = new CustomPopupPlacementCallback(CenterPlacement);
        }

        private CustomPopupPlacement[] CenterPlacement(Size popupSize, Size targetSize, Point offset)
        {
            var p1 = new CustomPopupPlacement(new Point(-(popupSize.Width - targetSize.Width) / 2, -popupSize.Height + offset.Y), PopupPrimaryAxis.Horizontal);
            return new CustomPopupPlacement[] { p1 };
        }

        // Now playing

        private void ListPlaylist_MouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            var elem = e.OriginalSource as FrameworkElement;
            if (elem == null) return;

            var item = elem.DataContext as SongItem;
            if (item == null) return;

            App.Model.LoadFile(item);
            App.Model.Play();
        }

        private void ButtonReorder_PreviewMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            if (ListPlaylist.SelectedItems.Count > 0)
            {
                DragDrop.DoDragDrop(this, GetSelectedItems, DragDropEffects.Move);
            }
        }

        private void ListPlaylist_DragOver(object sender, DragEventArgs e)
        {
            if (!e.Data.GetDataPresent(typeof(SongItem[])))
            {
                e.Effects = DragDropEffects.None;
                return;
            }

            var pos = e.GetPosition(ListPlaylist);

            var sv = VisualTreeHelper.GetChild(ListPlaylist, 0) as ScrollViewer;
            var lines = Keyboard.IsKeyDown(Key.LeftShift) ? 5 : 1;

            if (pos.Y <= 50)
            {
                sv.ScrollToVerticalOffset(sv.VerticalOffset - lines);
            }
            if (pos.Y >= ListPlaylist.ActualHeight - 50)
            {
                sv.ScrollToVerticalOffset(sv.VerticalOffset + lines);
            }

            var items = (SongItem[])e.Data.GetData(typeof(SongItem[]));

            var hover = (e.OriginalSource as FrameworkElement).DataContext as SongItem;
            if (hover == null) return;

            foreach (var item in items)
            {
                if (item == hover)
                {
                    return;
                }
            }

            var newIndex = App.Model.Playlist.Items.IndexOf(hover);
            if (newIndex >= App.Model.Playlist.Items.Count) return;

            foreach (var item in items)
            {
                var index = App.Model.Playlist.Items.IndexOf(item);
                App.Model.Playlist.Items.Move(index, newIndex);
            }
        }

        private void ListPlaylist_Drop(object sender, DragEventArgs e)
        {
            App.Model.Playlist.SavePlaylist();
        }

        public void ShowCurrent()
        {
            ListPlaylist.SelectedItem = App.Model.CurrentPlaying;
            ListPlaylist.ScrollIntoView(App.Model.CurrentPlaying);
        }

        public SongItem[] GetSelectedItems
        {
            get
            {
                var items = new SongItem[ListPlaylist.SelectedItems.Count];
                for (int i = 0; i < ListPlaylist.SelectedItems.Count; i++)
                {
                    items[i] = ListPlaylist.SelectedItems[i] as SongItem;
                }

                return items;
            }
        }

        private void TextBoxSearch_TextChanged(object sender, TextChangedEventArgs e)
        {
            App.Model.Playlist.FilterItems((sender as TextBox).Text);
        }

        private void ButtonAddFiles_Click(object sender, RoutedEventArgs e)
        {
            var dialog = new OpenFileDialog()
            {
                Multiselect = true
            };
            var result = dialog.ShowDialog();

            if (result == true)
            {
                App.Model.Playlist.AddFiles(dialog.FileNames);
            }
        }

        private void ButtonAddFolder_Click(object sender, RoutedEventArgs e)
        {
            var dialog = new System.Windows.Forms.FolderBrowserDialog();
            if (dialog.ShowDialog() == System.Windows.Forms.DialogResult.OK)
            {
                App.Model.Playlist.AddFolder(dialog.SelectedPath);
            }
        }

        private void ButtonRemoveItems_Click(object sender, RoutedEventArgs e)
        {
            var items = GetSelectedItems;
            App.Model.Playlist.RemoveItems(items);
        }

        private void ButtonShowCurrent_Click(object sender, RoutedEventArgs e)
        {
            ListPlaylist.SelectedItem = App.Model.CurrentPlaying;
            ListPlaylist.ScrollIntoView(ListPlaylist.SelectedItem);
        }

        // Playlists

        private void ButtonSavePlaylist_Click(object sender, RoutedEventArgs e)
        {
            if (!string.IsNullOrWhiteSpace(TextPlaylistName.Text))
            {
                App.Model.Playlist.SaveLocalPlaylist(TextPlaylistName.Text);
            }
        }

        private void ButtonLoadPlaylist_Click(object sender, RoutedEventArgs e)
        {
            if (ListSavedPlaylists.SelectedItem != null)
            {
                var item = ListSavedPlaylists.SelectedItem as PlaylistItem;
                App.Model.Playlist.LoadPlaylist(item.Path);
            }
        }

        private void ButtonDeletePlaylist_Click(object sender, RoutedEventArgs e)
        {
            if (ListSavedPlaylists.SelectedItems.Count > 0)
            {
                var list = new PlaylistItem[ListSavedPlaylists.SelectedItems.Count];
                for (int i = 0; i < ListSavedPlaylists.SelectedItems.Count; i++) list[i] = ListSavedPlaylists.SelectedItems[i] as PlaylistItem;

                foreach (var item in list)
                {
                    App.Model.Playlist.DeletePlaylist(item);
                }
            }
        }

        protected override void OnOpened(System.EventArgs e)
        {
            TextPlaylistName.Text = "Playlist [" + DateTime.Now.ToShortDateString() + "]";
        }

        // Home sharing

        private void ListBoxExplorer_MouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            var item = (e.OriginalSource as FrameworkElement).DataContext as mPlayer.Model.SharingModel.FileItem;
            if (item.IsFolder)
            {
                App.Model.Sharing.SendMessage("LIST " + item.Path);
            }
        }

        private void ButtonSelectAll_Click(object sender, RoutedEventArgs e)
        {
            foreach (var item in App.Model.Sharing.NavigationList)
            {
                if (!item.IsFolder) item.IsChecked = true;
            }
        }

        private void ButtonSelectNone_Click(object sender, RoutedEventArgs e)
        {
            foreach (var item in App.Model.Sharing.NavigationList)
            {
                if (!item.IsFolder) item.IsChecked = false;
            }
        }

        private void ButtonDownload_Click(object sender, RoutedEventArgs e)
        {
            var list = App.Model.Sharing.NavigationList.Where(person => person.IsChecked);
            App.Model.Sharing.DownloadFiles(list.ToArray());
        }

        private void ButtonConnect_Click(object sender, RoutedEventArgs e)
        {
            App.Model.Sharing.Connect();
        }

        private void ButtonCancel_Click(object sender, RoutedEventArgs e)
        {
            App.Model.Sharing.IsCanceled = true;
        }

        private void ButtonDisconnect_Click(object sender, RoutedEventArgs e)
        {
            App.Model.Sharing.IsConnected = false;
        }
    }

    public class ExplorerTemplateSelector : DataTemplateSelector
    {
        public DataTemplate FileTemplate
        { get; set; }

        public DataTemplate FolderTemplate
        { get; set; }

        public override DataTemplate SelectTemplate(object item, DependencyObject container)
        {
            var data = item as SharingModel.FileItem;
            return data.IsFolder ? FolderTemplate : FileTemplate;
        }
    }
}
