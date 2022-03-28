namespace Haland.CamundaExternalTask;

public record ExternalTask(Guid Id, string WorkerId, IDictionary<string, Variable> Variables);
