namespace WDE.Common.Services.MessageBox
{
    public interface IMessageBoxButton<out T>
    {
        string Name { get; }
        T ReturnValue { get; }
    }
}