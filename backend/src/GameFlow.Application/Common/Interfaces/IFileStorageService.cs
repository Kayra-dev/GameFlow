using GameFlow.Application.Common.Models;

namespace GameFlow.Application.Common.Interfaces;

public interface IFileStorageService
{
    /// <summary>Dosyayı kalıcı depoya yazar ve erişim bilgilerini döner.</summary>
    Task<StoredFileResult> SaveAsync(
        Stream content,
        string originalFileName,
        string contentType,
        string folder,
        CancellationToken cancellationToken = default);

    Task DeleteAsync(string storedFileName, string folder, CancellationToken cancellationToken = default);
}
