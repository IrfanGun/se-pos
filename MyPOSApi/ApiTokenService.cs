using System.Security.Cryptography;
using System.Text;

public sealed class ApiTokenService(IConfiguration configuration)
{
    private const int LifetimeSeconds = 8 * 60 * 60;

    public string Create(string username)
    {
        var expiresAt = DateTimeOffset.UtcNow.AddSeconds(LifetimeSeconds).ToUnixTimeSeconds();
        var payload = $"{username}|{expiresAt}";
        var payloadBytes = Encoding.UTF8.GetBytes(payload);
        var signature = HMACSHA256.HashData(GetKey(), payloadBytes);
        return $"{Base64UrlEncode(payloadBytes)}.{Base64UrlEncode(signature)}";
    }

    public bool TryValidate(string token, out string username)
    {
        username = string.Empty;
        var parts = token.Split('.');
        if (parts.Length != 2)
        {
            return false;
        }

        try
        {
            var payloadBytes = Base64UrlDecode(parts[0]);
            var suppliedSignature = Base64UrlDecode(parts[1]);
            var expectedSignature = HMACSHA256.HashData(GetKey(), payloadBytes);
            if (!CryptographicOperations.FixedTimeEquals(suppliedSignature, expectedSignature))
            {
                return false;
            }

            var payload = Encoding.UTF8.GetString(payloadBytes).Split('|');
            if (payload.Length != 2 || !long.TryParse(payload[1], out var expiresAt) ||
                expiresAt <= DateTimeOffset.UtcNow.ToUnixTimeSeconds())
            {
                return false;
            }

            username = payload[0];
            return !string.IsNullOrWhiteSpace(username);
        }
        catch (FormatException)
        {
            return false;
        }
    }

    public static bool FixedTimeEquals(string? supplied, string expected)
    {
        if (supplied is null)
        {
            return false;
        }

        var suppliedBytes = Encoding.UTF8.GetBytes(supplied);
        var expectedBytes = Encoding.UTF8.GetBytes(expected);
        return suppliedBytes.Length == expectedBytes.Length &&
               CryptographicOperations.FixedTimeEquals(suppliedBytes, expectedBytes);
    }

    private byte[] GetKey()
    {
        var key = configuration["Authentication:SigningKey"];
        if (string.IsNullOrWhiteSpace(key) || Encoding.UTF8.GetByteCount(key) < 32)
        {
            throw new InvalidOperationException("Authentication:SigningKey must contain at least 32 bytes.");
        }

        return Encoding.UTF8.GetBytes(key);
    }

    private static string Base64UrlEncode(byte[] value) =>
        Convert.ToBase64String(value).TrimEnd('=').Replace('+', '-').Replace('/', '_');

    private static byte[] Base64UrlDecode(string value) =>
        Convert.FromBase64String(value.Replace('-', '+').Replace('_', '/') +
                                 new string('=', (4 - value.Length % 4) % 4));
}
