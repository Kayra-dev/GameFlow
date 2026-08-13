using GameFlow.Application.Common.Interfaces;

namespace GameFlow.Infrastructure.Identity;

/// <summary>BCrypt (work factor 12) ile şifre özetleme.</summary>
public class BCryptPasswordHasher : IPasswordHasher
{
    private const int WorkFactor = 12;

    public string Hash(string password)
        => BCrypt.Net.BCrypt.HashPassword(password, WorkFactor);

    public bool Verify(string password, string passwordHash)
    {
        try
        {
            return BCrypt.Net.BCrypt.Verify(password, passwordHash);
        }
        catch (BCrypt.Net.SaltParseException)
        {
            // Bozuk hash kaydı, doğrulama başarısız sayılır.
            return false;
        }
    }
}
