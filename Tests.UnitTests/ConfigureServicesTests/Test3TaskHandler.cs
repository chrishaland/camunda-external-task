namespace Tests.UnitTests.ConfigureServicesTests;

public class Test3TaskHandler : ExternalTaskHandler
{
    public override string Topic => "test3";

    public override async Task<ExternalTaskResult> Execute(ExternalTask externalTask, CancellationToken ct)
    {
        await Task.CompletedTask;
        return new ExternalTaskBpmnErrorResult(
            ErrorCode: "",
            ErrorMessage: ""
        );
    }
}
