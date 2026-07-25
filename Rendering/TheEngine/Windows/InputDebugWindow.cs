using Hexa.NET.ImGui;
using TheEngine.Input;
using TheMaths;

namespace TheEngine.Windows;

public class InputDebugWindow
{
    private readonly Engine engine;

    public InputDebugWindow(Engine engine)
    {
        this.engine = engine;
    }

    public struct LastEvents
    {
        public long LeftClick;
        public long RightClick;
        public long LeftRelease;
        public long RightRelease;
    }

    private LastEvents LastGameEvents;
    private LastEvents LastRawEvents;

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
        var io = ImGui.GetIO();
        var boldFont = io.Fonts.Fonts[1]; // DroidSans-Bold.ttf at size 15

        bool open = IsOpen;
        ImGuiEx.Begin("Input debugger\0"u8, ref open);
        IsOpen = open;

        var mouse = engine.inputManager.mouse;
        var keyboard = engine.inputManager.keyboard;

        if (mouse.RawHasJustClicked(MouseButton.Left)) LastRawEvents.LeftClick = engine.FrameCount;
        if (mouse.RawHasJustClicked(MouseButton.Right)) LastRawEvents.RightClick = engine.FrameCount;
        if (mouse.RawHasJustReleased(MouseButton.Left)) LastRawEvents.LeftRelease = engine.FrameCount;
        if (mouse.RawHasJustReleased(MouseButton.Right)) LastRawEvents.RightRelease = engine.FrameCount;
        if (mouse.HasJustClicked(MouseButton.Left)) LastGameEvents.LeftClick = engine.FrameCount;
        if (mouse.HasJustClicked(MouseButton.Right)) LastGameEvents.RightClick = engine.FrameCount;
        if (mouse.HasJustReleased(MouseButton.Left)) LastGameEvents.LeftRelease = engine.FrameCount;
        if (mouse.HasJustReleased(MouseButton.Right)) LastGameEvents.RightRelease = engine.FrameCount;

        ImGui.Columns(2);
        ImGuiEx.TextUnformatted("Axis\0"u8);
        ImGui.NextColumn();
        var axis = keyboard.GetAxis(Vectors.Down, Key.W, Key.S) +
                       keyboard.GetAxis(Vectors.Backward, Key.A, Key.D) +
                       keyboard.GetAxis(Vectors.Left, Key.E, Key.Q);
        ImGui.TextUnformatted($"{axis.X:0.000}, {axis.Y:0.000}, {axis.Z:0.000}");
        ImGui.NextColumn();

        
        ImGuiEx.TextUnformatted("Mouse delta\0"u8);
        ImGui.NextColumn();
        ImGui.TextUnformatted($"{mouse.Delta.X:0.000}, {mouse.Delta.Y:0.000}");
        ImGui.NextColumn();

        ImGuiEx.TextUnformatted("Mouse wheel\0"u8);
        ImGui.NextColumn();
        ImGui.TextUnformatted($"{mouse.WheelDelta.X:0.000}, {mouse.WheelDelta.Y:0.000}");
        ImGui.NextColumn();

        ImGui.Columns(1);
        ImGui.PushFont(boldFont, 0);
        ImGui.TextUnformatted("Game view:"u8);
        ImGui.PopFont();
        ImGui.Columns(2);

        ImGuiEx.TextUnformatted("Screen point\0"u8);
        ImGui.NextColumn();
        ImGui.TextUnformatted($"{mouse.ScreenPoint.X:0}, {mouse.ScreenPoint.Y:0}");
        ImGui.NextColumn();

        ImGuiEx.TextUnformatted("Left mouse button\0"u8);
        ImGui.NextColumn();
        ImGui.TextUnformatted($"{mouse.IsMouseDown(MouseButton.Left)}");
        ImGui.NextColumn();

        ImGuiEx.TextUnformatted("Right mouse button\0"u8);
        ImGui.NextColumn();
        ImGui.TextUnformatted($"{mouse.IsMouseDown(MouseButton.Right)}");
        ImGui.NextColumn();

        void PrintLastEvents(ref readonly LastEvents lastEvents)
        {
            ImGuiEx.TextUnformatted("Last left click\0"u8);
            ImGui.NextColumn();
            ImGui.TextUnformatted(lastEvents.LeftClick == 0 ? "" : lastEvents.LeftClick.ToString());
            ImGui.NextColumn();

            ImGuiEx.TextUnformatted("Last right click\0"u8);
            ImGui.NextColumn();
            ImGui.TextUnformatted(lastEvents.RightClick == 0 ? "" : lastEvents.RightClick.ToString());
            ImGui.NextColumn();

            ImGuiEx.TextUnformatted("Last left release\0"u8);
            ImGui.NextColumn();
            ImGui.TextUnformatted(lastEvents.LeftRelease == 0 ? "" : lastEvents.LeftRelease.ToString());
            ImGui.NextColumn();

            ImGuiEx.TextUnformatted("Last right release\0"u8);
            ImGui.NextColumn();
            ImGui.TextUnformatted(lastEvents.RightRelease == 0 ? "" : lastEvents.RightRelease.ToString());
            ImGui.NextColumn();
        }

        PrintLastEvents(in LastGameEvents);

        ImGui.Columns(1);
        ImGui.PushFont(boldFont, 0);
        ImGui.TextUnformatted("Raw events:"u8);
        ImGui.PopFont();
        ImGui.Columns(2);

        ImGuiEx.TextUnformatted("Raw screen point\0"u8);
        ImGui.NextColumn();
        ImGui.TextUnformatted($"{mouse.RawScreenPoint.X:0}, {mouse.RawScreenPoint.Y:0}");
        ImGui.NextColumn();

        ImGuiEx.TextUnformatted("Left mouse button\0"u8);
        ImGui.NextColumn();
        ImGui.TextUnformatted($"{mouse.RawIsMouseDown(MouseButton.Left)}");
        ImGui.NextColumn();

        ImGuiEx.TextUnformatted("Right mouse button\0"u8);
        ImGui.NextColumn();
        ImGui.TextUnformatted($"{mouse.RawIsMouseDown(MouseButton.Right)}");
        ImGui.NextColumn();

        PrintLastEvents(in LastRawEvents);

        ImGui.Columns(1);

        ImGui.End();
        return open;
    }
}