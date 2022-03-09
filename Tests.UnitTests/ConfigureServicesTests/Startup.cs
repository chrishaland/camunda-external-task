#if NETCOREAPP3_1 || NET5_0
namespace Tests.UnitTests.ConfigureServicesTests;

public class Startup
{
    public void ConfigureServices(IServiceCollection services)
    {
        var camunda = services.AddCamunda(options =>
        {
            options.WorkerId = "worker";
            options.Uri = "http://localhost:8080/rest-engine/";
        });

        camunda
            .AddExternalTask<Test1TaskHandler>()
            .AddExternalTask<Test2TaskHandler>()
            .AddExternalTask<Test3TaskHandler>()
            .ConfigurePrimaryHttpMessageHandler(_ => new Mock<HttpMessageHandler>().Object)
        ;
    }

    public void Configure(IApplicationBuilder app)
    {
    }
}
#endif
