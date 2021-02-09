namespace WDE.Common.Tasks
{
    public interface IMainThread
    {
        void Dispatch(System.Action action);
    }
}