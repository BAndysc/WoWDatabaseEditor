using TheEngine.Entities;
using TheMaths;

namespace TheEngine.Interfaces
{
    public interface ICamera
    {
        Transform Transform { get; }
        float FOV { get; set; }

        float NearClip { get; set; }
        float FarClip { get; set; }
        float Aspect { get; set; }
        Matrix ProjectionMatrix { get; }
        Matrix ViewMatrix { get; }
        Matrix InverseViewMatrix { get; }

        /// <summary>Per-camera fog. Lets the game view fog while the editor scene view (or any extra
        /// camera) renders fog-free; resolved into the scene buffer in RenderManager.UpdateSceneBuffer.</summary>
        FogSettings Fog { get; set; }

        /// <summary>Clear/background color this camera's target is cleared to.</summary>
        Color4 BackgroundColor { get; set; }

        /// <summary>Per-camera view-distance multiplier used by the renderer's distance cull
        /// (ObjectDrawRenderStage.PrepareFrame). Implementations should ignore non-positive values.</summary>
        float ViewDistanceModifier { get; set; }
    }
}
