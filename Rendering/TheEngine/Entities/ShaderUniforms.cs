namespace TheEngine.Entities
{
    /// <summary>
    /// Cache of <see cref="Material.GetUniformLocation"/> results for engine-wide uniform names.
    /// <para>
    /// <see cref="Material.GetUniformLocation(string)"/> is a string-keyed dictionary lookup (string
    /// hashing on every call). The returned <see cref="GlobalUniformHandle"/> is a process-stable
    /// int from a registry that only ever grows and never reassigns, so for any given name it can be
    /// resolved exactly once. Hot per-frame / per-draw paths should bind through these cached handles
    /// (via the <c>GlobalUniformHandle</c> overloads on <see cref="Material"/> and
    /// <see cref="TheEngine.Rendering.EngineCommandList"/>) instead of passing the string each time.
    /// </para>
    /// Names that are specific to a single subsystem (e.g. the WoW renderer's post-process textures)
    /// should be cached as <c>static readonly</c> fields next to their use site rather than added here.
    /// </summary>
    public static class ShaderUniforms
    {
        // Instancing SSBOs - bound per draw batch by the object render stages.
        public static readonly GlobalUniformHandle InstancingModels = Material.GetUniformLocation("InstancingModels");
        public static readonly GlobalUniformHandle InstancingInverseModels = Material.GetUniformLocation("InstancingInverseModels");
        public static readonly GlobalUniformHandle InstancingObjectIndices = Material.GetUniformLocation("InstancingObjectIndices");
        public static readonly GlobalUniformHandle InstancingDrawData = Material.GetUniformLocation("InstancingDrawData");
        public static readonly GlobalUniformHandle InstancingMaterialIndex = Material.GetUniformLocation("InstancingMaterialIndex");

        // UIManager glyph batches (note: "glpyhUVs" spelling must match the shader declaration).
        public static readonly GlobalUniformHandle GlyphPositions = Material.GetUniformLocation("glyphPositions");
        public static readonly GlobalUniformHandle GlyphUVs = Material.GetUniformLocation("glpyhUVs");
        public static readonly GlobalUniformHandle Font = Material.GetUniformLocation("font");

        // LinesRenderStage.
        public static readonly GlobalUniformHandle LineVertices = Material.GetUniformLocation("LineVertices");

        // Generic / shared.
        public static readonly GlobalUniformHandle Texture1 = Material.GetUniformLocation("texture1");
        public static readonly GlobalUniformHandle MainTex = Material.GetUniformLocation("_MainTex");
        public static readonly GlobalUniformHandle DepthTex = Material.GetUniformLocation("_DepthTex");
    }
}
