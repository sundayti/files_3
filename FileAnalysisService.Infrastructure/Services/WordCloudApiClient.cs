using FileAnalysisService.Domain.Interfaces;

using Microsoft.Extensions.Options;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;


namespace FileAnalysisService.Infrastructure.Services;



public class WordCloudApiClientSettings
{
    public string BaseUrl { get; set; } = default!;  // например, "https://api.wordcloud.example.com"
    public string ApiKey  { get; set; } = string.Empty;
}

public class WordCloudApiClient : IWordCloudApiClient
{
    private readonly HttpClient _httpClient;
    private readonly WordCloudApiClientSettings _settings;

    public WordCloudApiClient(HttpClient http, IOptions<WordCloudApiClientSettings> options)
    {
        _settings = options.Value;
        _httpClient = http;
        _httpClient.BaseAddress = new Uri(_settings.BaseUrl);
        if (!string.IsNullOrEmpty(_settings.ApiKey))
        {
            _httpClient.DefaultRequestHeaders.Authorization =
                new AuthenticationHeaderValue("Bearer", _settings.ApiKey);
        }
    }

    public async Task<Stream> GenerateWordCloudAsync(string text, CancellationToken ct = default)
    {
        var payload = new { text, width = 800, height = 600, format = "png" };
        var content = new StringContent(JsonSerializer.Serialize(payload), Encoding.UTF8, "application/json");

        var response = await _httpClient.PostAsync("/generate", content, ct);
        response.EnsureSuccessStatusCode();

        var ms = new MemoryStream();
        await response.Content.CopyToAsync(ms, ct);
        ms.Position = 0;
        return ms;
    }
}
