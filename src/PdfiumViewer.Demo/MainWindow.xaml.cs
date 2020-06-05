using Microsoft.Win32;
using System;
using System.Diagnostics;
using System.IO;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;

namespace PdfiumViewer.Demo
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        private Process CurrentProcess { get; } = Process.GetCurrentProcess();
        private CancellationTokenSource Cts { get; }
        public string InfoText { get; set; }


        public MainWindow()
        {
            InitializeComponent();

            Cts = new CancellationTokenSource();
            DataContext = this;
        }


        private async void RenderToMemory(object sender, RoutedEventArgs e)
        {
            var sw = new Stopwatch();
            sw.Start();

            try
            {
                await Task.Run(async () =>
                {
                    for (Renderer.PageNo = 0; Renderer.PageNo < Renderer.PageCount - 1; Renderer.PageNo++)
                    {
                        // Note: No need any code because OnPageNoChanged handler do everything perfectly ;)
                        await Dispatcher.InvokeAsync(() =>
                            InfoText = $"Renderd Pages: {Renderer.PageNo}, " +
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
            InfoText = $"Rendered {Renderer.PageCount} Pages within {sw.Elapsed.TotalSeconds:0.0} seconds, Memory: {CurrentProcess.PrivateMemorySize64 / (1024 * 1024)} MB";
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
                Renderer.OpenPdf(mem);
            }
        }
        protected override void OnClosed(EventArgs e)
        {
            base.OnClosed(e);

            Renderer?.Dispose();
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
            var matches = Renderer.Search(text, matchCase, wholeWord);
            var sb = new StringBuilder();

            foreach (var match in matches.Items)
            {
                sb.AppendLine($"Found \"{match.Text}\" in page: {match.Page}");
            }

            //searchResultLabel.Text = sb.ToString();
        }

        private void OnPrevPageClick(object sender, RoutedEventArgs e)
        {
            if (Renderer.IsDocumentLoaded)
                Renderer.PageNo = Math.Min(Math.Max(Renderer.PageNo - 1, 0), Renderer.PageCount - 1);
        }
        private void OnNextPageClick(object sender, RoutedEventArgs e)
        {
            if (Renderer.IsDocumentLoaded)
                Renderer.PageNo = Math.Min(Math.Max(Renderer.PageNo + 1, 0), Renderer.PageCount - 1);
        }

        private void OnFitWidth(object sender, RoutedEventArgs e)
        {
            Renderer.ZoomMode = PdfViewerZoomMode.FitWidth;
        }
        private void OnFitHeight(object sender, RoutedEventArgs e)
        {
            Renderer.ZoomMode = PdfViewerZoomMode.FitHeight;
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
    }
}
