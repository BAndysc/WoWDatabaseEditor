using System.Collections;
using System.Diagnostics;
using SixLabors.ImageSharp.PixelFormats;
using TheAvaloniaOpenGL.Resources;
using TheEngine.Handles;
using TheEngine.Interfaces;
using WDE.MpqReader.Structures;

namespace WDE.MapRenderer.Managers
{
    public class WoWTextureManager : System.IDisposable
    {
        private readonly ITextureManager textureManager;
        private readonly IGameFiles gameFiles;
        private readonly IGameContext gameContext;
        private Dictionary<FileId, TextureHandle> texts = new();

        public TextureHandle EmptyTexture { get; }
        
        public WoWTextureManager(ITextureManager textureManager, IGameFiles gameFiles, IGameContext gameContext)
        {
            this.textureManager = textureManager;
            this.gameFiles = gameFiles;
            this.gameContext = gameContext;
            EmptyTexture = textureManager.CreateTexture(
                    new[] { new Rgba32[] { new(255, 0, 0, 255) } }, 1, 1, false);
        }

        public IEnumerator GetTexture(FileId? texturePath, TaskCompletionSource<TextureHandle> result)
        {
            if (!texturePath.HasValue)
            {
                result.SetResult(EmptyTexture);
                yield break;
            }
            
            if (texts.TryGetValue(texturePath.Value, out var t))
            {
                result.SetResult(t);
                yield break;
            }

            var dummy = textureManager.CreateDummyHandle();

            texts[texturePath.Value] = dummy;
            
            gameContext.StartCoroutine(InternalLoadTexture(dummy, texturePath.Value));
            
            result.SetResult(dummy);
        }

        private IEnumerator InternalLoadTexture(TextureHandle handle, FileId texturePath)
        {
            var bytes = gameFiles.ReadFile(texturePath);
            yield return bytes;
            if (bytes.Result == null)
                yield break;

            BLP blp = null!;
            yield return Task.Run(() =>
            {
                blp = new BLP(bytes.Result.AsArray(), 0, bytes.Result.Length, maxSize);
            });
            bytes.Result.Dispose();
        
            Debug.Assert(texts[texturePath] == handle);
            var generateMips = blp.Header.Mips == BLP.MipmapLevelAndFlagType.MipsNone;
            var actualHandle = textureManager.CreateTexture(blp.Data, (int)blp.RealWidth, (int)blp.RealHeight, generateMips);
            textureManager.SetFiltering(texts[texturePath], FilteringMode.Linear);
            textureManager.ReplaceHandles(handle, actualHandle);
        }

        public void Dispose()
        {
            textureManager.DisposeTexture(EmptyTexture);
            foreach (var tex in texts.Values)
                textureManager.DisposeTexture(tex);
            texts.Clear();
        }

        public void SetQuality(int quality)
        {
            quality = Math.Clamp(quality, 0, 9);
            maxSize = maxSizes[quality];
        }

        private int maxSize;
        private int[] maxSizes = new[] { 0, 1024, 512, 256, 128, 64, 32, 16, 8, 4 };
    }
}