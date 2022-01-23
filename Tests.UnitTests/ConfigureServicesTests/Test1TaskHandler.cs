namespace Tests.UnitTests.ConfigureServicesTests;

public class Test1TaskHandler : ExternalTaskHandler
{
    public override string Topic => "test1";

    public override async Task<ExternalTaskResult> Execute(ExternalTask externalTask, CancellationToken ct)
    {
        await Task.CompletedTask;
        return new ExternalTaskCompleteResult();
    }
}
