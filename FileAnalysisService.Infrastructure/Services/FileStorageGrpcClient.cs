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

public class FileStorageGrpcClient : IFileStorageClient
{
    private readonly FileStorage.FileStorageClient _client;

    public FileStorageGrpcClient(FileStorage.FileStorageClient client)
    {
        _client = client;
    }

    public async Task<FileDto> GetFileAsync(FileId fileId, CancellationToken ct = default)
    {
        var request = new FileRequest { FileId = fileId.Value.ToString() };

        var reply = await _client.DownloadFileAsync(request, cancellationToken: ct);

        return new FileDto
        {
            ContentBytes = reply.Content.ToByteArray(),
            FileName = reply.FileName,
        };
    }
}