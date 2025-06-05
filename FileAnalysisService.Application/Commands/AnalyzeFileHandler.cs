using FileAnalysisService.Application.DTOs;
using FileAnalysisService.Domain.DTOs;
using FileAnalysisService.Domain.Entities;
using FileAnalysisService.Domain.Interfaces;
using FileAnalysisService.Domain.ValueObjects;
using MediatR;
using System.Text;
using System.Text.RegularExpressions;
using FileAnalysisService.Application.Interfaces;
using FileAnalysisService.Domain.Exceptions;
using Microsoft.Extensions.Configuration;

namespace FileAnalysisService.Application.Commands;

public class AnalyzeFileHandler : IRequestHandler<AnalyzeFileCommand, AnalysisResultDto>
{
    private readonly IFileAnalysisRepository _repository;
    private readonly IFileStorageClient _fileStorageClient;  // gRPC‐клиент к FileStoringService
    private readonly IStorageClient _storageClient;          // MinIO‐клиент (для PNG)
    private readonly IWordCloudApiClient _wordCloudClient;   // HTTP‐клиент для QuickChart
    private readonly IConfiguration _configuration;

    public AnalyzeFileHandler(
        IFileAnalysisRepository repository,
        IFileStorageClient fileStorageClient,
        IStorageClient storageClient,
        IWordCloudApiClient wordCloudClient,
        IConfiguration configuration)
    {
        _repository = repository;
        _fileStorageClient = fileStorageClient;
        _storageClient = storageClient;
        _wordCloudClient = wordCloudClient;
        _configuration = configuration;
    }

    public async Task<AnalysisResultDto> Handle(AnalyzeFileCommand request, CancellationToken ct)
    {
        var fileIdVo = new FileId(request.FileId);

        // 1) Проверяем, есть ли уже в БД результат анализа
        var existing = await _repository.GetByFileIdAsync(fileIdVo, ct);
        if (existing is not null)
        {
            // Вернём кешированный результат (без повторного анализа)
            return new AnalysisResultDto
            {
                FileId = existing.FileId.Value.ToString(),
                ImageLocation = existing.CloudImageLocation.Value,
                CreatedAtUtc = existing.CreatedAtUtc,
                ParagraphCount = existing.ParagraphCount,
                WordCount = existing.WordCount,
                CharacterCount = existing.CharacterCount
            };
        }

        // 2) Иначе запрашиваем байты файла через gRPC от FileStoringService
        var fileDto = await _fileStorageClient.GetFileAsync(fileIdVo, ct);

        // 3) Проверяем, что это .txt (простейшая проверка по расширению)
        if (!fileDto.FileName.EndsWith(".txt", StringComparison.OrdinalIgnoreCase))
            throw new FileAnalysisException("Анализ возможен только для текстовых файлов .txt");

        // 4) Получаем полные текстовые данные (UTF‐8)
        string text;
        try
        {
            text = Encoding.UTF8.GetString(fileDto.ContentBytes);
        }
        catch
        {
            throw new FileAnalysisException("Не удалось декодировать содержимое файла как UTF-8.");
        }

        // 5) Считаем статистику:
        //    – кол-во абзацев (разбиваем по "\r\n" или "\n")
        var paragraphCount = text
            .Split(new[] { "\r\n", "\n" }, StringSplitOptions.RemoveEmptyEntries)
            .Length;

        //    – кол-во слов (регуляркой \w+)
        var wordCount = CountWords(text);

        //    – кол-во символов (за исключением '\r')
        var characterCount = CountCharacters(text);

        // 6) Генерируем PNG с облаком слов через QuickChart (WordCloudApiClient).
        using var imageStream = await _wordCloudClient.GenerateWordCloudAsync(text, ct);

        // 7) Сохраняем PNG в MinIO
        var bucketName = _configuration.GetValue<string>("Minio:BucketName", "analytics-images");
        // Создадим «уникальный» ключ, например: {fileId}/{UnixTimeMilliseconds}.png
        var objectKey = $"{request.FileId}/{DateTimeOffset.UtcNow.ToUnixTimeMilliseconds()}.png";
        // Сохраняем в minio, получаем тот же objectKey
        var savedKey = await _storageClient.SaveAsync(bucketName, objectKey, imageStream, ct);

        // 8) Записываем данные в БД
        var record = FileAnalysisRecord.CreateNew(
            fileIdVo,
            new ImageLocation(savedKey),
            paragraphCount,
            wordCount,
            characterCount);

        await _repository.AddAsync(record, ct);

        // 9) Возвращаем DTO с результатом
        return new AnalysisResultDto
        {
            FileId = record.FileId.Value.ToString(),
            ImageLocation = record.CloudImageLocation.Value,
            CreatedAtUtc = record.CreatedAtUtc,
            ParagraphCount = record.ParagraphCount,
            WordCount = record.WordCount,
            CharacterCount = record.CharacterCount
        };
    }

    private int CountWords(string text)
    {
        var matches = Regex.Matches(text, @"\w+");
        return matches.Count;
    }

    private int CountCharacters(string text)
    {
        // считаем все символы, кроме '\r'
        return text.Where(c => c != '\r').Count();
    }
}