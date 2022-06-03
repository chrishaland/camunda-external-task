using Microsoft.Extensions.Logging;
using Haland.CamundaExternalTask.DependencyInjection;

namespace Haland.CamundaExternalTask;

internal class ManagerService : BackgroundService
{
    private readonly IChannel _channel;
    private readonly CamundaOptions _options;
    private readonly IExternalTaskClient _client;
    private readonly ILogger<ManagerService> _logger;
    private readonly IServiceProvider _serviceProvider;
    private readonly IEnumerable<IExternalTaskHandler> _handlers;

    public ManagerService(
        IChannel channel,
        CamundaOptions options,
        IExternalTaskClient client,
        ILogger<ManagerService> logger,
        IEnumerable<IExternalTaskHandler> handlers,
        IServiceProvider serviceProvider
    )
    {
        _logger = logger;
        _client = client;
        _options = options;
        _channel = channel;
        _handlers = handlers;
        _serviceProvider = serviceProvider;
    }

    protected override async Task ExecuteAsync(CancellationToken cancellationToken)
    {
        while (!cancellationToken.IsCancellationRequested)
        {
            await ExecuteExternalTask(cancellationToken);
        }
    }

    internal async Task ExecuteExternalTask(CancellationToken cancellationToken)
    {
        try
        {
            var task = await _channel.Read(cancellationToken);

            var handler = _handlers.FirstOrDefault(h => h.Topic == task.TopicName);
            await ExecuteExternalTask(handler?.GetType(), task, cancellationToken);
        }
        catch (Exception ex)
        {
            if (cancellationToken.IsCancellationRequested && ex is TaskCanceledException) return;

            _logger.LogError(ex, "An unexpected error occurred while processing Camunda external tasks for worker '{workerId}'. " +
                "Error message: {errorMessage}", _options.WorkerId, ex.Message);
        }
        finally
        {
            _channel.Release();
        }
    }

    private async Task ExecuteExternalTask(Type? externalTaskHandlerType, LockedExternalTaskDto task, CancellationToken cancellationToken)
    {
        int? maxRetries = null;
        Func<int, TimeSpan> retryTimeout = _ => TimeSpan.Zero;

        try
        {
            if (externalTaskHandlerType == null)
            {
                await NotifyTaskExecutionResult(task, new ExternalTaskFailureResult(
                    ErrorMessage: $"No handler found for topic '{task.TopicName}'",
                    ErrorDetails: $""
                ), maxRetries, retryTimeout, cancellationToken);
                return;
            }

            _logger.LogInformation("Processing external task for process instance '{ProcessInstanceId}'", task.Id);

            var externalTask = new ExternalTask(
                Id: task.Id,
                WorkerId: task.WorkerId,
                Variables: task.Variables.ToDictionary((kv) => kv.Key, kv => Variable.From(kv.Value))
            );

            ExternalTaskResult result;
            var lockDuration = 0;
            var cts = new CancellationTokenSource();

            try
            {
                using var scope = _serviceProvider.CreateScope();
                var handler = (IExternalTaskHandler)scope.ServiceProvider.GetRequiredService(externalTaskHandlerType);
                
                maxRetries = handler.Retries;
                retryTimeout = handler.RetryTimeout;
                result = await handler.Execute(externalTask, cts.Token)
                    .WithTimeout(TimeSpan.FromMilliseconds(lockDuration = handler.LockDuration));
            }
            catch (TimeoutException)
            {
                cts.Cancel();
                result = new ExternalTaskFailureResult(
                    ErrorMessage: "The task execution timed out",
                    ErrorDetails: $"The task execution did not complete within the lock duration of {lockDuration} milliseconds"
                );
            }

            await NotifyTaskExecutionResult(task, result, maxRetries, retryTimeout, cancellationToken);
        }
        catch (Exception ex)
        {
            await NotifyTaskExecutionResult(task, new ExternalTaskFailureResult(
                ErrorMessage: ex.Message,
                ErrorDetails: ex.Message + Environment.NewLine + (ex.StackTrace ?? string.Empty)
            ), maxRetries, retryTimeout, cancellationToken);
        }
    }

    private async Task NotifyTaskExecutionResult(LockedExternalTaskDto task, ExternalTaskResult result, int? maxRetries, Func<int, TimeSpan> retryTimeout, CancellationToken cancellationToken)
    {
        if (result is ExternalTaskFailureResult failureResult)
        {
            var retries = task.Retries.HasValue ? task.Retries - 1 : maxRetries;
            await _client.Fail(task.Id, new FailExternalTaskDto
            {
                WorkerId = task.WorkerId,
                ErrorDetails = failureResult.ErrorDetails,
                ErrorMessage = failureResult.ErrorMessage,
                Variables = failureResult.Variables?.ToDto(),
                LocalVariables = failureResult.LocalVariables?.ToDto(),
                Retries = retries,
                RetryTimeout = retries.HasValue && retries.Value > 0 ? (long)retryTimeout(retries.Value).TotalMilliseconds : null
            }, cancellationToken);
        }
        else if (result is ExternalTaskCompleteResult completeResult)
        {
            await _client.Complete(task.Id, new CompleteExternalTaskDto
            {
                WorkerId = task.WorkerId,
                Variables = completeResult.Variables?.ToDto(),
                LocalVariables = completeResult.LocalVariables?.ToDto(),
            }, cancellationToken);
        }
        else if (result is ExternalTaskBpmnErrorResult bpmnErrorResult)
        {
            await _client.BpmnError(task.Id, new BpmnErrorExternalTaskDto
            {
                WorkerId = task.WorkerId,
                ErrorCode = bpmnErrorResult.ErrorCode,
                ErrorMessage = bpmnErrorResult.ErrorMessage,
                Variables = bpmnErrorResult.Variables?.ToDto()
            }, cancellationToken);
        }
    }
}
