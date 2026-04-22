using PdfiumViewer.Enums;
using System;
using System.Collections.Generic;
using System.Drawing.Imaging;
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
using System.Windows.Media.Animation;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace PdfiumViewer.Core
{
    /// <summary>
    /// Interaktionslogik für PdfPage.xaml
    /// </summary>
    public partial class PdfFrame : UserControl
    {
        public PdfRenderer Renderer { get; }
        public PdfDocument Document => Renderer.Document;
        public int PageIndex { get; private set; }
        public bool IsRendered { get; private set; }
        private PdfFrame()
        {
            InitializeComponent();
        }

        public PdfFrame(PdfRenderer renderer) : this()
        {
            Renderer = renderer;
        }
        public bool SetPage(int i)
        {
            if(PageIndex != i)
            {
                PageIndex = i;
                StopBlinking();
                IsRendered = false;
                return true;
            }
            return false;
        }

        public ImageSource Render(int dpi, PdfRotation rotation, PdfRenderFlags flags)
        {
            if(Width == 0 || Height == 0) 
                return null;
            var imageSource = Document.Render(PageIndex, (int)Width, (int)Height, dpi, dpi, rotation, flags);
            Dispatcher.Invoke(() =>
            {
                image.Source = imageSource;
                canvas.Children.Clear();
                var markers = Renderer.Markers.Get(PageIndex);
                foreach (var marker in markers)
                {
                    var elements = marker.Draw(this);
                    foreach(var element in elements)
                    {
                        if(element.Parent is Panel panel)
                            panel.Children.Remove(element);
                        element.Tag = marker;
                        canvas.Children.Add(element);
                    }
                }
            });
            IsRendered = true;
            return imageSource;
        }

        public void Clear()
        {
            image.Source = null;
            canvas.Children.Clear();
            StopBlinking();
            IsRendered = false;
        }

        public void HighlightRegion(Point[] bounds)
        {
            var rect = BoundsToFrame(bounds);
            blinkRectangle.Margin = new Thickness(rect.Left, rect.Top, Width - rect.Right, Height - rect.Bottom);
            blinkRectangle.BringIntoView();
            StartBlinking();
        }

        private void StartBlinking()
        {
            StopBlinking();
            var board = (Storyboard)FindResource("blinkAnimation");
            board.Begin();
        }
        private void StopBlinking()
        {
            var board = (Storyboard)FindResource("blinkAnimation");
            board.Stop();
            blinkRectangle.Opacity = 0;
        }

        public Rect BoundsToFrame(Point[] bounds)
        {
            var pageSize = Document.GetPageSize(PageIndex);
            return new Rect(
                Math.Min(bounds[0].X, bounds[1].X) / pageSize.Width * Width,
                Height - Math.Max(bounds[0].Y, bounds[1].Y) / pageSize.Height * Height,
                Math.Abs(bounds[1].X - bounds[0].X) / pageSize.Width * Width,
                Math.Abs(bounds[1].Y - bounds[0].Y) / pageSize.Height * Height);
        }
        public Point[] BoundsFromFrame(Rect rect)
        {
            var pageSize = Document.GetPageSize(PageIndex);
            return new[]
            {
                new Point(rect.Left / Width * pageSize.Width, pageSize.Height - rect.Bottom / Height * pageSize.Height),
                new Point(rect.Right / Width * pageSize.Width, pageSize.Height - rect.Top / Height * pageSize.Height),
            };
        }

        protected override void OnPreviewMouseUp(MouseButtonEventArgs e)
        {
            base.OnPreviewMouseUp(e);

            if (e.Handled)
                return;

        }
    }
}
