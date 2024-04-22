using System;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Markup.Xaml;
using Avalonia.Rendering.Composition;
using Avalonia.Rendering.Composition.Animations;
using Avalonia.Threading;
using Avalonia.VisualTree;

namespace WoWDatabaseEditorCore.Avalonia.Views
{
    public partial class QuickStartView : UserControl
    {
        private ImplicitAnimationCollection? _implicitAnimations;

        public QuickStartView()
        {
            InitializeComponent();
        }

        private void EnsureImplicitAnimations()
        {
            if (_implicitAnimations == null)
            {
                var compositor = ElementComposition.GetElementVisual(this)!.Compositor;

                var offsetAnimation = compositor.CreateVector3KeyFrameAnimation();
                offsetAnimation.Target = "Offset";
                offsetAnimation.InsertExpressionKeyFrame(1.0f, "this.FinalValue");
                offsetAnimation.Duration = TimeSpan.FromMilliseconds(400);

                var rotationAnimation = compositor.CreateScalarKeyFrameAnimation();
                rotationAnimation.Target = "RotationAngle";
                rotationAnimation.InsertKeyFrame(.5f, 0.160f);
                rotationAnimation.InsertKeyFrame(1f, 0f);
                rotationAnimation.Duration = TimeSpan.FromMilliseconds(400);

                var animationGroup = compositor.CreateAnimationGroup();
                animationGroup.Add(offsetAnimation);
                animationGroup.Add(rotationAnimation);

                _implicitAnimations = compositor.CreateImplicitAnimationCollection();
                _implicitAnimations["Offset"] = animationGroup;
            }
        }

        private System.IDisposable? animationAttachTimer;

        protected override void OnAttachedToVisualTree(VisualTreeAttachmentEventArgs e)
        {
            base.OnAttachedToVisualTree(e);
            animationAttachTimer?.Dispose();
            animationAttachTimer = DispatcherTimer.RunOnce(() =>
            {
                EnsureImplicitAnimations();
                foreach (var container in IconsControl.GetRealizedContainers())
                {
                    if (ElementComposition.GetElementVisual(container) is { } compositionVisual)
                    {
                        compositionVisual.ImplicitAnimations = _implicitAnimations;
                    }
                }
                animationAttachTimer = null;
            }, TimeSpan.FromMilliseconds(10));
        }

        protected override void OnDetachedFromVisualTree(VisualTreeAttachmentEventArgs e)
        {
            base.OnDetachedFromVisualTree(e);
            animationAttachTimer?.Dispose();
            animationAttachTimer = null;
            foreach (var container in IconsControl.GetRealizedContainers())
            {
                if (ElementComposition.GetElementVisual(container) is {} compositionVisual)
                {
                    compositionVisual.ImplicitAnimations = null;
                }
            }
        }
    }
}