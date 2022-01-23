namespace Tests.UnitTests;

public static class TestServiceCollection
{
    public static IServiceProvider CreateServiceProvider(Action<IServiceCollection> serviceConfiguration)
    {
        var services = new ServiceCollection();
        serviceConfiguration(services);

        return services.BuildServiceProvider();
    }
}
