namespace Haland.CamundaExternalTask;

internal static class TaskExtentions
{
    internal static async Task<TResult> WithTimeout<TResult>(this Task<TResult> task, TimeSpan timeout)
    {
        using var cts = new CancellationTokenSource();
        var completedTask = await Task.WhenAny(task, Task.Delay(timeout, cts.Token));
        
        if (completedTask != task) throw new TimeoutException();
     
        cts.Cancel();
        return await task;
    }
}
