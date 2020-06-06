using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Printing;
using System.IO;

namespace PdfiumViewer
{
    // ScrollPanel.PdfDocument
    public partial class ScrollPanel
    {
        public void Render(int page, Graphics graphics, float dpiX, float dpiY, Rectangle bounds, bool forPrinting)
        {
            Document.Render(page, graphics, dpiX, dpiY, bounds, forPrinting);
        }

        public void Render(int page, Graphics graphics, float dpiX, float dpiY, Rectangle bounds, PdfRenderFlags flags)
        {
            Document.Render(page, graphics, dpiX, dpiY, bounds, flags);
        }

        public System.Drawing.Image Render(int page, float dpiX, float dpiY, bool forPrinting)
        {
            return Document.Render(page, dpiX, dpiY, forPrinting);
        }

        public System.Drawing.Image Render(int page, float dpiX, float dpiY, PdfRenderFlags flags)
        {
            return Document.Render(page, dpiX, dpiY, flags);
        }

        public System.Drawing.Image Render(int page, int width, int height, float dpiX, float dpiY, bool forPrinting)
        {
            return Document.Render(page, width, height, dpiX, dpiY, forPrinting);
        }

        public System.Drawing.Image Render(int page, int width, int height, float dpiX, float dpiY, PdfRenderFlags flags)
        {
            return Document.Render(page, width, height, dpiX, dpiY, flags);
        }

        public System.Drawing.Image Render(int page, int width, int height, float dpiX, float dpiY, PdfRotation rotate, PdfRenderFlags flags)
        {
            return Document.Render(page, width, height, dpiX, dpiY, rotate, flags);
        }

        public void Save(string path)
        {
            Document.Save(path);
        }

        public void Save(Stream stream)
        {
            Document.Save(stream);
        }

        public PdfMatches Search(string text, bool matchCase, bool wholeWord)
        {
            return Document.Search(text, matchCase, wholeWord);
        }

        public PdfMatches Search(string text, bool matchCase, bool wholeWord, int page)
        {
            return Document.Search(text, matchCase, wholeWord, page);
        }

        public PdfMatches Search(string text, bool matchCase, bool wholeWord, int startPage, int endPage)
        {
            return Document.Search(text, matchCase, wholeWord, startPage, endPage);
        }

        public PrintDocument CreatePrintDocument()
        {
            return Document.CreatePrintDocument();
        }

        public PrintDocument CreatePrintDocument(PdfPrintMode printMode)
        {
            return Document.CreatePrintDocument(printMode);
        }

        public PrintDocument CreatePrintDocument(PdfPrintSettings settings)
        {
            return Document.CreatePrintDocument(settings);
        }

        public PdfPageLinks GetPageLinks(int page, Size size)
        {
            return Document.GetPageLinks(page, size);
        }

        public void DeletePage(int page)
        {
            Document.DeletePage(page);
        }

        public void RotatePage(int page, PdfRotation rotate)
        {
            Rotate = rotate;
            OnPagesDisplayModeChanged();
        }

        public PdfInformation GetInformation()
        {
            return Document.GetInformation();
        }

        public string GetPdfText(int page)
        {
            return Document.GetPdfText(page);
        }

        public string GetPdfText(PdfTextSpan textSpan)
        {
            return Document.GetPdfText(textSpan);
        }

        public IList<PdfRectangle> GetTextBounds(PdfTextSpan textSpan)
        {
            return Document.GetTextBounds(textSpan);
        }

        public PointF PointToPdf(int page, Point point)
        {
            return Document.PointToPdf(page, point);
        }

        public Point PointFromPdf(int page, PointF point)
        {
            return Document.PointFromPdf(page, point);
        }

        public RectangleF RectangleToPdf(int page, Rectangle rect)
        {
            return Document.RectangleToPdf(page, rect);
        }

        public Rectangle RectangleFromPdf(int page, RectangleF rect)
        {
            return Document.RectangleFromPdf(page, rect);
        }
    }
}
