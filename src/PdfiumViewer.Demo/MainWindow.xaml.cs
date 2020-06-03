using Microsoft.Win32;
using System;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media.Imaging;

namespace PdfiumViewer.Demo
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window, INotifyPropertyChanged
    {
        CancellationTokenSource tokenSource;
        Process currentProcess = Process.GetCurrentProcess();
        PdfDocument pdfDoc;

        public event PropertyChangedEventHandler PropertyChanged;
        public int PageNo { get; set; }
        public ICommand GoNextPageCommand { get; set; }


        public MainWindow()
        {
            InitializeComponent();

            DataContext = this;
            tokenSource = new CancellationTokenSource();
        }

        // Note: called by `PropertyChanged.Fody` when PageNo changed
        protected void OnPageNoChanged()
        {
            GotoPage(PageNo);
        }
        private async void RenderToMemDCButton_Click(object sender, RoutedEventArgs e)
        {
            if (pdfDoc == null)
            {
                MessageBox.Show("First load the document");
                return;
            }

            var width = (int)(this.ActualWidth - 30) / 2;
            var height = (int)this.ActualHeight - 30;

            var sw = new Stopwatch();
            sw.Start();

            try
            {
                for (var i = 0; i < pdfDoc.PageCount; i++)
                {
                    imageMemDC.Source = await Task.Run(() =>
                    {
                        tokenSource.Token.ThrowIfCancellationRequested();
                        return RenderPageToMemDC(i, width, height);
                    }, tokenSource.Token);

                    Title = $"Renderd Pages: {i}, Memory: {currentProcess.PrivateMemorySize64 / (1920 * 1080)} MB, Time: {sw.Elapsed.TotalSeconds:0.0} sec";

                    GC.Collect();
                }
            }
            catch (Exception ex)
            {
                tokenSource.Cancel();
                MessageBox.Show(ex.Message);
            }

            sw.Stop();
            Title = $"Rendered {pdfDoc.PageCount} Pages within {sw.Elapsed.TotalSeconds:0.0} seconds, Memory: {currentProcess.PrivateMemorySize64 / (1024 * 1024)} MB";
        }
        private BitmapSource RenderPageToMemDC(int page, int width, int height)
        {
            var image = pdfDoc.Render(page, width, height, 300, 300, false);
            var bs = BitmapHelper.ToBitmapSource(image);
            currentProcess?.Refresh();
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
                pdfDoc = PdfDocument.Load(mem);
                PageNo = 1;
                GotoPage(PageNo);
            }
        }
        protected override void OnClosed(EventArgs e)
        {
            base.OnClosed(e);

            tokenSource?.Cancel();
            pdfDoc?.Dispose();
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
            var matches = pdfDoc.Search(text, matchCase, wholeWord);
            var sb = new StringBuilder();

            foreach (var match in matches.Items)
            {
                sb.AppendLine($"Found \"{match.Text}\" in page: {match.Page}");
            }

            //searchResultLabel.Text = sb.ToString();
        }
        private void GotoPage(int page)
        {
            var width = (int)(this.ActualWidth - 95) / 2;
            var height = (int)this.ActualHeight - 95;
            imageMemDC.Source = RenderPageToMemDC(page, width, height);
        }

        private void OnPrevPageClick(object sender, RoutedEventArgs e)
        {
            if (PageNo > 1)
                PageNo -= 1;
        }
        private void OnNextPageClick(object sender, RoutedEventArgs e)
        {
            if (PageNo < pdfDoc.PageCount - 1)
                PageNo += 1;
        }
    }
}
