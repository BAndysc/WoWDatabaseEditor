using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Avalonia;
using Avalonia.Rendering.Composition;

namespace GpuInterop;


internal interface ISwapchainImage : IAsyncDisposable
{
    PixelSize Size { get; }

    Task? LastPresent { get; }

    void BeginDraw();

    void Present();
}


/// <summary>
/// A helper class for composition-backed swapchains, should not be a public API yet
/// </summary>
internal abstract class SwapchainBase<TImage> : IAsyncDisposable where TImage : class, ISwapchainImage
{
  private readonly List<TImage> _pendingImages = new List<TImage>();

  protected ICompositionGpuInterop Interop { get; }

  protected CompositionDrawingSurface Target { get; }

  public SwapchainBase(ICompositionGpuInterop interop, CompositionDrawingSurface target)
  {
    this.Interop = interop;
    this.Target = target;
  }

  private static bool IsBroken(TImage image)
  {
    Task lastPresent = image.LastPresent;
    return lastPresent != null && lastPresent.IsFaulted;
  }

  private static bool IsReady(TImage image)
  {
    return image.LastPresent == null || image.LastPresent.Status == TaskStatus.RanToCompletion;
  }

  private TImage? CleanupAndFindNextImage(PixelSize size)
  {
    TImage image = default (TImage);
    bool flag1 = false;
    for (int index = this._pendingImages.Count - 1; index > -1; --index)
    {
      TImage pendingImage = this._pendingImages[index];
      bool flag2 = SwapchainBase<TImage>.IsReady(pendingImage);
      bool flag3 = pendingImage.Size == size;
      if (SwapchainBase<TImage>.IsBroken(pendingImage) || !flag3 & flag2)
      {
        pendingImage.DisposeAsync();
        this._pendingImages.RemoveAt(index);
      }
      if (flag3 & flag2)
      {
        if ((object) image == null)
          image = pendingImage;
        else
          flag1 = true;
      }
    }
    return !flag1 ? default (TImage) : image;
  }

  protected abstract TImage CreateImage(PixelSize size);

  protected IDisposable BeginDrawCore(PixelSize size, out TImage image)
  {
    TImage img = this.CleanupAndFindNextImage(size) ?? this.CreateImage(size);
    img.BeginDraw();
    this._pendingImages.Remove(img);
    image = img;
    return new Disposable((Action) (() =>
    {
      img.Present();
      this._pendingImages.Add(img);
    }));
  }

  private class Disposable : IDisposable
  {
      private Action action;

      public Disposable(Action action)
      {
          this.action = action;
      }

      public void Dispose()
      {
          action?.Invoke();
      }
  }

  public async ValueTask DisposeAsync()
  {
    foreach (TImage pendingImage in this._pendingImages)
      await pendingImage.DisposeAsync();
  }
}
