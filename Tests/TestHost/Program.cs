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
        var mock = new Mock<HttpMessageHandler>();
        mock.Protected()
            .Setup<Task<HttpResponseMessage>>("SendAsync", ItExpr.IsAny<HttpRequestMessage>(), ItExpr.IsAny<CancellationToken>())
            .ReturnsAsync(new HttpResponseMessage { StatusCode = HttpStatusCode.OK, Content = new StringContent("[]") });
        return mock.Object;        
    })
;

var app = builder.Build();
app.Run();

[ExcludeFromCodeCoverage]
public partial class Program { }
