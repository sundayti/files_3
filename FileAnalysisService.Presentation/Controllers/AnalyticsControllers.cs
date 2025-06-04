using FileAnalysisService.Application.Commands;
using FileAnalysisService.Application.DTOs;
using FileAnalysisService.Application.Queries;
using FileAnalysisService.Domain.Exceptions;
using MediatR;
using Microsoft.AspNetCore.Mvc;

namespace FileAnalysisService.Presentation.Controllers;

// File: FileAnalysisService.Presentation/Controllers/AnalyticsController.cs


[ApiController]
[Route("api/[controller]")]
public class AnalyticsController : ControllerBase
{
    private readonly IMediator _mediator;
    private readonly IConfiguration _configuration;

    public AnalyticsController(IMediator mediator, IConfiguration configuration)
    {
        _mediator = mediator;
        _configuration = configuration;
    }

    /// <summary>
    /// POST /api/analytics/{fileId}
    /// – запускает анализ (или возвращает уже готовый результат).
    /// – возвращает JSON:
    ///   {
    ///     "fileId": "...",
    ///     "imageUrl": "...",
    ///     "createdAtUtc": "...",
    ///     "paragraphCount": ...,
    ///     "wordCount": ...,
    ///     "characterCount": ...
    ///   }
    /// </summary>
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
            // 400, если не .txt или другая проверка
            return BadRequest(new { error = ex.Message });
        }
        catch
        {
            // 500 при любой иной критической ошибке
            return StatusCode(500, new { error = "Внутренняя ошибка сервиса анализа файла." });
        }

        // Формируем абсолютный URL к контроллеру ImagesController.GetImage
        var imageUrl = Url.Action(
            action: nameof(ImagesController.GetImage),
            controller: "Images",
            values: new { imageKey = resultDto.ImageLocation },
            protocol: Request.Scheme
        );

        var response = new
        {
            fileId          = resultDto.FileId,
            imageUrl,
            createdAtUtc    = resultDto.CreatedAtUtc,
            paragraphCount  = resultDto.ParagraphCount,
            wordCount       = resultDto.WordCount,
            characterCount  = resultDto.CharacterCount
        };

        return Ok(response);
    }

    /// <summary>
    /// GET /api/analytics/{fileId}
    /// – если запись уже сделана, возвращаем тот же JSON, как у POST.
    /// – если нет (ещё не проанализирован) — 404.
    /// </summary>
    [HttpGet("{fileId:guid}")]
    public async Task<IActionResult> Get([FromRoute] Guid fileId, CancellationToken ct)
    {
        var resultDto = await _mediator.Send(new GetAnalysisQuery(fileId), ct);
        if (resultDto is null)
            return NotFound();

        var imageUrl = Url.Action(
            action: nameof(ImagesController.GetImage),
            controller: "Images",
            values: new { imageKey = resultDto.ImageLocation },
            protocol: Request.Scheme
        );

        var response = new
        {
            fileId          = resultDto.FileId,
            imageUrl,
            createdAtUtc    = resultDto.CreatedAtUtc,
            paragraphCount  = resultDto.ParagraphCount,
            wordCount       = resultDto.WordCount,
            characterCount  = resultDto.CharacterCount
        };

        return Ok(response);
    }
}