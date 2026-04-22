using System;
using System.Windows;

namespace PdfiumViewer.Drawing
{
    public struct PdfRectangle : IEquatable<PdfRectangle>
    {
        public static readonly PdfRectangle Empty = new PdfRectangle();

        // _page is offset by 1 so that Empty returns an invalid rectangle.
        private readonly int _page;

        public int Page => _page - 1;

        public Point[] Bounds { get; }

        public bool IsValid => _page != 0;

        public PdfRectangle(int page, Point[] bounds)
        {
            _page = page + 1;
            Bounds = bounds;
        }

        public bool Equals(PdfRectangle other)
        {
            return
                Page == other.Page &&
                Bounds[0] == other.Bounds[0] &&
                Bounds[1] == other.Bounds[1];
        }

        public override bool Equals(object obj)
        {
            return
                obj is PdfRectangle rectangle &&
                Equals(rectangle);
        }

        public override int GetHashCode()
        {
            unchecked
            {
                return (Page * 397) + (Bounds?[0].GetHashCode() ?? 1) * 5 + (Bounds?[1].GetHashCode() ?? 1) * 7;
            }
        }

        public static bool operator ==(PdfRectangle left, PdfRectangle right)
        {
            return left.Equals(right);
        }

        public static bool operator !=(PdfRectangle left, PdfRectangle right)
        {
            return !left.Equals(right);
        }
    }
}
