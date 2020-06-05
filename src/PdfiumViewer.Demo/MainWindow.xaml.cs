using Microsoft.Win32;
using System;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Media.Imaging;

namespace PdfiumViewer.Demo
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window, INotifyPropertyChanged
    {
        private Process CurrentProcess { get; } = Process.GetCurrentProcess();
        private CancellationTokenSource Cts { get; }
        private PdfDocument Document { get; set; }

        public event PropertyChangedEventHandler PropertyChanged;
        public int PageNo { get; set; }
        public int Dpi { get; set; }
        public string InfoText { get; set; }
        public PdfViewerZoomMode ZoomMode { get; set; }


        public MainWindow()
        {
            InitializeComponent();

            Cts = new CancellationTokenSource();
            ZoomMode = PdfViewerZoomMode.FitHeight;
            Dpi = 96;
            DataContext = this;
        }

        // Note: called by `PropertyChanged.Fody` when PageNo changed
        protected void OnPageNoChanged()
        {
            GotoPage(PageNo);
        }
        private async void RenderToMemory(object sender, RoutedEventArgs e)
        {
            if (Document == null)
            {
                MessageBox.Show("First load the document");
                return;
            }

            var sw = new Stopwatch();
            sw.Start();

            try
            {
                await Task.Run(async () =>
                {
                    for (PageNo = 0; PageNo < Document.PageCount - 1; PageNo++)
                    {
                        // Note: No need any code because OnPageNoChanged handler do everything perfectly ;)
                        await Dispatcher.InvokeAsync(() =>
                            InfoText = $"Renderd Pages: {PageNo}, " +
                                       $"Memory: {CurrentProcess.PrivateMemorySize64 / (1920 * 1080)} MB, " +
                                       $"Time: {sw.Elapsed.TotalSeconds:0.0} sec");
                        await Task.Delay(1);
                    }
                });
            }
            catch (Exception ex)
            {
                Cts.Cancel();
                Debug.Fail(ex.Message);
                MessageBox.Show(this, ex.Message, "Error!");
            }

            sw.Stop();
            InfoText = $"Rendered {Document.PageCount} Pages within {sw.Elapsed.TotalSeconds:0.0} seconds, Memory: {CurrentProcess.PrivateMemorySize64 / (1024 * 1024)} MB";
        }
        private BitmapSource RenderPageToMemory(int page, int width, int height)
        {
            var image = Document.Render(page, width, height, Dpi, Dpi, false);
            var bs = BitmapHelper.ToBitmapSource(image);
            CurrentProcess?.Refresh();
            GC.Collect();
            return bs;
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
                var bytes = File.ReadAllBytes(dialog.FileName);
                var mem = new MemoryStream(bytes);
                Document = PdfDocument.Load(mem);
                PageNo = 0;
                GotoPage(PageNo);
            }
        }
        protected override void OnClosed(EventArgs e)
        {
            base.OnClosed(e);

            Document?.Dispose();
        }
        private void DoSearch_Click(object sender, RoutedEventArgs e)
        {
            // string text = searchValueTextBox.Text;
            // bool matchCase = matchCaseCheckBox.IsChecked.GetValueOrDefault();
            // bool wholeWordOnly = wholeWordOnlyCheckBox.IsChecked.GetValueOrDefault();
            //
            // DoSearch(text, matchCase, wholeWordOnly);
        }
        private void DoSearch(string text, bool matchCase, bool wholeWord)
        {
            var matches = Document.Search(text, matchCase, wholeWord);
            var sb = new StringBuilder();

            foreach (var match in matches.Items)
            {
                sb.AppendLine($"Found \"{match.Text}\" in page: {match.Page}");
            }

            //searchResultLabel.Text = sb.ToString();
        }
        private void GotoPage(int page)
        {
            if (Document != null)
            {
                var actualHeight = (int)this.ActualHeight - 100;
                var actualWidth = (int)this.ActualWidth - 50;
                var currentPageSize = Document.PageSizes[page];
                var whRatio = currentPageSize.Width / currentPageSize.Height;

                var height = actualHeight;
                var width = (int)(whRatio * actualHeight);

                if (ZoomMode == PdfViewerZoomMode.FitWidth)
                {
                    width = actualWidth;
                    height = (int)(1 / whRatio * actualWidth);
                }

                Dispatcher.Invoke(() => imageMemDC.Source = RenderPageToMemory(page, width, height));
            }
        }

        private void OnPrevPageClick(object sender, RoutedEventArgs e)
        {
            if (Document != null)
                PageNo = Math.Min(Math.Max(PageNo - 1, 0), Document.PageCount - 1);
        }
        private void OnNextPageClick(object sender, RoutedEventArgs e)
        {
            if (Document != null)
                PageNo = Math.Min(Math.Max(PageNo + 1, 0), Document.PageCount - 1);
        }

        private void OnFitWidth(object sender, RoutedEventArgs e)
        {
            ZoomMode = PdfViewerZoomMode.FitWidth;
            GotoPage(PageNo);
        }
        private void OnFitHeight(object sender, RoutedEventArgs e)
        {
            ZoomMode = PdfViewerZoomMode.FitHeight;
            GotoPage(PageNo);
        }

        protected override void OnRenderSizeChanged(SizeChangedInfo sizeInfo)
        {
            base.OnRenderSizeChanged(sizeInfo);

            GotoPage(PageNo);
        }

        private void OnZoomInClick(object sender, RoutedEventArgs e)
        {
            throw new NotImplementedException();
        }

        private void OnZoomOutClick(object sender, RoutedEventArgs e)
        {
            throw new NotImplementedException();
        }

        private void OnRotateLeftClick(object sender, RoutedEventArgs e)
        {
            throw new NotImplementedException();
        }

        private void OnRotateRightClick(object sender, RoutedEventArgs e)
        {
            throw new NotImplementedException();
        }

        private void OnFindText(object sender, RoutedEventArgs e)
        {
            throw new NotImplementedException();
        }

        private void OnInfo(object sender, RoutedEventArgs e)
        {
            throw new NotImplementedException();
        }

        private void OnGetText(object sender, RoutedEventArgs e)
        {
            throw new NotImplementedException();
        }

        private void OnDisplayBookmarks(object sender, RoutedEventArgs e)
        {
            throw new NotImplementedException();
        }
    }
}
