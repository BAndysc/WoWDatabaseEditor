namespace TheEngine.Rendering;

/// <summary>What the game/scene view displays — the final shaded image, or a debug visualization of
/// an intermediate buffer. Selected from the per-view toolbar combobox. The integer values are the
/// `mode` the debug_view fragment shader switches on.</summary>
public enum DebugView
{
    FinalImage = 0,
    Depth = 1,
    ShadowCascade = 2,
    LightComplexity = 3,
    DecalComplexity = 4,
}
