namespace Infrastructure;

public interface IDeviceProvisioner
{

    Task<bool> CreateDeviceClientAsync(string username, string password, CancellationToken ct);
    Task<bool> SetDevicePasswordAsync(string username, string password, CancellationToken ct);
    Task<bool> DisableDevice(string username, CancellationToken ct);
}
