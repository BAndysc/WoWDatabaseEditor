namespace TheEngine.ECS
{
    public interface IChunkDataIterator
    {
        int Length { get; }
        Entity this[int index] { get; }
        ComponentDataAccess<T> DataAccess<T>() where T : unmanaged, IComponentData;
        ManagedComponentDataAccess<T> ManagedDataAccess<T>() where T : IManagedComponentData;
        ComponentDataAccess<T>? OptionalDataAccess<T>() where T : unmanaged, IComponentData;
        ManagedComponentDataAccess<T>? OptionalManagedDataAccess<T>() where T : IManagedComponentData;
    }
}