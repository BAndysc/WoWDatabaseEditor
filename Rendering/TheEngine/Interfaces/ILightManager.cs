using TheEngine.Entities;

namespace TheEngine.Interfaces
{
    public interface ILightManager
    {
       DirectionalLight MainLight { get; }
       DirectionalLight SecondaryLight { get; }
    }
}
