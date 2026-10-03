namespace Infrastructure;

public interface IDeviceProvisioner
{

    Task<bool> CreateDeviceClientAsync(string username, string password, CancellationToken ct);
}
