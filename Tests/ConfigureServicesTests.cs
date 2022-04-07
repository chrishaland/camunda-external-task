using Haland.CamundaExternalTask.DependencyInjection;
using Microsoft.AspNetCore.Mvc.Testing;
#if !NET6_0_OR_GREATER
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Hosting;
#endif

namespace Tests;

[TestFixture]
public partial class ConfigureServicesTests
{
    [Test]
    public void Ensure_host_can_register_camunda_external_task_and_start_up_successfully()
    {
        Assert.DoesNotThrow(() =>
        {
            _application = new();
        });

        if (_application == null) throw new Exception("Application should not be null here");

        var serviceProvider = _application.Services;

        using var scope = serviceProvider.CreateScope();
        var externalTaskHandlers = scope.ServiceProvider.GetRequiredService<IEnumerable<IExternalTaskHandler>>().ToArray();

        Assert.Multiple(() =>
        {
            Assert.That(externalTaskHandlers.Length, Is.EqualTo(3));
            Assert.That(externalTaskHandlers[0].Topic, Is.EqualTo("test1"));
            Assert.That(externalTaskHandlers[1].Topic, Is.EqualTo("test2"));
            Assert.That(externalTaskHandlers[2].Topic, Is.EqualTo("test3"));
        });

        _application.Dispose();
    }

    [Test]
    public void Should_fail_registration_with_invalid_worker_id()
    {
        var services = new ServiceCollection();
        Assert.Throws<ApplicationException>(() => services.AddCamunda(options =>
        {
            options.WorkerId = string.Empty;
            options.Uri = "http://localhost/";
        }));
    }

    [TestCase("", Description = "Uri can't be empty")]
    [TestCase("invalid_uri", Description = "Uri can't be an invalid url")]
    [TestCase("http://localhost", Description = "Uri must end with '/'")]
    public void Should_fail_registration_with_invalid_uri(string uri)
    {
        var services = new ServiceCollection();
        Assert.Throws<ApplicationException>(() => services.AddCamunda(options =>
        {
            options.WorkerId = "worker";
            options.Uri = uri;
        }));
    }
}

#if NET6_0_OR_GREATER
public partial class ConfigureServicesTests
{
    WebApplicationFactory<Program>? _application = null;
}
#else
public partial class ConfigureServicesTests
{
    TestWebApplicationFactory<Startup>? _application = null;

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
