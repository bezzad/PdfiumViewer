using PdfiumViewer.Enums;
using System;

namespace PdfiumViewer
{
    public partial class ScrollPanel
    {
        /// <summary>
        /// Zooms the PDF document in one step.
        /// </summary>
        public void ZoomIn()
        {
            SetZoom(Zoom * ZoomFactor);
        }

        /// <summary>
        /// Zooms the PDF document out one step.
        /// </summary>
        public void ZoomOut()
        {
            SetZoom(Zoom / ZoomFactor);
        }

        public void SetZoom(double zoom)
        {
            zoom = Math.Min(Math.Max(zoom, ZoomMin), ZoomMax);
            if(zoom != Zoom || ZoomMode != PdfViewerZoomMode.None)
            {
                Zoom = zoom;
                ZoomMode = PdfViewerZoomMode.None;
                OnPagesDisplayModeChanged();
            }
        }

        public void SetZoomMode(PdfViewerZoomMode mode)
        {
            if(mode != ZoomMode)
            {
                ZoomMode = mode;
                OnPagesDisplayModeChanged();
            }
        }
    }
}
