namespace Haland.CamundaExternalTask.DependencyInjection;

public interface ICamundaBuilder : IHttpClientBuilder { }

internal class DefaultCamundaBuilder : ICamundaBuilder
{
    public DefaultCamundaBuilder(string name, IServiceCollection services)
    {
        Name = name;
        Services = services;
    }

    public string Name { get; }

    public IServiceCollection Services { get; }
}
