using WDE.MapRenderer.StaticData;

namespace WDE.MapRenderer.Managers;

public class PerChunkHolder<T> where T : new()
{
    private T?[,] array = new T?[Constants.Blocks, Constants.Blocks];

    public T? this[(int x, int y) pair]
    {
        get => this[pair.x, pair.y];
        set => this[pair.x, pair.y] = value;
    }
    
    public T? this[int x, int y]
    {
        get
        {
            if (array[x, y] == null)
                return array[x, y] = new T();
            return array[x, y];
        }
        set => array[x, y] = value;
    }
    
    public void Clear()
    {
        for (int i = 0; i < Constants.Blocks; i++)
        {
            for (int j = 0; j < Constants.Blocks; j++)
            {
                array[i, j] = default(T);
            }
        }
    }
}