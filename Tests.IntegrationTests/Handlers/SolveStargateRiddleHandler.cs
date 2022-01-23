namespace Tests.IntegrationTests.Handlers;

public class SolveStargateRiddleHandler : ExternalTaskHandler
{
    public override string Topic => "stargate";

    public override async Task<ExternalTaskResult> Execute(ExternalTask externalTask, CancellationToken ct)
    {
        await Task.CompletedTask;
        return new ExternalTaskCompleteResult(new Dictionary<string, JToken>
        {
            { "point_of_origin", new JValue("Å") }
        });
    }
}
