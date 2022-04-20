using System.Xml.Linq;
using Haland.CamundaExternalTask.DependencyInjection;
using Newtonsoft.Json.Linq;

namespace Tests;

[TestFixture]
public class ManagerServiceTests
{
    private static readonly Mock<IExternalTaskClient> _client = new();

    private static readonly Channel _channel = new(10);
    private static readonly CamundaOptions _options = new();

    private static readonly ILogger<ManagerService> _logger = new LoggerFactory()
        .CreateLogger<ManagerService>();

    private static readonly ICamundaBuilder _camundaBuilder = new DefaultCamundaBuilder(string.Empty, new ServiceCollection())
        .AddExternalTask<CompleteExternalTaskHandler>()
        .AddExternalTask<FailureExternalTaskHandler>()
        .AddExternalTask<BpmnErrorExternalTaskHandler>()
        .AddExternalTask<TimedOutExternalTaskHandler>()
        .AddExternalTask<ExceptionExternalTaskHandler>()
    ;

    private static readonly IServiceProvider _serviceProvider = _camundaBuilder.Services.BuildServiceProvider();

    private readonly ManagerService _sut = new(
        channel: _channel,
        options: _options,
        client: _client.Object,
        logger: _logger,
        handlers: _serviceProvider.GetRequiredService<IEnumerable<IExternalTaskHandler>>(),
        serviceProvider: _serviceProvider
    );

    [Test]
    public async Task Report_failure_if_there_is_no_handler_to_execute_task()
    {
        var id = Guid.NewGuid();

        await _channel.Write(new LockedExternalTaskDto
        {
            Id = id,
            TopicName = "other-topic",
            WorkerId = nameof(ManagerServiceTests)
        });
        await _sut.ExecuteExternalTask(CancellationToken.None);

        _client.Verify(m => m.Fail(id, It.Is<FailExternalTaskDto>(d => d.ErrorMessage.Equals("No handler found for topic 'other-topic'")), It.IsAny<CancellationToken>()), Times.Once());
    }

    [Test]
    public async Task Report_failure_if_the_task_execution_exceeds_its_lock_duration()
    {
        var id = Guid.NewGuid();

        await _channel.Write(new LockedExternalTaskDto
        {
            Id = id,
            TopicName = "timeout-topic",
            WorkerId = nameof(ManagerServiceTests)
        });
        await _sut.ExecuteExternalTask(CancellationToken.None);

        _client.Verify(m => m.Fail(id, It.Is<FailExternalTaskDto>(d => d.ErrorMessage.Equals("The task execution timed out")), It.IsAny<CancellationToken>()), Times.Once());
    }

    [Test]
    public async Task Report_failure_if_the_task_execution_throws_an_exception()
    {
        var id = Guid.NewGuid();

        await _channel.Write(new LockedExternalTaskDto
        {
            Id = id,
            TopicName = "exception-topic",
            WorkerId = nameof(ManagerServiceTests)
        });
        await _sut.ExecuteExternalTask(CancellationToken.None);

        _client.Verify(m => m.Fail(id, It.Is<FailExternalTaskDto>(d => d.ErrorMessage.Equals("Exception from handler")), It.IsAny<CancellationToken>()), Times.Once());
    }

    [Test]
    public async Task Report_task_execution_result_complete()
    {
        var id = Guid.NewGuid();

        await _channel.Write(new LockedExternalTaskDto
        {
            Id = id,
            TopicName = "complete-topic",
            WorkerId = nameof(ManagerServiceTests),
            Variables = new Dictionary<string, VariableDto>
                {
                    { "xml", new VariableDto(
                        Value: new JValue("<root/>"),
                        Type: null,
                        ValueInfo: new ValueInfoDto
                        {
                            SerializationDataFormat = "xml",
                            FileName = "request.xml",
                            MimeType = "application/xml",
                            Encoding = "utf-8"
                        })
                    }
                }
        });
        await _sut.ExecuteExternalTask(CancellationToken.None);

        _client.Verify(m => m.Complete(id, It.IsAny<CompleteExternalTaskDto>(), It.IsAny<CancellationToken>()), Times.Once());
    }

    [Test]
    public async Task Report_task_execution_result_failure()
    {
        var id = Guid.NewGuid();

        await _channel.Write(new LockedExternalTaskDto
        {
            Id = id,
            TopicName = "failure-topic",
            WorkerId = nameof(ManagerServiceTests)
        });
        await _sut.ExecuteExternalTask(CancellationToken.None);

        _client.Verify(m => m.Fail(id, It.IsAny<FailExternalTaskDto>(), It.IsAny<CancellationToken>()), Times.Once());
    }

    [Test]
    public async Task Report_task_execution_result_bpmn_error()
    {
        var id = Guid.NewGuid();

        await _channel.Write(new LockedExternalTaskDto
        {
            Id = id,
            TopicName = "bpmn-error-topic",
            WorkerId = nameof(ManagerServiceTests)
        });
        await _sut.ExecuteExternalTask(CancellationToken.None);

        _client.Verify(m => m.BpmnError(id, It.IsAny<BpmnErrorExternalTaskDto>(), It.IsAny<CancellationToken>()), Times.Once());
    }

    class CompleteExternalTaskHandler : ExternalTaskHandler
    {
        public override string Topic => "complete-topic";

        public override async Task<ExternalTaskResult> Execute(ExternalTask externalTask, CancellationToken ct)
        {
            await Task.CompletedTask;
            return new ExternalTaskCompleteResult(new Dictionary<string, Variable>()
            {
                { "xmlResult", Variable.From(XDocument.Parse("<result/>")) }
            });
        }
    }

    class FailureExternalTaskHandler : ExternalTaskHandler
    {
        public override string Topic => "failure-topic";

        public override async Task<ExternalTaskResult> Execute(ExternalTask externalTask, CancellationToken ct)
        {
            await Task.CompletedTask;
            return new ExternalTaskFailureResult(
                ErrorMessage: "Task failure",
                ErrorDetails: "Task failed"
            );
        }
    }

    class BpmnErrorExternalTaskHandler : ExternalTaskHandler
    {
        public override string Topic => "bpmn-error-topic";

        public override async Task<ExternalTaskResult> Execute(ExternalTask externalTask, CancellationToken ct)
        {
            await Task.CompletedTask;
            return new ExternalTaskBpmnErrorResult(
                ErrorCode: "418",
                ErrorMessage: "Teapot"
            );
        }
    }

    class TimedOutExternalTaskHandler : ExternalTaskHandler
    {
        public override string Topic => "timeout-topic";
        public override int LockDuration => 10;

        public override async Task<ExternalTaskResult> Execute(ExternalTask externalTask, CancellationToken ct)
        {
            await Task.Delay(200, ct);
            return new ExternalTaskCompleteResult();
        }
    }

    class ExceptionExternalTaskHandler : ExternalTaskHandler
    {
        public override string Topic => "exception-topic";

        public override Task<ExternalTaskResult> Execute(ExternalTask externalTask, CancellationToken ct)
        {
            throw new Exception("Exception from handler");
        }
    }
}
