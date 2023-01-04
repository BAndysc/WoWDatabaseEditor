using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using NUnit.Framework;
using WDE.Common.Utils;

namespace WDE.Common.Test.Utils;

public class TaskQueueTests
{
    [Test]
    public async Task Schedule_ExecutesTaskSuccessfully()
    {
        var queue = new TaskQueue();
        bool taskExecuted = false;
        await queue.Schedule(_ =>
        {
            taskExecuted = true;
            return Task.CompletedTask;
        });

        Assert.IsTrue(taskExecuted);
    }
    
    [Test]
    public async Task CancelAll_CancelsScheduledTask()
    {
        var queue = new TaskQueue();
        var task = queue.Schedule(_ => Task.Delay(Timeout.Infinite, _));

        await queue.CancelAll();
        Assert.ThrowsAsync<TaskCanceledException>(async () => await task);
    }
    
    [Test]
    public async Task CancelledTask_ReturnsCancelledTask()
    {
        var queue = new TaskQueue();
        var cancelledTask = new TaskCompletionSource();
        cancelledTask.SetCanceled();
        var task = queue.Schedule(_ => cancelledTask.Task);
        Assert.IsTrue(task.IsCompleted);
        Assert.IsTrue(task.IsCanceled);
    }

    [Test]
    public async Task CancelAll_CancelsAllScheduledTask()
    {
        var queue = new TaskQueue();
        var task = queue.Schedule(_ => Task.Delay(Timeout.Infinite, _));
        var task2 = queue.Schedule(_ => Task.Delay(Timeout.Infinite, _));

        await queue.CancelAll();
        Assert.ThrowsAsync<TaskCanceledException>(async () => await task);
        Assert.ThrowsAsync<TaskCanceledException>(async () => await task2);
    }

    [Test]
    public async Task Schedule_ThrowsException_PropagatesException()
    {
        var queue = new TaskQueue();
        var task = queue.Schedule(_ => throw new InvalidOperationException());

        Assert.ThrowsAsync<InvalidOperationException>(async () => await task);
    }
    
    [Test]
    public async Task Schedule_MultipleTasks_ExecutesAll()
    {
        var queue = new TaskQueue();
        const int numberOfTasks = 5;
        TaskCompletionSource[] tasks =
            Enumerable.Range(0, numberOfTasks).Select(x => new TaskCompletionSource()).ToArray();

        List<int> taskResultOutput = new List<int>();

        List<Task> scheduledTasks = new List<Task>();

        for (int i = 0; i < numberOfTasks; i++)
        {
            int taskId = i;
            scheduledTasks.Add(queue.Schedule(async _ =>
            {
                taskResultOutput.Add(taskId);
                await tasks[taskId].Task;
            }));
        }

        // finish the tasks in reverse order
        for (int i = numberOfTasks - 1; i >= 0; i--)
            tasks[i].SetResult();

        await Task.WhenAll(scheduledTasks.ToArray());

        // but they should run in order
        CollectionAssert.AreEqual(new int[]{0, 1, 2, 3, 4}, taskResultOutput);
    }

    [Test]
    public async Task Schedule_AfterCancelAll_ExecutesTask()
    {
        var queue = new TaskQueue();
        var tcs = new TaskCompletionSource();
        var t1 = queue.Schedule(async _ =>
        {
            // doesn't respect the cancellation token
            await tcs.Task;
        });
        var cancellationTask = queue.CancelAll();

        bool taskExecuted = false;
        var t2 = queue.Schedule(_ =>
        {
            taskExecuted = true;
            return Task.CompletedTask;
        });

        Assert.IsFalse(taskExecuted); // t1 should be still blocking despite calling CancelAll
        tcs.SetResult(); // unlock task t1
        Assert.IsTrue(t1.IsCanceled); // but cancelled should be set anyway
        Assert.IsTrue(taskExecuted); // t2 should be executed, because it was called after CancelAll
        Assert.IsTrue(t2.IsCompletedSuccessfully);
    }
    
    [Test]
    [Ignore("For some reason on appveyor this test fails, even tho on both my machines it works fine")]
    public async Task KillAll_AfterCancelFails()
    {
        var queue = new TaskQueue();
        var tcs = new TaskCompletionSource();
        bool innerTaskFinished = false;
        var t1 = queue.Schedule(async token =>
        {
            // doesn't respect the cancellation token
            await tcs.Task;
            innerTaskFinished = true;
        });
        var cancellationTask = queue.CancelAll();
        
        Assert.IsFalse(t1.IsCompleted);
        Assert.IsFalse(cancellationTask.IsCompleted);
        
        queue.KillAll();
        
        Assert.IsTrue(t1.IsCompleted);
        Assert.IsTrue(cancellationTask.IsCompletedSuccessfully);
        Assert.IsTrue(t1.IsFaulted);

        Assert.IsFalse(innerTaskFinished);
    }
    
    [Test]
    public async Task KillAll_AlsoCancels()
    {
        var queue = new TaskQueue();
        var tcs = new TaskCompletionSource();
        bool innerTaskFinished = false;
        bool innerTaskHadTokenCancelled = false;
        var t1 = queue.Schedule(async token =>
        {
            // doesn't respect the cancellation token
            await tcs.Task;
            innerTaskFinished = true;
            innerTaskHadTokenCancelled = token.IsCancellationRequested;
        });
        Assert.IsFalse(t1.IsCompleted);
        
        queue.KillAll();
        
        Assert.IsTrue(t1.IsCompleted);
        Assert.IsTrue(t1.IsFaulted);

        Assert.IsFalse(innerTaskFinished);

        tcs.SetResult();
        Assert.IsTrue(innerTaskFinished);
        Assert.IsTrue(innerTaskHadTokenCancelled);
    }
    
    
    // TYPED<T> variant tests
    
    [Test]
    public async Task Schedule_ExecutesTypedTaskSuccessfully()
    {
        var queue = new TaskQueue();
        bool taskExecuted = false;
        var result = await queue.Schedule(async _ =>
        {
            taskExecuted = true;
            return 5;
        });

        Assert.IsTrue(taskExecuted);
        Assert.AreEqual(5, result);
    }
    
    [Test]
    public async Task Schedule_ThrowsException_Typed_PropagatesException()
    {
        var queue = new TaskQueue();
        var task = queue.Schedule<int>(_ => throw new InvalidOperationException());

        Assert.ThrowsAsync<InvalidOperationException>(async () => await task);
    }

    [Test]
    public async Task CancelAll_TypedTasks()
    {
        TaskCompletionSource wait = new();
        var queue = new TaskQueue();
        
        var t1 = queue.Schedule(async _ =>
        {
            await wait.Task;
            return 5;
        });
        var t2 = queue.Schedule(async _ => 4);
        
        var cancel =  queue.CancelAll();
        wait.SetResult();
        await cancel;
        
        Assert.IsTrue(t1.IsCanceled);
        Assert.IsTrue(t2.IsCanceled);
    }
}