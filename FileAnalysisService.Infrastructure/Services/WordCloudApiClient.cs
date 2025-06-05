using FileAnalysisService.Domain.Interfaces;
using Microsoft.Extensions.Configuration;
using System.Net.Http;
using System.Net.Http.Headers;

namespace FileAnalysisService.Infrastructure.Services;

/// <summary>
/// Реализация IWordCloudApiClient, обращающаяся к QuickChart.io (GET /wordcloud?text=...).
/// </summary>
public class WordCloudApiClient : IWordCloudApiClient
{
    private readonly HttpClient _httpClient;

    public WordCloudApiClient(HttpClient httpClient)
    {
        _httpClient = httpClient;
    }

    /// <summary>
    /// Генерирует PNG с облаком слов, обращаясь по GET {BaseUrl}/wordcloud?text={Uri.EscapeDataString(text)}.
    /// QuickChart.io возвращает изображение.
    /// </summary>
    public async Task<Stream> GenerateWordCloudAsync(string text, CancellationToken ct = default)
    {
        // Конструируем URL: "/wordcloud?text=..."
        var encoded = Uri.EscapeDataString(text);
        var response = await _httpClient.GetAsync($"/wordcloud?text={encoded}", ct);
        response.EnsureSuccessStatusCode();

        var ms = new MemoryStream();
        await response.Content.CopyToAsync(ms, ct);
        ms.Seek(0, SeekOrigin.Begin);
        return ms;
    }
}