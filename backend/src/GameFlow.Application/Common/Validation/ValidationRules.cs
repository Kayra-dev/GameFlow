using FluentValidation;

namespace GameFlow.Application.Common.Validation;

/// <summary>Modüller arasında tekrar eden doğrulama kuralları.</summary>
public static class ValidationRules
{
    /// <summary>Şifre politikası: en az 8 karakter, büyük/küçük harf ve rakam.</summary>
    public static IRuleBuilderOptions<T, string> Password<T>(this IRuleBuilder<T, string> rule)
        => rule
            .NotEmpty().WithMessage("Şifre zorunludur.")
            .MinimumLength(8).WithMessage("Şifre en az 8 karakter olmalıdır.")
            .MaximumLength(128).WithMessage("Şifre en fazla 128 karakter olabilir.")
            .Matches("[A-ZÇĞİÖŞÜ]").WithMessage("Şifre en az bir büyük harf içermelidir.")
            .Matches("[a-zçğıöşü]").WithMessage("Şifre en az bir küçük harf içermelidir.")
            .Matches("[0-9]").WithMessage("Şifre en az bir rakam içermelidir.");

    public static IRuleBuilderOptions<T, string> EmailAddressRule<T>(this IRuleBuilder<T, string> rule)
        => rule
            .NotEmpty().WithMessage("E-posta zorunludur.")
            .EmailAddress().WithMessage("Geçerli bir e-posta adresi girin.")
            .MaximumLength(256).WithMessage("E-posta en fazla 256 karakter olabilir.");

    public static IRuleBuilderOptions<T, string> PersonName<T>(this IRuleBuilder<T, string> rule)
        => rule
            .NotEmpty().WithMessage("Ad soyad zorunludur.")
            .MinimumLength(3).WithMessage("Ad soyad en az 3 karakter olmalıdır.")
            .MaximumLength(128).WithMessage("Ad soyad en fazla 128 karakter olabilir.");

    /// <summary>Geçerli bir enum değeri olmalı (0 veya tanımsız değerleri reddeder).</summary>
    public static IRuleBuilderOptions<T, TEnum> ValidEnum<T, TEnum>(this IRuleBuilder<T, TEnum> rule)
        where TEnum : struct, Enum
        => rule.Must(value => Enum.IsDefined(value)).WithMessage("Geçersiz değer seçildi.");
}
