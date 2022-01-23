using Microsoft.AspNetCore.Mvc.Testing;

namespace Tests.UnitTests.ConfigureServicesTests;

#if NET6_0_OR_GREATER
[TestFixture]
public class ConfigureServicesTests
{
    [Test]
    public void Ensure_host_can_register_camunda_external_task_and_start_up_successfully()
    {
        WebApplicationFactory<Program>? application = null;

        Assert.DoesNotThrow(() =>
        {
            application = new WebApplicationFactory<Program>();
        });

        if (application == null) throw new Exception("Application should not be null here");

        var serviceProvider = application.Services;

        using var scope = serviceProvider.CreateScope();
        var externalTaskHandlers = scope.ServiceProvider.GetRequiredService<IEnumerable<IExternalTaskHandler>>();

        Assert.Multiple(() =>
        {
            Assert.That(externalTaskHandlers.Count(), Is.EqualTo(2));
            Assert.That(externalTaskHandlers.First().Topic, Is.EqualTo("test1"));
            Assert.That(externalTaskHandlers.Last().Topic, Is.EqualTo("test2"));
        });

        application.Dispose();
    }
}
#else
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Hosting;

[TestFixture]
public class ConfigureServicesTests
{
    [Test]
    public void Ensure_host_can_register_camunda_external_task_and_start_up_successfully()
    {
        TestWebApplicationFactory<Startup>? application = null;

        Assert.DoesNotThrow(() =>
        {
            application = new TestWebApplicationFactory<Startup>();
        });

        if (application == null) throw new Exception("Application should not be null here");

        var serviceProvider = application.Services;

        using var scope = serviceProvider.CreateScope();
        var externalTaskHandlers = scope.ServiceProvider.GetRequiredService<IEnumerable<IExternalTaskHandler>>();

        Assert.Multiple(() =>
        {
            Assert.That(externalTaskHandlers.Count(), Is.EqualTo(2));
            Assert.That(externalTaskHandlers.First().Topic, Is.EqualTo("test1"));
            Assert.That(externalTaskHandlers.Last().Topic, Is.EqualTo("test2"));
        });

        application.Dispose();
    }

    public class TestWebApplicationFactory<TEntryPoint> : WebApplicationFactory<TEntryPoint> where TEntryPoint : class
    {
        protected override IHostBuilder? CreateHostBuilder() => Host
        .CreateDefaultBuilder()
        .ConfigureWebHostDefaults(webBuilder => webBuilder
            .UseStartup<Startup>()
        );
    }
}
#endif
