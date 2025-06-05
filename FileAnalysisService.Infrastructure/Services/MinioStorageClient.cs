using Minio;
using Microsoft.Extensions.Options;
using FileAnalysisService.Application.Interfaces;
using Minio.DataModel.Args;

namespace FileAnalysisService.Infrastructure.Services;

/// <summary>
/// Реализация IStorageClient с помощью MinIO (используя пакет Minio 6.x).
/// </summary>
public class MinioStorageClient : IStorageClient
{
    private readonly IMinioClient _minioClient;

    public MinioStorageClient(IOptions<MinioSettings> options)
    {
        var s = options.Value;
        _minioClient = new MinioClient()
            .WithEndpoint(s.Endpoint)
            .WithCredentials(s.AccessKey, s.SecretKey)
            .WithSSL(s.UseSsl)
            .Build();
    }

    public async Task<string> SaveAsync(string bucketName, string objectKey, Stream data, CancellationToken ct = default)
    {
        // Проверяем, существует ли bucket; если нет — создаём
        if (!await _minioClient.BucketExistsAsync(new BucketExistsArgs().WithBucket(bucketName), ct))
        {
            await _minioClient.MakeBucketAsync(new MakeBucketArgs().WithBucket(bucketName), ct);
        }

        // Чтобы получить размер, нам нужно либо искать data.Length, либо скопировать во временный MemoryStream
        long objectSize;
        if (data.CanSeek)
        {
            objectSize = data.Length;
            data.Seek(0, SeekOrigin.Begin);
        }
        else
        {
            using var msCheck = new MemoryStream();
            await data.CopyToAsync(msCheck, ct);
            objectSize = msCheck.Length;
            msCheck.Position = 0;
            data = msCheck;
        }

        // Загружаем объект
        await _minioClient.PutObjectAsync(new PutObjectArgs()
                .WithBucket(bucketName)
                .WithObject(objectKey)
                .WithStreamData(data)
                .WithObjectSize(objectSize)
                .WithContentType("image/png"),
            ct);

        return objectKey; // Возвращаем ключ объекта
    }

    public async Task<Stream> GetAsync(string bucketName, string objectKey, CancellationToken ct = default)
    {
        // Скачиваем объект в MemoryStream
        var ms = new MemoryStream();
        await _minioClient.GetObjectAsync(new GetObjectArgs()
            .WithBucket(bucketName)
            .WithObject(objectKey)
            .WithCallbackStream(stream => stream.CopyTo(ms)), ct);
        ms.Seek(0, SeekOrigin.Begin);
        return ms;
    }
}