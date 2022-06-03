namespace Haland.CamundaExternalTask;

internal interface IExternalTaskHandler
{
    /// <summary>
    /// The external tasks topic name
    /// </summary>
    public string Topic { get; }

    /// <summary>
    /// List of variables that should be received if they are accessible from the external task's execution. If not provided - all variables will be fetched.
    /// <br/>Default: null
    /// </summary>
    public string[]? Variables { get; }

    /// <summary>
    /// The duration to lock the external tasks for in milliseconds.
    /// <br/>Default: 30000
    /// </summary>
    public int LockDuration { get; }

    /// <summary>
    /// The maximum number of times to retry tasks when they report failure.
    /// <br/>Default: null (no retries)
    /// </summary>
    public int? Retries { get; }

    /// <summary>
    /// The time to wait before the external task becomes available again for fetching.
    /// <br/>Default: a function returning 1 minute (wait 1 minute between each retry)
    /// </summary>
    public Func<int, TimeSpan> RetryTimeout { get; }

    public Task<ExternalTaskResult> Execute(ExternalTask externalTask, CancellationToken cancellationToken);
}

public abstract class ExternalTaskHandler : IExternalTaskHandler
{
    public abstract string Topic { get; }
    public virtual string[]? Variables { get; } = null;
    public virtual int LockDuration { get; } = 30000;
    public virtual int? Retries { get; } = null;
    public virtual Func<int, TimeSpan> RetryTimeout { get; } = retriesLeft => TimeSpan.FromMinutes(1);

    public abstract Task<ExternalTaskResult> Execute(ExternalTask externalTask, CancellationToken cancellationToken);
}
