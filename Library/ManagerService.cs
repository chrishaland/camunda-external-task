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
        try
        {
            if (externalTaskHandlerType == null)
            {
                await NotifyTaskExecutionResult(task, new ExternalTaskFailureResult(
                    ErrorMessage: $"No handler found for topic '{task.TopicName}'",
                    ErrorDetails: $""
                ), cancellationToken);
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

            await NotifyTaskExecutionResult(task, result, cancellationToken);
        }
        catch (Exception ex)
        {
            await NotifyTaskExecutionResult(task, new ExternalTaskFailureResult(
                ErrorMessage: ex.Message,
                ErrorDetails: ex.Message + Environment.NewLine + (ex.StackTrace ?? string.Empty)
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
                Variables = completeResult.Variables?.ToDictionary((kv) => kv.Key, kv => new VariableDto(
                    Value: kv.Value.Token,
                    Type: kv.Value.Type,
                    ValueInfo: new ValueInfoDto
                    {
                        Encoding = kv.Value.ValueInfo?.Encoding,
                        FileName = kv.Value.ValueInfo?.FileName,
                        MimeType = kv.Value.ValueInfo?.MimeType,
                        ObjectTypeName = kv.Value.ValueInfo?.ObjectTypeName,
                        SerializationDataFormat = kv.Value.ValueInfo?.SerializationDataFormat
                    }
                ))
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
}
