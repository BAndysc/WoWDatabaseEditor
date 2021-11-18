using System;
using System.Collections.Generic;
using System.Linq;
using TheAvaloniaOpenGL.Resources;
using TheEngine.Data;
using TheEngine.Entities;
using TheEngine.Handles;
using TheEngine.Interfaces;
using TheMaths;

namespace TheEngine.Managers
{
    public class UIManager : IUIManager, System.IDisposable
    {
        private readonly Engine engine;
        private readonly ShaderHandle textShader;
        private readonly Material material;
        private readonly IMesh quad;
        private readonly NativeBuffer<Vector4> glyphUVsBuffer;
        private readonly NativeBuffer<Vector4> glyphPositionsBuffer;
        private Vector4[] glyphUVs = new Vector4[1];
        private Vector4[] glyphPositions = new Vector4[1];

        public UIManager(Engine engine)
        {
            this.engine = engine;
            textShader = engine.ShaderManager.LoadShader("internalShaders/sdf.json");
            material = engine.MaterialManager.CreateMaterial(textShader);
            material.BlendingEnabled = true;
            material.SourceBlending = Blending.SrcAlpha;
            material.DestinationBlending = Blending.OneMinusSrcAlpha;
            material.Culling = CullingMode.Off;
            quad = engine.MeshManager.CreateMesh(new MeshData(new Vector3[]
            {
                new(0, 0, 0),
                new(1, 0, 0),
                new(1, 1, 0),
                new(0, 1, 0)
            }, null, new Vector2[]
            {
                new(0, 1),
                new(1, 1),
                new(1, 0),
                new(0, 0),
            }, new int[] { 0, 1,2, 2, 3, 0 }));
            glyphPositionsBuffer = engine.Device.CreateBuffer<Vector4>(BufferTypeEnum.StructuredBufferVertexOnly, 1, BufferInternalFormat.Float4);
            glyphUVsBuffer = engine.Device.CreateBuffer<Vector4>(BufferTypeEnum.StructuredBufferVertexOnly, 1, BufferInternalFormat.Float4);
            material.SetBuffer("glpyhUVs", glyphUVsBuffer);
            material.SetBuffer("glyphPositions", glyphPositionsBuffer);
        }

        public void Dispose()
        {
            glyphUVsBuffer.Dispose();
            glyphPositionsBuffer.Dispose();
        }


        public void DrawBox(float x, float y, float w, float h, Vector4 color)
        {
            material.SetUniform("fillColor", color);
            material.SetUniformInt("mode", 1);
            material.SetTexture("font", engine.TextureManager.EmptyTexture);
            
            glyphPositions[0] = new Vector4(x, y + h, w, h);
            glyphUVs[0] = new Vector4(0);
            glyphPositionsBuffer.UpdateBuffer(glyphPositions);
            glyphUVsBuffer.UpdateBuffer(glyphUVs);
            engine.RenderManager.RenderInstancedIndirect(quad, material, 0, 1);
        }
        
        public void DrawText(string font, ReadOnlySpan<char> text, float fontSize, float x, float y, float? maxWidth)
        {
            var fontDef = engine.fontManager.GetFont(font);
            material.SetUniform("fillColor", new Vector4(1, 1, 1, 1));
            material.SetUniformInt("mode", 0);
            material.SetTexture("font", engine.fontManager.GetTexture(font));

            fontSize = fontSize / fontDef.BaseSize;

            int glyphsCount = 0;
            if (glyphPositions.Length < text.Length)
            {
                glyphPositions = new Vector4[text.Length];
                glyphUVs = new Vector4[text.Length];
            }
            
            float xPixel = x;
            float yPixel = y;
            foreach (var chr in text)
            {
                if (chr == '\n')
                {
                    yPixel += fontDef.LineHeight * fontSize;
                    xPixel = (int)x;
                    continue;
                }
                
                ref var charDef = ref fontDef.GetChar(chr);

                Vector4 glyphUv = new Vector4(1.0f * charDef.x / fontDef.Width,
                    1.0f * charDef.y / fontDef.Height,
                    1.0f * charDef.w / fontDef.Width,
                    1.0f * charDef.h / fontDef.Height);

                Vector4 glyphPosition = new Vector4(xPixel + charDef.xOff * fontSize, yPixel  + charDef.h * fontSize + charDef.yOff * fontSize, charDef.w * fontSize, charDef.h * fontSize);
                
                glyphUVs[glyphsCount] = glyphUv;
                glyphPositions[glyphsCount++]  = glyphPosition;
                
                xPixel += charDef.xAdv * fontSize;
            }
            
            glyphPositionsBuffer.UpdateBuffer(glyphPositions);
            glyphUVsBuffer.UpdateBuffer(glyphUVs);
            engine.RenderManager.RenderInstancedIndirect(quad, material, 0, glyphsCount);
            engine.RenderManager.Render(quad, material, 0, new Transform());
        }

        public Vector2 MeasureText(string font, ReadOnlySpan<char> text, float fontSize)
        {
            return engine.FontManager.MeasureText(font, text, fontSize);
        }

        public Border BorderDrawing(Vector4 color, float padding, IDrawingCommand child)
        {
            return new Border(this, color, padding, child);
        }

        public Text TextDrawing(string font, string str, float size, Vector4 color)
        {
            return new Text(this, str, font, size, color);
        }

        public VerticalGroup VerticalLayoutDrawing(params IDrawingCommand[] commands)
        {
            return new VerticalGroup(commands);
        }

        public class Stretch : IDrawingCommand
        {
            private readonly float minWidth;
            private readonly float minHeight;

            public Stretch(float minWidth, float minHeight)
            {
                this.minWidth = minWidth;
                this.minHeight = minHeight;
            }

            public Vector2 Measure()
            {
                return new Vector2(minWidth, minHeight);
            }

            public Vector2 Draw(float x, float y, float width, float height)
            {
                return new Vector2(width, height);
            }
        }

        public class Text : IDrawingCommand
        {
            private readonly UIManager uiManager;
            private readonly string str;
            private readonly string font;
            private readonly float size;
            private readonly Vector4 color;

            public Text(UIManager uiManager, string str, string font, float size, Vector4 color)
            {
                this.uiManager = uiManager;
                this.str = str;
                this.font = font;
                this.size = size;
                this.color = color;
            }
            
            public Vector2 Measure()
            {
                return uiManager.MeasureText(font, str, size);
            }

            public Vector2 Draw(float x, float y, float w, float h)
            {
                uiManager.DrawText(font, str, size, x, y, null);
                return Measure();
            }
        }

        public class HorizontalGroup : IChildContainer
        {
            private List<IDrawingCommand> children;
            public HorizontalGroup(params IDrawingCommand[] children)
            {
                this.children = children.ToList();
            }

            public HorizontalGroup()
            {
                children = new();
            }

            public void Add(IDrawingCommand child)
            {
                children.Add(child);
            }
            
            public Vector2 Measure()
            {
                float totalX = 0;
                float maxY = 0;
                foreach (var child in children)
                {
                    var size = child.Measure();
                    totalX += size.X;
                    maxY = Math.Max(size.Y, maxY);
                    
                }
                return new Vector2(totalX, maxY);
            }

            public Vector2 Draw(float x, float y, float w, float h)
            {
                float maxY = 0;
                var total = Measure();
                var stretchSize = w - total.X;
                foreach (var child in children)
                {
                    var measure = child.Measure();
                    var size = child.Draw(x, y, Math.Max(measure.X, stretchSize), h);
                    if (size.X > measure.X)
                        stretchSize -= (size.X - measure.X);
                    x += size.X;
                    maxY = Math.Max(size.Y, maxY);
                }
                return new Vector2(x, maxY);
            }
        }

        public interface IChildContainer : IDrawingCommand
        {
            void Add(IDrawingCommand child);
        }
        
        public class VerticalGroup : IChildContainer
        {
            private List<IDrawingCommand> children;
            public VerticalGroup(params IDrawingCommand[] children)
            {
                this.children = children.ToList();
            }

            public VerticalGroup()
            {
                children = new();
            }

            public void Add(IDrawingCommand child)
            {
                children.Add(child);
            }
            
            public Vector2 Measure()
            {
                float totalY = 0;
                float maxX = 0;
                foreach (var child in children)
                {
                    var size = child.Measure();
                    totalY += size.Y;
                    maxX = Math.Max(size.X, maxX);
                    
                }
                return new Vector2(maxX, totalY);
            }

            public Vector2 Draw(float x, float y, float w, float h)
            {
                float maxX = 0;
                var total = new Vector2();
                foreach (var child in children)
                {
                    var measure = child.Measure();
                    var size = child.Draw(x, y, w, measure.Y);
                    y += size.Y;
                    maxX = Math.Max(size.X, maxX);
                }
                return new Vector2(maxX, y);
            }
        }
        
        public class Border : IDrawingCommand
        {
            private readonly UIManager uiManager;
            private readonly Vector4 color;
            private readonly float padding;
            private readonly IDrawingCommand? child;
            private readonly float minWidth;
            private readonly float minHeight;

            public Border(UIManager uiManager, Vector4 color, float padding, IDrawingCommand? child, float minWidth = 0, float minHeight = 0)
            {
                this.uiManager = uiManager;
                this.color = color;
                this.padding = padding;
                this.child = child;
                this.minWidth = minWidth;
                this.minHeight = minHeight;
            }

            public Vector2 Measure()
            {
                var desired = new Vector2(padding * 2) + (child?.Measure() ?? Vector2.Zero);
                return new Vector2(Math.Max(desired.X, minWidth), Math.Max(desired.Y, minHeight));
            }

            public Vector2 Draw(float x, float y, float w, float h)
            {
                var size = Measure();
                uiManager.DrawBox(x, y, w, h, color);
                child?.Draw(x + padding, y + padding, w - 2 * padding, h - 2 * padding);
                return new Vector2(w, h);
            }
        }
        
        public IImGui BeginImmediateDraw(float x, float y)
        {
            return new ImGui(this, new Vector2(x, y));
        }

        internal class ImGui : IImGui
        {
            private readonly UIManager uiManager;
            private readonly Vector2 position;
            private IDrawingCommand? root;
            private Stack<IChildContainer> parents = new();

            public ImGui(UIManager uiManager, Vector2 position)
            {
                this.uiManager = uiManager;
                this.position = position;
            }

            public void BeginVerticalBox(Vector4 color, float padding)
            {
                var group = new VerticalGroup();
                var border = new Border(uiManager, color, padding, group);
                if (root == null)
                {
                    root = border;
                }
                else
                {
                    if (parents.TryPeek(out var parent))
                        parent.Add(border);
                }
                parents.Push(group);
            }

            public void BeginHorizontalBox()
            {
                var group = new HorizontalGroup();
                if (root == null)
                {
                    root = group;
                }
                else
                {
                    if (parents.TryPeek(out var parent))
                        parent.Add(group);
                }
                parents.Push(group);
            }

            public void EndBox()
            {
                parents.Pop();
            }

            public void Rectangle(Vector4 color, float padding, float width, float height)
            {
                var border = new Border(uiManager, color, padding, null, width, height);
                if (parents.TryPeek(out var parent))
                    parent.Add(border);
            }

            public void StretchFill(float minWidth, float minHeight)
            {
                var s = new Stretch(minWidth, minHeight);
                if (parents.TryPeek(out var parent))
                    parent.Add(s);
            }

            public void Text(string font, string str, float size, Vector4 color)
            {
                var t = new Text(uiManager, str, font, size, color);
                if (parents.TryPeek(out var parent))
                    parent.Add(t);
            }

            public void Dispose()
            {
                var measure = root.Measure();
                root.Draw(position.X, position.Y, measure.X, measure.Y);
            }
        }

    }
}