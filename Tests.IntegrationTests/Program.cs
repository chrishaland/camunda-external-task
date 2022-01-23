using System.Net.Http.Headers;
using System.Text;

var builder = WebApplication.CreateBuilder(args);

var camunda = builder.Services.AddCamunda(options =>
{
    options.WorkerId = "worker";
    options.Uri = "http://localhost:8080/engine-rest/";
});

camunda
    .AddExternalTask<Tests.IntegrationTests.Handlers.GetRidiculedHandler>()
    .AddExternalTask<Tests.IntegrationTests.Handlers.StartTeachingHandler>()
    .AddExternalTask<Tests.IntegrationTests.Handlers.SolveStargateRiddleHandler>()
    .ConfigureHttpClient(client =>
    {
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Basic", 
            Convert.ToBase64String(Encoding.UTF8.GetBytes("demo:demo")));
    })
;

var app = builder.Build();
app.Run();

#pragma warning disable CA1050 // Declare types in namespaces
public partial class Program { }
#pragma warning restore CA1050 // Declare types in namespaces
