using System.Threading.Channels;

namespace Haland.CamundaExternalTask;

internal interface IChannel
{
    int CurrentCapacity { get; }
    int Release();
    Task Write(LockedExternalTaskDto dto, CancellationToken cancellationToken = default);
    Task<LockedExternalTaskDto> Read(CancellationToken cancellationToken = default);
}

internal class Channel : IChannel
{
    private readonly SemaphoreSlim _semaphore;
    private readonly Channel<LockedExternalTaskDto> _channel;

    public Channel(int maximumCapacity)
    {
        _semaphore = new SemaphoreSlim(maximumCapacity);
        _channel = System.Threading.Channels.
            Channel.CreateBounded<LockedExternalTaskDto>(maximumCapacity);
    }
    
    public int CurrentCapacity => _semaphore.CurrentCount;

    public async Task Write(LockedExternalTaskDto dto, CancellationToken cancellationToken = default)
    {
        await _channel.Writer.WaitToWriteAsync(cancellationToken);
        await _channel.Writer.WriteAsync(dto, cancellationToken);
        await _semaphore.WaitAsync(cancellationToken);
    }

    public async Task<LockedExternalTaskDto> Read(CancellationToken cancellationToken = default)
    {
        await _channel.Reader.WaitToReadAsync(cancellationToken);
        return await _channel.Reader.ReadAsync(cancellationToken);
    }

    public int Release() => _semaphore.Release();
}
