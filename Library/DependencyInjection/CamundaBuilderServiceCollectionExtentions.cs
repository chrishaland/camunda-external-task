﻿namespace Haland.CamundaExternalTask.DependencyInjection;

public static class CamundaBuilderServiceCollectionExtentions
{
    public static ICamundaBuilder AddCamunda(this IServiceCollection services, Action<CamundaOptions> configure)
    {
        var options = new CamundaOptions();
        configure(options);

        if (!string.IsNullOrEmpty(options.Uri) && !options.Uri.EndsWith('/'))
        {
            options.Uri = $"{options.Uri}/";
        }

        if (string.IsNullOrEmpty(options.WorkerId))
        {
            throw new ApplicationException($"Missing or invalid value provided for '{nameof(CamundaOptions.WorkerId)}'");
        }

        if (string.IsNullOrEmpty(options.Uri) || !Uri.TryCreate(options.Uri, UriKind.Absolute, out _))
        {
            throw new ApplicationException($"Missing or invalid value provided for '{nameof(CamundaOptions.Uri)}'");
        }

        if (options.MaximumTasks <= 0)
        {
            throw new ApplicationException($"Invalid value provided for '{nameof(CamundaOptions.MaximumTasks)}'");
        }

        if (options.ResponseTimeoutInSeconds > 1800)
        {
            throw new ApplicationException($"Invalid value provided for '{nameof(CamundaOptions.ResponseTimeoutInSeconds)}'");
        }

        var httpClientBuilder = services.AddHttpClient<IExternalTaskClient, ExternalTaskClient>(_ =>
        {
            _.BaseAddress = new Uri(options.Uri);
            _.Timeout = TimeSpan.FromSeconds(options.ResponseTimeoutInSeconds + 1);
        });

        services.AddSingleton(options);
        services.AddSingleton<IChannel>(new Channel(options.MaximumTasks));

        services.AddHostedService<FetcherService>();
        
        for (var i = 0; i < options.MaximumTasks; i++)
        {
            services.AddSingleton<IHostedService, ManagerService>();
        }

        return new DefaultCamundaBuilder(httpClientBuilder.Name, httpClientBuilder.Services);
    }

    public static ICamundaBuilder AddExternalTask<T>(this ICamundaBuilder builder) where T : ExternalTaskHandler
    {
        builder.Services.AddScoped<T>();
        builder.Services.AddSingleton<IExternalTaskHandler, T>();
        return builder;
    }
}
