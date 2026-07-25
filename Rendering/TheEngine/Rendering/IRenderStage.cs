using TheEngine.Interfaces;

namespace TheEngine.Rendering;

/// <summary>
/// The points of the frame a render stage can hook into. Not to be confused with a
/// rendering pass (<see cref="ICommandList.BeginRenderingPass"/>), which is the
/// attachment-level concept - the orchestrator owns those; a stage only records draws
/// into whatever target is active when it is invoked.
/// </summary>
[Flags]
public enum RenderPoint
{
    None = 0,
    Opaque = 1,
    Transparent = 2,

    /// <summary>Depth-only pass over opaque geometry, recorded before <see cref="Opaque"/> so the
    /// main opaque pass can rely on LEQUAL early-Z to skip shading occluded fragments.</summary>
    DepthPrepass = 4,

    /// <summary>Depth-only pass over opaque geometry rendered from the sun's point of view into a
    /// cascaded shadow map. Recorded once per cascade before <see cref="Opaque"/>, with the active
    /// camera's <see cref="SceneData"/> view/projection swapped for the light's ortho matrices by
    /// the orchestrator (see CascadedShadowMapManager).</summary>
    Shadow = 8,

    /// <summary>A custom pass recorded before <see cref="Opaque"/>, outside any orchestrator-managed
    /// target. The engine just invokes the stage here (no pass/target is active); the stage is fully
    /// responsible for allocating its own render texture, beginning/ending its pass (see
    /// <see cref="EngineCommandList.BeginRenderTexture"/>) and barriering it to shader-read so the
    /// opaque pass can sample it. Use for off-screen overlays produced ahead of the main scene (e.g.
    /// a top-down texture projected onto terrain by a decal).</summary>
    BeforeOpaque = 16,
}

/// <summary>
/// A self-contained piece of frame rendering (world objects, lines, text, ...),
/// orchestrated by the render manager. The frame contract is:
///
///  1. <see cref="PrepareFrame"/> - once per frame, after transforms are updated and
///     before any recording. CPU-only work: culling, sorting, collecting. Must not
///     record commands - on a future backend this runs on worker threads while the
///     previous stage is still recording.
///  2. <see cref="Render"/> - once per subscribed <see cref="RenderPoint"/> *per view*
///     (game view and scene view), with that view's camera. Recording only; all CPU
///     work should have happened in <see cref="PrepareFrame"/>. The render target,
///     scene data and pass boundaries are managed by the orchestrator.
///  3. <see cref="EndFrame"/> - once per frame after the last render point; clear
///     per-frame accumulations here.
///
/// Stages are registered via <see cref="Interfaces.IRenderManager.RegisterRenderStage"/>
/// and rendered in registration order. The registrant keeps ownership and disposes the
/// stage after unregistering it.
/// </summary>
public interface IRenderStage : IDisposable
{
    string Name { get; }

    RenderPoint RenderPoints { get; }

    void PrepareFrame(ICamera camera);

    void Render(RenderPoint point, EngineCommandList commandList, ICamera camera);

    void EndFrame();
}
