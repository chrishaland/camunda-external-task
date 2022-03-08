namespace Haland.CamundaExternalTask;
/*
 
    public string? ObjectTypeName { get; set; }
    public string? SerializationDataFormat { get; set; }
    [JsonProperty("filename")]
    public string? FileName { get; set; }
    [JsonProperty("mimetype")]
    public string? MimeType { get; set; }
    public string? Encoding { get; set; }
 */
public record ValueInfo(string? ObjectTypeName, string? SerializationDataFormat, string? FileName, string? MimeType, string? Encoding);

public record Variable(JToken Value, ValueInfo? ValueInfo = null);

public record ExternalTask(Guid Id, string WorkerId, IDictionary<string, Variable> Variables);

public abstract record ExternalTaskResult;

public record ExternalTaskBpmnErrorResult(string ErrorCode, string ErrorMessage)
    : ExternalTaskResult;

public record ExternalTaskFailureResult(string ErrorMessage, string ErrorDetails)
    : ExternalTaskResult;

public record ExternalTaskCompleteResult(IDictionary<string, Variable>? Variables = null)
    : ExternalTaskResult;


