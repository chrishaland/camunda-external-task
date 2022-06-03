namespace Haland.CamundaExternalTask.DependencyInjection;

public class CamundaOptions
{
    /// <summary>
    /// An unique id for this external task worker
    /// </summary>
    public virtual string WorkerId { get; set; } = string.Empty;

    /// <summary>
    /// The uri for the Camunda REST Engine
    /// </summary>
    public virtual string Uri { get; set; } = string.Empty;

    /// <summary>
    /// Time to wait for external tasks from Camunda. 
    /// <br/>Default: 30
    /// <br/>Can not be larger than 1800 (30 minutes)
    /// </summary>
    public virtual int ResponseTimeoutInSeconds { get; set; } = 30;

    /// <summary>
    /// Maximum number of tasks to process at a time. 
    /// <br/>Default: 100
    /// </summary>
    public virtual int MaximumTasks { get; set; } = 100;

    /// <summary>
    /// Should Camunda return tasks based on their priority? 
    /// <br/>Default: true
    /// </summary>
    public virtual bool ProcessTasksBasedOnPriority { get; set; } = true;
}
