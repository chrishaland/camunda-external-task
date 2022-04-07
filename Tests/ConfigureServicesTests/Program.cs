#if NET6_0_OR_GREATER
using Haland.CamundaExternalTask.DependencyInjection;

var builder = WebApplication.CreateBuilder(args);

var camunda = builder.Services.AddCamunda(options =>
{
    options.WorkerId = "worker";
    options.Uri = "http://localhost:8080/rest-engine/";
});

camunda
    .AddExternalTask<Tests.UnitTests.ConfigureServicesTests.Test1TaskHandler>()
    .AddExternalTask<Tests.UnitTests.ConfigureServicesTests.Test2TaskHandler>()
    .AddExternalTask<Tests.UnitTests.ConfigureServicesTests.Test3TaskHandler>()
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

#pragma warning disable CA1050 // Declare types in namespaces
[ExcludeFromCodeCoverage]
public partial class Program { }
#pragma warning restore CA1050 // Declare types in namespaces
#endif
