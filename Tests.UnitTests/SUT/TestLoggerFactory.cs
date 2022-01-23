namespace Tests.UnitTests;

public static class TestLoggerFactory
{
    private static readonly ILoggerFactory _loggerFactory;

    static TestLoggerFactory()
    {
        _loggerFactory = LoggerFactory.Create(options => options.AddConsole());
    }

    public static ILogger<T> GetLogger<T>()
    {
        return _loggerFactory.CreateLogger<T>();
    }
}
