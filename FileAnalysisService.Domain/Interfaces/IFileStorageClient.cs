using FileAnalysisService.Domain.DTOs;
using FileAnalysisService.Domain.ValueObjects;

namespace FileAnalysisService.Domain.Interfaces;


public interface IFileStorageClient
{
    /// <summary>
    /// Возвращает DTO, содержащий и байты файла, и оригинальное FileName (чтобы проверить .txt).
    /// </summary>
    Task<FileDto> GetFileAsync(FileId fileId, CancellationToken ct = default);
}