using System.Security.Cryptography;
using System.Text;
using ZeroPaper.Services.Interfaces;

namespace ZeroPaper.Services;

public class PasswordHasher : IPasswordHasher
{
    private const int SaltSize = 16;
    private const int KeySize = 32;
    private const int Iterations = 100_000;

    public string Hash(string value)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(value);

        var passwordBytes = Encoding.UTF8.GetBytes(value.Trim());
        var salt = RandomNumberGenerator.GetBytes(SaltSize);
        var hash = Rfc2898DeriveBytes.Pbkdf2(passwordBytes, salt, Iterations, HashAlgorithmName.SHA512, KeySize);
        var prefix = "pbkdf2-sha512$";
        var totalLength = prefix.Length + (salt.Length * 2) + 1 + (hash.Length * 2);

        return string.Create(
            totalLength,
            (salt, hash),
            static (span, state) =>
            {
                const string prefix = "pbkdf2-sha512$";
                prefix.AsSpan().CopyTo(span);
                var offset = prefix.Length;
                Convert.ToHexString(state.salt).AsSpan().CopyTo(span[offset..]);
                offset += state.salt.Length * 2;
                "$".AsSpan().CopyTo(span[offset..]);
                offset += 1;
                Convert.ToHexString(state.hash).AsSpan().CopyTo(span[offset..]);
            });
    }

    public bool Verify(string value, string hashedValue)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(value);
        ArgumentException.ThrowIfNullOrWhiteSpace(hashedValue);

        var normalizedHash = hashedValue.Trim().TrimEnd('\0');
        var segments = normalizedHash.Split('$', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);

        if (segments.Length != 3 || !segments[0].Equals("pbkdf2-sha512", StringComparison.Ordinal))
        {
            return false;
        }

        try
        {
            var salt = Convert.FromHexString(segments[1]);
            var expectedHash = Convert.FromHexString(segments[2]);
            var computedHash = Rfc2898DeriveBytes.Pbkdf2(
                Encoding.UTF8.GetBytes(value.Trim()),
                salt,
                Iterations,
                HashAlgorithmName.SHA512,
                KeySize);

            return CryptographicOperations.FixedTimeEquals(computedHash, expectedHash);
        }
        catch (FormatException)
        {
            return false;
        }
    }
}
