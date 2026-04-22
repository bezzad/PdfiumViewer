using PdfiumViewer.Core;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media;

namespace PdfiumViewer
{
    public partial class ScrollPanel
    {
        public event EventHandler MouseClick;
        public event EventHandler<MarkerClickedEventArgs> MarkersClicked;
        protected override void OnMouseLeftButtonDown(MouseButtonEventArgs e)
        {
            base.OnMouseLeftButtonDown(e);
            MouseClick?.Invoke(this, EventArgs.Empty);
        }
        protected override void OnPreviewMouseLeftButtonUp(MouseButtonEventArgs e)
        {
            base.OnPreviewMouseLeftButtonUp(e);
            if (e.Handled)
                return;
            var point = e.GetPosition(this);
            var markers = new HashSet<IPdfMarker>();
            VisualTreeHelper.HitTest(
                this,
                d => HitTestFilterBehavior.Continue, r =>
                {
                    if (r.VisualHit is FrameworkElement element && element.Tag is IPdfMarker marker)
                        markers.Add(marker);
                    return HitTestResultBehavior.Continue;
                },
                new PointHitTestParameters(point));

            if(markers.Count > 0)
                MarkersClicked?.Invoke(this, new MarkerClickedEventArgs(markers.OrderBy(m => m.GetDistance(point)).ToList()));
        }
    }
    public class MarkerClickedEventArgs : EventArgs
    {
        public IReadOnlyList<IPdfMarker> Markers { get; }

        public MarkerClickedEventArgs(IReadOnlyList<IPdfMarker> markers)
        {
            Markers = markers;
        }
    }
}
