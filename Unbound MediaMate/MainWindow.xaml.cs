using LibVLCSharp.Shared;
using LibVLCSharp.WPF;
using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using System.Windows.Threading;
using MediaPlayer = LibVLCSharp.Shared.MediaPlayer; // Using vlc's Media Player Class

namespace Unbound_MediaMate
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        private readonly LibVLC _vlcLibrary; // Required for library operations. Initializes VLC Engine.
        private readonly MediaPlayer _mediaPlayer; // Provides media playback functionalities
        private bool isMediaPlayerPlaying = false;
        private bool isUserDraggingSlider = false;
        private int lastVolumeLevel = Constants.kHalfVolume; // Defaulting volume to an acceptable level


        public MainWindow()
        {
            InitializeComponent();

            // Initializing LibVLCSharp
            Core.Initialize();

            // Create new LibVLC instance
            _vlcLibrary = new LibVLC();

            // Create new MediaPlayer instance
            _mediaPlayer = new MediaPlayer(_vlcLibrary);

            videoView.MediaPlayer = _mediaPlayer; // Connecting MediaPlayer logic to VideoView visual component

            DispatcherTimer timer = new DispatcherTimer(); // Initializing timer
            timer.Interval = TimeSpan.FromSeconds(Constants.kOne); // Timer interval is 1 second
            timer.Tick += Timer_Tick; // Will create this method
            timer.Start();
        }

        private void Timer_Tick(object sender, EventArgs e)
        {
            if (_mediaPlayer.Media != null && _mediaPlayer.IsSeekable && !isUserDraggingSlider)
            { // Statement above checks if theres a source, if there is any lenght to the video and that the slider is not being dragged
                sliProgress.Minimum = Constants.kZero;
                sliProgress.Maximum = _mediaPlayer.Length; // Length of media
                sliProgress.Value = _mediaPlayer.Time; // Current elapsed time
            }
        }

        private void Open_CanExecute(object sender, CanExecuteRoutedEventArgs e)
        {
            e.CanExecute = true;
        }

        private void Open_Executed(object sender, ExecutedRoutedEventArgs e)
        {
            OpenFileDialog openFileDialog = new OpenFileDialog(); 
            openFileDialog.Filter = "Media files (*.mp3;*.mpg;*.mpeg;*.mkv;*.mp4)|*.mp3;*.mpg;*.mpeg;*.mkv;*.mp4|All files (*.*)|*.*";
            if (openFileDialog.ShowDialog() == true) // If the user selects a file to open
            {
                try
                {
                    var mediaToPlay = new Media(_vlcLibrary, new Uri(openFileDialog.FileName));
                    // Creating a new instance of "media" that points to the user chosen file ^^^
                    _mediaPlayer.Media = mediaToPlay; // "Media" object is getting deliverd so it can be played back
                    _mediaPlayer.Play(); // Play the selected media using VLC Library
                }
                catch (FileNotFoundException) 
                {
                    MessageBox.Show("The selected file was not found.", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                }
                catch (Exception ex) // This will catch general exceptions, including those from LibVLCSharp
                {
                    MessageBox.Show($"An error occurred: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error); 
                    // Adding exception message to string ^
                }


            }
        }

        private void Play_CanExecute(object sender, CanExecuteRoutedEventArgs e)
        {
            e.CanExecute = false; // Initialize to false as a safe default

            if (_mediaPlayer != null && _mediaPlayer.Media != null)
            { // If the media player has been initialized and there is a media source loaded
                e.CanExecute = true;
            }
            
        }

        private void Play_Executed(object sender, ExecutedRoutedEventArgs e)
        {
            try
            {
                // State Checking: If media player is not playing, attempt to play
                if (_mediaPlayer.State != VLCState.Playing)
                {
                    // User Feedback: Display a loading or buffering message
                    statusLabel.Content = "Loading..."; 

                    _mediaPlayer.Play(); // Play media

                    // Volume: Set to the last known volume level
                    _mediaPlayer.Volume = lastVolumeLevel; 

                    isMediaPlayerPlaying = true; // Media player is playing

                    // User Feedback: Clear loading message or update status
                    statusLabel.Content = "Playing";
                }
            }
            catch (Exception ex)
            {
                // Error Handling: Inform the user of the error
                MessageBox.Show($"An error occurred: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void Pause_CanExecute(object sender, CanExecuteRoutedEventArgs e)
        { // If the media is playing then it can be paused, else no. 
            e.CanExecute = (_mediaPlayer != null) && (_mediaPlayer.State == VLCState.Playing);
        }

        private void Pause_Executed(object sender, ExecutedRoutedEventArgs e)
        {
            try
            {
                // Check if the media player is currently playing
                if (_mediaPlayer.State == VLCState.Playing)
                {
                    _mediaPlayer.Pause();
                    isMediaPlayerPlaying = false;
                    statusLabel.Content = "Paused";
                }
            }
            catch (Exception ex)
            {
                // Error Handling: Inform the user of the error
                MessageBox.Show($"An error occurred while trying to pause: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void Stop_CanExecute(object sender, CanExecuteRoutedEventArgs e)
        { // if there is media loaded and it is either playing or paused then the event can execute
            e.CanExecute = (_mediaPlayer != null) && (_mediaPlayer.State == VLCState.Playing || _mediaPlayer.State == VLCState.Paused);
        }

        private void Stop_Executed(object sender, ExecutedRoutedEventArgs e)
        {
            const int kDefaultProgress = 0; // The default slide bar progress when videos are stopped
            try
            {
                // Stop the media player
                _mediaPlayer.Stop();
                isMediaPlayerPlaying = false;

                // Reset the progress 
                sliProgress.Value = kDefaultProgress;
                statusLabel.Content = "Stopped";
            }
            catch (Exception ex)
            {
                // Inform the user of the error
                MessageBox.Show($"An error occurred while trying to stop: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void Grid_MouseWheel(object sender, MouseWheelEventArgs e)
        {
            int volumeChangeAmount = 5;  // Represents a 5% change in volume. 
            const int kDefaultMouseScrollValue = 0; // Numeric value that determins if a mouse has scrolled or not

            if (e.Delta > kDefaultMouseScrollValue)  // If mouse wheel scrolled forward/upward
            {
                // Increase volume but don't exceed 100
                _mediaPlayer.Volume = Math.Min(_mediaPlayer.Volume + volumeChangeAmount, Constants.kFullVolume);
            }
            else  // If mouse wheel scrolled backward/downward
            {
                // Decrease volume but don't go below 0
                _mediaPlayer.Volume = Math.Max(_mediaPlayer.Volume - volumeChangeAmount, Constants.kMinVolume);
            }
        }




        private void VolumeSlider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            _mediaPlayer.Volume = (int)e.NewValue; // Set the volume in the media player
            lastVolumeLevel = (int)e.NewValue; // Update last volume level.
        }





    }
}
