using System;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;

namespace PdfiumViewer
{
    public partial class ScrollPanel
    {
        private const double DefaultFriction = 0.95;
        private Point ScrollStartPoint { get; set; }
        private Point ScrollStartOffset { get; set; }
        private Point PreviousPoint { get; set; }
        private Vector Velocity { get; set; }
        private Point _scrollTarget;
        private int InertiaHandlerInterval { get; set; } = 20; // milliseconds
        private int InertiaMaxAnimationTime { get; set; } = 3000; // milliseconds
        protected bool IsMouseDown { get; set; }

        #region Friction

        /// <summary>
        /// Friction Attached Dependency Property
        /// </summary>
        public static readonly DependencyProperty FrictionProperty =
            DependencyProperty.RegisterAttached(nameof(Friction), typeof(double), typeof(ScrollPanel), 
                new FrameworkPropertyMetadata(DefaultFriction));

        public double Friction
        {
            get => (double)GetValue(FrictionProperty);
            set => SetValue(FrictionProperty, value);
        }

        #endregion

        #region EnableKinetic

        /// <summary>
        /// EnableKinetic Attached Dependency Property
        /// </summary>
        public static readonly DependencyProperty EnableKineticProperty =
            DependencyProperty.RegisterAttached(nameof(EnableKinetic), typeof(bool), typeof(ScrollPanel), 
                new FrameworkPropertyMetadata(false));

        public bool EnableKinetic
        {
            get => (bool)GetValue(EnableKineticProperty);
            set
            {
                SetValue(EnableKineticProperty, value);
                Cursor = value ? Cursors.Hand : Cursors.Arrow;
            }
        }

        #endregion

        protected override void OnPreviewMouseDown(MouseButtonEventArgs e)
        {
            base.OnPreviewMouseDown(e);

            if (EnableKinetic && IsMouseOver)
            {
                Cursor = Cursors.ScrollAll;
                // Save starting point, used later when
                // determining how much to scroll.
                Velocity = new Vector();
                ScrollStartPoint = e.GetPosition(this);
                ScrollStartOffset = new Point(HorizontalOffset, VerticalOffset);
                IsMouseDown = true;
            }
        }

        protected override void OnPreviewMouseMove(MouseEventArgs e)
        {
            base.OnPreviewMouseMove(e);

            if (EnableKinetic && IsMouseDown)
            {
                var currentPoint = e.GetPosition(this);
                // Determine the new amount to scroll.
                _scrollTarget = GetScrollTarget(currentPoint);
                InertiaHandleMouseMove();
                // Scroll to the new position.
                ScrollToHorizontalOffset(_scrollTarget.X);
                ScrollToVerticalOffset(_scrollTarget.Y);
            }
        }

        protected override void OnPreviewMouseUp(MouseButtonEventArgs e)
        {
            base.OnPreviewMouseUp(e);

            if (EnableKinetic)
            {
                Cursor = Cursors.Hand;
                IsMouseDown = false;
                InertiaHandleMouseUp();
            }
        }

        private Point GetScrollTarget(Point currentPoint)
        {
            var delta = new Point(ScrollStartPoint.X - currentPoint.X, ScrollStartPoint.Y - currentPoint.Y);
            var scrollTarget = new Point(ScrollStartOffset.X + delta.X, ScrollStartOffset.Y + delta.Y);

            if (scrollTarget.Y < 0)
            {
                scrollTarget.Y = 0;
            }

            if (scrollTarget.Y > ScrollableHeight)
            {
                scrollTarget.Y = ScrollableHeight;
            }

            return scrollTarget;
        }

        private void InertiaHandleMouseMove()
        {
            var currentPoint = Mouse.GetPosition(this);
            Velocity = PreviousPoint - currentPoint;
            PreviousPoint = currentPoint;
        }

        private async void InertiaHandleMouseUp()
        {
            for (var i = 0; i < InertiaMaxAnimationTime / InertiaHandlerInterval; i++)
            {
                if (IsMouseDown || Velocity.Length <= 1 || Environment.TickCount64 - MouseWheelUpdateTime < InertiaHandlerInterval * 2)
                    break;

                ScrollToHorizontalOffset(_scrollTarget.X);
                ScrollToVerticalOffset(_scrollTarget.Y);
                _scrollTarget.X += Velocity.X;
                _scrollTarget.Y += Velocity.Y;
                Velocity *= Friction;
                await Task.Delay(InertiaHandlerInterval);
            }
        }
    }
}
