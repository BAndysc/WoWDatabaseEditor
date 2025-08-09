using System.Diagnostics;
using SixLabors.ImageSharp.PixelFormats;
using TheAvaloniaOpenGL.Resources;
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
        private Dictionary<FileId, Task<ITexture>> loadingTasks = new();

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

            var tcs = new TaskCompletionSource<ITexture>();
            loadingTasks[texturePath.Value] = tcs.Task;

            var text = await InternalLoadTexture(texturePath.Value) ?? EmptyTexture;

            texts[texturePath.Value] = new WeakReference<ITexture>(text);
            loadingTasks.Remove(texturePath.Value);

            return text;
        }

        private async ValueTask<ITexture?> InternalLoadTexture(FileId texturePath)
        {
            var bytes = await gameFiles.ReadFile(texturePath);
            if (bytes == null)
                return null;

            BLP blp = null!;
            await engine.EnterThreadPool;
            blp = new BLP(bytes.AsArray(), 0, bytes.Length, maxSize);
            bytes.Dispose();
            await engine.EnterGameLoop;

            var generateMips = blp.Header.Mips == BLP.MipmapLevelAndFlagType.MipsNone;
            var actualHandle = textureManager.CreateTexture(blp.Data, (int)blp.RealWidth, (int)blp.RealHeight, generateMips);
            textureManager.SetFiltering(actualHandle, FilteringMode.Linear);
            return actualHandle;
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