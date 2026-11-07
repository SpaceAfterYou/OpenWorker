using System.Security.Cryptography;

namespace OpenWorker.Persistence.Utils;

public static class PasswordHash
{
    internal const int PasswordSaltSize = 16;

    internal const int PasswordHashSize = 64;

    private const int Iterations = 50_000;

    private static readonly HashAlgorithmName Algorithm = HashAlgorithmName.SHA3_512;

    /// <summary>
    /// Creates a hash in the form stored in text columns.
    /// </summary>
    public static string CreateEncoded(string password)
    {
        Create(password, out var hash, out var salt);

        return $"{Convert.ToBase64String(salt)}:{Convert.ToBase64String(hash)}";
    }

    public static bool VerifyEncoded(string password, string encoded)
    {
        var parts = encoded.Split(':');

        return parts.Length == 2 &&
               Verify(password, Convert.FromBase64String(parts[1]), Convert.FromBase64String(parts[0]));
    }

    public static void Create(string password, out byte[] hash, out byte[] salt)
    {
        salt = RandomNumberGenerator.GetBytes(PasswordSaltSize);
        hash = Rfc2898DeriveBytes.Pbkdf2(password, salt, Iterations, Algorithm, PasswordHashSize);;
    }

    public static bool Verify(string password, ReadOnlySpan<byte> hash, byte[] salt)
    {
        var bytes = Rfc2898DeriveBytes.Pbkdf2(password, salt, Iterations, Algorithm, PasswordHashSize);;

        return CryptographicOperations.FixedTimeEquals(bytes, hash);
    }
}
