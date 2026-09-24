using Domain;

namespace Domain.Tests;

public class TimestampResolverTests
{
    private readonly TimestampResolver _resolver = new(TimeSpan.FromMinutes(5));

    [Fact]
    public void ChybejiciCasZarizeni_PouzijeCasPrijeti()
    {
        var receivedAt = new DateTimeOffset(2026, 9, 19, 12, 0, 0, TimeSpan.Zero);

        var result = _resolver.Resolve(null, receivedAt);

        Assert.Equal(receivedAt, result);
    }

    [Fact]
    public void CasOHodinuDrive_PouzijeCasPrijeti()
    {
        var receivedAt = new DateTimeOffset(2026, 9, 19, 22, 0, 0, TimeSpan.Zero);
        var DeviceTime = new DateTimeOffset(2026, 9, 19, 23, 0, 0, TimeSpan.Zero);
        var result = _resolver.Resolve(DeviceTime, receivedAt);

        Assert.Equal(receivedAt, result);
    }

    [Fact]
    public void CasZarizeniOSekJine_PouzijeCasZarizeni()
    {
        var receivedAt = new DateTimeOffset(2026, 9, 19, 12, 0, 0, TimeSpan.Zero);
        var DeviceTime = new DateTimeOffset(2026, 9, 19, 12, 5, 0, TimeSpan.Zero);
        var result = _resolver.Resolve(DeviceTime, receivedAt);

        Assert.Equal(DeviceTime, result);
    }

}
