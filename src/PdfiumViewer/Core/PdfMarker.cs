using System;
using System.Drawing;
using System.Windows;
using System.Windows.Media;
using PdfiumViewer.Drawing;
using Color = System.Windows.Media.Color;
using Pen = System.Windows.Media.Pen;

namespace PdfiumViewer.Core
{
    public class PdfMarker : IPdfMarker
    {
        public int Page { get; }
        public RectangleF Bounds { get; }
        public Color Color { get; }
        public Color BorderColor { get; }
        public float BorderWidth { get; }

        public PdfMarker(int page, RectangleF bounds, Color color)
            : this(page, bounds, color, Colors.Transparent, 0)
        {
        }

        public PdfMarker(int page, RectangleF bounds, Color color, Color borderColor, float borderWidth)
        {
            Page = page;
            Bounds = bounds;
            Color = color;
            BorderColor = borderColor;
            BorderWidth = borderWidth;
        }

        public void Draw(PdfRenderer renderer, DrawingContext graphics)
        {
            if (renderer == null)
                throw new ArgumentNullException(nameof(renderer));
            if (graphics == null)
                throw new ArgumentNullException(nameof(graphics));

            Rect bounds = renderer.BoundsFromPdf(new PdfRectangle(Page, Bounds));
            var brush = new SolidColorBrush(Color) { Opacity = .8 };
            var pen = new Pen(new SolidColorBrush(BorderColor) { Opacity = .8 }, BorderWidth);
            graphics.DrawRectangle(brush, null, bounds);

            if (BorderWidth > 0)
            {
                graphics.DrawRectangle(null, pen, bounds);
            }
        }
    }
}
