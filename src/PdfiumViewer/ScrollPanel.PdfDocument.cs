using System;
using System.Collections.Generic;
using System.Drawing.Printing;
using System.IO;
using PdfiumViewer.Core;
using PdfiumViewer.Drawing;
using PdfiumViewer.Enums;
using System.Windows;
using System.Windows.Media;

namespace PdfiumViewer
{
    // ScrollPanel.PdfDocument
    public partial class ScrollPanel
    {
        public void Render(int page, System.Drawing.Graphics graphics, float dpiX, float dpiY, Rect bounds, bool forPrinting)
        {
            Document.Render(page, graphics, dpiX, dpiY, bounds, forPrinting);
        }

        public void Render(int page, System.Drawing.Graphics graphics, float dpiX, float dpiY, Rect bounds, PdfRenderFlags flags)
        {
            Document.Render(page, graphics, dpiX, dpiY, bounds, flags);
        }

        public ImageSource Render(int page, float dpiX, float dpiY, bool forPrinting)
        {
            return Document.Render(page, dpiX, dpiY, forPrinting);
        }

        public ImageSource Render(int page, float dpiX, float dpiY, PdfRenderFlags flags)
        {
            return Document.Render(page, dpiX, dpiY, flags);
        }

        public ImageSource Render(int page, int width, int height, float dpiX, float dpiY, bool forPrinting)
        {
            return Document.Render(page, width, height, dpiX, dpiY, forPrinting);
        }

        public ImageSource Render(int page, int width, int height, float dpiX, float dpiY, PdfRenderFlags flags)
        {
            return Document.Render(page, width, height, dpiX, dpiY, flags);
        }

        public ImageSource Render(int page, int width, int height, float dpiX, float dpiY, PdfRotation rotate, PdfRenderFlags flags)
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
            return Document?.Search(text, matchCase, wholeWord);
        }

        public PdfMatches Search(string text, bool matchCase, bool wholeWord, int page)
        {
            return Document?.Search(text, matchCase, wholeWord, page);
        }

        public PdfMatches Search(string text, bool matchCase, bool wholeWord, int startPage, int endPage)
        {
            return Document?.Search(text, matchCase, wholeWord, startPage, endPage);
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

        public IReadOnlyList<PdfPageLink> GetPageLinks(int page)
        {
            return Document.GetPageLinks(page);
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
            return Document?.GetInformation();
        }

        public string GetPdfText(int page)
        {
            return Document?.GetPdfText(page);
        }

        public string GetPdfText(PdfTextSpan textSpan)
        {
            return Document?.GetPdfText(textSpan);
        }

        public IList<PdfRectangle> GetTextBounds(PdfTextSpan textSpan)
        {
            return Document?.GetTextBounds(textSpan);
        }

        public Point PointToPdf(int page, Point point)
        {
            return Document.PointToPdf(page, point);
        }

        public Point PointFromPdf(int page, Point point)
        {
            return Document.PointFromPdf(page, point);
        }

        public Rect RectangleToPdf(int page, Rect rect)
        {
            return Document.RectangleToPdf(page, rect);
        }

        public Rect RectangleFromPdf(int page, Rect rect)
        {
            return Document.RectangleFromPdf(page, rect);
        }


        public PdfFrame GotoPage(int page)
        {
            if (IsDocumentLoaded)
            {
                PageNo = page;
                CurrentPageSize = CalculatePageSize(page);
                if(Frame1 != null)
                {
                    Frame1.Width = CurrentPageSize.Width;
                    Frame1.Height = CurrentPageSize.Height;
                    Frame1.SetPage(page);
                    Frame1.Render(Dpi, Rotate, Flags);
                }

                if (PagesDisplayMode == PdfViewerPagesDisplayMode.BookMode && page + 1 < Document.PageCount)
                {
                    if (Frame2 != null)
                    {
                        Frame2.Width = CurrentPageSize.Width;
                        Frame2.Height = CurrentPageSize.Height;
                        Frame2.SetPage(page + 1);
                        Frame2.Render(Dpi, Rotate, Flags);
                    }
                }

                return ScrollToPage(PageNo);
            }
            return null;
        }
        public PdfFrame NextPage()
        {
            if (IsDocumentLoaded)
            {
                var extentVal = PagesDisplayMode == PdfViewerPagesDisplayMode.BookMode ? 2 : 1;
                return GotoPage(Math.Min(Math.Max(PageNo + extentVal, 0), PageCount - extentVal));
            }
            return null;
        }
        public PdfFrame PreviousPage()
        {
            if (IsDocumentLoaded)
            {
                var extentVal = PagesDisplayMode == PdfViewerPagesDisplayMode.BookMode ? 2 : 1;
                return GotoPage(Math.Min(Math.Max(PageNo - extentVal, 0), PageCount - extentVal));
            }
            return null;
        }
    }
}
