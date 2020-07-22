using System;
using System.Collections.Generic;
using System.IO;
using System.Windows;
using System.Windows.Media;
using PdfiumViewer.Core;
using PdfiumViewer.Drawing;
using PdfiumViewer.Enums;
using Size = System.Drawing.Size;

namespace PdfiumViewer
{
    public class PdfRenderer : ScrollPanel
    {
        public PdfRenderer()
        {
            IsTabStop = true;
            Markers = new PdfMarkerCollection();
            Markers.CollectionChanged += Markers_CollectionChanged;
        }


        /// <summary>
        /// Gets a collection with all markers.
        /// </summary>
        public PdfMarkerCollection Markers { get; }
        private List<IPdfMarker>[] _markers;

        public void OpenPdf(string path, bool isRightToLeft = false)
        {
            UnLoad();
            IsRightToLeft = isRightToLeft;
            Document = PdfDocument.Load(path);
            OnPagesDisplayModeChanged();
            GotoPage(0);
        }
        public void OpenPdf(string path, string password, bool isRightToLeft = false)
        {
            UnLoad();
            IsRightToLeft = isRightToLeft;
            Document = PdfDocument.Load(path, password);
            OnPagesDisplayModeChanged();
            GotoPage(0);
        }
        public void OpenPdf(Stream stream, bool isRightToLeft = false)
        {
            UnLoad();
            IsRightToLeft = isRightToLeft;
            Document = PdfDocument.Load(stream);
            OnPagesDisplayModeChanged();
            GotoPage(0);
        }
        public void OpenPdf(Stream stream, string password, bool isRightToLeft = false)
        {
            UnLoad();
            IsRightToLeft = isRightToLeft;
            Document = PdfDocument.Load(stream, password);
            OnPagesDisplayModeChanged();
            GotoPage(0);
        }
        public void UnLoad()
        {
            Document?.Dispose();
            Document = null;
            Frames = null;
            Panel.Children.Clear();
            RenderedFramesMap?.Clear();
            GC.Collect();
        }
        public void ClockwiseRotate()
        {
            // _____
            //      |
            //      |
            //      v
            // Clockwise

            switch (Rotate)
            {
                case PdfRotation.Rotate0:
                    RotatePage(PageNo, PdfRotation.Rotate90);
                    break;
                case PdfRotation.Rotate90:
                    RotatePage(PageNo, PdfRotation.Rotate180);
                    break;
                case PdfRotation.Rotate180:
                    RotatePage(PageNo, PdfRotation.Rotate270);
                    break;
                case PdfRotation.Rotate270:
                    RotatePage(PageNo, PdfRotation.Rotate0);
                    break;
            }
        }
        public void Counterclockwise()
        {
            //      ^
            //      |
            //      |
            // _____|
            // Counterclockwise

            switch (Rotate)
            {
                case PdfRotation.Rotate0:
                    RotatePage(PageNo, PdfRotation.Rotate270);
                    break;
                case PdfRotation.Rotate90:
                    RotatePage(PageNo, PdfRotation.Rotate0);
                    break;
                case PdfRotation.Rotate180:
                    RotatePage(PageNo, PdfRotation.Rotate90);
                    break;
                case PdfRotation.Rotate270:
                    RotatePage(PageNo, PdfRotation.Rotate180);
                    break;
            }
        }

        /// <summary>
        /// Scroll the PDF bounds into view.
        /// </summary>
        /// <param name="bounds">The PDF bounds to scroll into view.</param>
        public void ScrollIntoView(PdfRectangle bounds)
        {
           ScrollIntoView(BoundsFromPdf(bounds));
        }

        /// <summary>
        /// Scroll the client rectangle into view.
        /// </summary>
        /// <param name="rectangle">The client rectangle to scroll into view.</param>
        public void ScrollIntoView(Rect rectangle)
        {
            var clientArea = GetScrollClientArea();

            // if (rectangle.Top < 0 || rectangle.Bottom > clientArea.Height)
            // {
            //     var displayRectangle = DisplayRectangle;
            //     int center = rectangle.Top + rectangle.Height / 2;
            //     int documentCenter = center - displayRectangle.Y;
            //     int displayCenter = clientArea.Height / 2;
            //     int offset = documentCenter - displayCenter;
            //
            //     SetDisplayRectLocation(new Point(
            //         displayRectangle.X,
            //         -offset
            //     ));
            // }
        }

        /// <summary>
        /// Converts PDF bounds to client bounds.
        /// </summary>
        /// <param name="bounds">The PDF bounds to convert.</param>
        /// <returns>The bounds of the PDF bounds in client coordinates.</returns>
        public Rect BoundsFromPdf(PdfRectangle bounds)
        {
            return BoundsFromPdf(bounds, true);
        }

        private Rect BoundsFromPdf(PdfRectangle bounds, bool translateOffset)
        {
            var offset = translateOffset ? GetScrollOffset() : Size.Empty;
            // var pageBounds = _pageCache[bounds.Page].Bounds;
            // var pageSize = Document.PageSizes[bounds.Page];
            //
            // var translated = Document.RectangleFromPdf(
            //     bounds.Page,
            //     bounds.Bounds
            // );
            //
            // var topLeft = TranslatePointFromPdf(pageBounds.Size, pageSize, new PointF(translated.Left, translated.Top));
            // var bottomRight = TranslatePointFromPdf(pageBounds.Size, pageSize, new PointF(translated.Right, translated.Bottom));
            //
            // return new Rectangle(
            //     pageBounds.Left + offset.Width + Math.Min(topLeft.X, bottomRight.X),
            //     pageBounds.Top + offset.Height + Math.Min(topLeft.Y, bottomRight.Y),
            //     Math.Abs(bottomRight.X - topLeft.X),
            //     Math.Abs(bottomRight.Y - topLeft.Y)
            // );

            return new Rect(offset.Width, offset.Height, offset.Width, offset.Height);
        }

        private Size GetScrollOffset()
        {
            var bounds = GetScrollClientArea();
            // int maxWidth = (int)(_maxWidth * _scaleFactor) + ShadeBorder.Size.Horizontal + PageMargin.Horizontal;
            // int leftOffset = (HScroll ? DisplayRectangle.X : (bounds.Width - maxWidth) / 2) + maxWidth / 2;
            // int topOffset = VScroll ? DisplayRectangle.Y : 0;
            //
            // return new Size(leftOffset, topOffset);
            return new Size((int)bounds.Width, (int)bounds.Height);
        }

        private Rect GetScrollClientArea()
        {
            return new Rect(0, 0, (int)ViewportWidth, (int)ViewportHeight);
        }

        private void EnsureMarkers()
        {
            if (_markers != null)
                return;

            _markers = new List<IPdfMarker>[1];

            foreach (var marker in Markers)
            {
                if (marker.Page < 0 || marker.Page >= _markers.Length)
                    continue;

                _markers[marker.Page] ??= new List<IPdfMarker>();
                _markers[marker.Page].Add(marker);
            }
        }

        private void DrawMarkers(DrawingContext graphics, int page)
        {
            if (_markers?.Length > 0 && _markers.Length > page)
            {
                var markers = _markers[page];
                if (markers == null)
                    return;

                foreach (var marker in markers)
                {
                    marker.Draw(this, graphics);
                }
            }
        }

        

        private void Markers_CollectionChanged(object sender, EventArgs e)
        {
            RedrawMarkers();
        }
        private void RedrawMarkers()
        {
            _markers = null;

            GotoPage(PageNo);
        }
        protected override void OnRender(DrawingContext drawingContext)
        {
            base.OnRender(drawingContext);

            if (Document == null)
                return;

            EnsureMarkers();

            DrawMarkers(drawingContext, PageNo);
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                base.Dispose(disposing);
                _markers = null;
                GC.SuppressFinalize(this);
                GC.Collect();
            }
        }

        ~PdfRenderer() => Dispose(true);
    }
}
