using TheEngine.Handles;

namespace TheEngine.Interfaces;

public interface IPostProcess
{
    void RenderPostprocess(IRenderManager context, TextureHandle currentImage);
}