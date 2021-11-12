using TheAvaloniaOpenGL.Resources;
using TheEngine.Data;
using TheEngine.Entities;
using TheEngine.fonts;
using TheEngine.Handles;
using TheEngine.Interfaces;
using TheMaths;

namespace TheEngine.Managers
{
    public class FontManager : IFontManager, System.IDisposable
    {
        private readonly Engine engine;
        private readonly Dictionary<string, FontDefinition> fontDefinitions = new();
        private readonly Dictionary<string, TextureHandle> textures = new();
        private readonly ShaderHandle textShader;
        private readonly Material material;
        private readonly IMesh quad;
        private readonly NativeBuffer<Vector4> glyphUVsBuffer;
        private readonly NativeBuffer<Vector4> glyphPositionsBuffer;
        private readonly List<Vector4> glyphUVs = new();
        private readonly List<Vector4> glyphPositions = new();

        public FontManager(Engine engine)
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
            LoadFonts();
        }

        private void LoadFonts()
        {
            string[] fonts = Directory.GetFiles("fonts", "*.fnt");
            foreach (var font in fonts)
            {
                var fontName = Path.GetFileNameWithoutExtension(font);
                var texture = Path.ChangeExtension(font, "png");
                textures[fontName] = engine.TextureManager.LoadTexture(texture);
                engine.TextureManager.SetFiltering(textures[fontName], FilteringMode.Linear);
                fontDefinitions[fontName] = new FontDefinition(font);
            }
        }
        
        public void Dispose()
        {
            foreach (var tex in textures.Values)
                engine.TextureManager.DisposeTexture(tex);
            textures.Clear();
            fontDefinitions.Clear();
            glyphUVsBuffer.Dispose();
            glyphPositionsBuffer.Dispose();
        }

        public Vector2 MeasureText(string font, ReadOnlySpan<char> text, float fontSize)
        {
            var fontDef = fontDefinitions[font];
            fontSize = fontSize / fontDef.BaseSize;
            
            var lineHeight = fontDef.LineHeight * fontSize;
            
            float xPixel = 0;
            float maxX = 0;
            float yPixel = lineHeight;

            foreach (var chr in text)
            {
                if (chr == '\n')
                {
                    yPixel += lineHeight;
                    xPixel = 0;
                    continue;
                }
                ref var charDef = ref fontDef.GetChar(chr);
                maxX = Math.Max(maxX, xPixel + charDef.xOff * fontSize + charDef.w * fontSize);
                xPixel += charDef.xAdv * fontSize;
            }

            return new Vector2(maxX, yPixel);
        }

        public void DrawBox(float x, float y, float w, float h, Vector4 color)
        {
            material.SetUniform("fillColor", color);
            material.SetUniformInt("mode", 1);
            material.SetTexture("font", textures.Values.First());
            material.SetUniform("glyphPosition", new Vector4(x, y + h, w, h));
            engine.RenderManager.Render(quad, material, 0, new Transform());
        }
        
        public void DrawText(string font, ReadOnlySpan<char> text, float fontSize, float x, float y, float? maxWidth)
        {
            var fontDef = fontDefinitions[font];
            material.SetUniform("fillColor", new Vector4(1, 1, 1, 1));
            material.SetUniformInt("mode", 0);
            material.SetTexture("font", textures[font]);

            fontSize = fontSize / fontDef.BaseSize;

            
            glyphUVs.Clear();
            glyphPositions.Clear();
            
            float xPixel = x;
            float yPixel = y;//fontDef.BaseHeight;
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

                //+ charDef.yOff
                Vector4 glyphPosition = new Vector4(xPixel + charDef.xOff * fontSize, yPixel  + charDef.h * fontSize + charDef.yOff * fontSize, charDef.w * fontSize, charDef.h * fontSize);
                material.SetUniform("glpyhUV", glyphUv);
                material.SetUniform("glyphPosition", glyphPosition);
                engine.RenderManager.Render(quad, material, 0, new Transform());

                xPixel += charDef.xAdv * fontSize;
            }
        }
    }
}