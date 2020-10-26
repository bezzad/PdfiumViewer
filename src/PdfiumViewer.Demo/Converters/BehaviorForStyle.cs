using System.Linq;
using System.Windows;
using Microsoft.Xaml.Behaviors;

namespace PdfiumViewer.Demo.Converters
{
    /// <summary>
    /// Wrapper to allow a behavior to be included in a style definition
    /// </summary>
    /// <typeparam name="TTarget"></typeparam>
    /// <typeparam name="TBehavior"></typeparam>
    public class BehaviorForStyle<TTarget, TBehavior> : Behavior<TTarget>
        where TTarget : DependencyObject
        where TBehavior : BehaviorForStyle<TTarget, TBehavior>, new()
    {
        public static readonly DependencyProperty IsEnabledForStyleProperty =
            DependencyProperty.RegisterAttached("IsEnabledForStyle",
                typeof(bool),
                typeof(BehaviorForStyle<TTarget, TBehavior>),
                new FrameworkPropertyMetadata(false, OnIsEnabledForStyleChanged));

        public bool IsEnabledForStyle
        {
            get => (bool)GetValue(IsEnabledForStyleProperty);
            set => SetValue(IsEnabledForStyleProperty, value);
        }

        private static void OnIsEnabledForStyleChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (!(e.NewValue is bool))
                return;

            var newValue = (bool)e.NewValue;

            var behaviors = Interaction.GetBehaviors(d);
            var existingBehavior = behaviors.FirstOrDefault(b => b.GetType() == typeof(TBehavior)) as TBehavior;

            if (!newValue && existingBehavior != null)
                behaviors.Remove(existingBehavior);
            else if (newValue && existingBehavior == null)
                behaviors.Add(new TBehavior());
        }
    }
}