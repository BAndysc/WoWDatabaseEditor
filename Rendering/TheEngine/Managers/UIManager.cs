using System;
using System.Collections.Generic;
using System.Linq;
using ImGuiNET;
using TheAvaloniaOpenGL.Resources;
using TheEngine.Components;
using TheEngine.Data;
using TheEngine.ECS;
using TheEngine.Entities;
using TheEngine.Handles;
using TheEngine.Interfaces;
using TheEngine.Utils;
using TheMaths;

namespace TheEngine.Managers
{
    public class UIManager : IUIManager, System.IDisposable
    {
        private readonly Engine engine;
        private readonly ICameraManager cameraManager;
        private readonly IEntityManager entityManager;
        private readonly ShaderHandle textShader;
        private readonly Material material;
        private readonly Material worldMaterial;
        private readonly IMesh quad;
        private readonly NativeBuffer<Vector4> glyphUVsBuffer;
        private readonly NativeBuffer<Vector4> glyphPositionsBuffer;
        private Vector4[] glyphUVs = new Vector4[1];
        private Vector4[] glyphPositions = new Vector4[1];
        private ImGuiController imGuiController;

        private float Scaling => engine.WindowHost.DpiScaling;

        public class DrawTextData : IManagedComponentData
        {
            public string font;
            public Vector2 pivot;
            public string text;
            public float fontSize;
            public float visibilityDistanceSquare;
        }
        
        private Archetype persistentTextArchetype;
        
        public UIManager(Engine engine)
        {
            this.engine = engine;
            entityManager = engine.EntityManager;
            cameraManager = engine.CameraManager;
            persistentTextArchetype = entityManager.NewArchetype()
                .WithManagedComponentData<DrawTextData>()
                .WithComponentData<DisabledObjectBit>()
                .WithComponentData<LocalToWorld>();
            textShader = engine.ShaderManager.LoadShader("internalShaders/sdf.json", false);
            worldMaterial = engine.MaterialManager.CreateMaterial("internalShaders/world_text.json");
            worldMaterial.BlendingEnabled = true;
            worldMaterial.SourceBlending = Blending.SrcAlpha;
            worldMaterial.DestinationBlending = Blending.OneMinusSrcAlpha;
            worldMaterial.Culling = CullingMode.Off;
            worldMaterial.ZWrite = false;
            worldMaterial.DepthTesting = DepthCompare.Always;

            material = engine.MaterialManager.CreateMaterial(textShader, null);
            material.BlendingEnabled = true;
            material.SourceBlending = Blending.One;
            material.DestinationBlending = Blending.OneMinusSrcAlpha;
            material.Culling = CullingMode.Off;
            material.ZWrite = false;
            material.DepthTesting = DepthCompare.Always;
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
            }, new ushort[] { 0, 1,2, 2, 3, 0 }));
            glyphPositionsBuffer = engine.Device.CreateBuffer<Vector4>(BufferTypeEnum.StructuredBufferVertexOnly, 1, BufferInternalFormat.Float4);
            glyphUVsBuffer = engine.Device.CreateBuffer<Vector4>(BufferTypeEnum.StructuredBufferVertexOnly, 1, BufferInternalFormat.Float4);
            material.SetBuffer("glpyhUVs", glyphUVsBuffer);
            material.SetBuffer("glyphPositions", glyphPositionsBuffer);
            
            worldMaterial.SetBuffer("glpyhUVs", glyphUVsBuffer);
            worldMaterial.SetBuffer("glyphPositions", glyphPositionsBuffer);

            imGuiController = new ImGuiController(engine);
            ei = new EntityInspector(engine);
        }

        private EntityInspector ei;

        public void Dispose()
        {
            engine.MeshManager.DisposeMesh(quad);
            glyphUVsBuffer.Dispose();
            glyphPositionsBuffer.Dispose();
            imGuiController.Dispose();
        }

        internal void UpdateGui(float delta)
        {
            imGuiController.UpdateImGui(delta);
        }

        internal void Render()
        {
#if DEBUG
            ei.Draw();
#endif
            var cameraPos = cameraManager.MainCamera.Transform.Position;
            persistentTextArchetype.ForEach<LocalToWorld, DisabledObjectBit, DrawTextData>((itr, start, end, matrices, disabledAccess, datas) =>
            {
                for (int i = start; i < end; ++i)
                {
                    if (disabledAccess[i])
                        continue;
                    var data = datas[i];
                    var matrix = matrices[i];
                    if ((matrix.Position - cameraPos).LengthSquared() < data.visibilityDistanceSquare)
                        DrawWorldText(data.font, data.pivot, data.text, data.fontSize, matrix);
                }
            });
            
            // using var ui = BeginImmediateDrawRel(0, 1, 0, 1);
            // ui.BeginVerticalBox(new Vector4(0, 0, 0, 0.5f), 2);
            // var em = engine.entityManager;
            // foreach (var a in em.DataManager.Archetypes)
            // {
            //     if (a.Length == 0)
            //         continue;
            //     
            //     ui.Text("calibri", a.Archetype.ToString() +": " + a.Length, 10, Vector4.One);
            // }
            
            imGuiController.Render();
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

        public Entity DrawPersistentWorldText(string font, Vector2 pivot, string text, float fontSize, Matrix localToWorld, float visibilityDistance)
        {
            var entity = entityManager.CreateEntity(persistentTextArchetype);

            entityManager.SetManagedComponent(entity, new DrawTextData()
            {
                font = font,
                fontSize = fontSize,
                pivot = pivot,
                text = text,
                visibilityDistanceSquare = visibilityDistance * visibilityDistance
            });
            entityManager.GetComponent<LocalToWorld>(entity).Matrix = localToWorld;
            
            return entity;
        }

        public void DrawWorldText(string font, Vector2 pivot, ReadOnlySpan<char> text, float fontSize, Matrix localToWorld)
        {
            var fontDef = engine.fontManager.GetFont(font);
            worldMaterial.SetUniform("fillColor", Vector4.One);
            worldMaterial.SetUniformInt("mode", 0);
            worldMaterial.SetTexture("font", engine.fontManager.GetTexture(font));

            var measurement = MeasureText(font, text, fontSize);
            
            fontSize = fontSize / fontDef.BaseSize;

            int glyphsCount = 0;
            if (glyphPositions.Length < text.Length)
            {
                glyphPositions = new Vector4[text.Length];
                glyphUVs = new Vector4[text.Length];
            }
            
            float xPixel = -measurement.X * pivot.X;
            float yPixel = -measurement.Y * pivot.Y;
            foreach (var chr in text)
            {
                if (chr == '\n')
                {
                    yPixel += fontDef.LineHeight * fontSize;
                    xPixel = -measurement.X * pivot.Y;
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
            engine.RenderManager.RenderInstancedIndirect(quad, worldMaterial, 0, glyphsCount, localToWorld);
        }

        public void DrawText(string font, ReadOnlySpan<char> text, float fontSize, float x, float y, float? maxWidth, Vector4 color)
        {
            var fontDef = engine.fontManager.GetFont(font);
            material.SetUniform("fillColor", color);
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
            engine.RenderManager.Render(quad, material, 0,  Matrix.Identity);
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
                this.size = size * uiManager.Scaling;
                this.color = color;
            }
            
            public Vector2 Measure()
            {
                return uiManager.MeasureText(font, str, size);
            }

            public Vector2 Draw(float x, float y, float w, float h)
            {
                uiManager.DrawText(font, str, size, x, y, null, color);
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
                this.padding = padding * uiManager.Scaling;
                this.child = child;
                this.minWidth = minWidth * uiManager.Scaling;
                this.minHeight = minHeight * uiManager.Scaling;
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
        
        public IImGui BeginImmediateDrawAbs(float x, float y)
        {
            return new TheImGui(this, new Vector2(x, y), false, Vector2.Zero);
        }

        public IImGui BeginImmediateDrawRel(float x, float y, float pivotX, float pivotY)
        {
            return new TheImGui(this, new Vector2(x, y), true, new Vector2(pivotX, pivotY));
        }

        internal class TheImGui : IImGui
        {
            private readonly UIManager uiManager;
            private readonly Vector2 position;
            private readonly bool relative;
            private readonly Vector2 pivot;
            private IDrawingCommand? root;
            private Stack<IChildContainer> parents = new();

            public TheImGui(UIManager uiManager, Vector2 position, bool relative, Vector2 pivot)
            {
                this.uiManager = uiManager;
                this.position = position;
                this.relative = relative;
                this.pivot = pivot;
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
                if (root == null)
                    return;
                var measure = root.Measure();
                float posX = position.X;
                float posY = position.Y;
                if (relative)
                {
                    posY = position.Y * uiManager.engine.WindowHost.WindowHeight;
                    posX = position.X * uiManager.engine.WindowHost.WindowWidth;
                }
                posX -= pivot.X * measure.X;
                posY -= pivot.Y * measure.Y;
                root.Draw(posX, posY, measure.X, measure.Y);
            }
        }

    }
}