namespace Domain;

public interface IDeviceKeyVerifier
{
    string Calculate(string HardwareId);
    bool Validate(string HardwareId, string? ClaimToken);
}
