using Microsoft.Win32;
using PdfiumViewer.Core;
using PdfiumViewer.Demo.Annotations;
using PdfiumViewer.Drawing;
using PdfiumViewer.Enums;
using System;
using System.ComponentModel;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace PdfiumViewer.Demo
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window, INotifyPropertyChanged
    {
        public MainWindow()
        {
            InitializeComponent();

            var version = GetType().Assembly.GetName().Version.ToString(3);
            Title = $"WPF PDFium Viewer Demo v{version}";
            CurrentProcess = Process.GetCurrentProcess();
            Cts = new CancellationTokenSource();
            DataContext = this;
            Renderer.PropertyChanged += delegate
            {
                OnPropertyChanged(nameof(Page));
                OnPropertyChanged(nameof(ZoomPercent));
            };

            MemoryChecker = new System.Windows.Threading.DispatcherTimer();
            MemoryChecker.Tick += OnMemoryChecker;
            MemoryChecker.Interval = new TimeSpan(0, 0, 1);
            MemoryChecker.Start();

            SearchManager = new PdfSearchManager(Renderer);
            MatchCaseCheckBox.IsChecked = SearchManager.MatchCase;
            WholeWordOnlyCheckBox.IsChecked = SearchManager.MatchWholeWord;
            HighlightAllMatchesCheckBox.IsChecked = SearchManager.HighlightAllMatches;
            Rendererw.PagesDisplayMode = PdfViewerPagesDisplayMode.ContinuousMode;
            Rendererw.Visibility = Visibility.Hidden;
        }


        private Process CurrentProcess { get; }
        private CancellationTokenSource Cts { get; }
        private System.Windows.Threading.DispatcherTimer MemoryChecker { get; }
        private PdfSearchManager SearchManager { get; }
        public string InfoText { get; set; }
        public string SearchTerm { get; set; }
        public PdfBookmarkCollection Bookmarks { get; set; }
        public bool ShowBookmarks { get; set; }
        public PdfBookmark SelectedBookIndex { get; set; }
        public double ZoomPercent
        {
            get => Renderer.Zoom * 100;
            set => Renderer.SetZoom(value / 100);
        }
        public bool IsSearchOpen { get; set; }
        public int SearchMatchItemNo { get; set; }
        public int SearchMatchesCount { get; set; }
        public int Page
        {
            get => Renderer.PageNo + 1;
            set => Renderer.GotoPage(Math.Min(Math.Max(value - 1, 0), Renderer.PageCount - 1));
        }
        public FlowDirection IsRtl
        {
            get => Renderer.IsRightToLeft ? FlowDirection.RightToLeft : FlowDirection.LeftToRight;
            set => Renderer.IsRightToLeft = value == FlowDirection.RightToLeft ? true : false;
        }
        private void OnMemoryChecker(object sender, EventArgs e)
        {
            CurrentProcess.Refresh();
            InfoText = $"Memory: {CurrentProcess.PrivateMemorySize64 / 1024 / 1024} MB";
            OnPropertyChanged(nameof(InfoText));
        }


        private async void RenderToMemory(object sender, RoutedEventArgs e)
        {
            try
            {
                var pageStep = Renderer.PagesDisplayMode == PdfViewerPagesDisplayMode.BookMode ? 2 : 1;
                Dispatcher.Invoke(() => Renderer.GotoPage(0));
                while (Renderer.PageNo < Renderer.PageCount - pageStep)
                {
                    Dispatcher.Invoke(() => Renderer.NextPage());
                    await Task.Delay(1);
                }
            }
            catch (Exception ex)
            {
                Cts.Cancel();
                Debug.Fail(ex.Message);
            }
        }
        private void OpenThumbnail(object sender, RoutedEventArgs e)
        {
            if (Rendererw.Visibility == Visibility.Visible)
                Rendererw.Visibility = Visibility.Hidden;
            else
                Rendererw.Visibility = Visibility;
        }
        private void OpenPdf(object sender, RoutedEventArgs e)
        {
            var dialog = new OpenFileDialog
            {
                Filter = "PDF Files (*.pdf)|*.pdf|All Files (*.*)|*.*",
                Title = "Open PDF File"
            };

            if (dialog.ShowDialog() == true)
            {
                Renderer.OpenPdf(new FileStream(dialog.FileName, FileMode.Open, FileAccess.Read, FileShare.Read));
                Rendererw.OpenPdf(new FileStream(dialog.FileName, FileMode.Open, FileAccess.Read, FileShare.Read));
                
                Rendererw.SetZoom(0.16);
                VerticalOffset = 0;
            }
        }
        protected override void OnClosed(EventArgs e)
        {
            base.OnClosed(e);

            MemoryChecker?.Stop();
            Renderer?.Dispose();
        }
        private void OnPrevPageClick(object sender, RoutedEventArgs e)
        {
            Renderer.PreviousPage();
        }
        private void OnNextPageClick(object sender, RoutedEventArgs e)
        {
            Renderer.NextPage();
        }
        private void OnFitWidth(object sender, RoutedEventArgs e)
        {
            Renderer.SetZoomMode(PdfViewerZoomMode.FitWidth);
        }
        private void OnFitHeight(object sender, RoutedEventArgs e)
        {
            Renderer.SetZoomMode(PdfViewerZoomMode.FitHeight);
        }
        private void OnZoomInClick(object sender, RoutedEventArgs e)
        {
            Renderer.ZoomIn();
        }
        private void OnZoomOutClick(object sender, RoutedEventArgs e)
        {
            Renderer.ZoomOut();
        }
        private void OnRotateLeftClick(object sender, RoutedEventArgs e)
        {
            Renderer.Counterclockwise();
        }
        private void OnRotateRightClick(object sender, RoutedEventArgs e)
        {
            Renderer.ClockwiseRotate();
        }
        private void OnInfo(object sender, RoutedEventArgs e)
        {
            var info = Renderer.GetInformation();
            if (info != null)
            {
                var sb = new StringBuilder();
                sb.AppendLine($"Author: {info.Author}");
                sb.AppendLine($"Creator: {info.Creator}");
                sb.AppendLine($"Keywords: {info.Keywords}");
                sb.AppendLine($"Producer: {info.Producer}");
                sb.AppendLine($"Subject: {info.Subject}");
                sb.AppendLine($"Title: {info.Title}");
                sb.AppendLine($"Create Date: {info.CreationDate}");
                sb.AppendLine($"Modified Date: {info.ModificationDate}");

                MessageBox.Show(sb.ToString(), "Information", MessageBoxButton.OK, MessageBoxImage.Information);
            }
        }
        private void OnGetText(object sender, RoutedEventArgs e)
        {
            var txtViewer = new TextViewer();
            var page = Renderer.PageNo;
            txtViewer.Body = Renderer.GetPdfText(page);
            txtViewer.Caption = $"Page {page + 1} contains {txtViewer.Body?.Length} character(s):";
            txtViewer.ShowDialog();
        }
        private void OnDisplayBookmarks(object sender, RoutedEventArgs e)
        {
            Bookmarks = Renderer.Bookmarks;
            if (Bookmarks?.Count > 0)
                ShowBookmarks = !ShowBookmarks;
        }
        private void OnContinuousModeClick(object sender, RoutedEventArgs e)
        {
            Renderer.PagesDisplayMode = PdfViewerPagesDisplayMode.ContinuousMode;
        }
        private void OnBookModeClick(object sender, RoutedEventArgs e)
        {
            Renderer.PagesDisplayMode = PdfViewerPagesDisplayMode.BookMode;
        }
        private void OnSinglePageModeClick(object sender, RoutedEventArgs e)
        {
            Renderer.PagesDisplayMode = PdfViewerPagesDisplayMode.SinglePageMode;
        }
        public event PropertyChangedEventHandler PropertyChanged;

        [NotifyPropertyChangedInvocator]
        protected virtual void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
        private void OnTransparent(object sender, RoutedEventArgs e)
        {
            if ((Renderer.Flags & PdfRenderFlags.Transparent) != 0)
            {
                Renderer.Flags &= ~PdfRenderFlags.Transparent;
            }
            else
            {
                Renderer.Flags |= PdfRenderFlags.Transparent;
            }
        }
        private void OpenCloseSearch(object sender, RoutedEventArgs e)
        {
            IsSearchOpen = !IsSearchOpen;
            OnPropertyChanged(nameof(IsSearchOpen));
        }
        private void OnSearchTermKeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter)
            {
                Search();
            }
        }
        private void SaveAsImages(object sender, RoutedEventArgs e)
        {
            // Create a "Save As" dialog for selecting a directory (HACK)
            var dialog = new Microsoft.Win32.SaveFileDialog
            {
                Title = "Select a Directory",
                Filter = "Directory|*.this.directory",
                FileName = "select"
            };
            // instead of default "Save As"
            // Prevents displaying files
            // Filename will then be "select.this.directory"
            if (dialog.ShowDialog() == true)
            {
                string path = dialog.FileName;
                // Remove fake filename from resulting path
                path = path.Replace("\\select.this.directory", "");
                path = path.Replace(".this.directory", "");
                // If user has changed the filename, create the new directory
                if (!Directory.Exists(path))
                {
                    Directory.CreateDirectory(path);
                }
                // Our final value is in path
                SaveAsImages(path);
            }
        }

        private void SaveAsImages(string path)
        {
            try
            {
                for (var i = 0; i < Renderer.PageCount; i++)
                {
                    var size = Renderer.Document.PageSizes[i];
                    var image = Rendererw.Document.Render(i, 150, 200, 60, 60, false);
                    var encoder = new PngBitmapEncoder();
                    encoder.Frames.Add(BitmapFrame.Create(image as BitmapSource));
                    using (var fileStream = new FileStream(Path.Combine(path, $"img{i}.png"), FileMode.Create))
                        encoder.Save(fileStream);
                }
            }
            catch (Exception ex)
            {
                Cts.Cancel();
                Debug.Fail(ex.Message);
                MessageBox.Show(this, ex.Message, "Error!");
            }
        }

        private void Search()
        {
            SearchMatchItemNo = 0;
            SearchManager.MatchCase = MatchCaseCheckBox.IsChecked.GetValueOrDefault();
            SearchManager.MatchWholeWord = WholeWordOnlyCheckBox.IsChecked.GetValueOrDefault();
            SearchManager.HighlightAllMatches = HighlightAllMatchesCheckBox.IsChecked.GetValueOrDefault();
            SearchMatchesTextBlock.Visibility = Visibility.Visible;

            if (!SearchManager.Search(SearchTerm))
            {
                MessageBox.Show(this, "No matches found.");
            }
            else
            {
                SearchMatchesCount = SearchManager.MatchesCount;
                // DisplayTextSpan(SearchMatches.Items[SearchMatchItemNo++].TextSpan);
            }

            if (!SearchManager.FindNext(true))
                MessageBox.Show(this, "Find reached the starting point of the search.");
        }

        private void DisplayTextSpan(PdfTextSpan span)
        {
            Page = span.Page + 1;
            Renderer.ScrollToVerticalOffset(span.Offset);
        }

        private void OnNextFoundClick(object sender, RoutedEventArgs e)
        {
            if (SearchMatchesCount > SearchMatchItemNo)
            {
                SearchMatchItemNo++;
                //DisplayTextSpan(SearchMatches.Items[SearchMatchItemNo - 1].TextSpan);
                SearchManager.FindNext(true);
            }
        }

        private void OnPrevFoundClick(object sender, RoutedEventArgs e)
        {
            if (SearchMatchItemNo > 1)
            {
                SearchMatchItemNo--;
                // DisplayTextSpan(SearchMatches.Items[SearchMatchItemNo - 1].TextSpan);
                SearchManager.FindNext(false);
            }
        }

        private void ToRtlClick(object sender, RoutedEventArgs e)
        {
            Renderer.IsRightToLeft = true;
            OnPropertyChanged(nameof(IsRtl));
        }

        private void ToLtrClick(object sender, RoutedEventArgs e)
        {
            Renderer.IsRightToLeft = false;
            OnPropertyChanged(nameof(IsRtl));
        }

        private async void OnClosePdf(object sender, RoutedEventArgs e)
        {
            try
            {
                InfoBar.Foreground = System.Windows.Media.Brushes.Red;
                Renderer.UnLoad();
                await Task.Delay(5000);
                InfoBar.Foreground = System.Windows.Media.Brushes.Black;
            }
            catch (Exception exception)
            {
                Console.WriteLine(exception);
            }
        }
        private void EnableHandTools(object sender, RoutedEventArgs e)
        {
            var toggle = (ToggleButton)sender;
            Renderer.EnableKinetic = toggle.IsChecked == true;
        }

        /// <summary>
        /// Call when SelectedBookIndex changed.
        /// </summary>
        private void OnSelectedBookIndexChanged()
        {
            if (SelectedBookIndex != null)
                Renderer.GotoPage(SelectedBookIndex.PageIndex);
        }

        private void Rendererw_MouseClick(object sender, EventArgs e)
        {
            var i = Mouse.GetPosition(Application.Current.MainWindow);
            int index = (int)(((int)VerticalOffset + i.Y - 50) / (Rendererw.CurrentPageSize.Height + 10));
            Renderer.GotoPage(index);
        }
        private double VerticalOffset;
        private void Rendererw_ScrollChanged(object sender, System.Windows.Controls.ScrollChangedEventArgs e)
        {
            VerticalOffset = + e.VerticalOffset;
            if(VerticalOffset < 0)
                VerticalOffset = 0;
        }
    }
}
