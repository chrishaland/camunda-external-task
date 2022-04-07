namespace Tests.TestHost;

[ExcludeFromCodeCoverage]
public class Test2TaskHandler : ExternalTaskHandler
{
    public override string Topic => "test2";

    public override async Task<ExternalTaskResult> Execute(ExternalTask externalTask, CancellationToken ct)
    {
        await Task.CompletedTask;
        return new ExternalTaskFailureResult(
            ErrorMessage: "",
            ErrorDetails: ""
        );
    }
}
