using mPlayer.Model;
using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace mPlayer.View
{
    public partial class SettingsWindow : Window
    {
        private bool loadedEQ;

        public SettingsWindow()
        {
            InitializeComponent();

            this.DataContext = App.Model;

            this.Loaded += (sender, e) =>
            {
                ComboBoxPresets.SelectedIndex = 0;
                GridSliders.IsEnabled = false;

                if (App.Model.Settings.EQValues != null)
                {
                    for (int i = 0; i < App.Model.Settings.EQValues.Length; i++)
                    {
                        (this.FindName("EQ" + (i + 1)) as Slider).Value = App.Model.Settings.EQValues[i];
                    }
                }

                if (App.Model.Settings.EQName != null)
                {
                    for (int i = 0; i < App.Model.Equalizer.Presets.Count; i++)
                    {
                        if (App.Model.Equalizer.Presets[i].Name == App.Model.Settings.EQName)
                        {
                            ComboBoxPresets.SelectedIndex = i;
                            GridSliders.IsEnabled = i == 1;
                            break;
                        }
                    }
                }

                loadedEQ = true;
            };
        }

        private void ButtonApply_Click(object sender, RoutedEventArgs e)
        {
            var color = ListThemes.SelectedItem as string;
            if (color != null) App.Model.Settings.Theme = color;
        }

        private void ComboBoxEQ_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (!loadedEQ) return;

            if (e.AddedItems.Count > 0)
            {
                var item = e.AddedItems[0] as EqualizerModel.EqualizerItem;

                if (item != null)
                {
                    App.Model.EQEnabled = item.Type != 0;

                    if (item.Type == 1)
                    {
                        GridSliders.IsEnabled = true;
                        ComboBoxPresets.IsEditable = true;
                    }
                    else
                    {
                        GridSliders.IsEnabled = false;
                        ComboBoxPresets.IsEditable = false;
                    }

                    if (item.Type > 1)
                    {
                        if (item.Values != null)
                        {
                            var values = item.Values.Split(';');
                            for (int i = 0; i < values.Length; i++)
                            {
                                (this.FindName("EQ" + (i + 1)) as Slider).Value = float.Parse(values[i]);
                            }
                        }
                    }

                    App.Model.Settings.EQName = item.Name;
                }
            }
        }

        private void ComboBoxPresets_KeyUp(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter && !string.IsNullOrWhiteSpace(ComboBoxPresets.Text))
            {
                var item = App.Model.Equalizer.SavePreset(ComboBoxPresets.Text, App.Model.Settings.EQValues);
                ComboBoxPresets.SelectedItem = item;
            }
        }

        private void SliderEQ_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            if (!loadedEQ) return;

            var slider = sender as FrameworkElement;
            var band = int.Parse(slider.Tag.ToString());
            var value = (float)e.NewValue;

            App.Model.Settings.EQValues[band] = value;
            App.Model.UpdateEQ(band, value);

            var values = new float[10];
            for (int i = 0; i < values.Length; i++)
            {
                values[i] = (float)(this.FindName("EQ" + (i + 1)) as Slider).Value;
            }
            App.Model.Settings.EQValues = values;
        }

        private void ButtonClearCache_Click(object sender, RoutedEventArgs e)
        {
            var dir = Path.Combine(MainModel.GetUserDataFolder, "Cache");
            if (Directory.Exists(dir))
            {
                foreach (var file in Directory.GetFiles(dir))
                {
                    try { File.Delete(file); }
                    catch { }
                }
            }

            MessageBox.Show("Cache was successfully cleared!");
        }

        private void ButtonCheckUpdates_Click(object sender, RoutedEventArgs e)
        {
            App.Model.Updater.CheckForUpdate();
        }

        private void ButtonDownloadUpdate_Click(object sender, RoutedEventArgs e)
        {
            App.Model.Updater.DownloadUpdate();
        }

        private void HyperlinkChangelog_Click(object sender, RoutedEventArgs e)
        {
            if (App.Model.Updater.Changelog != null)
            {
                new ChangelogWindow(App.Model.Updater.Changelog).ShowDialog();
            }
        }
    }
}
