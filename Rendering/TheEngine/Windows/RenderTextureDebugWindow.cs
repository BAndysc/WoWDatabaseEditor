using ImGuiNET;
using TheAvaloniaOpenGL.Resources;

namespace TheEngine.Windows;

public class RenderTextureDebugWindow
{
    private readonly Engine engine;

    public RenderTextureDebugWindow(Engine engine)
    {
        this.engine = engine;
    }

    public void Update(float delta)
    {
        if (IsOpen)
        {
            IsOpen = UpdateWindow(delta);
        }
    }

    public bool IsOpen { get; set; }

    private bool UpdateWindow(float delta)
    {
        bool open = IsOpen;
        ImGuiEx.Begin("Render texture debugger\0"u8, ref open);

        var space = ImGui.GetContentRegionAvail();

        foreach (var tex in engine.textureManager.AllTextures)
        {
            if (tex.NativeTexture is RenderTexture rt)
            {
                for (int i = 0; i < rt.ColorAttachments; ++i)
                {
                    var attachment = rt.GetTexture(i);
                    var aspect = (float)attachment.Height / attachment.Width;
                    var nativeHandle = attachment.NativeHandle;
                    ImGui.Image(new IntPtr((long)int.MaxValue + nativeHandle), new Vector2(space.X, space.X * aspect));
                }
            }
        }

        ImGui.End();

        return open;
    }
}