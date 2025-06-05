using FileAnalysisService.Application.Commands;
using FileAnalysisService.Application.DTOs;
using FileAnalysisService.Application.Queries;
using FileAnalysisService.Domain.Exceptions;
using MediatR;
using Microsoft.AspNetCore.Mvc;

namespace FileAnalysisService.Presentation.Controllers;

// File: FileAnalysisService.Presentation/Controllers/AnalyticsController.cs


// [ApiController]
// [Route("api/[controller]")]
// public class AnalyticsController : ControllerBase
// {
//     private readonly IMediator _mediator;
//     private readonly IConfiguration _configuration;
//
//     public AnalyticsController(IMediator mediator, IConfiguration configuration)
//     {
//         _mediator = mediator;
//         _configuration = configuration;
//     }
//
//     /// <summary>
//     /// POST /api/analytics/{fileId}
//     /// – запускает анализ (или возвращает уже готовый результат).
//     /// – возвращает JSON:
//     ///   {
//     ///     "fileId": "...",
//     ///     "imageUrl": "...",
//     ///     "createdAtUtc": "...",
//     ///     "paragraphCount": ...,
//     ///     "wordCount": ...,
//     ///     "characterCount": ...
//     ///   }
//     /// </summary>
//     [HttpPost("{fileId:guid}")]
//     public async Task<IActionResult> Analyze([FromRoute] Guid fileId, CancellationToken ct)
//     {
//         AnalysisResultDto resultDto;
//         try
//         {
//             resultDto = await _mediator.Send(new AnalyzeFileCommand(fileId), ct);
//         }
//         catch (FileAnalysisException ex)
//         {
//             // 400, если не .txt или другая проверка
//             return BadRequest(new { error = ex.Message });
//         }
//         catch (Exception ex)
//         {
//             // 500 при любой иной критической ошибке
//             return StatusCode(500, new { error = $"Внутренняя ошибка сервиса анализа файла {ex.Message}." });
//         }
//
//         // Формируем абсолютный URL к контроллеру ImagesController.GetImage
//         var imageUrl = Url.Action(
//             action: nameof(ImagesController.GetImage),
//             controller: "Images",
//             values: new { imageKey = resultDto.ImageLocation },
//             protocol: Request.Scheme
//         );
//
//         var response = new
//         {
//             fileId          = resultDto.FileId,
//             imageUrl,
//             createdAtUtc    = resultDto.CreatedAtUtc,
//             paragraphCount  = resultDto.ParagraphCount,
//             wordCount       = resultDto.WordCount,
//             characterCount  = resultDto.CharacterCount
//         };
//
//         return Ok(response);
//     }
//
//     /// <summary>
//     /// GET /api/analytics/{fileId}
//     /// – если запись уже сделана, возвращаем тот же JSON, как у POST.
//     /// – если нет (ещё не проанализирован) — 404.
//     /// </summary>
//     [HttpGet("{fileId:guid}")]
//     public async Task<IActionResult> Get([FromRoute] Guid fileId, CancellationToken ct)
//     {
//         var resultDto = await _mediator.Send(new GetAnalysisQuery(fileId), ct);
//         if (resultDto is null)
//             return NotFound();
//
//         var imageUrl = Url.Action(
//             action: nameof(ImagesController.GetImage),
//             controller: "Images",
//             values: new { imageKey = resultDto.ImageLocation },
//             protocol: Request.Scheme
//         );
//
//         var response = new
//         {
//             fileId          = resultDto.FileId,
//             imageUrl,
//             createdAtUtc    = resultDto.CreatedAtUtc,
//             paragraphCount  = resultDto.ParagraphCount,
//             wordCount       = resultDto.WordCount,
//             characterCount  = resultDto.CharacterCount
//         };
//
//         return Ok(response);
//     }
// }

[ApiController]
[Route("api/[controller]")]
public class AnalyticsController : ControllerBase
{
    private readonly IMediator _mediator;
    private readonly ILogger<AnalyticsController> _logger;

    public AnalyticsController(IMediator mediator, ILogger<AnalyticsController> logger)
    {
        _mediator = mediator;
        _logger = logger;
    }

    [HttpPost("{fileId:guid}")]
    public async Task<IActionResult> Analyze([FromRoute] Guid fileId, CancellationToken ct)
    {
        AnalysisResultDto resultDto;
        try
        {
            resultDto = await _mediator.Send(new AnalyzeFileCommand(fileId), ct);
        }
        catch (FileAnalysisException ex)
        {
            return BadRequest(new { error = ex.Message });
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { error = $"Внутренняя ошибка сервиса анализа файла {ex.Message}." });
        }

        // Логируем, чтобы понять, есть ли Host и Scheme
        _logger.LogInformation("Request Scheme = {Scheme}, Host = {Host}", Request.Scheme, Request.Host);

        // Пытаемся получить относительную ссылку на картинку
        string imageUrl = null;

        // Вариант 1: Относительный URL (не передаём protocol)
        imageUrl = Url.Action(
            action: nameof(ImagesController.GetImage),
            controller: "Images",
            values: new { imageKey = resultDto.ImageLocation }
        );
        // Например: "/api/Images/abc123.png"

        // Если нужен полный URL, можно проверить Host:
        if (!string.IsNullOrEmpty(Request.Scheme) && Request.Host.HasValue)
        {
            // Вариант 2: Абсолютный URL (передаём protocol и хост)
            imageUrl = Url.Action(
                action:    nameof(ImagesController.GetImage),
                controller:"Images",
                values:     new { imageKey = resultDto.ImageLocation },
                protocol:   Request.Scheme
            );
            // Пример: "https://example.com/api/Images/abc123.png"
        }

        var response = new
        {
            fileId         = resultDto.FileId,
            imageUrl       = imageUrl, 
            createdAtUtc   = resultDto.CreatedAtUtc,
            paragraphCount = resultDto.ParagraphCount,
            wordCount      = resultDto.WordCount,
            characterCount = resultDto.CharacterCount
        };

        return Ok(response);
    }

    [HttpGet("{fileId:guid}")]
    public async Task<IActionResult> Get([FromRoute] Guid fileId, CancellationToken ct)
    {
        var resultDto = await _mediator.Send(new GetAnalysisQuery(fileId), ct);
        if (resultDto is null)
            return NotFound();

        _logger.LogInformation("Request Scheme = {Scheme}, Host = {Host}", Request.Scheme, Request.Host);

        string imageUrl = Url.Action(
            action:     nameof(ImagesController.GetImage),
            controller: "Images",
            values:     new { imageKey = resultDto.ImageLocation }
            // без protocol — отдадим относительный путь
        );

        if (!string.IsNullOrEmpty(Request.Scheme) && Request.Host.HasValue)
        {
            imageUrl = Url.Action(
                action:     nameof(ImagesController.GetImage),
                controller: "Images",
                values:     new { imageKey = resultDto.ImageLocation },
                protocol:   Request.Scheme
            );
        }

        var response = new
        {
            fileId         = resultDto.FileId,
            imageUrl       = imageUrl,
            createdAtUtc   = resultDto.CreatedAtUtc,
            paragraphCount = resultDto.ParagraphCount,
            wordCount      = resultDto.WordCount,
            characterCount = resultDto.CharacterCount
        };

        return Ok(response);
    }
}