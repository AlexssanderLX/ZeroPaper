namespace ZeroPaper.Services.Interfaces;

public interface IPasswordHasher
{
    string Hash(string value);
    bool Verify(string value, string hashedValue);
}
