using TheEngine.ECS;

namespace TheEngine.Components;

public struct DisabledObjectBit : IComponentData
{
    private byte disabled;

    private DisabledObjectBit(bool b)
    {
        disabled = b ? (byte)1 : (byte)0;
    }

    public void Enable()
    {
        disabled = 0;
    }

    public void Disable()
    {
        disabled = 1;
    }

    public static implicit operator bool(DisabledObjectBit d) => d.disabled == 1;
    public static explicit operator DisabledObjectBit(bool b) => new DisabledObjectBit(b);
}

public static class DisabledObjectBitExtensions
{
    public static void SetDisabledObject(this Entity entity, IEntityManager entityManager, bool disabled)
    {
        if (disabled)
            entity.DisableObject(entityManager);
        else
            entity.EnableObject(entityManager);
    }
    
    public static void DisableObject(this Entity entity, IEntityManager entityManager)
    {
        entityManager.GetComponent<DisabledObjectBit>(entity).Disable();
    }
    
    public static void EnableObject(this Entity entity, IEntityManager entityManager)
    {
        entityManager.GetComponent<DisabledObjectBit>(entity).Enable();
    }
}