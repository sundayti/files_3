using FileAnalysisService.Domain.DTOs;
using FileAnalysisService.Domain.Interfaces;
using FileAnalysisService.Domain.ValueObjects;
using Filestoring;
using Google.Protobuf;

namespace FileAnalysisService.Infrastructure.Services;

using System.Threading;
using System.Threading.Tasks;
using Filestoring;
using Google.Protobuf;
using Grpc.Net.Client;

/// <summary>
/// Обёртка над сгенерированным gRPC-клиентом Filestoring.FileStorageClient.
/// Позволяет получить файл из первого сервиса по его ID.
/// </summary>
public class FileStorageGrpcClient
{
    private readonly Filestoring.FileStorage.FileStorageClient _grpcClient;

    public FileStorageGrpcClient(Filestoring.FileStorage.FileStorageClient grpcClient)
    {
        _grpcClient = grpcClient;
    }

    /// <summary>
    /// Запускает метод DownloadFile у FileStoringService.
    /// Возвращает кортеж: (байты файла, имя файла, contentType).
    /// </summary>
    public async Task<(byte[] Content, string FileName, string ContentType)> DownloadFileAsync(
        Guid fileId,
        CancellationToken ct)
    {
        var request = new FileRequest { FileId = fileId.ToString() };
        var reply = await _grpcClient.DownloadFileAsync(request, cancellationToken: ct);
        // reply.Content – ByteString, .ToByteArray() даст byte[]
        return (
            Content    : reply.Content.ToByteArray(),
            FileName   : reply.FileName,
            ContentType: reply.ContentType
        );
    }

    /// <summary>
    /// (Если нужно) метод UploadFileAsync – для загрузки файлов в первый сервис.
    /// </summary>
    public async Task<Guid> UploadFileAsync(byte[] content, string fileName, CancellationToken ct)
    {
        var request = new UploadFileRequest
        {
            Content  = ByteString.CopyFrom(content),
            FileName = fileName
        };
        var reply = await _grpcClient.UploadFileAsync(request, cancellationToken: ct);
        return Guid.Parse(reply.FileId);
    }
}