using FileAnalysisService.Domain.ValueObjects;

namespace FileAnalysisService.Application.Interfaces;

/// <summary>
/// Абстракция над внешним хранилищем (MinIO).
/// </summary>
public interface IStorageClient
{
    Task<string> SaveAsync(string bucketName, string objectKey, Stream data, CancellationToken ct = default);
    Task<Stream> GetAsync(string bucketName, string objectKey, CancellationToken ct = default);
}