using System.Text.Json;
using System.Threading.Channels;
using Domain;

namespace Ingest;

public record TelemetryItem(string HardwareId, TelemetryMessage Message, DateTimeOffset ReceivedAt);

public class TelemetryQueue
{
    private readonly Channel<TelemetryItem> _channel;

    public TelemetryQueue()
    {
        var options = new BoundedChannelOptions(10000) { FullMode = BoundedChannelFullMode.Wait };
        _channel = Channel.CreateBounded<TelemetryItem>(options);
    }

    public ChannelWriter<TelemetryItem> Writer => _channel.Writer;
    public ChannelReader<TelemetryItem> Reader => _channel.Reader;
}
