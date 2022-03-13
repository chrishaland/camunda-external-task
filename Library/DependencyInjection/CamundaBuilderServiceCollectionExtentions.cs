namespace Haland.CamundaExternalTask;

public static class CamundaBuilderServiceCollectionExtentions
{
    public static ICamundaBuilder AddCamunda(this IServiceCollection services, Action<CamundaOptions> configure)
    {
        var options = new CamundaOptions();
        configure(options);

        if (string.IsNullOrEmpty(options.WorkerId))
        {
            throw new ApplicationException($"Missing or invalid value provided for '{nameof(CamundaOptions.WorkerId)}'");
        }

        if (string.IsNullOrEmpty(options.Uri) || !Uri.TryCreate(options.Uri, UriKind.Absolute, out _) || !options.Uri.EndsWith('/'))
        {
            throw new ApplicationException($"Missing or invalid value provided for '{nameof(CamundaOptions.Uri)}' (must be valid URL and end with '/'");
        }

        var httpClientBuilder = services.AddHttpClient<IExternalTaskClient, ExternalTaskClient>(_ =>
        {
            _.BaseAddress = new Uri(options.Uri);
            _.Timeout = TimeSpan.FromSeconds(options.ResponseTimeoutInSeconds + 1);
        });

        services.AddSingleton(options);
        services.AddSingleton<ExternalTaskManager>();
        services.AddHostedService<CamundaHostedService>();

        return new DefaultCamundaBuilder(httpClientBuilder.Name, httpClientBuilder.Services);
    }

    public static ICamundaBuilder AddExternalTask<T>(this ICamundaBuilder builder) where T : ExternalTaskHandler
    {
        builder.Services.AddScoped<IExternalTaskHandler, T>();
        return builder;
    }
}

public class CamundaOptions
{
    /// <summary>
    /// An unique id for this external task worker
    /// </summary>
    public virtual string WorkerId { get; set; } = string.Empty;

    /// <summary>
    /// The uri for the Camunda REST Engine
    /// </summary>
    public virtual string Uri { get; set; } = string.Empty;

    /// <summary>
    /// Time to wait for external tasks from Camunda. 
    /// <br/>Default: 30
    /// </summary>
    public virtual int ResponseTimeoutInSeconds { get; set; } = 30;

    /// <summary>
    /// Maximum number of tasks to process at a time. 
    /// <br/>Default: 100
    /// </summary>
    public virtual int MaximumTasks { get; set; } = 100;

    /// <summary>
    /// Should Camunda return tasks based on their priority? 
    /// <br/>Default: true
    /// </summary>
    public virtual bool ProcessTasksBasedOnPriority { get; set; } = true;
}
