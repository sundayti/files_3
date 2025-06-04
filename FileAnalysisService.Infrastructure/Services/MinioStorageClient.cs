using Minio;
using Minio.Exceptions;
using Microsoft.Extensions.Options;
using FileAnalysisService.Application.Interfaces;
using FileAnalysisService.Domain.ValueObjects;
using Minio.DataModel.Args;

namespace FileAnalysisService.Infrastructure.Services
{
    public class MinioStorageClient : IStorageClient
    {
        private readonly IMinioClient   _minioClient;
        private readonly MinioSettings _settings;

        public MinioStorageClient(IOptions<MinioSettings> options)
        {
            _settings = options.Value;
            _minioClient = new MinioClient()
                .WithEndpoint(_settings.Endpoint)
                .WithCredentials(_settings.AccessKey, _settings.SecretKey)
                .WithSSL(_settings.UseSsl)
                .Build();
        }

        /// <summary>
        /// Сохраняет содержимое <paramref name="content"/> в бакет, указанный в настройках (_settings.BucketName),
        /// даёт ему уникальное имя (GUID + исходное fileName) и возвращает ImageLocation (ключ/имя объекта).
        /// </summary>
        public async Task<ImageLocation> SaveAsync(byte[] content, string fileName, CancellationToken cancellationToken = default)
        {
            // Генерируем уникальное имя объекта в бакете, чтобы не было коллизий
            var objectName = $"{Guid.NewGuid()}_{fileName}";

            // 1) Убедимся, что бакет существует; если нет — создадим
            try
            {
                bool foundBucket = await _minioClient.BucketExistsAsync(
                    new BucketExistsArgs().WithBucket(_settings.BucketName),
                    cancellationToken);
                if (!foundBucket)
                {
                    await _minioClient.MakeBucketAsync(
                        new MakeBucketArgs().WithBucket(_settings.BucketName),
                        cancellationToken);
                }
            }
            catch (Exception ex)
            {
                throw new Exception($"Не удалось проверить или создать бакет '{_settings.BucketName}' в MinIO.", ex);
            }

            // 2) Загружаем объект в MinIO
            try
            {
                using var ms = new MemoryStream(content);
                var putArgs = new PutObjectArgs()
                    .WithBucket(_settings.BucketName)
                    .WithObject(objectName)
                    .WithStreamData(ms)
                    .WithObjectSize(ms.Length);
                    // .WithContentType("application/octet-stream") // при желании можно указать контент-тайп

                await _minioClient.PutObjectAsync(putArgs, cancellationToken);
                return new ImageLocation(objectName);
            }
            catch (MinioException minioEx)
            {
                throw new Exception($"Ошибка при загрузке объекта в MinIO: {minioEx.Message}", minioEx);
            }
            catch (Exception ex)
            {
                throw new Exception("Неизвестная ошибка при сохранении файла в MinIO.", ex);
            }
        }

        // Остальные методы реализации IStorageClient остаются без изменений:
        // например, GetAsync:
        public async Task<Stream> GetAsync(string bucketName, string objectKey, CancellationToken ct = default)
        {
            var ms = new MemoryStream();
            await _minioClient.GetObjectAsync(new GetObjectArgs()
                .WithBucket(bucketName)
                .WithObject(objectKey)
                .WithCallbackStream(stream => stream.CopyTo(ms)),
                ct);
            ms.Position = 0;
            return ms;
        }

        // И, если у вас есть перегрузка SaveAsync по интерфейсу IStorageClient:
        Task<string> IStorageClient.SaveAsync(string bucketName, string objectKey, Stream data, CancellationToken ct)
        {
            // Здесь можно оставить старую реализацию или просто бросить NotImplementedException,
            // если вы решили использовать только метод, принимающий byte[] + fileName.
            throw new NotImplementedException();
        }
    }
}