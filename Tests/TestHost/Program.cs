using Haland.CamundaExternalTask.DependencyInjection;

var builder = WebApplication.CreateBuilder(args);

var camunda = builder.Services.AddCamunda(options =>
{
    options.WorkerId = "worker";
    options.Uri = "http://localhost:8080/rest-engine/";
});

camunda
    .AddExternalTask<Test1TaskHandler>()
    .AddExternalTask<Test2TaskHandler>()
    .AddExternalTask<Test3TaskHandler>()
    .ConfigurePrimaryHttpMessageHandler(_ => 
    {
        var mock = Substitute.For<HttpMessageHandler>();
        mock.GetType()?
            .GetMethod("SendAsync", BindingFlags.NonPublic | BindingFlags.Instance)?
            .Invoke(mock, [Arg.Any<HttpRequestMessage>(), Arg.Any<CancellationToken>()])
            .Returns(Task.FromResult(new HttpResponseMessage { StatusCode = HttpStatusCode.OK, Content = new StringContent("[]") }));
        
        return mock;        
    })
;

var app = builder.Build();
app.Run();

[ExcludeFromCodeCoverage]
public partial class Program { }
