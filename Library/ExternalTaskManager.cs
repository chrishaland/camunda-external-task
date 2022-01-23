using Microsoft.Extensions.Logging;

namespace Haland.CamundaExternalTask;

internal class ExternalTaskManager
{
    private readonly IExternalTaskClient _client;
    private readonly CamundaOptions _options;
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<ExternalTaskManager> _logger;

    public ExternalTaskManager(
        ILogger<ExternalTaskManager> logger, 
        IExternalTaskClient client, 
        CamundaOptions options, 
        IServiceProvider serviceProvider
    )
    {
        _logger = logger;
        _client = client;
        _options = options;
        _serviceProvider = serviceProvider;
    }
    
    internal async Task Execute(CancellationToken ct)
    {
        using var scope = _serviceProvider.CreateScope();
        var externalTaskHandlers = scope.ServiceProvider.GetRequiredService<IEnumerable<IExternalTaskHandler>>();

        var topics = externalTaskHandlers.Select(handler => new FetchExternalTaskTopicDto
        {
            TopicName = handler.Topic,
            Variables = handler.Variables,
            LockDuration = handler.LockDuration
        });

        _logger.LogInformation("Getting external tasks to execute for topics '{@topics}'", topics.Select(t => t.TopicName));
        
        var tasks = await GetExternalTasksToExecute(topics.ToArray(), ct);
        
        _logger.LogInformation("Got {externalTaskCount} external tasks to execute: {@tasks}", tasks.Count(), tasks.Select(t => new { TaskId = t.Id, Topic = t.TopicName }));

        var externalTaskExecutions = new List<Task>();
        foreach (var task in tasks)
        {
            var handler = externalTaskHandlers.FirstOrDefault(h => h.Topic == task.TopicName);
            externalTaskExecutions.Add(ExecuteExternalTask(task, handler, ct));
        }
        
        await Task.WhenAll(externalTaskExecutions);
    }

    private async Task ExecuteExternalTask(LockedExternalTaskDto task, IExternalTaskHandler? handler, CancellationToken cancellationToken)
    {
        try
        {
            if (handler == null)
            {
                await NotifyTaskExecutionResult(task, new ExternalTaskFailureResult(
                    ErrorMessage: $"No handler found for topic '{task.TopicName}'",
                    ErrorDetails: $""
                ), cancellationToken);
                return;
            }

            var externalTask = new ExternalTask(
                Id: task.Id, 
                WorkerId: task.WorkerId, 
                Variables: task.Variables.ToDictionary((kv) => kv.Key, kv => kv.Value.Value)
            );

            ExternalTaskResult result;
            var cts = new CancellationTokenSource();

            try
            {
                result = await handler.Execute(externalTask, cts.Token)
                    .WithTimeout(TimeSpan.FromMilliseconds(handler.LockDuration));
            }
            catch(TimeoutException)
            {
                cts.Cancel();
                result = new ExternalTaskFailureResult(
                    ErrorMessage: "The task execution timed out", 
                    ErrorDetails: $"The task execution did not complete within the lock duration of {handler.LockDuration} milliseconds"
                );
            }

            await NotifyTaskExecutionResult(task, result, cancellationToken);
        }
        catch (Exception ex)
        {
            await NotifyTaskExecutionResult(task, new ExternalTaskFailureResult(
                ErrorMessage: ex.Message,
                ErrorDetails: ex.StackTrace ?? string.Empty
            ), cancellationToken);
        }
    }

    private async Task NotifyTaskExecutionResult(LockedExternalTaskDto task, ExternalTaskResult result, CancellationToken cancellationToken)
    {
        if (result is ExternalTaskFailureResult failureResult)
        {
            await _client.Fail(task.Id, new FailExternalTaskDto
            {
                WorkerId = task.WorkerId,
                ErrorDetails = failureResult.ErrorDetails,
                ErrorMessage = failureResult.ErrorMessage
            }, cancellationToken);
        }
        else if (result is ExternalTaskCompleteResult completeResult)
        {
            await _client.Complete(task.Id, new CompleteExternalTaskDto
            {
                WorkerId = task.WorkerId,
                Variables = completeResult.Variables?.ToDictionary((kv) => kv.Key, kv => new VariableDto(kv.Value))
            }, cancellationToken);
        }
        else if (result is ExternalTaskBpmnErrorResult bpmnErrorResult)
        {
            await _client.BpmnError(task.Id, new BpmnErrorExternalTaskDto
            {
                WorkerId = task.WorkerId,
                ErrorCode = bpmnErrorResult.ErrorCode,
                ErrorMessage = bpmnErrorResult.ErrorMessage
            }, cancellationToken);
        }
    }

    private async Task<IEnumerable<LockedExternalTaskDto>> GetExternalTasksToExecute(FetchExternalTaskTopicDto[] topics, CancellationToken ct)
    {
        var request = new FetchExternalTasksDto
        {
            WorkerId = _options.WorkerId,
            MaxTasks = _options.MaximumTasks,
            UsePriority = _options.ProcessTasksBasedOnPriority,
            AsyncResponseTimeout = _options.ResponseTimeoutInSeconds * 1000,
            Topics = topics
        };

        var response = await _client.FetchAndLock(request, ct);
        return response ?? Enumerable.Empty<LockedExternalTaskDto>();
    }
}
