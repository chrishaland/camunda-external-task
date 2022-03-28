namespace Haland.CamundaExternalTask;

public abstract record ExternalTaskResult;

public record ExternalTaskBpmnErrorResult(string ErrorCode, string ErrorMessage)
    : ExternalTaskResult;

public record ExternalTaskFailureResult(string ErrorMessage, string ErrorDetails)
    : ExternalTaskResult;

public record ExternalTaskCompleteResult(IDictionary<string, Variable>? Variables = null)
    : ExternalTaskResult;
