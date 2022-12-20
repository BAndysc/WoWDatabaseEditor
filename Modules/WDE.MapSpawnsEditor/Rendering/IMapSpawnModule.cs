using WDE.Module.Attributes;

namespace WDE.MapSpawnsEditor.Rendering;

[NonUniqueProvider]
public interface IMapSpawnModule
{
    public void Update(float diff) { }
    public void Render(float diff) { }
    public void RenderTransparent(float diff) { }
    public void RenderGUI() { }
}