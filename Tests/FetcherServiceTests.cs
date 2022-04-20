using Haland.CamundaExternalTask.DependencyInjection;

namespace Tests;

[TestFixture]
public class FetcherServiceTests
{
    private static readonly Mock<IChannel> _channel = new();
    private static readonly Mock<IExternalTaskClient> _client = new();
    
    private static readonly CamundaOptions _options = new();
    private static readonly ILogger<FetcherService> _logger = new LoggerFactory()
        .CreateLogger<FetcherService>();

    private static readonly ICamundaBuilder _camundaBuilder = new DefaultCamundaBuilder(string.Empty, new ServiceCollection());
    private static readonly IServiceProvider _serviceProvider = _camundaBuilder.Services.BuildServiceProvider();

    private readonly FetcherService _sut = new(
        channel: _channel.Object,
        options: _options,
        client: _client.Object,
        logger: _logger,
        handlers: _serviceProvider.GetRequiredService<IEnumerable<IExternalTaskHandler>>()
    );

    [Test]
    public async Task Writes_received_locked_tasks_to_channel()
    {
        var id = Guid.NewGuid();

        _channel.Setup(m => m.CurrentCapacity).Returns(1);
        _client.Setup(m => m.FetchAndLock(It.IsAny<FetchExternalTasksDto>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new[] { new LockedExternalTaskDto { Id = id } });

        await _sut.FetchExternalTasks(CancellationToken.None);

        _channel.Verify(m => m.Write(It.Is<LockedExternalTaskDto>(dto => dto.Id.Equals(id)), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Test]
    public void Does_not_throw_on_unhandled_exceptions()
    {
        _client.Setup(m => m.FetchAndLock(It.IsAny<FetchExternalTasksDto>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new Exception("Ouch!"));

        Assert.DoesNotThrowAsync(() => _sut.FetchExternalTasks(CancellationToken.None));
    }
}
