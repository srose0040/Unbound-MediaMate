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
using System.Windows.Controls.Primitives;
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
        private int lastVolumeLevel;


        public MainWindow()
        {
            InitializeComponent();

            // Initializing LibVLCSharp
            Core.Initialize();

            // Create new LibVLC instance
            _vlcLibrary = new LibVLC();

            // Create new MediaPlayer instance
            _mediaPlayer = new MediaPlayer(_vlcLibrary);

            // Unsubscribe and then subscribe to the Playing event so the event handler is not called multiple times for a single event occurrence.
            _mediaPlayer.Playing -= MediaPlayer_Playing;
            _mediaPlayer.Playing += MediaPlayer_Playing;

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
            openFileDialog.Filter = "Media files (*.mp3;*.mpg;*.mpeg;*.mkv;*.mp4;*.wav;*.flac;*.aac;*.ogg;*.wma;.avi;*.mov;*.flv;*.3gp;*.vob;*.srt;*.sub;*.ass)" +
                "|*.mp3;*.mpg;*.mpeg;*.mkv;*.mp4;*.wav;*.flac;*.aac;*.ogg;*.wma;.avi;*.mov;*.flv;*.3gp;*.vob;*.srt;*.sub;*.ass|All files (*.*)|*.*";
            if (openFileDialog.ShowDialog() == true) // If the user selects a file to open
            {
                try
                {
                    var mediaToPlay = new Media(_vlcLibrary, new Uri(openFileDialog.FileName));
                    // Creating a new instance of "media" that points to the user chosen file ^^^
                    _mediaPlayer.Media = mediaToPlay; // "Media" object is getting deliverd so it can be played back

                    lastVolumeLevel = _mediaPlayer.Volume; // Remembering volume level
                    Play_Executed(sender, e); // Calling play method
                    
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
            // If the media player has been initialized and there is a media source loaded
            e.CanExecute = (_mediaPlayer != null) &&
                   (_mediaPlayer.Media != null) &&
                   (_mediaPlayer.State != VLCState.Playing &&
                    _mediaPlayer.State != VLCState.Buffering &&
                    _mediaPlayer.State != VLCState.Opening);
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

        private void MediaPlayer_Playing(object sender, EventArgs e)
        {
            // These variables keep track of the presence of video and audio tracks
            bool hasVideo = false;
            bool hasAudio = false;

            // Iterate through each of the tracks present in the media being played
            foreach (var track in _mediaPlayer.Media.Tracks)
            {
                // If its a video
                if (track.TrackType == TrackType.Video)
                {
                    hasVideo = true;
                }
                else if (track.TrackType == TrackType.Audio) // or audio
                {
                    hasAudio = true;
                }
            }

            // Use the Dispatcher to run the code on the UI thread ensuring that any operations inside that code block that modify the UI are safe
            Dispatcher.Invoke(() =>
            {

            if (hasVideo)
            {
                // If the media has video, display the video and hide the default image
                defaultImage.Visibility = Visibility.Collapsed;
                videoView.Visibility = Visibility.Visible;
                
            }
            else if (hasAudio)
            {
                // If the media only has audio, hide the video view and display the default image
                videoView.Visibility = Visibility.Collapsed;
                defaultImage.Visibility = Visibility.Visible;
            }
            else
            {
                // Neither video nor audio tracks were detected
                MessageBox.Show("The media doesn't contain recognizable audio or video tracks.", "Media Error", MessageBoxButton.OK, MessageBoxImage.Warning);
            }

            });
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
            e.CanExecute = (_mediaPlayer != null) &&
                    (_mediaPlayer.State != VLCState.Stopped &&
                     _mediaPlayer.State != VLCState.Buffering &&
                     _mediaPlayer.State != VLCState.Opening && _mediaPlayer.State != VLCState.NothingSpecial);
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

        /* REMOVED FROM FINAL PROJECT
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
        } */

        private void sliProgress_DragStarted(object sender, DragStartedEventArgs e)
        {
            isUserDraggingSlider = true;
        }

        private void sliProgress_DragCompleted(object sender, DragCompletedEventArgs e)
        {
            // Resetting slider drag to false
            isUserDraggingSlider = false;
            // The position of the slider is now new position in time for the media
            _mediaPlayer.Time = (long)sliProgress.Value;
        }

        private void sliProgress_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            if (isUserDraggingSlider)
            {
                _mediaPlayer.Time = (long)sliProgress.Value; // Supports clicking positions on the slider
            }
            // Updates the label to show the current playback time in a format of hours:minutes:seconds based on the value of the sliProgress slider
            lblProgressStatus.Text = TimeSpan.FromMilliseconds(sliProgress.Value).ToString(@"hh\:mm\:ss");
        }






    }
}
