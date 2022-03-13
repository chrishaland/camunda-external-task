using System.Diagnostics.CodeAnalysis;
using Newtonsoft.Json.Linq;

namespace Tests.UnitTests.ConfigureServicesTests;

[ExcludeFromCodeCoverage]
public class Test1TaskHandler : ExternalTaskHandler
{
    public override string Topic => "test1";

    public override async Task<ExternalTaskResult> Execute(ExternalTask externalTask, CancellationToken ct)
    {
        await Task.CompletedTask;
        return new ExternalTaskCompleteResult(new Dictionary<string, Variable>
        {
            {
                "var", 
                new Variable(
                    Value: new JValue("value"), 
                    ValueInfo: new ValueInfo(
                        ObjectTypeName: "",
                        SerializationDataFormat: "",
                        FileName: "file.pdf",
                        MimeType: "application/pdf",
                        Encoding: "utf-8"
                    )
                ) 
            }
        });
    }
}
