using TheEngine.ECS;
using TheEngine.Interfaces;
using TheEngine.Utils;
using TheMaths;

namespace TheEngine.Entities;

public struct Decal : IComponentData
{
    public bool Disabled;
    public Vector4 Color;       // rgb tint, a = opacity multiplier
    public float FadeAngleCos;  // cosine threshold; surfaces more perpendicular to the decal's
                                 // projection axis than this fade to fully transparent
    private StaticReference albedoGcHandle;

    public ITexture? Albedo
    {
        get => albedoGcHandle.IsEmpty ? null : (Static.TryGet(albedoGcHandle, out var value) ? (ITexture?)value : null);
        set
        {
            if (!albedoGcHandle.IsEmpty)
            {
                albedoGcHandle.Free();
                albedoGcHandle = default;
            }

            if (value != null)
                albedoGcHandle = value.GetStaticReference();
        }
    }

    // picked up automatically by ComponentTypeData<Decal> via naming convention
    public static void OnRemoved(Engine engine, Entity entity, ref Decal component)
    {
        component.Albedo = null;
    }
}
