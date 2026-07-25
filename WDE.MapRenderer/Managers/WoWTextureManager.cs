using System.Diagnostics;
using SixLabors.ImageSharp.PixelFormats;
using TheEngine.Resources;
using TheEngine;
using TheEngine.Handles;
using TheEngine.Interfaces;
using TheEngine.Utils;
using WDE.MpqReader.Structures;

namespace WDE.MapRenderer.Managers
{
    public class WoWTextureManager : System.IDisposable
    {
        private readonly ITextureManager textureManager;
        private readonly IGameFiles gameFiles;
        private readonly IGameContext gameContext;
        private readonly Engine engine;
        private Dictionary<FileId, WeakReference<ITexture>?> texts = new();

        public ITexture EmptyTexture { get; }
        
        public WoWTextureManager(ITextureManager textureManager,
            IGameFiles gameFiles,
            IGameContext gameContext,
            Engine engine)
        {
            this.textureManager = textureManager;
            this.gameFiles = gameFiles;
            this.gameContext = gameContext;
            this.engine = engine;
            EmptyTexture = textureManager.CreateTexture(
                    new[] { new Rgba32[] { new(255, 0, 0, 255) } }, 1, 1, false);
        }

        public async ValueTask<ITexture> GetTexture(FileId? texturePath)
        {
            if (!texturePath.HasValue)
            {
                return EmptyTexture;
            }
            
            if (texts.TryGetValue(texturePath.Value, out var t))
            {
                if (t.TryGetTarget(out var target))
                    return target;
                texts.Remove(texturePath.Value);
            }

            var text = await InternalLoadTexture(texturePath.Value) ?? EmptyTexture;
            texts[texturePath.Value] = new WeakReference<ITexture>(text);
            return text;
        }

        private async ValueTask<ITexture?> InternalLoadTexture(FileId texturePath)
        {
            var bytes = await gameFiles.ReadFile(texturePath);
            if (bytes == null)
                return null;

            await engine.EnterThreadPool;
            BLP? blp = null;
            try
            {
                blp = new BLP(bytes.AsArray(), 0, bytes.Length, maxSize);
            }
            catch (Exception e)
            {
                // a corrupt/unknown-format BLP must degrade to the fallback texture - throwing here
                // propagates into chunk loading (ChunkManager.LoadChunkImpl) and leaves its
                // chunkLoading task forever incomplete, wedging the whole map load
                Console.WriteLine($"Invalid BLP file {texturePath}: {e.Message}");
            }
            finally
            {
                bytes.Dispose();
            }

            if (blp == null)
            {
                // the success path resumes the caller on the engine thread (via CreateTextureAsync),
                // so the failure path must too - GetTexture writes the cache dictionary right after
                await engine.EnterGameLoop;
                return null;
            }

            // GPU upload runs async on the transfer queue and the ValueTask completes on the render thread
            var generateMips = blp.Header.Mips == BLP.MipmapLevelAndFlagType.MipsNone;
            return await textureManager.CreateTextureAsync(blp.Data, (int)blp.RealWidth, (int)blp.RealHeight, generateMips,
                FilteringMode.Linear, WrapMode.Repeat);
        }

        public void Dispose()
        {
            textureManager.DisposeTexture(EmptyTexture);
            foreach (var tex in texts.Values)
            {
                if (tex.TryGetTarget(out var target))
                    textureManager.DisposeTexture(target);
            }
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