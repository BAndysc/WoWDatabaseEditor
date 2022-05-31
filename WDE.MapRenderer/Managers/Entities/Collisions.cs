namespace WDE.MapRenderer.Managers.Entities;

public static class Collisions
{
    public static uint COLLISION_MASK_TERRAIN = 1;
    public static uint COLLISION_MASK_WMO = 2;
    public static uint COLLISION_MASK_CREATURE = 4;
    public static uint COLLISION_MASK_GAMEOBJECT = 8;
    
    
    public static uint COLLISION_MASK_DYNAMIC = COLLISION_MASK_CREATURE | COLLISION_MASK_GAMEOBJECT;
    public static uint COLLISION_MASK_STATIC = COLLISION_MASK_TERRAIN | COLLISION_MASK_WMO;
}