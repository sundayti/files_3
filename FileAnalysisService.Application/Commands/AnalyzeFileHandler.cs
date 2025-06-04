using FileAnalysisService.Application.DTOs;
using FileAnalysisService.Domain.DTOs;        // FileDto
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
    private readonly IFileStorageClient _fileStorageClient;  // теперь возвращает FileDto
    private readonly IStorageClient _storageClient;          // MinIO
    private readonly IWordCloudApiClient _wordCloudClient;   // HTTP-клиент к WordCloud
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
        var fileIdVo = FileId.From(request.FileId);

        // 1) Проверяем, есть ли уже запись в БД
        var existing = await _repository.GetByFileIdAsync(fileIdVo, ct);
        if (existing is not null)
        {
            return new AnalysisResultDto
            {
                FileId         = existing.FileId.Value.ToString(),
                ImageLocation  = existing.CloudImageLocation.Value,
                CreatedAtUtc   = existing.CreatedAtUtc,
                ParagraphCount = existing.ParagraphCount,
                WordCount      = existing.WordCount,
                CharacterCount = existing.CharacterCount
            };
        }

        // 2) Запрашиваем файл у первого сервиса
        FileDto fileDto;
        try
        {
            fileDto = await _fileStorageClient.GetFileAsync(fileIdVo, ct);
        }
        catch (Exception ex)
        {
            throw new FileAnalysisException($"Не удалось получить файл {request.FileId} из FileStoringService", ex);
        }

        // 3) Проверяем расширение .txt (case-insensitive)
        if (!fileDto.FileName.EndsWith(".txt", StringComparison.OrdinalIgnoreCase))
        {
            throw new FileAnalysisException($"Формат файла не поддерживается: {fileDto.FileName}. Ожидается .txt.");
        }

        // 4) Конвертируем байты в строку (UTF-8)
        string text;
        try
        {
            text = Encoding.UTF8.GetString(fileDto.ContentBytes);
        }
        catch (Exception ex)
        {
            throw new FileAnalysisException("Ошибка при декодировании .txt как UTF-8.", ex);
        }

        // 5) Считаем статистику
        int paragraphCount = CountParagraphs(text);
        int wordCount      = CountWords(text);
        int charCount      = CountCharacters(text);

        // 6) Генерируем облако слов (HTTP-клиент WordCloud API)
        Stream wordCloudStream;
        try
        {
            wordCloudStream = await _wordCloudClient.GenerateWordCloudAsync(text, ct);
        }
        catch (Exception ex)
        {
            throw new FileAnalysisException($"WordCloud API отказал для файла {fileDto.FileName}", ex);
        }

        // 7) Сохраняем картинку в MinIO
        var bucketName = _configuration.GetValue<string>("Minio:BucketName", "analytics-images");
        var imageKey   = $"{request.FileId}.png";
        try
        {
            await _storageClient.SaveAsync(bucketName, imageKey, wordCloudStream, ct);
        }
        catch (Exception ex)
        {
            throw new FileAnalysisException($"Не удалось сохранить изображение WordCloud для файла {fileDto.FileName}", ex);
        }

        // 8) Создаём и сохраняем запись в БД с статистикой
        var record = FileAnalysisRecord.CreateNew(
            fileIdVo,
            new ImageLocation(imageKey),
            paragraphCount,
            wordCount,
            charCount
        );

        try
        {
            await _repository.AddAsync(record, ct);
        }
        catch (Exception ex)
        {
            throw new FileAnalysisException($"Не удалось сохранить запись анализа для файла {fileDto.FileName}", ex);
        }

        // 9) Возвращаем DTO с результатом и статистикой
        return new AnalysisResultDto
        {
            FileId         = record.FileId.Value.ToString(),
            ImageLocation  = record.CloudImageLocation.Value,
            CreatedAtUtc   = record.CreatedAtUtc,
            ParagraphCount = record.ParagraphCount,
            WordCount      = record.WordCount,
            CharacterCount = record.CharacterCount
        };
    }

    private int CountParagraphs(string text)
    {
        var normalized = text.Replace("\r\n", "\n").Replace("\r", "\n");
        var parts = normalized.Split(new[] { "\n\n" }, StringSplitOptions.RemoveEmptyEntries);
        return parts.Length;
    }

    private int CountWords(string text)
    {
        var matches = Regex.Matches(text, @"\w+");
        return matches.Count;
    }

    private int CountCharacters(string text)
    {
        // считаем все, кроме CR (\r)
        return text.Where(c => c != '\r').Count();
    }
}
