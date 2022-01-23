namespace Haland.CamundaExternalTask;

internal interface IExternalTaskHandler 
{
    public string Topic { get; }
    public string[]? Variables { get; }
    public int LockDuration { get; }

    public Task<ExternalTaskResult> Execute(ExternalTask externalTask, CancellationToken ct);
}

public abstract class ExternalTaskHandler : IExternalTaskHandler
{
    /// <summary>
    /// The external tasks topic name
    /// </summary>
    public abstract string Topic { get; }

    /// <summary>
    /// List of variables that should be received if they are accessible from the external task's execution. If not provided - all variables will be fetched.
    /// <br/>Default: null
    /// </summary>
    public virtual string[]? Variables { get; } = null;
    
    /// <summary>
    /// The duration to lock the external tasks for in milliseconds.
    /// <br/>Default: 30000
    /// </summary>
    public virtual int LockDuration { get; } = 30000;

    public abstract Task<ExternalTaskResult> Execute(ExternalTask externalTask, CancellationToken ct);
}
