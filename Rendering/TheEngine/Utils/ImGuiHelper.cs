using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ImGuiNET;

namespace TheEngine.Utils.ImGuiHelper
{
    public enum BoxPlacement
    {
        None, TopLeft, BottomLeft, TopRight, BottomRight, LeftCenter, RightCenter, TopCenter, BottomCenter, ScreenCenter, CustomPosition
    }

    public class SimpleBox
    {
        private Vector2 Pivot = new Vector2();
        private Vector2 Position = new Vector2();
        private Vector2 DisplaySize = new Vector2();
        private BoxPlacement BPlacement = BoxPlacement.None;
        private int id = 0;

        public SimpleBox(Vector2 customPos)
        {
            BPlacement = BoxPlacement.CustomPosition;
            this.Position = customPos;
            UpdatePosition();
            count++;
            id = count;
        }

        static int count = 0;

        public SimpleBox(BoxPlacement bPosition)
        {
            BPlacement = bPosition;
            UpdatePosition();
            count++;
            id = count;
        }

        private ImGuiWindowFlags boxFlags = ImGuiWindowFlags.NoResize
                                          | ImGuiWindowFlags.NoMove
                                          | ImGuiWindowFlags.NoTitleBar
                                          | ImGuiWindowFlags.AlwaysAutoResize
                                          | ImGuiWindowFlags.NoNav
                                          | ImGuiWindowFlags.NoFocusOnAppearing
                                          | ImGuiWindowFlags.NoDecoration;

        public float Alpha { get; set; } = 0.4f;

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
                    this.Position.Y = this.DisplaySize.Y;
                    this.Pivot.X = 0;
                    this.Pivot.Y = 1;
                    break;
                case BoxPlacement.TopRight:
                    this.Position.X = this.DisplaySize.X;
                    this.Position.Y = 0;
                    this.Pivot.X = 1;
                    this.Pivot.Y = 0;
                    break;
                case BoxPlacement.BottomRight:
                    this.Position.X = this.DisplaySize.X;
                    this.Position.Y = this.DisplaySize.Y;
                    this.Pivot.X = 1;
                    this.Pivot.Y = 1;
                    break;
                case BoxPlacement.LeftCenter:
                    this.Position.X = 0;
                    this.Position.Y = this.DisplaySize.Y / 2;
                    this.Pivot.X = 0;
                    this.Pivot.Y = 0.5f;
                    break;
                case BoxPlacement.RightCenter:
                    this.Position.X = this.DisplaySize.X;
                    this.Position.Y = this.DisplaySize.Y / 2;
                    this.Pivot.X = 1;
                    this.Pivot.Y = 0.5f;
                    break;
                case BoxPlacement.TopCenter:
                    this.Position.X = this.DisplaySize.X / 2;
                    this.Position.Y = 0;
                    this.Pivot.X = 0.5f;
                    this.Pivot.Y = 0;
                    break;
                case BoxPlacement.BottomCenter:
                    this.Position.X = this.DisplaySize.X / 2;
                    this.Position.Y = this.DisplaySize.Y;
                    this.Pivot.X = 0.5f;
                    this.Pivot.Y = 1;
                    break;
                case BoxPlacement.ScreenCenter:
                    this.Position.X = this.DisplaySize.X / 2;
                    this.Position.Y = this.DisplaySize.Y / 2;
                    this.Pivot.X = 0.5f;
                    this.Pivot.Y = 0.5f;
                    break;

                default:
                    this.Pivot.X = 0;
                    this.Pivot.Y = 0;
                    break;
            }
        }

        public void UpdatePosition()
        {
            var io = ImGui.GetIO();
            if (this.DisplaySize == io.DisplaySize)
                return;
            this.DisplaySize = io.DisplaySize / io.DisplayFramebufferScale;
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
            ImGui.End();
        }
    }
}
