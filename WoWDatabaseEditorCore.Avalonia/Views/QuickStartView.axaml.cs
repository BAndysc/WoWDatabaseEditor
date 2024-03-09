using System;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Markup.Xaml;
using Avalonia.Rendering.Composition;
using Avalonia.Rendering.Composition.Animations;
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

        private void InitializeComponent()
        {
            AvaloniaXamlLoader.Load(this);
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

        public static void SetEnableAnimations(Panel panel, bool value)
        {
            var page = panel.FindAncestorOfType<QuickStartView>();
            if (page == null)
            {
                panel.AttachedToVisualTree += delegate { SetEnableAnimations(panel, true); };
                return;
            }

            if (ElementComposition.GetElementVisual(page) == null)
                return;

            page.EnsureImplicitAnimations();
            if (panel.GetVisualParent() is Visual visualParent
                && ElementComposition.GetElementVisual(visualParent) is CompositionVisual compositionVisual)
            {
                compositionVisual.ImplicitAnimations = page._implicitAnimations;
            }
        }
    }
}