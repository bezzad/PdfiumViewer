using System.Collections.Generic;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

namespace PdfiumViewer.Core
{
    /// <summary>
    /// Represents a marker on a PDF page.
    /// </summary>
    public interface IPdfMarker
    {
        /// <summary>
        /// The page where the marker is drawn on.
        /// </summary>
        int Page { get; }

        object Tag { get; set; }

        /// <summary>
        /// Draw the marker.
        /// </summary>
        /// <param name="frame">The PdfFrame to draw the marker onto.</param>
        IEnumerable<FrameworkElement> Draw(PdfFrame frame);

        /// <summary>
        /// Get the distance of the marker to a point on the PdfFrame
        /// </summary>
        /// <param name="point"></param>
        /// <returns></returns>
        double GetDistance(Point point);
    }
}
