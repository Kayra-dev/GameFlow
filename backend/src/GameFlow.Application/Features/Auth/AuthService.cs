using GameFlow.Application.Common.Interfaces;
using GameFlow.Application.Features.Auth.Dtos;
using GameFlow.Domain.Entities;
using GameFlow.Domain.Enums;
using GameFlow.Domain.Exceptions;
using Microsoft.EntityFrameworkCore;

namespace GameFlow.Application.Features.Auth;

/// <inheritdoc cref="IAuthService"/>
public class AuthService(
    IApplicationDbContext context,
    IPasswordHasher passwordHasher,
    IJwtTokenService tokenService,
    ICurrentUserService currentUser,
    IDateTimeProvider dateTime,
    IActivityLogger activityLogger) : IAuthService
{
    /// <summary>Bir kullanıcı için tutulacak azami aktif refresh token sayısı.</summary>
    private const int MaxActiveTokensPerUser = 5;

    public async Task<AuthResponse> LoginAsync(
        LoginRequest request,
        string? ipAddress,
        CancellationToken cancellationToken = default)
    {
        var email = NormalizeEmail(request.Email);

        var user = await context.Users
            .Include(u => u.Role)
            .FirstOrDefaultAsync(u => u.Email == email, cancellationToken);

        // Kullanıcının var olup olmadığını sızdırmamak için her iki durumda aynı mesaj döner.
        if (user is null || !passwordHasher.Verify(request.Password, user.PasswordHash))
        {
            throw new UnauthorizedException("E-posta veya şifre hatalı.");
        }

        if (!user.IsActive)
        {
            throw new ForbiddenException("Hesabınız devre dışı bırakılmış. Yöneticinizle görüşün.");
        }

        user.LastSeenAt = dateTime.UtcNow;

        activityLogger.Log(
            ActivityType.UserLoggedIn,
            $"{user.FullName} sisteme giriş yaptı.",
            entityType: nameof(User),
            entityId: user.Id);

        var response = await IssueTokensAsync(user, ipAddress, cancellationToken);

        await context.SaveChangesAsync(cancellationToken);

        return response;
    }

    public async Task<AuthResponse> RefreshAsync(
        RefreshTokenRequest request,
        string? ipAddress,
        CancellationToken cancellationToken = default)
    {
        var existing = await context.RefreshTokens
            .Include(t => t.User)
            .ThenInclude(u => u.Role)
            .FirstOrDefaultAsync(t => t.Token == request.RefreshToken, cancellationToken);

        if (existing is null || !existing.IsActive)
        {
            throw new UnauthorizedException("Oturum süreniz doldu. Lütfen tekrar giriş yapın.");
        }

        if (!existing.User.IsActive || existing.User.IsDeleted)
        {
            throw new ForbiddenException("Hesabınız artık aktif değil.");
        }

        var response = await IssueTokensAsync(existing.User, ipAddress, cancellationToken);

        // Rotasyon: kullanılan token iptal edilir ve zincir takip edilebilir kalır.
        existing.RevokedAt = dateTime.UtcNow;
        existing.ReplacedByToken = response.RefreshToken;

        try
        {
            await context.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateConcurrencyException)
        {
            // Aynı token ile eş zamanlı ikinci bir yenileme yarışı kazandıysa bu satır
            // artık yok. Bu bir sunucu hatası değil, tokenın çoktan kullanılmış olması
            // demektir; istemci yeniden giriş yapmalı.
            throw new UnauthorizedException(
                "Oturum bilgileriniz başka bir sekmede yenilendi. Lütfen tekrar deneyin.");
        }

        return response;
    }

    public async Task LogoutAsync(RefreshTokenRequest request, CancellationToken cancellationToken = default)
    {
        var token = await context.RefreshTokens
            .FirstOrDefaultAsync(t => t.Token == request.RefreshToken, cancellationToken);

        // Zaten geçersiz bir token gönderilmesi hata değildir; çıkış işlemi başarılı sayılır.
        if (token is null || !token.IsActive)
        {
            return;
        }

        token.RevokedAt = dateTime.UtcNow;

        await context.SaveChangesAsync(cancellationToken);
    }

    public async Task<CurrentUserDto> GetCurrentUserAsync(CancellationToken cancellationToken = default)
    {
        var userId = currentUser.RequireUserId();

        var user = await context.Users
            .Include(u => u.Role)
            .FirstOrDefaultAsync(u => u.Id == userId, cancellationToken)
            ?? throw new NotFoundException("Kullanıcı", userId);

        return await ToCurrentUserDtoAsync(user, cancellationToken);
    }

    public async Task ChangePasswordAsync(
        ChangePasswordRequest request,
        CancellationToken cancellationToken = default)
    {
        var userId = currentUser.RequireUserId();

        var user = await context.Users.FirstOrDefaultAsync(u => u.Id == userId, cancellationToken)
                   ?? throw new NotFoundException("Kullanıcı", userId);

        if (!passwordHasher.Verify(request.CurrentPassword, user.PasswordHash))
        {
            throw new DomainException("Mevcut şifreniz hatalı.");
        }

        if (passwordHasher.Verify(request.NewPassword, user.PasswordHash))
        {
            throw new DomainException("Yeni şifre mevcut şifrenizden farklı olmalıdır.");
        }

        user.PasswordHash = passwordHasher.Hash(request.NewPassword);
        user.MustChangePassword = false;

        // Şifre değiştiğinde diğer tüm oturumlar düşürülür.
        await RevokeAllTokensAsync(user.Id, cancellationToken);

        await context.SaveChangesAsync(cancellationToken);
    }

    public async Task<CurrentUserDto> UpdateProfileAsync(
        UpdateProfileRequest request,
        CancellationToken cancellationToken = default)
    {
        var userId = currentUser.RequireUserId();

        var user = await context.Users
            .Include(u => u.Role)
            .FirstOrDefaultAsync(u => u.Id == userId, cancellationToken)
            ?? throw new NotFoundException("Kullanıcı", userId);

        user.FullName = request.FullName.Trim();
        user.JobTitle = string.IsNullOrWhiteSpace(request.JobTitle) ? null : request.JobTitle.Trim();
        user.Bio = string.IsNullOrWhiteSpace(request.Bio) ? null : request.Bio.Trim();

        activityLogger.Log(
            ActivityType.UserUpdated,
            $"{user.FullName} profil bilgilerini güncelledi.",
            entityType: nameof(User),
            entityId: user.Id);

        await context.SaveChangesAsync(cancellationToken);

        return await ToCurrentUserDtoAsync(user, cancellationToken);
    }

    /// <summary>Erişim ve refresh tokenlarını üretir, eski tokenları budar.</summary>
    private async Task<AuthResponse> IssueTokensAsync(
        User user,
        string? ipAddress,
        CancellationToken cancellationToken)
    {
        var (accessToken, expiresAt) = tokenService.CreateAccessToken(user);
        var refreshToken = tokenService.CreateRefreshToken();

        context.RefreshTokens.Add(new RefreshToken
        {
            UserId = user.Id,
            Token = refreshToken,
            ExpiresAt = dateTime.UtcNow.Add(tokenService.RefreshTokenLifetime),
            CreatedByIp = ipAddress
        });

        await PruneTokensAsync(user.Id, cancellationToken);

        return new AuthResponse(
            accessToken,
            refreshToken,
            expiresAt,
            await ToCurrentUserDtoAsync(user, cancellationToken));
    }

    /// <summary>
    /// Süresi geçmiş tokenları siler ve eş zamanlı oturum sayısını sınırlar;
    /// böylece tablo süresiz büyümez.
    ///
    /// Silme, değişiklik izlemeli <c>Remove</c> yerine küme tabanlı
    /// <c>ExecuteDeleteAsync</c> ile yapılır. Bunun nedeni somut bir hatadır:
    /// kullanıcı iki sekme açtığında ikisi de aynı anda token yeniler, ikisi de
    /// aynı satırları silmeye çalışır ve ikinci istek "1 satır etkilenmesi
    /// beklenirken 0 satır etkilendi" eşzamanlılık istisnasıyla 500 dönerdi.
    /// Küme tabanlı silme, satır zaten yoksa sessizce geçer.
    /// </summary>
    private async Task PruneTokensAsync(Guid userId, CancellationToken cancellationToken)
    {
        var now = dateTime.UtcNow;

        await context.RefreshTokens
            .Where(t => t.UserId == userId && (t.ExpiresAt <= now || t.RevokedAt != null))
            .ExecuteDeleteAsync(cancellationToken);

        // Eş zamanlı oturum sayısı sınırlanır: en yeni N token dışındakiler silinir.
        var surplusIds = await context.RefreshTokens
            .Where(t => t.UserId == userId && t.ExpiresAt > now && t.RevokedAt == null)
            .OrderByDescending(t => t.CreatedAt)
            .Skip(MaxActiveTokensPerUser)
            .Select(t => t.Id)
            .ToListAsync(cancellationToken);

        if (surplusIds.Count > 0)
        {
            await context.RefreshTokens
                .Where(t => surplusIds.Contains(t.Id))
                .ExecuteDeleteAsync(cancellationToken);
        }
    }

    private async Task RevokeAllTokensAsync(Guid userId, CancellationToken cancellationToken)
    {
        var tokens = await context.RefreshTokens
            .Where(t => t.UserId == userId && t.RevokedAt == null)
            .ToListAsync(cancellationToken);

        foreach (var token in tokens)
        {
            token.RevokedAt = dateTime.UtcNow;
        }
    }

    private async Task<CurrentUserDto> ToCurrentUserDtoAsync(User user, CancellationToken cancellationToken)
    {
        // Arayüz, lider yetkisi gerektiren alanları bu listeye göre gösterir.
        var ledTeamIds = await context.TeamMembers
            .Where(m => m.UserId == user.Id && m.Role == TeamRole.Leader)
            .Select(m => m.TeamId)
            .ToListAsync(cancellationToken);

        var role = Enum.TryParse<SystemRole>(user.Role?.Name, out var parsed)
            ? parsed
            : (SystemRole)user.RoleId;

        return new CurrentUserDto(
            user.Id,
            user.FullName,
            user.Email,
            user.JobTitle,
            user.AvatarUrl,
            role,
            user.MustChangePassword,
            ledTeamIds);
    }

    private static string NormalizeEmail(string email) => email.Trim().ToLowerInvariant();
}
