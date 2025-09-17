using System.Security.Cryptography;

namespace OpenWorker.Persistence.Utils;

public static class PasswordHash
{
    internal const int PasswordSaltSize = 16;

    internal const int PasswordHashSize = 64;

    private const int Iterations = 50_000;

    private static readonly HashAlgorithmName Algorithm = HashAlgorithmName.SHA3_512;

    public static void Create(string password, out byte[] hash, out byte[] salt)
    {
        salt = RandomNumberGenerator.GetBytes(PasswordSaltSize);
        using var pbkdf2 = new Rfc2898DeriveBytes(password, salt, Iterations, Algorithm);

        hash = pbkdf2.GetBytes(PasswordHashSize);
    }

    public static bool Verify(string password, ReadOnlySpan<byte> hash, byte[] salt)
    {
        using var pbkdf2 = new Rfc2898DeriveBytes(password, salt, Iterations, Algorithm);

        var bytes = pbkdf2.GetBytes(PasswordHashSize);

        return CryptographicOperations.FixedTimeEquals(bytes, hash);
    }
}