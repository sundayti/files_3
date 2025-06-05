using FileAnalysisService.Domain.DTOs;
using FileAnalysisService.Domain.Interfaces;
using FileAnalysisService.Domain.ValueObjects;
using Filestoring;                  // Пространство имён из сгенерированного gRPC
using Google.Protobuf;
using Grpc.Net.Client;
using Microsoft.Extensions.Configuration;

namespace FileAnalysisService.Infrastructure.Services;

/// <summary>
/// Обёртка над сгенерированным gRPC-клиентом Filestoring.FileStorageClient.
/// </summary>
public class FileStorageGrpcClient : IFileStorageClient
{
    private readonly FileStorage.FileStorageClient _grpcClient;

    public FileStorageGrpcClient(IConfiguration configuration)
    {
        // Читаем URL (например, "http://84.201.169.225:5001")
        var url = configuration.GetValue<string>("GrpcSettings:FileStorageUrl")
                  ?? throw new ArgumentException("GrpcSettings:FileStorageUrl не задан в конфигурации");
        var channel = GrpcChannel.ForAddress(url);
        _grpcClient = new FileStorage.FileStorageClient(channel);
    }

    public async Task<FileDto> GetFileAsync(FileId fileId, CancellationToken ct = default)
    {
        var request = new FileRequest { FileId = fileId.Value.ToString() };
        var reply = await _grpcClient.DownloadFileAsync(request, cancellationToken: ct);

        return new FileDto
        {
            ContentBytes = reply.Content.ToByteArray(),
            FileName = reply.FileName
        };
    }
}