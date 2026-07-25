using System.Runtime.InteropServices;
using Hexa.NET.ImGui;
using TheEngine.Resources;
using TheEngine.Components;
using TheEngine.Data;
using TheEngine.ECS;
using TheEngine.Entities;
using TheEngine.Handles;
using TheEngine.Interfaces;
using TheMaths;
using Veldrid;

namespace TheEngine.Managers
{
    public partial class UIManager : IUIManager, System.IDisposable
    {
        private readonly Engine engine;
        private readonly ICameraManager cameraManager;
        private readonly IEntityManager entityManager;
        private readonly Material<SdfMaterialData_t> material;
        private readonly Material<SdfMaterialData_t> worldMaterial;
        private readonly IMesh quad;
        private Vector4[] glyphUVs = new Vector4[1];
        private Vector4[] glyphPositions = new Vector4[1];
        private ImGuiController imGuiController;

        // World text is accumulated for the whole frame and flushed as one instanced draw per font:
        // every glyph of every label is one instance, indexed by gl_InstanceIndex into shared
        // per-glyph buffers. This keeps set=1 identical across all labels (bound once) instead of
        // allocating/updating a fresh descriptor set per label. See FlushWorldText.
        private readonly Dictionary<string, WorldTextBatch> worldTextBatches = new();
        private readonly Stack<WorldTextBatch> worldTextBatchPool = new();

        private sealed class WorldTextBatch
        {
            public ITexture FontTexture = null!;
            public Matrix[] Models = new Matrix[256];
            public Vector4[] Positions = new Vector4[256];
            public Vector4[] Uvs = new Vector4[256];
            public Int4[] DrawData = new Int4[256];
            public int Count;

            public void Append(in Matrix model, in Vector4 position, in Vector4 uv, int packedColor)
            {
                if (Count == Models.Length)
                {
                    int cap = Models.Length * 2;
                    Array.Resize(ref Models, cap);
                    Array.Resize(ref Positions, cap);
                    Array.Resize(ref Uvs, cap);
                    Array.Resize(ref DrawData, cap);
                }
                Models[Count] = model;
                Positions[Count] = position;
                Uvs[Count] = uv;
                DrawData[Count] = new Int4(packedColor, 0, 0, 0);
                Count++;
            }
        }

        private static int PackColor(Vector4 c)
        {
            static int B(float v) => (int)(Math.Clamp(v, 0f, 1f) * 255f + 0.5f);
            return B(c.X) | (B(c.Y) << 8) | (B(c.Z) << 16) | (B(c.W) << 24);
        }

        private WorldTextBatch GetWorldTextBatch(string font)
        {
            if (!worldTextBatches.TryGetValue(font, out var batch))
            {
                batch = worldTextBatchPool.Count > 0 ? worldTextBatchPool.Pop() : new WorldTextBatch();
                batch.Count = 0;
                batch.FontTexture = engine.fontManager.GetTexture(font);
                worldTextBatches[font] = batch;
            }
            return batch;
        }

        private float Scaling => engine.WindowHost.DpiScaling;

        private Resources.Pipeline worldPipeline;
        private Resources.Pipeline uiPipeline;

        public class DrawTextData : IManagedComponentData
        {
            public string font;
            public Vector2 pivot;
            public string text;
            public float fontSize;
            public float visibilityDistanceSquare;
            public Vector4 fontColor = Vector4.One;
            public Vector4? backgroundColor = null;
        }
        
        private Archetype persistentTextArchetype;

        [StructLayout(LayoutKind.Sequential, Pack = 4)]
        private struct SdfMaterialData_t
        {
            public Vector4 fillColor;
            public int mode;
            public BindlessTextureId fontIndex; // bindless index of the font atlas (or empty texture for boxes)
            public int padding2;
            public int padding3;
        }

        public UIManager(Engine engine)
        {
            this.engine = engine;
            entityManager = engine.EntityManager;
            cameraManager = engine.CameraManager;
            persistentTextArchetype = entityManager.NewArchetype()
                .WithManagedComponentData<DrawTextData>()
                .WithComponentData<DisabledObjectBit>()
                .WithComponentData<RenderEnabledBit>()
                .WithComponentData<LocalToWorld>();

            var worldTextShader = engine.ShaderManager.LoadShader("internalShaders/world_text.json");

            worldPipeline = this.engine.pipelineManager.CreatePipeline(worldTextShader, PrimitiveTopology.TriangleList, new GraphicsPipelineDescription()
            {
                BlendState = new BlendStateDescription()
                {
                    AttachmentStates = new []
                    {
                        new BlendAttachmentDescription(true, BlendFactor.SourceAlpha, BlendFactor.InverseSourceAlpha, BlendFunction.Add, BlendFactor.SourceAlpha, BlendFactor.InverseSourceAlpha, BlendFunction.Add)
                    }
                },
                DepthStencilState = DepthStencilStateDescription.Disabled,
                RasterizerState = RasterizerStateDescription.CullNone
            }, false);

            var textShader = engine.ShaderManager.LoadShader("internalShaders/sdf.json");

            uiPipeline = this.engine.pipelineManager.CreatePipeline(textShader, PrimitiveTopology.TriangleList, new GraphicsPipelineDescription()
            {
                BlendState = new BlendStateDescription()
                {
                    AttachmentStates = new []
                    {
                        new BlendAttachmentDescription(true, BlendFactor.One, BlendFactor.InverseSourceAlpha, BlendFunction.Add, BlendFactor.One, BlendFactor.InverseSourceAlpha, BlendFunction.Add)
                    }
                },
                DepthStencilState = DepthStencilStateDescription.Disabled,
                RasterizerState = RasterizerStateDescription.CullNone
            }, false);

            worldMaterial = engine.MaterialManager.CreateMaterial<SdfMaterialData_t>(worldPipeline);
            // worldMaterial.BlendingEnabled = true;
            // worldMaterial.SourceBlending = Blending.SrcAlpha;
            // worldMaterial.DestinationBlending = Blending.OneMinusSrcAlpha;
            // worldMaterial.Culling = CullingMode.Off;
            // worldMaterial.ZWrite = false;
            // worldMaterial.DepthTesting = DepthCompare.Always;

            material = engine.MaterialManager.CreateMaterial<SdfMaterialData_t>(uiPipeline);
            // material.BlendingEnabled = true;
            // material.SourceBlending = Blending.One;
            // material.DestinationBlending = Blending.OneMinusSrcAlpha;
            // material.Culling = CullingMode.Off;
            // material.ZWrite = false;
            // material.DepthTesting = DepthCompare.Always;
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

            imGuiController = new ImGuiController(engine);
        }

        public void Dispose()
        {
            engine.MeshManager.DisposeMesh(quad);
            imGuiController.Dispose();
        }

        private (INativeBuffer positions, INativeBuffer uvs) UploadGlyphs(int glyphsCount)
        {
            var commandList = engine.renderManager.CommandList;
            var positions = commandList.UploadTransientBuffer((ReadOnlySpan<Vector4>)glyphPositions.AsSpan(0, glyphsCount));
            var uvs = commandList.UploadTransientBuffer((ReadOnlySpan<Vector4>)glyphUVs.AsSpan(0, glyphsCount));
            return (positions, uvs);
        }

        // re-queues an already-uploaded glyph slice for the next material bind - lets one upload
        // back two draws (RenderInstancedIndirect + Render in DrawText) without re-streaming it.
        private void BindGlyphs(in (INativeBuffer positions, INativeBuffer uvs) glyphs)
        {
            var commandList = engine.renderManager.CommandList;
            commandList.SetBuffer(ShaderUniforms.GlyphPositions, glyphs.positions);
            commandList.SetBuffer(ShaderUniforms.GlyphUVs, glyphs.uvs);
        }

        private void SetupDocking()
        {
            var viewport = ImGui.GetMainViewport();
            ImGui.SetNextWindowPos(viewport.Pos);
            ImGui.SetNextWindowSize(viewport.Size / ImGui.GetIO().DisplayFramebufferScale);
            ImGui.SetNextWindowViewport(viewport.ID);

            ImGui.PushStyleVar(ImGuiStyleVar.WindowPadding, Vector2.Zero);
            ImGui.PushStyleVar(ImGuiStyleVar.WindowBorderSize, 0);
            ImGui.Begin("Root", ImGuiWindowFlags.NoBringToFrontOnFocus | ImGuiWindowFlags.NoTitleBar |
                                ImGuiWindowFlags.NoCollapse | ImGuiWindowFlags.NoResize |
                                ImGuiWindowFlags.NoMove | ImGuiWindowFlags.NoNavFocus |
                                ImGuiWindowFlags.NoBackground | ImGuiWindowFlags.MenuBar);
            ImGui.PopStyleVar(2);

            ImGui.DockSpace(ImGui.GetID("3D Dockspace"), Vector2.Zero, ImGuiDockNodeFlags.PassthruCentralNode);

            if (ImGui.BeginMenuBar())
            {
                OnMenuBarDraw?.Invoke();
                ImGui.EndMenuBar();
            }
            ImGui.End();
        }

        internal void UpdateGui(float delta)
        {
            imGuiController.UpdateImGui(delta);

            SetupDocking();
        }

        private partial struct Render3DTextJob : IJob
        {
            private IChunkDataIterator itr;
            private ComponentDataAccess<LocalToWorld> matrices;
            private ComponentDataAccess<DisabledObjectBit> disabledAccess;
            private ComponentDataAccess<RenderEnabledBit> renderEnabledBit;
            private ManagedComponentDataAccess<DrawTextData> datas;

            public UIManager owner;
            public Vector3 cameraPos;

            public void Execute(int start, int end)
            {
                for (int i = start; i < end; ++i)
                {
                    if (disabledAccess[i])
                        continue;
                    if (renderEnabledBit[i].Layer > 0 &&
                        !owner.engine.renderManager.IsRenderLayerEnabled(renderEnabledBit[i].Layer))
                        continue;
                    var data = datas[i];
                    var matrix = matrices[i];
                    if ((matrix.Position - cameraPos).LengthSquared() < data.visibilityDistanceSquare)
                        owner.DrawWorldText(data.font, data.pivot, data.text, data.fontSize, matrix, data.fontColor,
                            data.backgroundColor);
                }
            }
        }

        internal void Render3D()
        {
            var cameraPos = cameraManager.MainCamera.Transform.Position;
            new Render3DTextJob { owner = this, cameraPos = cameraPos }.Run(persistentTextArchetype);
            FlushWorldText();
        }

        // Uploads each font's accumulated glyphs into shared per-glyph buffers and issues one
        // instanced draw per font. set=1 is identical for every glyph in the batch, so it's bound
        // once per font instead of once per label.
        private void FlushWorldText()
        {
            if (worldTextBatches.Count == 0)
                return;

            var commandList = engine.renderManager.CommandList;
            foreach (var (_, batch) in worldTextBatches)
            {
                int n = batch.Count;
                if (n == 0)
                {
                    worldTextBatchPool.Push(batch);
                    continue;
                }

                var modelsBuffer = commandList.UploadTransientBuffer((ReadOnlySpan<Matrix>)batch.Models.AsSpan(0, n));
                var positionsBuffer = commandList.UploadTransientBuffer((ReadOnlySpan<Vector4>)batch.Positions.AsSpan(0, n));
                var uvsBuffer = commandList.UploadTransientBuffer((ReadOnlySpan<Vector4>)batch.Uvs.AsSpan(0, n));
                var drawDataBuffer = commandList.UploadTransientBuffer((ReadOnlySpan<Int4>)batch.DrawData.AsSpan(0, n));

                // world_text.vert reads its transform through the instancing SSBOs (model = per-glyph
                // label matrix). inverseModel is unused by the shader, so it reuses the models buffer.
                commandList.SetBuffer(ShaderUniforms.InstancingModels, modelsBuffer);
                commandList.SetBuffer(ShaderUniforms.InstancingInverseModels, modelsBuffer);
                commandList.SetBuffer(ShaderUniforms.InstancingDrawData, drawDataBuffer);
                commandList.SetBuffer(ShaderUniforms.GlyphPositions, positionsBuffer);
                commandList.SetBuffer(ShaderUniforms.GlyphUVs, uvsBuffer);

                SdfMaterialData_t worldData = new() { fontIndex = engine.TextureManager.GetBindlessIndex(batch.FontTexture) };
                worldMaterial.SetMaterialData(ref worldData);
                engine.RenderManager.RenderInstancedIndirect(quad, worldMaterial, ShaderPassType.Forward, 0, n);

                worldTextBatchPool.Push(batch);
            }
            worldTextBatches.Clear();
        }

        internal void Render()
        {
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
            SdfMaterialData_t data = new() { fillColor = color, mode = 1, fontIndex = engine.TextureManager.GetBindlessIndex(engine.TextureManager.EmptyTexture) };
            material.SetMaterialData(ref data);
            
            glyphPositions[0] = new Vector4(x, y + h, w, h);
            glyphUVs[0] = new Vector4(0);
            BindGlyphs(UploadGlyphs(1));
            engine.RenderManager.RenderInstancedIndirect(quad, material, ShaderPassType.Forward, 0, 1);
        }

        public Entity DrawPersistentWorldText(string font, Vector2 pivot, string text, float fontSize, Matrix localToWorld, float visibilityDistance, Vector4? fontColor = null,
            Vector4? backgroundColor = null)
        {
            var entity = entityManager.CreateEntity(persistentTextArchetype, text);

            entityManager.SetManagedComponent(entity, new DrawTextData()
            {
                font = font,
                fontSize = fontSize,
                pivot = pivot,
                text = text,
                visibilityDistanceSquare = visibilityDistance * visibilityDistance,
                fontColor = fontColor ?? Vector4.One,
                backgroundColor = backgroundColor
            });
            entityManager.GetComponent<LocalToWorld>(entity).Matrix = localToWorld;
            
            return entity;
        }

        public void DrawWorldText(string font, Vector2 pivot, ReadOnlySpan<char> text, float fontSize, Matrix localToWorld, Vector4 foreColor, Vector4? backgroundColor)
        {
            var fontDef = engine.fontManager.GetFont(font);
            var measurement = MeasureText(font, text, fontSize);
            var batch = GetWorldTextBatch(font);

            // background quad first (drawn behind the text - same draw, earlier instance), using the
            // hardcoded white-pixel UV. mode is always alpha (0) for world text.
            if (backgroundColor.HasValue)
            {
                Vector4 boxUv = new Vector4(50 / 512.0f, 20 / 512.0f, 1 / 512.0f, 1 / 512.0f); /* todo: find a better way to find white pixel UVs (instead of hardcoding) */
                Vector4 boxPos = new Vector4(-measurement.X * pivot.X, -measurement.Y * pivot.Y, measurement.X, -measurement.Y);
                batch.Append(in localToWorld, in boxPos, in boxUv, PackColor(backgroundColor.Value));
            }

            fontSize = fontSize / fontDef.BaseSize;
            int packedColor = PackColor(foreColor);

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

                Vector4 glyphPosition = new Vector4(xPixel + charDef.xOff * fontSize, yPixel + charDef.h * fontSize + charDef.yOff * fontSize, charDef.w * fontSize, charDef.h * fontSize);

                batch.Append(in localToWorld, in glyphPosition, in glyphUv, packedColor);

                xPixel += charDef.xAdv * fontSize;
            }
        }

        public void DrawText(string font, ReadOnlySpan<char> text, float fontSize, float x, float y, float? maxWidth, Vector4 color)
        {
            var fontDef = engine.fontManager.GetFont(font);
            SdfMaterialData_t data = new() { fillColor = color, mode = 0, fontIndex = engine.TextureManager.GetBindlessIndex(engine.fontManager.GetTexture(font)) };
            material.SetMaterialData(ref data);

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
            
            var glyphs = UploadGlyphs(glyphsCount);
            BindGlyphs(glyphs);
            engine.RenderManager.RenderInstancedIndirect(quad, material, ShaderPassType.Forward, 0, glyphsCount);
            BindGlyphs(glyphs);
            engine.RenderManager.Render(quad, material, ShaderPassType.Forward, 0, Matrix.Identity);
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

        public event Action? OnMenuBarDraw;

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