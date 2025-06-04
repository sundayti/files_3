using System;
using System.Threading;
using System.Threading.Tasks;
using FileAnalysisService.Domain.DTOs;
using FileAnalysisService.Domain.Interfaces;
using FileAnalysisService.Domain.ValueObjects;
using Grpc.Net.Client;
using Filestoring; 

namespace FileAnalysisService.Infrastructure.Services;

public class FileStorageGrpcClient : IFileStorageClient
{
    private readonly FileStorage.FileStorageClient _grpcClient;

    public FileStorageGrpcClient(string grpcEndpoint)
    {
        var channel = GrpcChannel.ForAddress(grpcEndpoint);
        _grpcClient = new FileStorage.FileStorageClient(channel);
    }

    public async Task<FileDto> GetFileAsync(FileId fileId, CancellationToken ct = default)
    {
        var request = new GetFileRequest { FileId = fileId.Value.ToString() };
        GetFileResponse response;
        try
        {
            response = await _grpcClient.GetFileAsync(request, cancellationToken: ct);
        }
        catch (Exception ex)
        {
            throw new Exception($"gRPC вызов к FileStoringService завершился неудачей: {ex.Message}", ex);
        }

        byte[] bytes;
        try
        {
            bytes = Convert.FromBase64String(response.ContentBase64);
        }
        catch (FormatException ex)
        {
            throw new Exception("Полученный из FileStoringService Base64-контент некорректен", ex);
        }

        return new FileDto
        {
            ContentBytes = bytes,
            FileName     = response.FileName
        };
    }
}
