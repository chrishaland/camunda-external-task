﻿using Haland.CamundaExternalTask.DependencyInjection;
using Polly;
using Polly.Extensions.Http;
using System.Net.Http.Headers;
using System.Text;

var builder = WebApplication.CreateBuilder(args);
var retryPolicyTimeoutInSeconds = TotalRetryTime(NumberOfRetries);

builder.Logging
    .AddFilter("Default", LogLevel.Information)
    .AddFilter("System", LogLevel.Warning)
    .AddFilter("Microsoft", LogLevel.Warning)
    .AddFilter("Microsoft.Hosting.Lifetime", LogLevel.Information)
;

var camunda = builder.Services.AddCamunda(options =>
{
    options.WorkerId = "worker";
    options.MaximumTasks = 10;
    options.Uri = "http://localhost:8080/engine-rest/";
    options.ResponseTimeoutInSeconds = ResponseTimeoutInSeconds;
});

camunda
    .AddExternalTask<Tests.IntegrationTests.Handlers.GetRidiculedHandler>()
    .AddExternalTask<Tests.IntegrationTests.Handlers.StartTeachingHandler>()
    .AddExternalTask<Tests.IntegrationTests.Handlers.SolveStargateRiddleHandler>()
    .ConfigureHttpClient(client =>
    {
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Basic", 
            Convert.ToBase64String(Encoding.UTF8.GetBytes("demo:demo")));
    })
    .SetHandlerLifetime(TimeSpan.FromSeconds(retryPolicyTimeoutInSeconds + 1))
    .AddPolicyHandler(RetryPolicy)
;

var app = builder.Build();
app.Run();

#pragma warning disable CA1050 // Declare types in namespaces
public partial class Program 
{
    private const int NumberOfRetries = 6;
    private const int ResponseTimeoutInSeconds = 30; // Default

    private static IAsyncPolicy<HttpResponseMessage> RetryPolicy => HttpPolicyExtensions
        .HandleTransientHttpError()
        .WaitAndRetryAsync(
            retryCount: NumberOfRetries,
            sleepDurationProvider: retryAttempt => TimeSpan.FromSeconds(Math.Pow(2, retryAttempt))
        )
    ;

    private static double TotalRetryTime(int retryAttempts) => retryAttempts < 1 ? throw new ArgumentException("Argument must be greater than 0", nameof(retryAttempts)) :
        (retryAttempts > 1 ? TotalRetryTime(retryAttempts - 1) + Math.Pow(2, retryAttempts) : Math.Pow(2, retryAttempts));
}
#pragma warning restore CA1050 // Declare types in namespaces
