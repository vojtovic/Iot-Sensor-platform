using System.Security.Cryptography;

namespace Ingest;

public class SecretGenerator()
{
    public static string Password()
    {
        var bytes = RandomNumberGenerator.GetBytes(32);
        var password = Convert.ToHexString(bytes);
        return password;
    }
}
