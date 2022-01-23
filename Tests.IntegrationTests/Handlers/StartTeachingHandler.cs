namespace Tests.IntegrationTests.Handlers;

public class StartTeachingHandler : ExternalTaskHandler
{
    public override string Topic => "teacher";

    public override async Task<ExternalTaskResult> Execute(ExternalTask externalTask, CancellationToken ct)
    {
        await Task.CompletedTask;
        return new ExternalTaskCompleteResult(new Dictionary<string, JToken>
        {
            { "started_teaching", new JValue(true) }
        });
    }
}
