using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;

namespace WDE.Common.Utils;

/// <summary>
/// TasksQueue is a queue of tasks that are executed in order, but only one at a time.
/// The class is not thread safe!
/// </summary>
public partial class TaskQueue : INotifyPropertyChanged
{
    private List<(TaskCompletionSource, Func<CancellationToken, Task>)> tasks = new ();
    private bool isRunning = false;
    public bool IsRunning => isRunning;
    private CancellationTokenSource? loopCancellationToken;
    private CancellationTokenSource? currentTaskToken;
    private Task<Task>? currentTask;

    private int threadId;
    
    public TaskQueue()
    {
        threadId = Thread.CurrentThread.ManagedThreadId;
    }
    
    private void VerifyAccess()
    {
        if (threadId != Thread.CurrentThread.ManagedThreadId)
            throw new InvalidThreadAccessException(
                $"Invalid thread access. The queue was created on thread {threadId} and is accessed from thread {Thread.CurrentThread.ManagedThreadId}.");
    }
    
    public Task Schedule(Func<CancellationToken, Task> task)
    {
        VerifyAccess();
        var tcs = new TaskCompletionSource();
        tasks.Add((tcs, task));
        if (!isRunning)
        {
            isRunning = true;
            loopCancellationToken = new();
#pragma warning disable CS4014 // Because this call is not awaited, execution of the current method continues before the call is completed
            Run(loopCancellationToken.Token).ListenErrors();
#pragma warning restore CS4014 // Because this call is not awaited, execution of the current method continues before the call is completed
        }
        return tcs.Task;
    }

    public async Task CancelAll()
    {
        VerifyAccess();
        foreach (var task in tasks)
            task.Item1.SetCanceled();
        tasks.Clear();
        currentTaskToken?.Cancel();
        if (currentTask == null)
            return;

        try
        {
            await currentTask;
        }
        catch (TaskCanceledException)
        {
        }
        catch (TaskKilledException)
        {
            
        }
        catch (Exception e)
        {
            // Todo: I think this can be safely ignored, because someone else already awaits this task, don't they?
            Console.WriteLine(e);
        }
    }

    /// <summary>
    /// 
    /// </summary>
    /// <param name="loopCancellationToken">Can be used to forcefully kill the loop tasks. Should be used only if CancelAll doesn't stop the tasks</param>
    private async Task Run(CancellationToken loopCancellationToken)
    {
        try
        {
            var loopCancelTask = new Task(() => { }, loopCancellationToken);

            SetField(ref isRunning, true, nameof(IsRunning));
            while (tasks.Count > 0)
            {
                VerifyAccess();
                var (tcs, task) = tasks[0];
                tasks.RemoveAt(0);
                var cancelToken = currentTaskToken = new CancellationTokenSource();
                try
                {
                    var startedTask = task(currentTaskToken.Token);
                    currentTask = Task.WhenAny(loopCancelTask, startedTask);
                    var finished = await currentTask;

                    if (finished == loopCancelTask)
                    {
                        // loop was force stopped, so instead of setting SetCanceled, let's throw an exception
                        tcs.SetException(new TaskKilledException("Task was force killed, because it didn't want to cancel itself."));
                        break; // break the loop
                    }
                    else
                    {
                        if (cancelToken.IsCancellationRequested)
                            tcs.SetCanceled(cancelToken.Token);
                        else
                            tcs.SetResult();   
                    }
                }
                catch (Exception e)
                {
                    tcs.SetException(e);
                }
                finally
                {
                    currentTaskToken = null;
                    currentTask = null;
                }
            }
        }
        catch (InvalidThreadAccessException e)
        {
            Console.WriteLine("CONCURRENCY EXCEPTION! The last task has run the continuation on another thread! This is not how async await should work. This is a critical error, please report to the devs.");
        }
        finally
        {
            SetField(ref isRunning, false, nameof(IsRunning));
            this.loopCancellationToken = null;
        }
    }

    public void KillAll()
    {
        if (!isRunning)
            return;
        
        VerifyAccess();
        foreach (var task in tasks)
            task.Item1.SetCanceled();
        tasks.Clear();
        currentTaskToken?.Cancel();
        loopCancellationToken?.Cancel();
        Debug.Assert(currentTaskToken == null);
        Debug.Assert(currentTask == null);
        Debug.Assert(loopCancellationToken == null);
    }

    public event PropertyChangedEventHandler? PropertyChanged;

    protected virtual void OnPropertyChanged([CallerMemberName] string? propertyName = null)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }

    protected bool SetField<T>(ref T field, T value, [CallerMemberName] string? propertyName = null)
    {
        if (EqualityComparer<T>.Default.Equals(field, value)) return false;
        field = value;
        OnPropertyChanged(propertyName);
        return true;
    }
}

public class InvalidThreadAccessException : Exception
{
    public InvalidThreadAccessException(string message) : base(message)
    {
    }
}

public class TaskKilledException : Exception
{
    public TaskKilledException(string message) : base(message)
    {
    }
}