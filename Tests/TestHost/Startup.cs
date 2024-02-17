#if NET5_0
using Haland.CamundaExternalTask.DependencyInjection;

namespace Tests.TestHost;

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
            .ConfigurePrimaryHttpMessageHandler(_ => 
            {
                var mock = new Mock<HttpMessageHandler>();
                mock.Protected()
                    .Setup<Task<HttpResponseMessage>>("SendAsync", ItExpr.IsAny<HttpRequestMessage>(), ItExpr.IsAny<CancellationToken>())
                    .ReturnsAsync(new HttpResponseMessage { StatusCode = HttpStatusCode.OK, Content = new StringContent("[]") });
                return mock.Object;        
            })
        ;
    }

    public void Configure(IApplicationBuilder app)
    {
    }
}
#endif
