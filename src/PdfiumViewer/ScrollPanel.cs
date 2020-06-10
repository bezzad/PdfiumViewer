using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media.Effects;
using System.Windows.Media.Imaging;
using Image = System.Windows.Controls.Image;
using Size = System.Drawing.Size;

namespace PdfiumViewer
{
    // ScrollPanel.Properties
    public partial class ScrollPanel : ScrollViewer, IPdfDocument, INotifyPropertyChanged
    {
        public ScrollPanel()
        {
            VerticalScrollBarVisibility = ScrollBarVisibility.Auto;
            HorizontalScrollBarVisibility = ScrollBarVisibility.Auto;
            Effect = new DropShadowEffect()
            {
                BlurRadius = 10,
                Direction = 270,
                RenderingBias = RenderingBias.Performance,
                ShadowDepth = 0
            };
            Panel = new StackPanel()
            {
                HorizontalAlignment = HorizontalAlignment.Center
            };
            VirtualizingPanel.SetIsVirtualizing(Panel, true);
            VirtualizingPanel.SetVirtualizationMode(Panel, VirtualizationMode.Recycling);
            Content = Panel;

            ZoomMode = PdfViewerZoomMode.FitHeight;
            Rotate = PdfRotation.Rotate0;
            Flags = PdfRenderFlags.None;
            PagesDisplayMode = PdfViewerPagesDisplayMode.SinglePageMode;
            MouseWheelMode = MouseWheelMode.PanAndZoom;
            Dpi = 96;
            ScrollWidth = 50;
            FrameSpace = new Thickness(5);
            RenderedFramesMap = new ConcurrentDictionary<int, Image>();
        }


        protected const int SmallScrollChange = 1;
        protected const int LargeScrollChange = 10;
        protected Process CurrentProcess { get; } = Process.GetCurrentProcess();
        protected StackPanel Panel { get; set; }
        protected Thickness FrameSpace { get; set; }
        protected Image Frame1 => Frames?.FirstOrDefault();
        protected Image Frame2 => Frames?.Length > 1 ? Frames[1] : null;
        protected Image[] Frames { get; set; }
        protected ConcurrentDictionary<int, Image> RenderedFramesMap { get; set; }
        protected System.Drawing.Size CurrentPageSize { get; set; }
        protected int ScrollWidth { get; set; }

        public event PropertyChangedEventHandler PropertyChanged;
        public PdfDocument Document { get; set; }
        public int PageNo { get; set; }
        public int Dpi { get; set; }
        public PdfViewerZoomMode ZoomMode { get; set; }
        public PdfRenderFlags Flags { get; set; }
        public PdfRotation Rotate { get; set; }
        public PdfViewerPagesDisplayMode PagesDisplayMode { get; set; }
        public MouseWheelMode MouseWheelMode { get; set; }
        public bool IsDocumentLoaded => Document != null;
        public int PageCount => Document?.PageCount ?? 0;
        public PdfBookmarkCollection Bookmarks => Document?.Bookmarks;
        public IList<SizeF> PageSizes => Document?.PageSizes;


        protected void OnPageNoChanged()
        {
            GotoPage(PageNo);
        }
        protected void OnDpiChanged()
        {
            GotoPage(PageNo);
        }
        protected void OnPagesDisplayModeChanged()
        {
            Panel.Children.Clear();
            RenderedFramesMap?.Clear();

            if (PagesDisplayMode == PdfViewerPagesDisplayMode.SinglePageMode)
            {
                Frames = new Image[1];
                Panel.Orientation = Orientation.Horizontal;
            }
            else if (PagesDisplayMode == PdfViewerPagesDisplayMode.BookMode)
            {
                Frames = new Image[2];
                Panel.Orientation = Orientation.Horizontal;
            }
            else if (PagesDisplayMode == PdfViewerPagesDisplayMode.ContinuousMode && IsDocumentLoaded)
            {
                // frames created at scrolling
                Frames = new Image[Document.PageCount];
                Panel.Orientation = Orientation.Vertical;
            }

            for (var i = 0; i < Frames.Length; i++)
            {
                if (Frames[i] == null)
                    Frames[i] = new Image() { Margin = FrameSpace };

                if (IsDocumentLoaded)
                {
                    var pageSize = CalculatePageSize(i);
                    Frames[i].Width = pageSize.Width;
                    Frames[i].Height = pageSize.Height;
                }

                Panel.Children.Add(Frames[i]);
            }

            if (PagesDisplayMode == PdfViewerPagesDisplayMode.ContinuousMode && IsDocumentLoaded)
                Frames[PageNo].BringIntoView(); // scroll to current page

            GC.Collect();
            GotoPage(PageNo);
        }
        protected void OnZoomModeChanged()
        {
            OnPagesDisplayModeChanged();
        }
        protected void OnFlagsChanged()
        {
            GotoPage(PageNo);
        }
        protected BitmapImage RenderPage(Image frame, int page, int width, int height)
        {
            var image = Document.Render(page, width, height, Dpi, Dpi, Rotate, Flags);
            BitmapImage bitmapImage;
            using (var memory = new MemoryStream())
            {
                image.Save(memory, ImageFormat.Png);
                memory.Position = 0;
                bitmapImage = new BitmapImage();
                bitmapImage.BeginInit();
                bitmapImage.StreamSource = memory;
                bitmapImage.CacheOption = BitmapCacheOption.OnLoad; // not a mistake - see below
                bitmapImage.EndInit();
            }
            // Why BitmapCacheOption.OnLoad?
            // It seems counter intuitive, but this flag has two effects:
            // It enables caching if caching is possible, and it causes the load to happen at EndInit().
            // In our case caching is impossible, so all it does it cause the load to happen immediately.

            CurrentProcess?.Refresh();
            GC.Collect();
            Dispatcher.Invoke(() =>
            {
                frame.Width = width;
                frame.Height = height;
                frame.Source = bitmapImage;
            });
            return bitmapImage;
        }
        protected Size CalculatePageSize(int? page = null)
        {
            page ??= PageNo;
            var isReverse = (Rotate == PdfRotation.Rotate90 || Rotate == PdfRotation.Rotate270);
            var containerWidth = ActualWidth - Padding.Left - Padding.Right; // ViewportWidth
            var containerHeight = ActualHeight - Padding.Top - Padding.Bottom; // ViewportHeight

            if (IsDocumentLoaded)
            {
                var currentPageSize = Document.PageSizes[page.Value];
                if (isReverse)
                    currentPageSize = new SizeF(currentPageSize.Height, currentPageSize.Width);

                var whRatio = currentPageSize.Width / currentPageSize.Height;
                var height = containerHeight;
                var width = whRatio * height;

                if (ZoomMode == PdfViewerZoomMode.FitWidth)
                {
                    width = containerWidth - ScrollWidth;
                    if (PagesDisplayMode == PdfViewerPagesDisplayMode.BookMode)
                        width /= 2;
                    height = (int)(1 / whRatio * width);
                }

                return new Size((int)width, (int)height);
            }

            return new Size();
        }
        protected void ReleaseFrames(int keepFrom, int keepTo)
        {
            foreach (var page in RenderedFramesMap.Keys)
            {
                if ((page < keepFrom || page > keepTo) &&
                    RenderedFramesMap.TryRemove(page, out var frame))
                {
                    GC.SuppressFinalize(frame.Source);
                    frame.Source = null;
                }
            }

            GC.Collect();
        }
        
        protected override void OnRenderSizeChanged(SizeChangedInfo sizeInfo)
        {
            base.OnRenderSizeChanged(sizeInfo);
            GotoPage(PageNo);
        }
        protected override void OnMouseWheel(MouseWheelEventArgs e)
        {
            base.OnMouseWheel(e);

            if (IsDocumentLoaded)
            {
                if (PagesDisplayMode != PdfViewerPagesDisplayMode.ContinuousMode)
                {
                    var pageStep = PagesDisplayMode == PdfViewerPagesDisplayMode.BookMode ? 2 : 1;

                    if (ViewportHeight > Frame1.ActualHeight)
                    {
                        if (e.Delta > 0) // prev page
                            PreviousPage();
                        else
                            NextPage();
                    }
                    else if (e.Delta < 0 && VerticalOffset >= ScrollableHeight && PageNo < PageCount - pageStep)
                    {
                        NextPage();
                        ScrollToVerticalOffset(0);
                    }
                    else if (e.Delta > 0 && VerticalOffset <= 0 && PageNo > 0)
                    {
                        PreviousPage();
                        ScrollToVerticalOffset(ScrollableHeight);
                    }
                }
            }
        }
        protected override void OnKeyDown(KeyEventArgs e)
        {
            base.OnKeyDown(e);

            switch (e.Key)
            {
                case Key.Up:
                    PerformScroll(ScrollAction.LineUp, Orientation.Vertical);
                    return;

                case Key.Down:
                    PerformScroll(ScrollAction.LineDown, Orientation.Vertical);
                    return;

                case Key.Left:
                    PerformScroll(ScrollAction.LineUp, Orientation.Horizontal);
                    return;

                case Key.Right:
                    PerformScroll(ScrollAction.LineDown, Orientation.Horizontal);
                    return; 

                case Key.PageUp:
                    PerformScroll(ScrollAction.PageUp, Orientation.Vertical);
                    return;

                case Key.PageDown:
                    PerformScroll(ScrollAction.PageDown, Orientation.Vertical);
                    return;

                case Key.Home:
                    PerformScroll(ScrollAction.Home, Orientation.Vertical);
                    return;

                case Key.End:
                    PerformScroll(ScrollAction.End, Orientation.Vertical);
                    return;
            }
        }
        protected override void OnKeyUp(KeyEventArgs e)
        {
            base.OnKeyUp(e);

            if (e.KeyboardDevice.Modifiers == ModifierKeys.Control)
                MouseWheelMode = MouseWheelMode.Pan;
        }
        protected override void OnScrollChanged(ScrollChangedEventArgs e)
        {
            base.OnScrollChanged(e);

            if (MouseWheelMode != MouseWheelMode.Zoom && 
                PagesDisplayMode == PdfViewerPagesDisplayMode.ContinuousMode)
            {
                var startOffset = e.VerticalOffset;
                var height = e.ViewportHeight;
                var pageSize = CalculatePageSize(0);

                var startFrameIndex = startOffset / (pageSize.Height + FrameSpace.Top + FrameSpace.Bottom);
                var endFrameIndex = (startOffset + height) / (pageSize.Height + FrameSpace.Top + FrameSpace.Bottom);

                PageNo = (int)Math.Min(Math.Max(startFrameIndex, 0), PageCount - 1);
                var endPageIndex = (int)Math.Min(Math.Max(endFrameIndex, 0), PageCount - 1);

                ReleaseFrames(PageNo, endPageIndex);

                for (var page = PageNo; page <= endPageIndex; page++)
                {
                    var frame = Frames[page];
                    if (frame.IsUserVisible() && frame.Source == null)
                    {
                        RenderPage(frame, page, (int)frame.Width, (int)frame.Height);
                        RenderedFramesMap.TryAdd(page, frame);
                    }
                }
            }
        }

        public void PerformScroll(ScrollAction action, Orientation scrollBar)
        {
            if (scrollBar == Orientation.Vertical)
            {
                switch (action)
                {
                    case ScrollAction.LineUp:
                        if(VerticalOffset > SmallScrollChange)
                            ScrollToVerticalOffset(VerticalOffset - SmallScrollChange);
                        break;

                    case ScrollAction.LineDown:
                        if (VerticalOffset < ScrollableHeight - SmallScrollChange)
                            ScrollToVerticalOffset(VerticalOffset + SmallScrollChange);
                        break;

                    case ScrollAction.PageUp:
                        if (VerticalOffset > LargeScrollChange)
                            ScrollToVerticalOffset(VerticalOffset - LargeScrollChange);
                        break;

                    case ScrollAction.PageDown:
                        if (VerticalOffset < ScrollableHeight - LargeScrollChange)
                            ScrollToVerticalOffset(VerticalOffset + LargeScrollChange);
                        break;

                    case ScrollAction.Home:
                        ScrollToHome();
                        break;

                    case ScrollAction.End:
                        ScrollToEnd();
                        break;
                }
            }
            else // Horizontal
            {
                switch (action)
                {
                    case ScrollAction.LineUp:
                        if (HorizontalOffset > SmallScrollChange)
                            ScrollToVerticalOffset(HorizontalOffset - SmallScrollChange);
                        break;

                    case ScrollAction.LineDown:
                        if (HorizontalOffset < ScrollableHeight - SmallScrollChange)
                            ScrollToVerticalOffset(HorizontalOffset + SmallScrollChange);
                        break;

                    case ScrollAction.PageUp:
                        if (HorizontalOffset > LargeScrollChange)
                            ScrollToVerticalOffset(HorizontalOffset - LargeScrollChange);
                        break;

                    case ScrollAction.PageDown:
                        if (HorizontalOffset < ScrollableHeight - LargeScrollChange)
                            ScrollToVerticalOffset(HorizontalOffset + LargeScrollChange);
                        break;

                    case ScrollAction.Home:
                        ScrollToHome();
                        break;

                    case ScrollAction.End:
                        ScrollToEnd();
                        break;
                }
            }
        }


        public void GotoPage(int page)
        {
            if (IsDocumentLoaded)
            {
                CurrentPageSize = CalculatePageSize(page);

                RenderPage(Frame1, page, CurrentPageSize.Width, CurrentPageSize.Height);

                if (PagesDisplayMode == PdfViewerPagesDisplayMode.BookMode && page + 1 < Document.PageCount)
                {
                    RenderPage(Frame2, page + 1, CurrentPageSize.Width, CurrentPageSize.Height);
                }
            }
        }
        public void NextPage()
        {
            if (IsDocumentLoaded)
            {
                var extentVal = PagesDisplayMode == PdfViewerPagesDisplayMode.BookMode ? 2 : 1;
                PageNo = Math.Min(Math.Max(PageNo + extentVal, 0), PageCount - extentVal);

                if (PagesDisplayMode == PdfViewerPagesDisplayMode.ContinuousMode)
                    Frames[PageNo].BringIntoView(); // scroll to current page
            }
        }
        public void PreviousPage()
        {
            if (IsDocumentLoaded)
            {
                var extentVal = PagesDisplayMode == PdfViewerPagesDisplayMode.BookMode ? 2 : 1;
                PageNo = Math.Min(Math.Max(PageNo - extentVal, 0), PageCount - extentVal);

                if (PagesDisplayMode == PdfViewerPagesDisplayMode.ContinuousMode)
                    Frames[PageNo].BringIntoView(); // scroll to current page
            }
        }

        public void Dispose()
        {
            Document?.Dispose();
        }
    }
}
