using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Hexa.NET.ImGui;
using Tedd;
using TheMaths;

namespace TheEngine.Utils.ImGuiHelper
{
    public enum BoxPlacement
    {
        None, TopLeft, BottomLeft, TopRight, BottomRight, LeftCenter, RightCenter, TopCenter, BottomCenter, ScreenCenter, CustomPosition
    }

    public class SimpleBox
    {
        private Vector2 Pivot = new Vector2();
        private readonly Engine engine;
        private Vector2 Position = new Vector2();
        private RectangleF DisplaySize = new RectangleF();
        private BoxPlacement BPlacement = BoxPlacement.None;
        private int id = 0;

        public SimpleBox(Engine engine, Vector2 customPos)
        {
            BPlacement = BoxPlacement.CustomPosition;
            this.engine = engine;
            this.Position = customPos;
            UpdatePosition();
            count++;
            id = count;
        }

        static int count = 0;

        public SimpleBox(Engine engine, BoxPlacement bPosition)
        {
            this.engine = engine;
            BPlacement = bPosition;
            UpdatePosition();
            count++;
            id = count;
        }

        private ImGuiWindowFlags boxFlags = ImGuiWindowFlags.NoResize
                                          | ImGuiWindowFlags.NoMove
                                          | ImGuiWindowFlags.NoTitleBar
                                          | ImGuiWindowFlags.AlwaysAutoResize
                                          | ImGuiWindowFlags.NoInputs
                                          | ImGuiWindowFlags.NoNav
                                          | ImGuiWindowFlags.NoFocusOnAppearing
                                          | ImGuiWindowFlags.NoDecoration;

        public float Alpha { get; set; } = 0.4f;

        /// <summary>Screen-space rect of the box as drawn last frame (empty until first drawn).
        /// Lets other overlays dodge the box instead of hardcoding its size.</summary>
        public RectangleF LastDrawnRect { get; private set; }

        private float edgeMargin;

        /// <summary>Insets edge-anchored placements from the view border by this many pixels
        /// (centered axes are unaffected; None/CustomPosition ignore it entirely).</summary>
        public float EdgeMargin
        {
            get => edgeMargin;
            set
            {
                edgeMargin = value;
                UpdatePlacement();
            }
        }

        public BoxPlacement Placement
        {
            get { return this.BPlacement; }
            set
            {
                this.BPlacement = value;
                UpdatePlacement();
            }
        }

        private void UpdatePlacement()
        {
            switch (BPlacement)
            {
                case BoxPlacement.TopLeft:
                    this.Position.X = 0;
                    this.Position.Y = 0;
                    this.Pivot.X = 0;
                    this.Pivot.Y = 0;
                    break;
                case BoxPlacement.BottomLeft:
                    this.Position.X = 0;
                    this.Position.Y = this.DisplaySize.Height;
                    this.Pivot.X = 0;
                    this.Pivot.Y = 1;
                    break;
                case BoxPlacement.TopRight:
                    this.Position.X = this.DisplaySize.Width;
                    this.Position.Y = 0;
                    this.Pivot.X = 1;
                    this.Pivot.Y = 0;
                    break;
                case BoxPlacement.BottomRight:
                    this.Position.X = this.DisplaySize.Width;
                    this.Position.Y = this.DisplaySize.Height;
                    this.Pivot.X = 1;
                    this.Pivot.Y = 1;
                    break;
                case BoxPlacement.LeftCenter:
                    this.Position.X = 0;
                    this.Position.Y = this.DisplaySize.Height / 2;
                    this.Pivot.X = 0;
                    this.Pivot.Y = 0.5f;
                    break;
                case BoxPlacement.RightCenter:
                    this.Position.X = this.DisplaySize.Width;
                    this.Position.Y = this.DisplaySize.Height / 2;
                    this.Pivot.X = 1;
                    this.Pivot.Y = 0.5f;
                    break;
                case BoxPlacement.TopCenter:
                    this.Position.X = this.DisplaySize.Width / 2;
                    this.Position.Y = 0;
                    this.Pivot.X = 0.5f;
                    this.Pivot.Y = 0;
                    break;
                case BoxPlacement.BottomCenter:
                    this.Position.X = this.DisplaySize.Width / 2;
                    this.Position.Y = this.DisplaySize.Height;
                    this.Pivot.X = 0.5f;
                    this.Pivot.Y = 1;
                    break;
                case BoxPlacement.ScreenCenter:
                    this.Position.X = this.DisplaySize.Width / 2;
                    this.Position.Y = this.DisplaySize.Height / 2;
                    this.Pivot.X = 0.5f;
                    this.Pivot.Y = 0.5f;
                    break;

                default:
                    this.Pivot.X = 0;
                    this.Pivot.Y = 0;
                    break;
            }

            // pivot 0 = anchored to the low edge (inset inward, +), 1 = high edge (-), 0.5 = centered (0)
            if (BPlacement != BoxPlacement.None && BPlacement != BoxPlacement.CustomPosition)
                this.Position += edgeMargin * new Vector2(1 - 2 * Pivot.X, 1 - 2 * Pivot.Y);

            this.Position += new Vector2(DisplaySize.X, DisplaySize.Y);
        }

        public void UpdatePosition()
        {
            var io = ImGui.GetIO();
            if (this.DisplaySize == engine.gameView.ViewRect)
                return;
            this.DisplaySize = engine.gameView.ViewRect;
            UpdatePlacement();
        }

        public void Draw(string message)
        {
            UpdatePosition();
            ImGui.SetNextWindowPos(this.Position, ImGuiCond.Always, this.Pivot);
            ImGui.SetNextWindowBgAlpha(this.Alpha);
            bool isopen = true;
            ImGui.Begin($"###Box{id}", ref isopen, boxFlags);
            ImGui.Text(message);
            var winPos = ImGui.GetWindowPos();
            var winSize = ImGui.GetWindowSize();
            LastDrawnRect = new RectangleF(winPos.X, winPos.Y, winSize.X, winSize.Y);
            ImGui.End();
        }

        public void Draw(ReadOnlySpan<byte> message)
        {
            UpdatePosition();
            ImGui.SetNextWindowPos(this.Position, ImGuiCond.Always, this.Pivot);
            ImGui.SetNextWindowBgAlpha(this.Alpha);
            bool isopen = true;
            Span<byte> boxId = stackalloc byte[6 + 20 + 1];
            var cpy = boxId;
            cpy.MoveWrite("###box"u8);
            cpy.MoveWriteAsDecimal(id);
            cpy.MoveWrite((byte)0);
            ImGui.Begin(boxId, ref isopen, boxFlags);
            ImGui.Text(message);
            var winPos = ImGui.GetWindowPos();
            var winSize = ImGui.GetWindowSize();
            LastDrawnRect = new RectangleF(winPos.X, winPos.Y, winSize.X, winSize.Y);
            ImGui.End();
        }
    }
}
