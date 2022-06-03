using Samples.Models;

namespace Tests.IntegrationTests.Handlers;

public class SolveStargateRiddleHandler : ExternalTaskHandler
{
    public override string Topic => "stargate";

    public override async Task<ExternalTaskResult> Execute(ExternalTask externalTask, CancellationToken ct)
    {
        await Task.CompletedTask;

        _ = externalTask.Variables["local_input"].String;
        _ = externalTask.Variables["contact"].Boolean;
        _ = externalTask.Variables["boolean"].Boolean;
        _ = externalTask.Variables["bytes"].Bytes;
        _ = externalTask.Variables["short"].Short;
        _ = externalTask.Variables["integer"].Integer;
        _ = externalTask.Variables["long"].Long;
        _ = externalTask.Variables["double"].Double;
        _ = externalTask.Variables["date"].Date;
        _ = externalTask.Variables["string"].String;
        _ = externalTask.Variables["file"].File;
        _ = externalTask.Variables["null"].String;
        _ = externalTask.Variables["object"].As<DataDto>();
        _ = externalTask.Variables["array"].As<DataDto[]>();

        var xmlValueSerialized = externalTask.Variables["xml"]?.String;
        _ = xmlValueSerialized == null ? null : XDocument.Parse(xmlValueSerialized);

        return new ExternalTaskCompleteResult(
            Variables: new Dictionary<string, Variable>
            {
                { "point_of_origin", Variable.From("Å") }
            },
            LocalVariables: new Dictionary<string, Variable>
            {
                { "local_string", Variable.From("Å") }
            }
        );
    }
}
