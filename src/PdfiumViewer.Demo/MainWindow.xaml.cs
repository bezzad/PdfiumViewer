using Microsoft.Win32;
using System;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using PdfiumViewer.Demo.Annotations;

namespace PdfiumViewer.Demo
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window, INotifyPropertyChanged
    {
        private Process CurrentProcess { get; }
        private CancellationTokenSource Cts { get; }
        private System.Windows.Threading.DispatcherTimer MemoryChecker { get; }
        public string InfoText { get; set; }


        public MainWindow()
        {
            InitializeComponent();

            CurrentProcess = Process.GetCurrentProcess();
            Cts = new CancellationTokenSource();
            DataContext = this;

            MemoryChecker = new System.Windows.Threading.DispatcherTimer();
            MemoryChecker.Tick += OnMemoryChecker;
            MemoryChecker.Interval = new TimeSpan(0, 0, 1);
            MemoryChecker.Start();
        }

        private void OnMemoryChecker(object? sender, EventArgs e)
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
                Dispatcher.Invoke(() => Renderer.PageNo = 0);
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
                MessageBox.Show(this, ex.Message, "Error!");
            }
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
            Renderer.ZoomMode = PdfViewerZoomMode.FitWidth;
        }
        private void OnFitHeight(object sender, RoutedEventArgs e)
        {
            Renderer.ZoomMode = PdfViewerZoomMode.FitHeight;
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
            Renderer.RotatePage(Renderer.PageNo, PdfRotation.Rotate90);
        }

        private void OnRotateRightClick(object sender, RoutedEventArgs e)
        {
            Renderer.RotatePage(Renderer.PageNo, PdfRotation.Rotate270);
        }

        private void OnFindText(object sender, RoutedEventArgs e)
        {
            // string text = searchValueTextBox.Text;
            // bool matchCase = matchCaseCheckBox.IsChecked.GetValueOrDefault();
            // bool wholeWordOnly = wholeWordOnlyCheckBox.IsChecked.GetValueOrDefault();
            //

            // var matches = Renderer.Search(text, matchCase, wholeWord);
            // var sb = new StringBuilder();
            //
            // foreach (var match in matches.Items)
            // {
            //     sb.AppendLine($"Found \"{match.Text}\" in page: {match.Page}");
            // }
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

        public event PropertyChangedEventHandler PropertyChanged;

        [NotifyPropertyChangedInvocator]
        protected virtual void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}
