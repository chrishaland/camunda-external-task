namespace Tests.UnitTests;

[TestFixture]
public class TaskExtentionTests
{
    [Test]
    public void Should_return_as_expected()
    {
        Assert.DoesNotThrowAsync(async () =>
        {
            await Task.Run(async () =>
            {
                await Task.Delay(100);
                return 1;
            }).WithTimeout(TimeSpan.FromMilliseconds(200));
        });
    }

    [Test]
    public void Should_throw_if_timeout_is_exceeded()
    {
        Assert.ThrowsAsync<TimeoutException>(async () =>
        {
            await Task.Run(async () =>
            {
                await Task.Delay(200);
                return 1;
            }).WithTimeout(TimeSpan.FromMilliseconds(100));
        });
    }
}
