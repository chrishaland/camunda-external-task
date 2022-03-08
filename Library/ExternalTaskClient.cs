using System.Text;
using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;

namespace Haland.CamundaExternalTask;

internal interface IExternalTaskClient
{
    Task<LockedExternalTaskDto[]> FetchAndLock(FetchExternalTasksDto dto, CancellationToken cancellationToken);
    Task Fail(Guid id, FailExternalTaskDto dto, CancellationToken cancellationToken);
    Task Complete(Guid id, CompleteExternalTaskDto dto, CancellationToken cancellationToken);
    Task BpmnError(Guid id, BpmnErrorExternalTaskDto dto, CancellationToken cancellationToken);
}

internal class ExternalTaskClient : IExternalTaskClient
{
    private readonly HttpClient _client;
    private readonly JsonSerializerSettings _serializerSettings;

    public ExternalTaskClient(HttpClient client)
    {
        _client = client;
        _serializerSettings = new JsonSerializerSettings
        {
            Formatting = Formatting.Indented,
            //RFC 3339 https://datatracker.ietf.org/doc/html/rfc3339
            //DateFormatString = "yyyy-MM-dd'T'HH:mm:ss.fffK",
            DateFormatString = "yyyy-MM-dd'T'HH:mm:ss.fff'UTC'",
            DateTimeZoneHandling = DateTimeZoneHandling.Utc,
            ContractResolver = new CamelCasePropertyNamesContractResolver()
        };
    }

    public Task BpmnError(Guid id, BpmnErrorExternalTaskDto dto, CancellationToken cancellationToken)
    {
        return Post($"external-task/{id}/bpmnError", dto, cancellationToken);
    }

    public Task Complete(Guid id, CompleteExternalTaskDto dto, CancellationToken cancellationToken)
    {
        return Post($"external-task/{id}/complete", dto, cancellationToken);
    }

    public Task Fail(Guid id, FailExternalTaskDto dto, CancellationToken cancellationToken)
    {
        return Post($"external-task/{id}/failure", dto, cancellationToken);
    }

    public async Task<LockedExternalTaskDto[]> FetchAndLock(FetchExternalTasksDto dto, CancellationToken cancellationToken)
    {
        var result = await Post<FetchExternalTasksDto, LockedExternalTaskDto[]>(
            requestUri: "external-task/fetchAndLock",
            dto: dto,
            cancellationToken: cancellationToken);

        return result ?? Array.Empty<LockedExternalTaskDto>();
    }

    private async Task<TResponse?> Post<TRequest, TResponse>(string requestUri, TRequest dto, CancellationToken cancellationToken)
    {
        var content = await Post(requestUri, dto, cancellationToken);
        var result = JsonConvert.DeserializeObject<TResponse>(content);
        return result;
    }

    private async Task<string> Post<TRequest>(string requestUri, TRequest dto, CancellationToken cancellationToken)
    {
        var request = new HttpRequestMessage(HttpMethod.Post, requestUri);

        var json = JsonConvert.SerializeObject(dto, _serializerSettings);
        request.Content = new StringContent(json, Encoding.UTF8, "application/json");

        var response = await _client.SendAsync(request, cancellationToken);
#if NETCOREAPP3_1
        var content = await response.Content.ReadAsStringAsync();
#else
        var content = await response.Content.ReadAsStringAsync(cancellationToken);
#endif
        
        if (!response.IsSuccessStatusCode)
        {
#if NETCOREAPP3_1
            throw new HttpRequestException($"Unsuccessful HTTP call to '{requestUri}'. Status code: {response.StatusCode}. Request: {json}. Response: '{content}'");
#else
            throw new HttpRequestException($"Unsuccessful HTTP call to '{requestUri}'. Status code: {response.StatusCode}. Request: {json}. Response: '{content}'", null, response.StatusCode);
#endif
        }
        
        return content;
    }
}

internal class FetchExternalTasksDto
{
    /// <summary>
    /// Mandatory. The id of the worker on which behalf tasks are fetched. The returned tasks are locked for that worker and can only be completed when providing the same worker id.
    /// </summary>
    public string WorkerId { get; set; } = default!;

    /// <summary>
    /// Mandatory. The maximum number of tasks to return.
    /// </summary>
    public int MaxTasks { get; set; } = 0;

    /// <summary>
    /// A boolean value, which indicates whether the task should be fetched based on its priority or arbitrarily.
    /// <br/>Default: false
    /// </summary>
    public bool UsePriority { get; set; } = false;

    /// <summary>
    /// The Long Polling timeout in milliseconds.
    /// <br/>Note: The value cannot be set larger than 1.800.000 milliseconds(corresponds to 30 minutes).
    /// <br/>https://docs.camunda.org/manual/7.16/user-guide/process-engine/external-tasks/#long-polling-to-fetch-and-lock-external-tasks
    /// </summary>
    public long AsyncResponseTimeout { get; set; } = 0;

    /// <summary>
    /// A JSON array of topic objects for which external tasks should be fetched. The returned tasks may be arbitrarily distributed among these topics.
    /// </summary>
    public FetchExternalTaskTopicDto[] Topics { get; set; } = Array.Empty<FetchExternalTaskTopicDto>();
}

internal class FetchExternalTaskTopicDto
{
    /// <summary>
    /// Mandatory. The topic's name.
    /// </summary>
    public string TopicName { get; set; } = default!;

    /// <summary>
    /// Mandatory. The duration to lock the external tasks for in milliseconds.
    /// </summary>
    public long LockDuration { get; set; }

    /// <summary>
    /// A JSON array of String values that represent variable names. For each result task belonging to this topic, the given variables are returned as well if they are accessible from the external task's execution. If not provided - all variables will be fetched.
    /// </summary>
    public string[]? Variables { get; set; }
}

internal class LockedExternalTaskDto
{
    /// <summary>
    /// The id of the external task.
    /// </summary>
    public Guid Id { get; set; }

    /// <summary>
    /// The topic name of the external task.
    /// </summary>
    public string TopicName { get; set; } = default!;

    /// <summary>
    /// The id of the worker that posesses or posessed the most recent lock.
    /// </summary>
    public string WorkerId { get; set; } = default!;

    /// <summary>
    /// A JSON object containing a property for each of the requested variables.
    /// </summary>
    public IDictionary<string, VariableDto> Variables { get; set; } = new Dictionary<string, VariableDto>();
}

internal class FailExternalTaskDto
{
    /// <summary>
    /// Mandatory. The ID of the worker who is performing the operation on the external task. If the task is already locked, must match the id of the worker who has most recently locked the task.
    /// </summary>
    public string WorkerId { get; set; } = default!;

    /// <summary>
    /// An message indicating the reason of the failure.
    /// </summary>
    public string ErrorMessage { get; set; } = default!;

    /// <summary>
    /// A detailed error description.
    /// </summary>
    public string ErrorDetails { get; set; } = default!;
}

internal class CompleteExternalTaskDto 
{
    /// <summary>
    /// Mandatory. The ID of the worker who is performing the operation on the external task. If the task is already locked, must match the id of the worker who has most recently locked the task.
    /// </summary>
    public string WorkerId { get; set; } = default!;

    /// <summary>
    /// A JSON object containing variable key-value pairs.
    /// </summary>
    public IDictionary<string, VariableDto>? Variables { get; set; }
}

internal class BpmnErrorExternalTaskDto
{
    /// <summary>
    /// The id of the worker that reports the failure. Must match the id of the worker who has most recently locked the task.
    /// </summary>
    public string WorkerId { get; set; } = default!;

    /// <summary>
    /// An error code that indicates the predefined error. It is used to identify the BPMN error handler.
    /// </summary>
    public string ErrorCode { get; set; } = default!;

    /// <summary>
    /// An error message that describes the error.
    /// </summary>
    public string ErrorMessage { get; set; } = default!;
}

internal class VariableDto
{
    public VariableDto(JToken value, ValueInfoDto? valueInfo = null)
    {
        Value = value;
        ValueInfo = valueInfo;
    }

    public JToken Value { get; set; }
    public string Type { get => Value.Type.ToString(); }
    public ValueInfoDto? ValueInfo { get; set; }
}

internal class ValueInfoDto
{
    public string? ObjectTypeName { get; set; }
    public string? SerializationDataFormat { get; set; }
    [JsonProperty("filename")]
    public string? FileName { get; set; }
    [JsonProperty("mimetype")]
    public string? MimeType { get; set; }
    public string? Encoding { get; set; }
}
