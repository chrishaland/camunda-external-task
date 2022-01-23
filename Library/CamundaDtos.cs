namespace Haland.CamundaExternalTask;

public record ExternalTask(Guid Id, string WorkerId, IDictionary<string, JToken> Variables);

public abstract record ExternalTaskResult;

public record ExternalTaskBpmnErrorResult(string ErrorCode, string ErrorMessage)
    : ExternalTaskResult;

public record ExternalTaskFailureResult(string ErrorMessage, string ErrorDetails)
    : ExternalTaskResult;

public record ExternalTaskCompleteResult(IDictionary<string, JToken>? Variables = null)
    : ExternalTaskResult;


