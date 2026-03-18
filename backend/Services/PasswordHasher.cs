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

        return string.Create(
            salt.Length * 2 + hash.Length * 2 + 16,
            (salt, hash),
            static (span, state) =>
            {
                "pbkdf2-sha512$".AsSpan().CopyTo(span);
                var offset = "pbkdf2-sha512$".Length;
                Convert.ToHexString(state.salt).AsSpan().CopyTo(span[offset..]);
                offset += state.salt.Length * 2;
                "$".AsSpan().CopyTo(span[offset..]);
                offset += 1;
                Convert.ToHexString(state.hash).AsSpan().CopyTo(span[offset..]);
            });
    }
}
