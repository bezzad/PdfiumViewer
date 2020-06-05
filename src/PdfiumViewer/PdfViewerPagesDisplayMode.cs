using System;

namespace PdfiumViewer
{
    [Flags]
    public enum PdfViewerPagesDisplayMode
    {
        SinglePageMode = 1,
        BookMode = 2,
        ContinuousMode = 4
    }
}
