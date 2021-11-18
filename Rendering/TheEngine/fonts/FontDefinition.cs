using System.IO;
using System.Text.RegularExpressions;

namespace TheEngine.fonts
{
    public readonly struct CharDefinition
    {
        public readonly int x, y, w, h, xOff, yOff, xAdv, page, channel;
        public readonly bool isUsed;

        public CharDefinition(int x, int y, int w, int h, int xOff, int yOff, int xAdv, int page, int channel) : this()
        {
            this.x = x;
            this.y = y;
            this.w = w;
            this.h = h;
            this.xOff = xOff;
            this.yOff = yOff;
            this.xAdv = xAdv;
            this.page = page;
            this.channel = channel;
            this.isUsed = true;
        }
    }
    
    public class FontDefinition
    {
        private static readonly Regex CharRegex =
            new(@"char id=(\d+) +x=(\d+) +y=(\d+) +width=(\d+) +height=(\d+) +xoffset=(-?\d+) +yoffset=(-?\d+) +xadvance=(-?\d+) +page=(\d+) +chnl=(-?\d+)");

        private static readonly Regex InfoRegex = new(@"info face=""(.*?)"" size=(\d+) bold=(\d+) italic=(\d+) charset=""(.*?)"" unicode=(\d+) stretchH=(\d+) smooth=(\d+) aa=(\d+) padding=(-?\d+),(-?\d+),(-?\d+),(-?\d+) spacing=(-?\d+),(-?\d+)");
        
        private static readonly Regex CommonRegex = new(@"common lineHeight=(\d+) +base=(\d+) +scaleW=(\d+) +scaleH=(\d+) +pages=(\d+) +packed=(\d+)");

        private CharDefinition emptyChar = new();
        private readonly CharDefinition[] chars = new CharDefinition[256];
        
        private readonly int lineHeight;
        private readonly int baseHeight;
        private readonly int scaleW;
        private readonly int scaleH;
        private readonly int pages;
        private readonly int packed;

        private readonly int baseSize;

        public int LineHeight => lineHeight;
        public int BaseHeight => baseHeight;
        public int BaseSize => baseSize;
        public int Width => scaleW;
        public int Height => scaleH;

        public ref CharDefinition GetChar(char c)
        {
            if ((int)c <= 255)
                return ref chars[(int)c];
            return ref emptyChar;
        }
        
        public FontDefinition(string fileName)
        {
            foreach (var line in File.ReadLines(fileName))
            {
                if (line.StartsWith("char id="))
                {
                    var m = CharRegex.Match(line);
                    if (m.Success)
                    {
                        var charId = int.Parse(m.Groups[1].Value);
                        var x = int.Parse(m.Groups[2].Value);
                        var y = int.Parse(m.Groups[3].Value);
                        var w = int.Parse(m.Groups[4].Value);
                        var h = int.Parse(m.Groups[5].Value);
                        var xOff = int.Parse(m.Groups[6].Value);
                        var yOff = int.Parse(m.Groups[7].Value);
                        var xAdv = int.Parse(m.Groups[8].Value);
                        var page = int.Parse(m.Groups[9].Value);
                        var channel = int.Parse(m.Groups[10].Value);
                        chars[charId] = new CharDefinition(x, y, w, h, xOff, yOff, xAdv, page, channel);
                    }
                }
                else if (line.StartsWith("info "))
                {
                    var m = InfoRegex.Match(line);
                    if (m.Success)
                    {
                        var fontName = m.Groups[1].Value;
                        baseSize = int.Parse(m.Groups[2].Value);
                    }
                }
                else if (line.StartsWith("common"))
                {
                    var m = CommonRegex.Match(line);
                    if (m.Success)
                    {
                        lineHeight = int.Parse(m.Groups[1].Value);
                        baseHeight = int.Parse(m.Groups[2].Value);
                        scaleW = int.Parse(m.Groups[3].Value);
                        scaleH = int.Parse(m.Groups[4].Value);
                        pages = int.Parse(m.Groups[5].Value);
                        packed = int.Parse(m.Groups[6].Value);
                    }
                }
            }
        }
    }
}