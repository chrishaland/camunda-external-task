#if NET6_0_OR_GREATER
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
    .ConfigurePrimaryHttpMessageHandler(_ => new Mock<HttpMessageHandler>().Object)
;

var app = builder.Build();
app.Run();

#pragma warning disable CA1050 // Declare types in namespaces
public partial class Program { }
#pragma warning restore CA1050 // Declare types in namespaces
#endif
