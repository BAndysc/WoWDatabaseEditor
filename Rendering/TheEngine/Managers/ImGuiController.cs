using System.Numerics;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using Hexa.NET.ImGui;
using Hexa.NET.ImGuizmo;
using Silk.NET.Vulkan;
using SixLabors.ImageSharp.PixelFormats;
using TheEngine;
using TheEngine.Resources;
using TheEngine.Entities;
using TheEngine.Handles;
using TheEngine.Input;
using TheEngine.Interfaces;
using TheEngine.Utils;
using Veldrid;
using BlendFactor = Veldrid.BlendFactor;
using FrontFace = Veldrid.FrontFace;
using VkIndexType = Silk.NET.Vulkan.IndexType;
using MouseButton = TheEngine.Input.MouseButton;
using PrimitiveTopology = Veldrid.PrimitiveTopology;

namespace TheEngine.Managers;

public class ImGuiController : IDisposable
{
    private static ImGuiKey[] AvaloniaToKeyMapping = new[]
    {
        ImGuiKey.None, // 0
        ImGuiKey.None, // 1
        ImGuiKey.Backspace, // 2
        ImGuiKey.Tab, // 3
        ImGuiKey.None, // 4
        ImGuiKey.None, // 5
        ImGuiKey.Enter, // 6
        ImGuiKey.Pause, // 7
        ImGuiKey.CapsLock, // 8
        ImGuiKey.None, // 9
        ImGuiKey.None, // 10
        ImGuiKey.None, // 11
        ImGuiKey.None, // 12
        ImGuiKey.Escape, // 13
        ImGuiKey.None, // 14
        ImGuiKey.None, // 15
        ImGuiKey.None, // 16
        ImGuiKey.None, // 17
        ImGuiKey.Space, // 18
        ImGuiKey.PageUp, // 19
        ImGuiKey.PageDown, // 20
        ImGuiKey.End, // 21
        ImGuiKey.Home, // 22
        ImGuiKey.LeftArrow, // 23
        ImGuiKey.UpArrow, // 24
        ImGuiKey.RightArrow, // 25
        ImGuiKey.DownArrow, // 26
        ImGuiKey.None, // 27
        ImGuiKey.None, // 28
        ImGuiKey.None, // 29
        ImGuiKey.PrintScreen, // 30
        ImGuiKey.Insert, // 31
        ImGuiKey.Delete, // 32
        ImGuiKey.None, // 33
        ImGuiKey.None, // 34
        ImGuiKey.None, // 35
        ImGuiKey.None, // 36
        ImGuiKey.None, // 37
        ImGuiKey.None, // 38
        ImGuiKey.None, // 39
        ImGuiKey.None, // 40
        ImGuiKey.None, // 41
        ImGuiKey.None, // 42
        ImGuiKey.None, // 43
        ImGuiKey.A, // 44
        ImGuiKey.B, // 45
        ImGuiKey.C, // 46
        ImGuiKey.D, // 47
        ImGuiKey.E, // 48
        ImGuiKey.F, // 49
        ImGuiKey.G, // 50
        ImGuiKey.H, // 51
        ImGuiKey.I, // 52
        ImGuiKey.J, // 53
        ImGuiKey.K, // 54
        ImGuiKey.L, // 55
        ImGuiKey.M, // 56
        ImGuiKey.N, // 57
        ImGuiKey.O, // 58
        ImGuiKey.P, // 59
        ImGuiKey.Q, // 60
        ImGuiKey.R, // 61
        ImGuiKey.S, // 62
        ImGuiKey.T, // 63
        ImGuiKey.U, // 64
        ImGuiKey.V, // 65
        ImGuiKey.W, // 66
        ImGuiKey.X, // 67
        ImGuiKey.Y, // 68
        ImGuiKey.Z, // 69
        ImGuiKey.LeftSuper, // 70
        ImGuiKey.RightSuper, // 71
        ImGuiKey.None, // 72
        ImGuiKey.None, // 73
        ImGuiKey.Keypad0, // 74
        ImGuiKey.Keypad1, // 75
        ImGuiKey.Keypad2, // 76
        ImGuiKey.Keypad3, // 77
        ImGuiKey.Keypad4, // 78
        ImGuiKey.Keypad5, // 79
        ImGuiKey.Keypad6, // 80
        ImGuiKey.Keypad7, // 81
        ImGuiKey.Keypad8, // 82
        ImGuiKey.Keypad9, // 83
        ImGuiKey.KeypadMultiply, // 84
        ImGuiKey.KeypadAdd, // 85
        ImGuiKey.KeypadDecimal, // 86
        ImGuiKey.KeypadSubtract, // 87
        ImGuiKey.KeypadDecimal, // 88
        ImGuiKey.KeypadDivide, // 89
        ImGuiKey.F1, // 90
        ImGuiKey.F2, // 91
        ImGuiKey.F3, // 92
        ImGuiKey.F4, // 93
        ImGuiKey.F5, // 94
        ImGuiKey.F6, // 95
        ImGuiKey.F7, // 96
        ImGuiKey.F8, // 97
        ImGuiKey.F9, // 98
        ImGuiKey.F10, // 99
        ImGuiKey.F11, // 100
        ImGuiKey.F12, // 101
        ImGuiKey.None, // 102
        ImGuiKey.None, // 103
        ImGuiKey.None, // 104
        ImGuiKey.None, // 105
        ImGuiKey.None, // 106
        ImGuiKey.None, // 107
        ImGuiKey.None, // 108
        ImGuiKey.None, // 109
        ImGuiKey.None, // 110
        ImGuiKey.None, // 111
        ImGuiKey.None, // 112
        ImGuiKey.None, // 113
        ImGuiKey.NumLock, // 114
        ImGuiKey.ScrollLock, // 115
        ImGuiKey.LeftShift, // 116
        ImGuiKey.RightShift, // 117
        ImGuiKey.LeftCtrl, // 118
        ImGuiKey.RightCtrl, // 119
        ImGuiKey.LeftAlt, // 120
        ImGuiKey.RightAlt, // 121
    };
    
    private readonly Engine engine;
    private readonly ImGuiContextPtr imGuiContext;
    private readonly Material<ImGuiMaterialData_t> material;
    private readonly ITexture fontTexture;
    private ImDrawVert[] verts = Array.Empty<ImDrawVert>();
    private ushort[] indices = Array.Empty<ushort>();

    [StructLayout(LayoutKind.Sequential, Pack = 4)]
    private struct ImGuiMaterialData_t
    {
        public Matrix projection_matrix;
    }

    public unsafe ImGuiController(Engine engine)
    {
        this.engine = engine;
        
        imGuiContext = ImGui.CreateContext();
        ImGui.SetCurrentContext(imGuiContext);
        var io = ImGui.GetIO();
        io.ConfigFlags |= ImGuiConfigFlags.DockingEnable;

        // Default the docked game/scene tab to "3D". The game ("3D") and scene ("Scene View")
        // windows share one dock node; only the active tab renders. The saved imgui.ini
        // restores whichever tab was last selected, and that persisted Selected= can't be
        // overridden by SetWindowFocus at runtime. So, before ImGui lazily loads the ini on
        // the first NewFrame, we surgically drop just the central node's Selected= token,
        // leaving the rest of the layout untouched; the SetWindowFocus("3D") in TheEngineUi
        // then reliably selects 3D (as it already does on a fresh layout).
        DefaultDockedTabToGameView();
        io.DisplaySize = new Vector2(1, 1); // init to something non zero
        var fonts = io.Fonts;

        // default font, each with the Lucide icon font merged in (MergeMode appends the icon
        // glyphs to the PREVIOUSLY added font, so text and icons mix freely in one string -
        // use the TheEngine.Lucide constants, e.g. $"{Lucide.Save} Save")
        fonts.AddFontFromFileTTF("fonts/DroidSans.ttf", 15);
        MergeIconFont(fonts, 15);
        fonts.AddFontFromFileTTF("fonts/DroidSans-Bold.ttf", 15);
        MergeIconFont(fonts, 15);
        fonts.AddFontFromFileTTF("fonts/DroidSans-Bold.ttf", 25);
        MergeIconFont(fonts, 25);

        io.BackendFlags |= ImGuiBackendFlags.RendererHasVtxOffset | ImGuiBackendFlags.HasSetMousePos | ImGuiBackendFlags.RendererHasTextures;
        ImGui.StyleColorsDark();

        var style = ImGui.GetStyle();
        style.FrameRounding = 4.0f;
        style.FrameBorderSize = 1.0f;
        style.Colors[(int)ImGuiCol.Button] = new Vector4(0.15f, 0.15f, 0.20f, 1.0f);
        style.Colors[(int)ImGuiCol.ButtonHovered] = new Vector4(0.30f, 0.30f, 0.35f, 1.0f);
        style.Colors[(int)ImGuiCol.ButtonActive] = new Vector4(0.15f, 0.15f, 0.20f, 1.0f);
        style.Colors[(int)ImGuiCol.Border] = new Vector4(0.10f, 0.10f, 0.10f, 1.0f);
        style.FramePadding = new Vector2(6, 6);

        var shaderHandle = engine.shaderManager.LoadShader("internalShaders/imgui.json");
        var desc = new GraphicsPipelineDescription()
        {
            BlendState = new BlendStateDescription(RgbaFloat.Clear)
            {AttachmentStates = new[]
                {
                    new BlendAttachmentDescription(true, BlendFactor.SourceAlpha, BlendFactor.InverseSourceAlpha,BlendFunction.Add, BlendFactor.One, BlendFactor.InverseSourceAlpha, BlendFunction.Add),
                }

            },
            DepthStencilState = new DepthStencilStateDescription(false, false, ComparisonKind.Always, false, default, default, default, default, default),
            RasterizerState = new RasterizerStateDescription(FaceCullMode.None, PolygonFillMode.Solid, FrontFace.Clockwise, true, true),
        };
        var pipeline = engine.pipelineManager.CreatePipeline(shaderHandle, PrimitiveTopology.TriangleList, desc, true);

        material = engine.materialManager.CreateMaterial<ImGuiMaterialData_t>(pipeline);
    }

    /// <summary>Merges the Lucide icon glyphs (U+E038..U+E6FD, see <see cref="Lucide"/>) into the
    /// font added just before it (MergeMode). Icons render slightly smaller than the text size and
    /// are nudged down so they sit on the text baseline instead of floating above it.</summary>
    private static unsafe void MergeIconFont(ImFontAtlasPtr fonts, float textSize)
    {
        // an explicitly-initialized config: a zeroed struct is NOT a valid ImFontConfig
        // (RasterizerMultiply/Density 0 and GlyphMaxAdvanceX 0 produce invisible glyphs)
        var cfg = new ImFontConfig(
            name: (byte*)null,
            fontDataOwnedByAtlas: true,
            mergeMode: true,
            pixelSnapH: true,
            rasterizerMultiply: 1f,
            rasterizerDensity: 1f,
            glyphMaxAdvanceX: float.MaxValue,
            glyphMinAdvanceX: textSize, // icons align like a column of monospaced glyphs
            glyphOffset: new Vector2(0, MathF.Round(textSize * 0.14f)));
        fonts.AddFontFromFileTTF("fonts/lucide.ttf", textSize - 2, &cfg);
    }

    private static void DefaultDockedTabToGameView()
    {
        // io.IniFilename defaults to "imgui.ini" (relative to the working directory); the
        // engine never overrides it. Best-effort: never let a layout tweak crash startup.
        try
        {
            const string iniPath = "imgui.ini";
            if (!System.IO.File.Exists(iniPath))
                return;
            var text = System.IO.File.ReadAllText(iniPath);
            // Remove the "Selected=0x........" token only on the central dock node line.
            var updated = System.Text.RegularExpressions.Regex.Replace(
                text,
                @"(?m)^(.*CentralNode=1[^\r\n]*?)\s+Selected=0x[0-9A-Fa-f]+",
                "$1");
            if (updated != text)
                System.IO.File.WriteAllText(iniPath, updated);
        }
        catch
        {
            // ignore - defaulting the tab is a convenience, not correctness
        }
    }

    public void UpdateImGui(float delta)
    {
        ImGui.SetCurrentContext(imGuiContext);
        ImGuiIOPtr io = ImGui.GetIO();

        io.DisplaySize = new Vector2(Math.Max(1, engine.WindowHost.WindowWidth), Math.Max(1, engine.WindowHost.WindowHeight));
        io.DisplayFramebufferScale = new Vector2(engine.WindowHost.DpiScaling, engine.WindowHost.DpiScaling);
        io.DeltaTime = delta; // DeltaTime is in seconds.

        io.MousePos = engine.inputManager.mouse.RawScreenPoint;
        io.MouseDown[0] = engine.inputManager.mouse.RawIsMouseDown(MouseButton.Left);
        io.MouseDown[1] = engine.inputManager.mouse.RawIsMouseDown(MouseButton.Right);
        io.MouseDown[2] = engine.inputManager.mouse.RawIsMouseDown(MouseButton.Middle);
        io.MouseWheel = engine.inputManager.mouse.WheelDelta.Y;
        io.MouseWheelH = engine.inputManager.mouse.WheelDelta.X;

        for (int i = 0; i < engine.inputManager.keyboard.justTextInputIndex; ++i)
        {
            var text = engine.inputManager.keyboard.justTextInput[i];
            io.AddInputCharacter(text);
        }
        
        for (int i = 0; i < engine.inputManager.keyboard.justPressedKeysIndex; ++i)
        {
            var pressedKey = engine.inputManager.keyboard.justPressedKeys[i];
            if ((int)pressedKey < AvaloniaToKeyMapping.Length)
            {
                var imGuiKey = AvaloniaToKeyMapping[(int)pressedKey];
                if (imGuiKey != ImGuiKey.None)
                {
                    io.AddKeyEvent(imGuiKey, true);
                }
            }
        }
        
        for (int i = 0; i < engine.inputManager.keyboard.justReleasedKeysIndex; ++i)
        {
            var releasedKey = engine.inputManager.keyboard.justReleasedKeys[i];
            if ((int)releasedKey < AvaloniaToKeyMapping.Length)
            {
                var imGuiKey = AvaloniaToKeyMapping[(int)releasedKey];
                if (imGuiKey != ImGuiKey.None)
                    io.AddKeyEvent(imGuiKey, false);
            }
        }

        io.KeyShift = engine.inputManager.keyboard.RawIsDown(Key.LeftShift);
        io.KeyAlt = engine.inputManager.keyboard.RawIsDown(Key.LeftAlt);
        io.KeyCtrl = engine.inputManager.keyboard.RawIsDown(Key.LeftCtrl);

        if (io.WantCaptureMouse && !engine.gameView.IsHovered && !engine.sceneView.IsHovered)
            engine.inputManager.mouse.PostUpdate();

        // if (io.WantCaptureKeyboard)
        // {
        //     Console.WriteLine("Want capture keyboard");
        //     engine.inputManager.keyboard.PostUpdate();
        //     engine.inputManager.keyboard.ReleaseAllKeys();
        // }

        for (int i = texturesToDelayedFree.Count - 1; i >= 0; --i)
        {
            if (engine.FrameCount >= texturesToDelayedFree[i].frame)
            {
                textureReferences.Remove(texturesToDelayedFree[i].Item1);
                texturesToDelayedFree[i].Item1.Free();
                texturesToDelayedFree.RemoveAt(i);
            }
        }

        ImGui.NewFrame();
        ImGuizmo.SetImGuiContext(ImGui.GetCurrentContext());
        ImGuizmo.BeginFrame();
    }

    public unsafe void Render()
    {
        ImGui.Render();
        var drawData = ImGui.GetDrawData();

        ref var textures = ref drawData.Textures;
        for (int i = 0; i < textures.Size; ++i)
        {
            var texture = textures[i];
            if (texture.Status != ImTextureStatus.Ok)
            {
                UpdateTexture(texture);
            }
        }

        uint vertexOffsetInBytes = 0;
        uint indexOffsetInBytes = 0;

        if (drawData.TotalVtxCount <= 0 || drawData.TotalIdxCount <= 0)
            return;
        
        if (verts.Length <= drawData.TotalVtxCount)
            verts = new ImDrawVert[drawData.TotalVtxCount];
        
        if (indices.Length <= drawData.TotalIdxCount)
            indices = new ushort[drawData.TotalIdxCount];

        for (int i = 0; i < drawData.CmdListsCount; i++)
        {
            ImDrawListPtr cmdList = drawData.CmdLists[i];

            fixed (void* ptr = verts)
                Unsafe.CopyBlock((byte*)ptr + vertexOffsetInBytes, (void*)cmdList.VtxBuffer.Data, (uint)(cmdList.VtxBuffer.Size * sizeof(ImDrawVert)));

            fixed (void* ptr = indices)
                Unsafe.CopyBlock((byte*)ptr + indexOffsetInBytes, (void*)cmdList.IdxBuffer.Data, (uint)(cmdList.IdxBuffer.Size * sizeof(ushort)));

            vertexOffsetInBytes += (uint)(cmdList.VtxBuffer.Size * sizeof(ImDrawVert));
            indexOffsetInBytes += (uint)(cmdList.IdxBuffer.Size * sizeof(ushort));
        }
        
        var commandList = engine.renderManager.CommandList;
        var vertexBuffer = commandList.UploadTransientBuffer(BufferTypeEnum.Vertex, (ReadOnlySpan<ImDrawVert>)verts.AsSpan(0, drawData.TotalVtxCount));
        var indexBuffer = commandList.UploadTransientBuffer(BufferTypeEnum.Index, (ReadOnlySpan<ushort>)indices.AsSpan(0, drawData.TotalIdxCount));

        ImGuiIOPtr io = ImGui.GetIO();
        Matrix4x4 mvp = Matrix4x4.CreateOrthographicOffCenter(0f, io.DisplaySize.X / io.DisplayFramebufferScale.X, io.DisplaySize.Y / io.DisplayFramebufferScale.Y, 0.0f, -1.0f, 1.0f);

        var shader = material.Pipeline.Shader;
        commandList.SetPipeline(material.Pipeline, shader.ForwardPass);
        // the vertex input layout comes from the pipeline (the "small layout"), so the
        // streamed buffers are bound like any other resource - the command list captures
        // the layout against the concrete buffer object in its own scratch VAO
        commandList.BindVertexBuffer(vertexBuffer);
        commandList.BindIndexBuffer(indexBuffer, VkIndexType.Uint16);

        ImGuiMaterialData_t data = new ImGuiMaterialData_t() { projection_matrix = mvp };
        material.SetMaterialData(ref data);
        commandList.BindMaterialResources(material);

        drawData.ScaleClipRects(io.DisplayFramebufferScale);

        // Render command lists
        int vtxOffset = 0;
        int idxOffset = 0;
        TextureHandle? prevHandle = null;
        for (int n = 0; n < drawData.CmdListsCount; n++)
        {
            ImDrawListPtr cmdList = drawData.CmdLists[n];
            for (int cmdI = 0; cmdI < cmdList.CmdBuffer.Size; cmdI++)
            {
                ImDrawCmd pcmd = cmdList.CmdBuffer[cmdI];
                if (pcmd.UserCallback != null)
                {
                    throw new NotImplementedException();
                }
                else
                {
                    var texId = pcmd.TexRef.TexID;
                    if (pcmd.TexRef.TexData != null && pcmd.TexRef.TexData->TexID != IntPtr.Zero)
                        texId = pcmd.TexRef.TexData->TexID;
                    if (texId != IntPtr.Zero)
                    {
                        var handle = TextureHandle.FromIntPtr(texId);
                        if (prevHandle != handle)
                        {
                            // a render texture referenced here (e.g. the 3D viewport image) is disposed
                            // and recreated on window resize, leaving ImGui with a stale handle for one
                            // frame - fall back to the empty texture instead of dereferencing null.
                            var texture = engine.textureManager[handle] ?? engine.textureManager.EmptyTexture;
                            // bindless: just point the shader at this texture's slot via a push
                            // constant - set 1 (the projection UBO) stays bound from before the loop.
                            commandList.SetBindlessTextureIndex(engine.textureManager.GetBindlessIndex(texture));
                            prevHandle = handle;
                        }
                    }
                    Vector2 clipMin = new(pcmd.ClipRect.X, pcmd.ClipRect.Y);
                    Vector2 clipMax = new(pcmd.ClipRect.Z, pcmd.ClipRect.W);

                    // top-left origin (the command list's convention matches ImGui's)
                    commandList.SetScissor((int)clipMin.X, (int)clipMin.Y, (int)(clipMax.X - clipMin.X), (int)(clipMax.Y - clipMin.Y));

                    commandList.DrawIndexed((int)pcmd.ElemCount, (int)pcmd.IdxOffset + idxOffset, (int)pcmd.VtxOffset + vtxOffset);
                }
            }
            vtxOffset += cmdList.VtxBuffer.Size;
            idxOffset += cmdList.IdxBuffer.Size;
        }
    }

    private List<(StaticReference, long frame)> texturesToDelayedFree = new();

    private unsafe void UpdateTexture(ImTextureDataPtr tex)
    {
        var create = tex.Status == ImTextureStatus.WantCreate || tex.Status == ImTextureStatus.WantUpdates;
        var destroy = tex.Status == ImTextureStatus.WantUpdates || tex.Status == ImTextureStatus.WantDestroy && tex.UnusedFrames > 0;

        if (destroy)
        {
            var handle = tex.TexID;
            var staticRef = StaticReference.FromIntPtr((IntPtr)tex.BackendUserData);
            texturesToDelayedFree.Add((staticRef, engine.FrameCount + 2));

            tex.SetTexID(default);
            tex.BackendUserData = default;

            tex.SetStatus(ImTextureStatus.Destroyed);
        }

        if (create)
        {
            void* pixels = tex.GetPixels();

            int width = tex.Width;
            int height = tex.Height;

            var texture = engine.textureManager.CreateTexture(
                (Rgba32*)pixels,
                width,
                height,
                false); // no mipmaps for fonts

            tex.SetTexID(new IntPtr(texture.Handle.Handle));

            var reference = texture.GetStaticReference();
            textureReferences.Add(reference);
            
            tex.BackendUserData = (void*)reference.AsIntPtr();

            tex.SetStatus(ImTextureStatus.Ok);
        }
    }

    private List<StaticReference> textureReferences = new();

    public void Dispose()
    {
        foreach (var reference in textureReferences)
        {
            reference.Free();
        }
        ImGui.DestroyContext(imGuiContext);
        engine.textureManager.DisposeTexture(fontTexture);
    }
}