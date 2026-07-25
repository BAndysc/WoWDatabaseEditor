namespace TheEngine.ECS;

public interface IJob
{
    void Execute(int start, int end);
}

public interface IParallelJob
{
    void Execute(int thread, int start, int end);
}