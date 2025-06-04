namespace FileAnalysisService.Domain.Interfaces;

// Domain/Interfaces/IWordCloudApiClient.cs
using System.IO;
using System.Threading;
using System.Threading.Tasks;

public interface IWordCloudApiClient
{
    /// <summary>
    /// Принимает полный текст, возвращает поток PNG.
    /// </summary>
    Task<Stream> GenerateWordCloudAsync(string text, CancellationToken ct = default);
}
