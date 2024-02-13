using Haland.CamundaExternalTask.DependencyInjection;
using NSubstitute.ExceptionExtensions;

namespace Tests;

[TestFixture]
public class FetcherServiceTests
{
    private static readonly IChannel _channel = Substitute.For<IChannel>();
    private static readonly IExternalTaskClient _client = Substitute.For<IExternalTaskClient>();
    
    private static readonly CamundaOptions _options = new();
    private static readonly ILogger<FetcherService> _logger = new LoggerFactory()
        .CreateLogger<FetcherService>();

    private static readonly ICamundaBuilder _camundaBuilder = new DefaultCamundaBuilder(string.Empty, new ServiceCollection());
    private static readonly IServiceProvider _serviceProvider = _camundaBuilder.Services.BuildServiceProvider();

    private readonly FetcherService _sut = new(
        channel: _channel,
        options: _options,
        client: _client,
        logger: _logger,
        handlers: _serviceProvider.GetRequiredService<IEnumerable<IExternalTaskHandler>>()
    );

    [Test]
    public async Task Writes_received_locked_tasks_to_channel()
    {
        var id = Guid.NewGuid();

        _channel.CurrentCapacity.Returns(1);
        _client.FetchAndLock(Arg.Any<FetchExternalTasksDto>(), Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(new[] { new LockedExternalTaskDto { Id = id } }));

        await _sut.FetchExternalTasks(CancellationToken.None);

        await _channel.Received(1)
            .Write(Arg.Is<LockedExternalTaskDto>(dto => dto.Id.Equals(id)), Arg.Any<CancellationToken>());
    }

    [Test]
    public void Does_not_throw_on_unhandled_exceptions()
    {
        _client.FetchAndLock(Arg.Any<FetchExternalTasksDto>(), Arg.Any<CancellationToken>())
            .Throws(new Exception("Ouch!"));

        Assert.DoesNotThrowAsync(() => _sut.FetchExternalTasks(CancellationToken.None));
    }
}
