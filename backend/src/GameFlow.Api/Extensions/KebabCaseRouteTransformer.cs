using System.Text.RegularExpressions;

namespace GameFlow.Api.Extensions;

/// <summary>
/// Controller adlarını URL'de küçük harfli kebab-case'e çevirir.
/// Örnek: <c>WorkItemsController</c> → <c>/api/work-items</c>.
/// Böylece tüm uç noktalar tutarlı ve okunabilir bir adres şemasına sahip olur.
/// </summary>
public partial class KebabCaseRouteTransformer : IOutboundParameterTransformer
{
    public string? TransformOutbound(object? value)
    {
        if (value is null)
        {
            return null;
        }

        var text = value.ToString();

        return string.IsNullOrEmpty(text)
            ? text
            : CamelCaseBoundary().Replace(text, "$1-$2").ToLowerInvariant();
    }

    /// <summary>Küçük harf/rakam ile büyük harf arasındaki geçişi yakalar.</summary>
    [GeneratedRegex("([a-z0-9])([A-Z])")]
    private static partial Regex CamelCaseBoundary();
}
