namespace FileAnalysisService.Infrastructure.Services;

/// <summary>
/// Настройки подключения к MinIO (S3-совместимое хранилище).
/// </summary>
public class MinioSettings
{
    /// <summary>
    /// Endpoint MinIO (например, "minio:9000" или "localhost:9000").
    /// </summary>
    public string Endpoint { get; set; } = string.Empty;

    /// <summary>
    /// Access Key для MinIO.
    /// </summary>
    public string AccessKey { get; set; } = string.Empty;

    /// <summary>
    /// Secret Key для MinIO.
    /// </summary>
    public string SecretKey { get; set; } = string.Empty;

    /// <summary>
    /// Имя bucket’а (например, "analytics-images").
    /// </summary>
    public string BucketName { get; set; } = string.Empty;

    /// <summary>
    /// Использовать SSL (true/false).
    /// </summary>
    public bool UseSsl { get; set; } = true;
}