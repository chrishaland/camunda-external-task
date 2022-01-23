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
            .ConfigurePrimaryHttpMessageHandler(_ => new Mock<HttpMessageHandler>().Object)
        ;
    }

    public void Configure(IApplicationBuilder app)
    {
        app.Map("/ok", app => app.Run(async context =>
        {
            await context.Response.WriteAsync("ok");
        }));
    }
}
#endif
