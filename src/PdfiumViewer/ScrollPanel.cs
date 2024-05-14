using PdfiumViewer.Core;
using PdfiumViewer.Enums;
using System;
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
            ChangePageOnScroll = ChangePageOnScroll.Enabled;
            Dpi = 96;
            ScrollWidth = 50;
            Zoom = 1;
            ZoomMin = DefaultZoomMin;
            ZoomMax = DefaultZoomMax;
            ZoomFactor = DefaultZoomFactor;
            FrameSpace = new Thickness(5);
        }

        public event EventHandler<int> PageChanged;
        public event EventHandler MouseClick;
        public const double DefaultZoomMin = 0.1;
        public const double DefaultZoomMax = 5;
        public const double DefaultZoomFactor = 1.2;
        protected bool IsDisposed;
        protected const int SmallScrollChange = 1;
        protected const int LargeScrollChange = 10;
        protected Process CurrentProcess { get; } = Process.GetCurrentProcess();
        protected StackPanel Panel { get; set; }
        protected Thickness FrameSpace { get; set; }
        protected Image Frame1 => Frames?.FirstOrDefault();
        protected Image Frame2 => Frames?.Length > 1 ? Frames[1] : null;
        protected Image[] Frames { get; set; }
        protected Size CurrentPageSize { get; set; }
        protected int ScrollWidth { get; set; }
        protected int MouseWheelDelta { get; set; }
        protected long MouseWheelUpdateTime { get; set; }

        public event PropertyChangedEventHandler PropertyChanged;
        public PdfDocument Document { get; set; }
        public int PageNo { get; protected set; }
        public int Dpi { get; set; }
        public PdfViewerZoomMode ZoomMode { get; protected set; }
        public PdfRenderFlags Flags { get; set; }
        public PdfRotation Rotate { get; set; }
        public PdfViewerPagesDisplayMode PagesDisplayMode { get; set; }
        public MouseWheelMode MouseWheelMode { get; set; }
        public ChangePageOnScroll ChangePageOnScroll { get; set; }
        public bool IsRightToLeft
        {
            get => Panel.FlowDirection == FlowDirection.RightToLeft;
            set => Panel.FlowDirection = value ? FlowDirection.RightToLeft : FlowDirection.LeftToRight;
        }
        public bool IsDocumentLoaded => Document != null && ActualWidth > 0 && ActualHeight > 0;
        public int PageCount => Document?.PageCount ?? 0;
        /// <summary>
        /// Gets or sets the current zoom level.
        /// </summary>
        [Browsable(false)]
        [DefaultValue(1.0)]
        public double Zoom { get; set; }
        [DefaultValue(DefaultZoomMin)] public double ZoomMin { get; set; }
        [DefaultValue(DefaultZoomMax)] public double ZoomMax { get; set; }
        [DefaultValue(DefaultZoomFactor)] public double ZoomFactor { get; set; }

        public PdfBookmarkCollection Bookmarks => Document?.Bookmarks;
        public IList<SizeF> PageSizes => Document?.PageSizes;

        protected void ScrollToPage(int page)
        {
            if (PagesDisplayMode == PdfViewerPagesDisplayMode.ContinuousMode)
            {
                //
                // scroll to current page
                //
                // var pageSize = CalculatePageSize(page);
                // var verticalOffset = page * (pageSize.Height + FrameSpace.Top + FrameSpace.Bottom);
                // ScrollToVerticalOffset(verticalOffset);
                Frames?[page].BringIntoView();
            }
        }
        protected void OnPageNoChanged()
        {
            PageChanged?.Invoke(this, PageNo);
        }
        protected void OnDpiChanged()
        {
            GotoPage(PageNo);
        }
        protected void OnPagesDisplayModeChanged()
        {
            if (IsDocumentLoaded)
            {
                Panel.Children.Clear();
                Frames = null;

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
                else if (PagesDisplayMode == PdfViewerPagesDisplayMode.ContinuousMode)
                {
                    // frames created at scrolling
                    Frames = new Image[Document.PageCount];
                    Panel.Orientation = Orientation.Vertical;
                }

                for (var i = 0; i < Frames.Length; i++)
                {
                    Frames[i] ??= new Image { Margin = FrameSpace };

                    var pageSize = CalculatePageSize(i);
                    Frames[i].Width = pageSize.Width;
                    Frames[i].Height = pageSize.Height;

                    Panel.Children.Add(Frames[i]);
                }

                GC.Collect();
                GotoPage(PageNo);
            }
        }

        protected override void OnMouseLeftButtonDown(MouseButtonEventArgs e)
        {
            base.OnMouseLeftButtonDown(e);
            MouseClick?.Invoke(this, EventArgs.Empty);
        }
        protected void OnFlagsChanged()
        {
            GotoPage(PageNo);
        }
        protected BitmapImage RenderPage(Image frame, int page, int width, int height)
        {
            if (frame == null) return null;
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
                image.Dispose();
            }
            // Why BitmapCacheOption.OnLoad?
            // It seems counter intuitive, but this flag has two effects:
            // It enables caching if caching is possible, and it causes the load to happen at EndInit().
            // In our case caching is impossible, so all it does it cause the load to happen immediately.

            CurrentProcess?.Refresh();
            Dispatcher.Invoke(() =>
            {
                frame.Width = width;
                frame.Height = height;
                frame.Source = bitmapImage;
            });
            GC.Collect();
            return bitmapImage;
        }
        protected Size CalculatePageSize(int? page = null)
        {
            page ??= PageNo;
            var isReverse = (Rotate == PdfRotation.Rotate90 || Rotate == PdfRotation.Rotate270);
            var containerWidth = ActualWidth - Padding.Left - Padding.Right - FrameSpace.Left - FrameSpace.Right; // ViewportWidth
            var containerHeight = ActualHeight - Padding.Top - Padding.Bottom - FrameSpace.Top - FrameSpace.Bottom; // ViewportHeight

            if (IsDocumentLoaded && containerWidth > 0 && containerHeight > 0)
            {
                var currentPageSize = Document.GetPageSize(page.Value);
                if (isReverse)
                    currentPageSize = new SizeF(currentPageSize.Height, currentPageSize.Width);

                if (ZoomMode == PdfViewerZoomMode.FitHeight)
                {
                    Zoom = containerHeight / currentPageSize.Height;
                }
                if (ZoomMode == PdfViewerZoomMode.FitWidth)
                {
                    Zoom = (containerWidth - ScrollWidth) / currentPageSize.Width;
                    if (PagesDisplayMode == PdfViewerPagesDisplayMode.BookMode)
                        Zoom /= 2;
                }

                return new Size((int)(currentPageSize.Width * Zoom), (int)(currentPageSize.Height * Zoom));
            }

            return new Size();
        }
        protected void ReleaseFrames(int keepFrom, int keepTo)
        {
            for (var f = 0; f < Frames?.Length; f++)
            {
                var frame = Frames[f];
                if ((f < keepFrom || f > keepTo) && frame.Source != null)
                {
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
        protected override void OnPreviewMouseWheel(MouseWheelEventArgs e)
        {
            base.OnPreviewMouseWheel(e);

            SetMouseWheelDelta(e.Delta);
            
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
                else if (PagesDisplayMode != PdfViewerPagesDisplayMode.ContinuousMode && ChangePageOnScroll == ChangePageOnScroll.Enabled)
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
        protected override void OnPreviewKeyDown(KeyEventArgs e)
        {
            base.OnPreviewKeyDown(e);

            if (e.KeyboardDevice.Modifiers == ModifierKeys.Control)
                MouseWheelMode = MouseWheelMode.Zoom;

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

                case Key.Add:
                case Key.OemPlus:
                    if (e.KeyboardDevice.Modifiers == ModifierKeys.Control)
                        ZoomIn();
                    return;

                case Key.Subtract:
                case Key.OemMinus:
                    if (e.KeyboardDevice.Modifiers == ModifierKeys.Control)
                        ZoomOut();
                    return;
            }
        }
        protected override void OnPreviewKeyUp(KeyEventArgs e)
        {
            base.OnPreviewKeyUp(e);

            if (e.KeyboardDevice.Modifiers == ModifierKeys.Control ||
                e.Key == Key.LeftCtrl || e.Key == Key.RightCtrl)
                MouseWheelMode = MouseWheelMode.Pan;
        }
        protected override void OnScrollChanged(ScrollChangedEventArgs e)
        {
            base.OnScrollChanged(e);
            if (IsDocumentLoaded &&
                PagesDisplayMode == PdfViewerPagesDisplayMode.ContinuousMode &&
                Frames != null)
            {
                var startOffset = e.VerticalOffset;
                var height = e.ViewportHeight;
                var pageSize = CalculatePageSize(0);

                var startFrameIndex = (int)(startOffset / (pageSize.Height + FrameSpace.Top + FrameSpace.Bottom));
                var endFrameIndex = (int)((startOffset + height) / (pageSize.Height + FrameSpace.Top + FrameSpace.Bottom));

                PageNo = Math.Min(Math.Max(startFrameIndex, 0), PageCount - 1);
                var endPageIndex = Math.Min(Math.Max(endFrameIndex, 0), PageCount - 1);

                ReleaseFrames(PageNo, endPageIndex);

                for (var page = PageNo; page <= endPageIndex; page++)
                {
                    var frame = Frames[page];
                    if (frame.Source == null) // && frame.IsUserVisible())
                    {
                        RenderPage(frame, page, (int)frame.Width, (int)frame.Height);
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
                        if (VerticalOffset > SmallScrollChange)
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
        protected void SetMouseWheelDelta(int delta)
        {
            MouseWheelUpdateTime = Environment.TickCount64;
            MouseWheelDelta = delta;
        }
        protected virtual void Dispose(bool disposing)
        {
            if (disposing)
            {
                IsDisposed = true;
            }
        }

        public void Dispose()
        {
            Dispose(!IsDisposed);
        }
    }
}
