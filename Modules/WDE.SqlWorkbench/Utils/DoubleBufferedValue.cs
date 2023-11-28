namespace WDE.SqlWorkbench.Utils;

public class DoubleBufferedValue<T> where T : new()
{
    private T t1 = new();
    private T t2 = new();
    private int index = 0;
    
    public T Value => index == 0 ? t1 : t2;
    public void Swap() => index = 1 - index;
}