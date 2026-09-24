using Domain;

namespace Domain.Tests;



public class TelemetryValidatorTests
{


    private readonly TelemetryValidator _validator = new();

    private static SensorType Teplota() => new()
    {
        Id = 1,
        Code = "temperature",
        Unit = "°C",
        MinValid = -40,
        MaxValid = 85
    };

    [Fact]
    public void HodnotaVRozsahu_VratiOk()
    {
        var reading = new Reading("temp1", 21.5, null);

        var result = _validator.Evaluate(reading, Teplota());

        Assert.Equal(Quality.Ok, result);
    }

    [Fact]
    public void HodnotaNadMaximem_VratiSuspect()
    {
        var reading = new Reading("temp1", 300, null);

        var result = _validator.Evaluate(reading, Teplota());

        Assert.Equal(Quality.Suspect, result);
    }

    [Fact]
    public void HodnotaNaHranici_VratiOk()
    {
        var reading = new Reading("temp1", -40, null);

        var result = _validator.Evaluate(reading, Teplota());

        Assert.Equal(Quality.Ok, result);
    }

    [Fact]
    public void HodnotaPodMinimem_VratiSuspect()
    {
        var reading = new Reading("temp1", -50, null);

        var result = _validator.Evaluate(reading, Teplota());

        Assert.Equal(Quality.Suspect, result);
    }

    [Fact]
    public void HodnotaNaHraniciAError_VratiError()
    {
        var reading = new Reading("temp1", -40, "error");

        var result = _validator.Evaluate(reading, Teplota());

        Assert.Equal(Quality.Error, result);
    }

    [Fact]
    public void HodnotaError_VratiError()
    {
        var reading = new Reading("temp1", 10, "error");

        var result = _validator.Evaluate(reading, Teplota());

        Assert.Equal(Quality.Error, result);
    }

}
