namespace Tests.IntegrationTests.Handlers;

public class GetRidiculedHandler : ExternalTaskHandler
{
    private readonly ILogger<GetRidiculedHandler> _logger;

    public GetRidiculedHandler(ILogger<GetRidiculedHandler> logger)
    {
        _logger = logger;
    }

    public override string Topic => "ridiculed";

    public override async Task<ExternalTaskResult> Execute(ExternalTask externalTask, CancellationToken ct)
    {
        await Task.CompletedTask;
        var surname = externalTask.Variables["Surname"];
        _logger.LogInformation("The process instances has a variable '{VariableName}' with value '{VariableValue}'", "Surname", surname);

        return new ExternalTaskCompleteResult(new Dictionary<string, JToken>
        {
            { "contact", new JValue(true) }
        });
    }
}
