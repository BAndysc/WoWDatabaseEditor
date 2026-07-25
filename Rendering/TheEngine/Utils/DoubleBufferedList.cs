namespace TheEngine.Utils;

public class DoubleBufferedList<T>
{
    private object listLock = new();
    private List<T>[] backingLists =
    [
        new(),
        new()
    ];

    private int currentList = 0;

    public void Add(T t)
    {
        lock (listLock)
        {
            backingLists[currentList % 2].Add(t);
        }
    }

    public List<T> Collect()
    {
        lock (listLock)
        {
            return backingLists[(currentList++) % 2];
        }
    }
}