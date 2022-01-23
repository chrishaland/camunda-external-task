using Microsoft.Extensions.Logging;

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

    protected override async Task ExecuteAsync(CancellationToken ct)
    {
        // Release the background service from blocking the host startup process
        await Task.Yield();

        while (!ct.IsCancellationRequested)
        {
            try
            {
                await _externalTaskManager.Execute(ct);
                await Task.Delay(100, ct);
            }
            catch (Exception ex) 
            {
                _logger.LogError(ex, "An unexpected error occurred while processing Camunda external tasks for worker {workerId}. " +
                    "Error message: {errorMessage}", _options.WorkerId, ex.Message);
            }
        }
    }
}
