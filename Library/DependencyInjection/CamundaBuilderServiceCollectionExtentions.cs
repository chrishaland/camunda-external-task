using Polly;
using Polly.Extensions.Http;

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

        var retryPolicyTimeoutInSeconds = 0.0;
        if (options.DefaultRetryPolicyNumberOfRetries > 0)
        {
            retryPolicyTimeoutInSeconds = TotalRetryTime(options.DefaultRetryPolicyNumberOfRetries);
        }

        var httpClientBuilder = services.AddHttpClient<IExternalTaskClient, ExternalTaskClient>(_ =>
        {
            _.BaseAddress = new Uri(options.Uri);
            _.Timeout = TimeSpan.FromSeconds(Math.Max(options.ResponseTimeoutInSeconds, retryPolicyTimeoutInSeconds) + 1);
        });

        httpClientBuilder = httpClientBuilder.SetHandlerLifetime(TimeSpan.FromSeconds(retryPolicyTimeoutInSeconds + 1));
        httpClientBuilder = httpClientBuilder.AddPolicyHandler(RetryPolicy(options));

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

    private static IAsyncPolicy<HttpResponseMessage> RetryPolicy(CamundaOptions options) => HttpPolicyExtensions
        .HandleTransientHttpError()
        .WaitAndRetryAsync(
            retryCount: options.DefaultRetryPolicyNumberOfRetries, 
            sleepDurationProvider: retryAttempt => TimeSpan.FromSeconds(Math.Pow(2, retryAttempt))
        )
    ;

    private static double TotalRetryTime(int retryAttempts) => retryAttempts < 1 ? throw new ArgumentException("Argument must be greater than 0", nameof(retryAttempts)) : 
        (retryAttempts > 1 ? TotalRetryTime(retryAttempts - 1) + Math.Pow(2, retryAttempts) : Math.Pow(2, retryAttempts));
}

public class CamundaOptions
{
    /// <summary>
    /// An unique id for this external task worker
    /// </summary>
    public string WorkerId { get; set; } = string.Empty;

    /// <summary>
    /// The uri for the Camunda REST Engine
    /// </summary>
    public string Uri { get; set; } = string.Empty;

    /// <summary>
    /// Time to wait for external tasks from Camunda. 
    /// <br/>Default: 30
    /// </summary>
    public int ResponseTimeoutInSeconds { get; set; } = 30;

    /// <summary>
    /// Maximum number of tasks to process at a time. 
    /// <br/>Default: 100
    /// </summary>
    public int MaximumTasks { get; set; } = 100;

    /// <summary>
    /// Should Camunda return tasks based on their priority? 
    /// <br/>Default: true
    /// </summary>
    public bool ProcessTasksBasedOnPriority { get; set; } = true;

    /// <summary>
    /// Number of retries to attempt for unsuccessful http requests in an exponential backoff policy
    /// <br/>Default: 6
    /// </summary>
    public int DefaultRetryPolicyNumberOfRetries { get; set; } = 6;
}
