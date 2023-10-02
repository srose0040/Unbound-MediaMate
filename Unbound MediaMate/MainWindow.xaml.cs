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


    }
}
