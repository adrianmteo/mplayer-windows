using Microsoft.Win32;
using mPlayer.View;
using System;
using System.Windows;
using System.Windows.Input;

namespace mPlayer
{
    public partial class MainWindow : Window
    {
        private DateTime sliderDelay = DateTime.Now;

        public MainWindow()
        {
            InitializeComponent();
            // Bind the main model
            this.DataContext = App.Model;
            // Model events
            App.Model.PositionChanged += Model_PositionChanged;
            // Load window settings
            this.Loaded += (sender, e) =>
            {
                App.Current.MainWindow = this;
                LoadSettings();
                // Check for updates
                if (App.Model.Settings.AutoCheckUpdates)
                {
                    App.Model.Updater.CheckForUpdate(true);
                }
            };
        }

        private void LoadSettings()
        {
            // Window size
            this.Width = App.Model.Settings.WindowSize.Width;
            this.Height = App.Model.Settings.WindowSize.Height;
            // Window position
            var pos = App.Model.Settings.WindowPosition;
            if (pos.X == 0 && pos.Y == 0)
            {
                this.Left = (SystemParameters.PrimaryScreenWidth - this.Width) / 2d;
                this.Top = (SystemParameters.PrimaryScreenHeight - this.Height) / 2d;
            }
            else
            {
                this.Left = pos.X;
                this.Top = pos.Y;
            }
            // Window state
            if (App.Model.Settings.WindowMaximized) this.WindowState = System.Windows.WindowState.Maximized;
        }

        private void Model_PositionChanged(object sender, EventArgs e)
        {
            if (!SliderSeek.IsMouseOver)
            {
                SliderSeek.Value = App.Model.Position;
            }
        }

        #region WindowCommands

        private void Window_SizeChanged(object sender, SizeChangedEventArgs e)
        {
            if (this.WindowState == System.Windows.WindowState.Normal)
            {
                App.Model.Settings.WindowSize = e.NewSize;
            }
        }

        private void Window_LocationChanged(object sender, EventArgs e)
        {
            if (this.WindowState == System.Windows.WindowState.Normal)
            {
                App.Model.Settings.WindowPosition = new Point(this.Left, this.Top);
            }
        }

        private void Window_StateChanged(object sender, EventArgs e)
        {
            if (this.WindowState != System.Windows.WindowState.Minimized)
            {
                App.Model.Settings.WindowMaximized = this.WindowState == System.Windows.WindowState.Maximized;

                if (this.WindowState == System.Windows.WindowState.Maximized)
                {
                    ButtonMaximize.Style = App.Current.Resources["RestoreButtonStyle"] as Style;
                }
                else
                {
                    ButtonMaximize.Style = App.Current.Resources["MaximizeButtonStyle"] as Style;
                    RootGrid.Margin = new Thickness(0);
                }
            }
        }

        private void ButtonMinimize_Click(object sender, RoutedEventArgs e)
        {
            this.WindowState = System.Windows.WindowState.Minimized;
        }

        private void ButtonMaximize_Click(object sender, RoutedEventArgs e)
        {
            if (this.WindowState == System.Windows.WindowState.Maximized)
            {
                this.WindowState = System.Windows.WindowState.Normal;
            }
            else
            {
                this.WindowState = System.Windows.WindowState.Maximized;
            }
        }

        private void ButtonClose_Click(object sender, RoutedEventArgs e)
        {
            this.Close();
        }

        #endregion

        #region PlayerCommands

        private void ButtonEject_Click(object sender, RoutedEventArgs e)
        {
            var open = new OpenFileDialog();
            var result = open.ShowDialog();

            if (result == true)
            {
                App.Model.LoadFile(open.FileName);
                App.Model.Play();
            }
        }

        private void ThumbButtonPrev_Click(object sender, EventArgs e)
        {
            ButtonPrev_Click(null, null);
        }

        private void ThumbButtonPlay_Click(object sender, EventArgs e)
        {
            ButtonPlay_Click(null, null);
        }

        private void ThumbButtonNext_Click(object sender, EventArgs e)
        {
            ButtonNext_Click(null, null);
        }

        private void ButtonPrev_Click(object sender, RoutedEventArgs e)
        {
            App.Model.PlayPrev();
        }

        private void ButtonNext_Click(object sender, RoutedEventArgs e)
        {
            App.Model.PlayNext();
        }

        private void ButtonPlay_Click(object sender, RoutedEventArgs e)
        {
            App.Model.TogglePlay();
        }

        private void SliderSeek_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            if ((SliderSeek.IsMouseOver) && DateTime.Now.Subtract(sliderDelay).TotalMilliseconds >= 500)
            {
                App.Model.Position = e.NewValue;
                sliderDelay = DateTime.Now;
            }
        }

        private void SliderSeek_PreviewMouseLeftButtonUp(object sender, MouseButtonEventArgs e)
        {
            App.Model.Position = SliderSeek.Value;
        }

        private void ButtonSettings_Click(object sender, RoutedEventArgs e)
        {
            new SettingsWindow().ShowDialog();
        }

        #endregion
    }
}
