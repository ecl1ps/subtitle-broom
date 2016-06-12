using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Navigation;
using Application = System.Windows.Application;


namespace SubtitleBroom
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        public ObservableCollection<SubtitleData> SubtitlesWithoutVideo { get; private set; }

        public ObservableCollection<string> VideosWithoutSubtitles { get; private set; }

        public ObservableCollection<string> IgnoredVideos { get; private set; }
 
        public MainWindow()
        {
            InitializeComponent();

            DataContext = this;

            lblDirectory.Content = Config.LastDirectory;

            IgnoredVideos = new ObservableCollection<string>(Config.IgnoredVideos);
            VideosWithoutSubtitles = new ObservableCollection<string>();
            SubtitlesWithoutVideo = new ObservableCollection<SubtitleData>();

            if (!string.IsNullOrEmpty(Config.LastDirectory))
            {
                #pragma warning disable 4014
                checkSubtitlesAsync();
                #pragma warning restore 4014
            }
        }

        private async void btnSelect_Click(object sender, RoutedEventArgs e)
        {
            var dialog = new System.Windows.Forms.FolderBrowserDialog
            {
                SelectedPath = Config.LastDirectory,
                ShowNewFolderButton = false
            };

            if (dialog.ShowDialog() != System.Windows.Forms.DialogResult.OK)
                return;

            lblDirectory.Content = Config.LastDirectory = dialog.SelectedPath;

            await checkSubtitlesAsync();
        }

        private void btnClose_Click(object sender, RoutedEventArgs e)
        {
            Application.Current.Shutdown(0);
        }

        private async void btnCheck_Click(object sender, RoutedEventArgs e)
        {
            if (Config.LastDirectory == null)
                return;

            await checkSubtitlesAsync();
        }

        private async Task checkSubtitlesAsync()
        {
            var groomer = new Groomer(Config.LastDirectory);

            lblStatus.Content = "Working...";
            lblTotal.Content = 0;
            lblNeedMoving.Content = 0;
            lblWithoutVideo.Content = 0;
            lblWithoutSubtitles.Content = 0;
            lblRenamed.Content = 0;

            await groomer.CheckSubtitlesAsync();

            lblStatus.Content = "Ready";

            VideosWithoutSubtitles.Clear();
            foreach (var video in groomer.VideosWithoutSubtitle
                .Where(video => !Config.IgnoredVideos.Any(ignored => video.FullName.EndsWith(ignored, StringComparison.OrdinalIgnoreCase))))
            {
                VideosWithoutSubtitles.Add(video.FullName);
            }

            lblTotal.Content = groomer.SubtitlesTotal;
            lblNeedMoving.Content = groomer.SubtitlesNeedMoving.Count;
            lblWithoutVideo.Content = groomer.SubtitlesWithoutVideo.Count;
            lblWithoutLang.Content = groomer.SubtitlesWithoutLang;
            updateVideosWithoutSubtitleCount();

            lbNeedMoving.ItemsSource = groomer.SubtitlesNeedMoving;
            SubtitlesWithoutVideo.Clear();
            groomer.SubtitlesWithoutVideo.ForEach(fi => SubtitlesWithoutVideo.Add(new SubtitleData { Subtitle = fi }));
        }

        private void updateVideosWithoutSubtitleCount()
        {
            lblWithoutSubtitles.Content = string.Format("{0} ({1} ignored)", VideosWithoutSubtitles.Count, Config.IgnoredVideos.Count);
        }

        private async void btnFixNaming_Click(object sender, RoutedEventArgs e)
        {
            if (Config.LastDirectory == null)
                return;

            var groomer = new Groomer(Config.LastDirectory);

            await groomer.RenameSubtitlesWithLangCodeAsync();

            await Task.Delay(100);

            await checkSubtitlesAsync();

            lblRenamed.Content = groomer.SubtitlesRenamed;
        }

        private async void btnFixLocations_Click(object sender, RoutedEventArgs e)
        {
            if (Config.LastDirectory == null)
                return;

            var groomer = new Groomer(Config.LastDirectory);

            await groomer.MoveSubtitlesNextToVideoAsync();

            await Task.Delay(100);

            await checkSubtitlesAsync();
        }

        private void Button_Click(object sender, RoutedEventArgs e)
        {
            var btn = sender as Button;
            if (btn == null)
                return;

            var ignoredVideo = btn.DataContext as string;

            VideosWithoutSubtitles.Remove(ignoredVideo);
            Config.IgnoredVideos.Add(Path.GetFileName(ignoredVideo));
            IgnoredVideos.Add(Path.GetFileName(ignoredVideo));

            updateVideosWithoutSubtitleCount();
        }

        private async void Button_Click_1(object sender, RoutedEventArgs e)
        {
            var btn = sender as Button;
            if (btn == null)
                return;

            var ignoredVideo = btn.DataContext as string;
            Config.IgnoredVideos.Remove(ignoredVideo);
            IgnoredVideos.Remove(ignoredVideo);
            
           await checkSubtitlesAsync();
        }

        private void Button_ClickView(object sender, RoutedEventArgs e)
        {
            var btn = sender as Button;
            if (btn == null)
                return;

            var file = btn.DataContext as string;

            System.Diagnostics.Process.Start("explorer.exe", "/select, " + file);
        }

        private void subtitleItem_OnMouseUp(object sender, MouseButtonEventArgs e)
        {
            if (e.ChangedButton != MouseButton.Left)
                return;

            var subtitleData = lvSubtitlesWithoutVideo.SelectedItem as SubtitleData;
            if (subtitleData == null)
                return;

            subtitleData.IsActive = !subtitleData.IsActive;

            if (!subtitleData.IsActive)
                return;

            subtitleData.AvailableVideos.Clear();
            foreach (var video in Groomer.GetAvailableVideosInDirectory(subtitleData.Subtitle.Directory).Where(video => !Groomer.HasVideoSubtitle(video)))
                subtitleData.AvailableVideos.Add(Path.GetFileName(video));
        }

        private void subtitleItemVideo_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            lblRenamed.Content = 0;

            if (e.AddedItems.Count == 0)
                return;

            var video = e.AddedItems[0] as string;
            if (video == null)
                return;

            var subtitleData = lvSubtitlesWithoutVideo.SelectedItem as SubtitleData;
            if (subtitleData == null)
                return;

            try
            {
                var hasLangCode = Groomer.HasSubtitleLangCode(subtitleData.Subtitle.Name);

                Groomer.RenameSubtitleToMatchVideo(subtitleData.Subtitle, video);

                SubtitlesWithoutVideo.Remove(subtitleData);
                lblWithoutVideo.Content = SubtitlesWithoutVideo.Count;
                if (!hasLangCode)
                    lblWithoutLang.Content = (int)lblWithoutLang.Content - 1;

                var videoItem = VideosWithoutSubtitles.FirstOrDefault(v => v.Contains(video));
                if (videoItem != null)
                {
                    VideosWithoutSubtitles.Remove(videoItem);
                    lblWithoutSubtitles.Content = VideosWithoutSubtitles.Count;
                }

                lblRenamed.Content = 1;
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message, "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
    }
}
