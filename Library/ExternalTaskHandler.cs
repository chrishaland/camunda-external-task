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

    public Task<ExternalTaskResult> Execute(ExternalTask externalTask, CancellationToken ct);
}

public abstract class ExternalTaskHandler : IExternalTaskHandler
{
    public abstract string Topic { get; }
    public virtual string[]? Variables { get; } = null;
    public virtual int LockDuration { get; } = 30000;

    public abstract Task<ExternalTaskResult> Execute(ExternalTask externalTask, CancellationToken ct);
}
