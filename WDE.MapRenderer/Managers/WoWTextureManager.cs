using System.Collections;
using System.Diagnostics;
using SixLabors.ImageSharp.PixelFormats;
using TheAvaloniaOpenGL.Resources;
using TheEngine.Handles;
using WDE.MpqReader.Structures;

namespace WDE.MapRenderer.Managers
{
    public class WoWTextureManager : System.IDisposable
    {
        private readonly IGameContext gameContext;
        private Dictionary<string, TextureHandle> texts = new();

        public TextureHandle EmptyTexture { get; }
        
        public WoWTextureManager(IGameContext gameContext)
        {
            this.gameContext = gameContext;
            EmptyTexture = gameContext.Engine.TextureManager.CreateTexture(
                    new[] { new Rgba32[] { new(255, 0, 0, 255) } }, 1, 1, false);
        }

        public IEnumerator GetTexture(string texturePath, TaskCompletionSource<TextureHandle> result)
        {
            if (texts.TryGetValue(texturePath, out var t))
            {
                result.SetResult(t);
                yield break;
            }

            var dummy = gameContext.Engine.TextureManager.CreateDummyHandle();

            texts[texturePath] = dummy;

            var bytes = gameContext.ReadFile(texturePath);
            yield return bytes;
            if (bytes.Result == null)
            {
                result.SetResult(gameContext.Engine.TextureManager.CreateTexture(null, 1, 1, true));
                yield break;
            }

            var blp = new BLP(bytes.Result.AsArray(), 0, bytes.Result.Length);
            bytes.Result.Dispose();
        
            Debug.Assert(texts[texturePath] == dummy);
            var actualHandle = gameContext.Engine.TextureManager.CreateTexture(blp.Data, (int)blp.Header.Width, (int)blp.Header.Height, blp.Header.Mips == BLP.MipmapLevelAndFlagType.MipsNone);
            gameContext.Engine.TextureManager.SetFiltering(texts[texturePath], FilteringMode.Linear);
            gameContext.Engine.TextureManager.ReplaceHandles(dummy, actualHandle);
            result.SetResult(dummy);
        }

        public void Dispose()
        {
            gameContext.Engine.TextureManager.DisposeTexture(EmptyTexture);
            foreach (var tex in texts.Values)
                gameContext.Engine.TextureManager.DisposeTexture(tex);
            texts.Clear();
        }
    }
}