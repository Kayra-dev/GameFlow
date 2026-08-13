namespace GameFlow.Domain.Exceptions;

/// <summary>İş kuralı ihlallerinde fırlatılan temel istisna. API katmanında 400'e çevrilir.</summary>
public class DomainException(string message) : Exception(message);

/// <summary>İstenen kayıt bulunamadığında fırlatılır. API katmanında 404'e çevrilir.</summary>
public class NotFoundException(string entityName, object key)
    : Exception($"{entityName} bulunamadı. (Anahtar: {key})");

/// <summary>Kullanıcının yetkisi olmayan bir işlem denemesinde fırlatılır. 403'e çevrilir.</summary>
public class ForbiddenException(string message = "Bu işlem için yetkiniz bulunmuyor.")
    : Exception(message);

/// <summary>Kimlik doğrulama hatalarında fırlatılır. 401'e çevrilir.</summary>
public class UnauthorizedException(string message = "Kimlik doğrulaması başarısız.")
    : Exception(message);

/// <summary>Aynı kaydın tekrar oluşturulması gibi çakışmalarda fırlatılır. 409'a çevrilir.</summary>
public class ConflictException(string message) : Exception(message);
