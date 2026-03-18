namespace ZeroPaper.Services.Interfaces;

public interface IPasswordHasher
{
    string Hash(string value);
}
