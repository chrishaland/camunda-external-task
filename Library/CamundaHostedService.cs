using Microsoft.Extensions.Logging;
using Haland.CamundaExternalTask.DependencyInjection;

namespace Haland.CamundaExternalTask;

internal class CamundaHostedService : BackgroundService
{
    private readonly CamundaOptions _options;
    private readonly ILogger<CamundaHostedService> _logger;
    private readonly ExternalTaskManager _externalTaskManager;

    public CamundaHostedService(
        CamundaOptions options, 
        ILogger<CamundaHostedService> logger, 
        ExternalTaskManager externalTaskManager
    )
    {
        _logger = logger;
        _options = options;
        _externalTaskManager = externalTaskManager;
    }

    protected override async Task ExecuteAsync(CancellationToken cancellationToken)
    {
        while (!cancellationToken.IsCancellationRequested)
        {
            try
            {
                await _externalTaskManager.Execute(cancellationToken);
                await Task.Delay(100, cancellationToken);
            }
            catch (Exception ex) 
            {
                if (cancellationToken.IsCancellationRequested && ex is TaskCanceledException) return;

                _logger.LogError(ex, "An unexpected error occurred while processing Camunda external tasks for worker '{workerId}'. " +
                    "Error message: {errorMessage}", _options.WorkerId, ex.Message);
            }
        }
    }
}
