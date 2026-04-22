using System;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Shapes;

namespace PdfiumViewer.Core
{
    public class PdfLinkMarker : IPdfMarker
    {
        public int Page { get; }
        public Point[] Bounds { get; }
        public Rectangle Rectangle { get; }
        public object Tag { get; set; }

        public PdfLinkMarker(int page, Point[] bounds, Brush stroke, string tooltip = null)
        {
            Page = page;
            Bounds = bounds;
            Rectangle = new Rectangle
            {
                Fill = Brushes.Transparent,
                Stroke = stroke,
                StrokeThickness = 0,
                ToolTip = tooltip,
                Cursor = Cursors.Hand,
            };
            Rectangle.MouseEnter += Rectangle_MouseEnter;
            Rectangle.MouseLeave += Rectangle_MouseLeave;
        }

        public IEnumerable<FrameworkElement> Draw(PdfFrame frame)
        {
            if (frame == null)
                throw new ArgumentNullException(nameof(frame));

            var rect = frame.BoundsToFrame(Bounds);
            Rectangle.Width = rect.Width;
            Rectangle.Height = rect.Height;
            Canvas.SetLeft(Rectangle, rect.Left);
            Canvas.SetTop(Rectangle, rect.Top);
            return new[] { Rectangle };
        }
        public double GetDistance(Point pt)
        {
            var center = new Point(Canvas.GetLeft(Rectangle) + Rectangle.Width / 2, Canvas.GetTop(Rectangle) + Rectangle.Height / 2);
            return Point.Subtract(center, pt).Length;
        }

        private void Rectangle_MouseEnter(object sender, MouseEventArgs e)
        {
            Rectangle.StrokeThickness = 2;
        }
        private void Rectangle_MouseLeave(object sender, MouseEventArgs e)
        {
            Rectangle.StrokeThickness = 0;
        }

    }
}
