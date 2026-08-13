using GameFlow.Infrastructure.Persistence;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Net.Http.Headers;

namespace GameFlow.Api.Controllers;

/// <summary>
/// Veritabanında saklanan dosyaları sunar
/// (<c>FileStorage:Provider = Database</c> iken kullanılır).
///
/// Uç nokta kimlik doğrulaması istemez. Sebebi: bu bağlantılar arayüzde
/// <c>&lt;img&gt;</c> ve indirme bağlantısı olarak kullanılır ve tarayıcı bu
/// isteklere <c>Authorization</c> başlığı ekleyemez. Erişim, tahmin edilemez
/// GUID v7 dosya adıyla korunur — diskten sunulan <c>/uploads</c> yolunun
/// güvenlik davranışıyla aynıdır.
/// </summary>
[ApiController]
[AllowAnonymous]
[Route("api/files")]
public class FilesController(ApplicationDbContext context) : ControllerBase
{
    [HttpGet("{storedFileName}")]
    public async Task<IActionResult> Get(string storedFileName, CancellationToken cancellationToken)
    {
        var file = await context.StoredFiles
            .AsNoTracking()
            .Where(f => f.StoredFileName == storedFileName)
            .Select(f => new { f.Content, f.ContentType, f.FileName, f.UpdatedAt, f.CreatedAt })
            .FirstOrDefaultAsync(cancellationToken);

        if (file is null)
        {
            return NotFound();
        }

        // İçerik adrese göre değişmez (dosya adı benzersiz ve yeniden kullanılmaz),
        // bu yüzden uzun süreli önbelleğe alınabilir.
        Response.Headers.CacheControl = "public, max-age=31536000, immutable";

        // "inline": görseller arayüzde doğrudan görünür. fileDownloadName kullanmak
        // Content-Disposition'ı "attachment" yapar ve <img> etiketini bozardı.
        // Özgün ad yine de indirmede kullanılsın diye başlığa eklenir; Türkçe
        // karakterler için RFC 5987 kodlamasını SetHttpFileName üstlenir.
        var disposition = new ContentDispositionHeaderValue("inline");
        disposition.SetHttpFileName(file.FileName);
        Response.Headers.ContentDisposition = disposition.ToString();

        return File(file.Content, file.ContentType);
    }
}
