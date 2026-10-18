
using System.Text;

using System.Security.Cryptography;



namespace Domain;

public class DeviceKeyVerifier : IDeviceKeyVerifier
{
    private byte[]? _GroupKey;

    public DeviceKeyVerifier(string GroupKey)
    {
        if (string.IsNullOrWhiteSpace(GroupKey))
        {
            return;
        }


        if (GroupKey.Length < 64)
        {
            throw new ArgumentException("GroupKey is too short");
        }

        foreach (char c in GroupKey)
        {
            if (!char.IsAsciiHexDigit(c))
            {
                throw new ArgumentException("GroupKey is not hex");
            }
        }

        this._GroupKey = Convert.FromHexString(GroupKey);

    }

    public string Calculate(string HardwareId)
    {
        var Idbytes = Encoding.UTF8.GetBytes(HardwareId);
        if (_GroupKey is null)
        {
            throw new InvalidOperationException("GroupKey is null");
        }
        var resultInByte = HMACSHA256.HashData(_GroupKey, Idbytes);
        var ResultInString = Convert.ToHexStringLower(resultInByte);

        return ResultInString;
    }

    public bool Validate(string HardwareId, string? ClaimToken)
    {
        if (string.IsNullOrEmpty(HardwareId) || _GroupKey is null || string.IsNullOrEmpty(ClaimToken))
        {
            return false;
        }
        var key = System.Text.Encoding.UTF8.GetBytes(Calculate(HardwareId));
        var fromId = System.Text.Encoding.UTF8.GetBytes(ClaimToken);
        var result = CryptographicOperations.FixedTimeEquals(key, fromId);
        return result;

    }

}
