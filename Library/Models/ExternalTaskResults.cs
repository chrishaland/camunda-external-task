namespace Haland.CamundaExternalTask;

public abstract record ExternalTaskResult;

public record ExternalTaskBpmnErrorResult(string ErrorCode, string ErrorMessage, IDictionary<string, Variable>? Variables = null)
    : ExternalTaskResult;

public record ExternalTaskFailureResult(string ErrorMessage, string ErrorDetails, IDictionary<string, Variable>? Variables = null, IDictionary<string, Variable>? LocalVariables = null)
    : ExternalTaskResult;

public record ExternalTaskCompleteResult(IDictionary<string, Variable>? Variables = null, IDictionary<string, Variable>? LocalVariables = null)
    : ExternalTaskResult;
