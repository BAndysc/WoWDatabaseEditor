using System.Runtime.CompilerServices;
using Avalonia.Input;
using ImGuiNET;
using OpenGLBindings;
using SixLabors.ImageSharp.PixelFormats;
using TheAvaloniaOpenGL.Resources;
using TheEngine.Entities;
using TheEngine.Handles;
using MouseButton = TheEngine.Input.MouseButton;

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
    
    private readonly NativeBuffer<ImDrawVert> verticesBuffer;
    private readonly NativeBuffer<ushort> indicesBuffer;
    private readonly Engine engine;
    private readonly IntPtr imGuiContext;
    private readonly int vertexArrayObject;
    private readonly ShaderHandle shaderHandle;
    private readonly Material material;
    private readonly TextureHandle fontTexture;
    private ImDrawVert[] verts = Array.Empty<ImDrawVert>();
    private ushort[] indices = Array.Empty<ushort>();

    public unsafe ImGuiController(Engine engine)
    {
        this.engine = engine;
        
        imGuiContext = ImGui.CreateContext();
        ImGui.SetCurrentContext(imGuiContext);
        var io = ImGui.GetIO();
        // ImFontConfig config = new();
        // config.OversampleH = 3;
        // config.OversampleV = 1;
        // config.RasterizerMultiply = 1;
        // config.EllipsisChar = ushort.MaxValue;
        // config.FontDataOwnedByAtlas = 1;
        io.Fonts.AddFontDefault();

        io.BackendFlags |= ImGuiBackendFlags.RendererHasVtxOffset | ImGuiBackendFlags.HasSetMousePos;
        ImGui.StyleColorsDark();
        
        verticesBuffer = engine.CreateBuffer<ImDrawVert>(BufferTypeEnum.Vertex, 1);
        indicesBuffer = engine.CreateBuffer<ushort>(BufferTypeEnum.Index, 1);
        vertexArrayObject = engine.Device.device.GenVertexArray();
        engine.Device.device.BindVertexArray(vertexArrayObject);
        verticesBuffer.Activate(0);
        indicesBuffer.Activate(0);
        int stride = 2 * 4 + 2 * 4 + 4;
        engine.Device.device.VertexAttribPointer(0, 2, VertexAttribPointerType.Float, false, stride, new IntPtr(0));
        engine.Device.device.EnableVertexAttribArray(0);

        engine.Device.device.VertexAttribPointer(1, 2, VertexAttribPointerType.Float, false, stride, new IntPtr(8));
        engine.Device.device.EnableVertexAttribArray(1);

        engine.Device.device.VertexAttribPointer(2, 4, VertexAttribPointerType.UnsignedByte, true, stride, new IntPtr(16));
        engine.Device.device.EnableVertexAttribArray(2);
        engine.Device.device.BindVertexArray(0);

        shaderHandle = engine.shaderManager.LoadShader("internalShaders/imgui.json", false);
        material = engine.materialManager.CreateMaterial(shaderHandle, null);
        
        var fonts = ImGui.GetIO().Fonts;
        io.Fonts.GetTexDataAsRGBA32(out IntPtr pixels, out int width, out int height, out int bytesPerPixel);
        fontTexture = engine.textureManager.CreateTexture((Rgba32*)pixels, width, height,  true);
        fonts.SetTexID(new IntPtr(fontTexture.Handle));
        ImGui.NewFrame();
    }

    public void BeforeRender(float delta)
    {
        ImGui.SetCurrentContext(imGuiContext);
        ImGuiIOPtr io = ImGui.GetIO();
        io.DisplaySize = new Vector2(Math.Max(1, engine.WindowHost.WindowWidth), Math.Max(1, engine.WindowHost.WindowHeight));
        io.DisplayFramebufferScale = new Vector2(engine.WindowHost.DpiScaling, engine.WindowHost.DpiScaling);
        io.DeltaTime = delta; // DeltaTime is in seconds.

        io.MousePos = engine.inputManager.mouse.ScreenPoint / engine.WindowHost.DpiScaling;
        io.MouseDown[0] = engine.inputManager.mouse.IsMouseDown(MouseButton.Left);
        io.MouseDown[1] = engine.inputManager.mouse.IsMouseDown(MouseButton.Right);
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

        io.KeyShift = engine.inputManager.Keyboard.IsDown(Key.LeftShift);
        io.KeyAlt = engine.inputManager.Keyboard.IsDown(Key.LeftAlt);
        io.KeyCtrl = engine.inputManager.Keyboard.IsDown(Key.LeftCtrl);

        ImGui.NewFrame();
    }

    public unsafe void Render()
    {
        ImGui.Render();
        var drawData = ImGui.GetDrawData();
        
        uint vertexOffsetInBytes = 0;
        uint indexOffsetInBytes = 0;

        if (drawData.TotalVtxCount < 0 || drawData.TotalIdxCount < 0)
            return;
        
        if (verts.Length <= drawData.TotalVtxCount)
            verts = new ImDrawVert[drawData.TotalVtxCount];
        
        if (indices.Length <= drawData.TotalIdxCount)
            indices = new ushort[drawData.TotalIdxCount];

        for (int i = 0; i < drawData.CmdListsCount; i++)
        {
            ImDrawListPtr cmdList = drawData.CmdListsRange[i];

            fixed (void* ptr = verts)
                Unsafe.CopyBlock((byte*)ptr + vertexOffsetInBytes, (void*)cmdList.VtxBuffer.Data, (uint)(cmdList.VtxBuffer.Size * sizeof(ImDrawVert)));

            fixed (void* ptr = indices)
                Unsafe.CopyBlock((byte*)ptr + indexOffsetInBytes, (void*)cmdList.IdxBuffer.Data, (uint)(cmdList.IdxBuffer.Size * sizeof(ushort)));

            vertexOffsetInBytes += (uint)(cmdList.VtxBuffer.Size * sizeof(ImDrawVert));
            indexOffsetInBytes += (uint)(cmdList.IdxBuffer.Size * sizeof(ushort));
        }
        
        verticesBuffer.UpdateBuffer(verts.AsSpan(0, drawData.TotalVtxCount));
        indicesBuffer.UpdateBuffer(indices.AsSpan(0, drawData.TotalIdxCount));
        
        ImGuiIOPtr io = ImGui.GetIO();
        Matrix4x4 mvp = Matrix4x4.CreateOrthographicOffCenter(0f, io.DisplaySize.X / io.DisplayFramebufferScale.X, io.DisplaySize.Y / io.DisplayFramebufferScale.Y, 0.0f, -1.0f, 1.0f);

        engine.Device.device.BindVertexArray(vertexArrayObject);
        verticesBuffer.Activate(0);
        indicesBuffer.Activate(0);

        var shader = engine.shaderManager.GetShaderByHandle(shaderHandle);
        shader.Activate();
        
        material.SetUniform("projection_matrix", mvp);
        material.ActivateUniforms(false);
        engine.Device.device.Enable(EnableCap.Blend);
        engine.Device.device.BlendEquation(BlendEquationMode.FuncAdd);
        engine.Device.device.BlendFuncSeparate(BlendingFactorSrc.SrcAlpha, BlendingFactorDest.OneMinusSrcAlpha, BlendingFactorSrc.One, BlendingFactorDest.OneMinusSrcAlpha);
        engine.Device.device.Disable(EnableCap.CullFace);
        engine.Device.device.Disable(EnableCap.DepthTest);
        engine.Device.device.Disable(EnableCap.StencilTest);
        engine.Device.device.Enable(EnableCap.ScissorTest);
        
        drawData.ScaleClipRects(io.DisplayFramebufferScale);

        // Render command lists
        int vtxOffset = 0;
        int idxOffset = 0;
        for (int n = 0; n < drawData.CmdListsCount; n++)
        {
            ImDrawListPtr cmdList = drawData.CmdListsRange[n];
            for (int cmdI = 0; cmdI < cmdList.CmdBuffer.Size; cmdI++)
            {
                ImDrawCmdPtr pcmd = cmdList.CmdBuffer[cmdI];
                if (pcmd.UserCallback != IntPtr.Zero)
                {
                    throw new NotImplementedException();
                }
                else
                {
                    if (pcmd.TextureId != IntPtr.Zero)
                    {
                        var handle = new TextureHandle(pcmd.TextureId.ToInt32());
                        material.SetTexture("FontTexture", handle);
                    }
                    Vector2 clipMin = new(pcmd.ClipRect.X, pcmd.ClipRect.Y);
                    Vector2 clipMax = new(pcmd.ClipRect.Z, pcmd.ClipRect.W);

                    engine.Device.device.Scissor((int)clipMin.X, (int)(io.DisplaySize.Y - clipMax.Y), (int)(clipMax.X - clipMin.X), (int)(clipMax.Y - clipMin.Y));

                    engine.Device.DrawIndexed((int)pcmd.ElemCount, (int)pcmd.IdxOffset + (int)idxOffset, (int)pcmd.VtxOffset + vtxOffset);
                }
            }
            vtxOffset += cmdList.VtxBuffer.Size;
            idxOffset += cmdList.IdxBuffer.Size;
        }
        
        engine.Device.device.Disable(EnableCap.ScissorTest);
    }

    public void Dispose()
    {
        ImGui.DestroyContext(imGuiContext);
        engine.textureManager.DisposeTexture(fontTexture);
        verticesBuffer.Dispose();
        indicesBuffer.Dispose();
        engine.Dispose();
    }
}