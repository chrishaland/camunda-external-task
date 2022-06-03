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

        var objectValueSerialized = externalTask.Variables["object"]?.String;
        _ = objectValueSerialized == null ? null : JsonConvert.DeserializeObject(objectValueSerialized);

        var arrayValueSerialized = externalTask.Variables["array"]?.String;
        _ = arrayValueSerialized == null ? null : JsonConvert.DeserializeObject(arrayValueSerialized);

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
