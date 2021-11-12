using TheAvaloniaOpenGL.Resources;
using TheEngine.fonts;
using TheEngine.Handles;
using TheEngine.Interfaces;
using TheMaths;

namespace TheEngine.Managers
{
    internal class FontManager : IFontManager, System.IDisposable
    {
        private readonly Engine engine;
        private readonly Dictionary<string, FontDefinition> fontDefinitions = new();
        private readonly Dictionary<string, TextureHandle> textures = new();

        public FontManager(Engine engine)
        {
            this.engine = engine;
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

        internal FontDefinition GetFont(string font) => fontDefinitions[font];
        
        public void Dispose()
        {
            foreach (var tex in textures.Values)
                engine.TextureManager.DisposeTexture(tex);
            textures.Clear();
            fontDefinitions.Clear();
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

        internal TextureHandle GetTexture(string font)
        {
            return textures[font];
        }
    }
}