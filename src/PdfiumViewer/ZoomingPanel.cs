using System;
using System.ComponentModel;
using System.Windows.Input;

namespace PdfiumViewer
{
    public class ZoomingPanel : ScrollPanel
    {
        public const double DefaultZoomMin = 0.1;
        public const double DefaultZoomMax = 5;
        public const double DefaultZoomFactor = 1.2;

        /// <summary>
        /// Gets or sets the current zoom level.
        /// </summary>
        [Browsable(false)]
        [DefaultValue(1.0)]
        public double Zoom { get; set; }
        [DefaultValue(DefaultZoomMin)] public double ZoomMin { get; set; }
        [DefaultValue(DefaultZoomMax)] public double ZoomMax { get; set; }
        [DefaultValue(DefaultZoomFactor)] public double ZoomFactor { get; set; }


        protected override void OnMouseWheel(MouseWheelEventArgs e)
        {
            base.OnMouseWheel(e);

            if (IsDocumentLoaded)
            {
                if (MouseWheelMode == MouseWheelMode.Zoom)
                {
                    e.Handled = true;
                    if (e.Delta > 0)
                        ZoomIn();
                    else
                        ZoomOut();
                }
            }
        }

        /// <summary>
        /// Zooms the PDF document in one step.
        /// </summary>
        public void ZoomIn()
        {
            Zoom = Math.Min(Math.Max(Zoom * ZoomFactor, ZoomMin), ZoomMax);
        }

        /// <summary>
        /// Zooms the PDF document out one step.
        /// </summary>
        public void ZoomOut()
        {
            Zoom = Math.Min(Math.Max(Zoom / ZoomFactor, ZoomMin), ZoomMax);
        }
    }
}
