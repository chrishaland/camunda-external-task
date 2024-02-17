using Microsoft.Extensions.Logging;
using Haland.CamundaExternalTask.DependencyInjection;

namespace Haland.CamundaExternalTask;

internal class FetcherService : BackgroundService
{
    private readonly IChannel _channel;
    private readonly CamundaOptions _options;
    private readonly IExternalTaskClient _client;
    private readonly ILogger<FetcherService> _logger;
    private readonly IEnumerable<FetchExternalTaskTopicDto> _topics;

    public FetcherService(
        IChannel channel,
        CamundaOptions options,
        IExternalTaskClient client,
        ILogger<FetcherService> logger,
        IEnumerable<IExternalTaskHandler> handlers
    )
    {
        _logger = logger;
        _client = client;
        _options = options;
        _channel = channel;
        _topics = handlers.Select(handler => new FetchExternalTaskTopicDto
        {
            TopicName = handler.Topic,
            Variables = handler.Variables,
            LockDuration = handler.LockDuration
        });
    }

    protected override async Task ExecuteAsync(CancellationToken cancellationToken)
    {
        while (!cancellationToken.IsCancellationRequested)
        {
            await FetchExternalTasks(cancellationToken);
        }
    }

    internal async Task FetchExternalTasks(CancellationToken cancellationToken)
    {
        try
        {
            var capacity = _channel.CurrentCapacity;

            if (capacity > 0)
            {
                _logger.LogInformation("Getting external tasks to execute for topics '{@topics}'", _topics.Select(t => t.TopicName));
                var tasks = await GetExternalTasksToExecute(_topics.ToArray(), capacity, cancellationToken);

                _logger.LogInformation("Got {externalTaskCount} external tasks to execute: {@tasks}", tasks.Count(), tasks.Select(t => new { TaskId = t.Id, Topic = t.TopicName }));

                foreach (var task in tasks)
                {
                    await _channel.Write(task, cancellationToken);
                }
            }

            await Task.Delay(100, cancellationToken);
        }
        catch (Exception ex)
        {
            if (cancellationToken.IsCancellationRequested && ex is TaskCanceledException) return;
            if (cancellationToken.IsCancellationRequested && ex is OperationCanceledException) return;

            _logger.LogError(ex, "An unexpected error occurred while fetching external tasks from Camunda for worker '{workerId}'. " +
                "Error message: {errorMessage}", _options.WorkerId, ex.Message);
        }
    }

    private async Task<IEnumerable<LockedExternalTaskDto>> GetExternalTasksToExecute(FetchExternalTaskTopicDto[] topics, int maxTasks, CancellationToken cancellationToken)
    {
        var request = new FetchExternalTasksDto
        {
            WorkerId = _options.WorkerId,
            MaxTasks = maxTasks,
            UsePriority = _options.ProcessTasksBasedOnPriority,
            AsyncResponseTimeout = _options.ResponseTimeoutInSeconds * 1000,
            Topics = topics
        };

        _logger.LogInformation("Using options {@options}", new { request.WorkerId, request.MaxTasks, request.UsePriority, request.AsyncResponseTimeout });
        var response = await _client.FetchAndLock(request, cancellationToken);
        return response ?? Enumerable.Empty<LockedExternalTaskDto>();
    }
}
