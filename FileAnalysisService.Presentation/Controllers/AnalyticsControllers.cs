using FileAnalysisService.Application.Commands;
using FileAnalysisService.Application.DTOs;
using FileAnalysisService.Application.Queries;
using FileAnalysisService.Domain.Exceptions;
using MediatR;
using Microsoft.AspNetCore.Mvc;

namespace FileAnalysisService.Presentation.Controllers;

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

    /// <summary>
    /// POST api/analytics/{fileId}
    /// 1) Если результат в БД уже есть — возвращаем его (ссылка на PNG и статистика).
    /// 2) Иначе — запускаем полный анализ, создаём «облако слов», сохраняем, возвращаем результат.
    /// </summary>
    [HttpPost("{fileId:guid}")]
    public async Task<IActionResult> Analyze([FromRoute] Guid fileId, CancellationToken ct)
    {
        try
        {
            // Вызываем команду MediatR
            var resultDto = await _mediator.Send(new AnalyzeFileCommand(fileId), ct);
            if (resultDto is null)
                return NotFound();

            _logger.LogInformation("Request Scheme = {Scheme}, Host = {Host}", Request.Scheme, Request.Host);

            // Конструируем URL для получения PNG
            string imageUrl = Url.Action(
                action:     nameof(ImagesController.GetImage),
                controller: "Images",
                values:     new { imageKey = resultDto.ImageLocation });

            // Если нужно абсолютную ссылку (с протоколом и хостом)
            if (!string.IsNullOrEmpty(Request.Scheme) && Request.Host.HasValue)
            {
                imageUrl = Url.Action(
                    action:     nameof(ImagesController.GetImage),
                    controller: "Images",
                    values:     new { imageKey = resultDto.ImageLocation },
                    protocol:   Request.Scheme);
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
        catch (FileAnalysisException ex)
        {
            // Если мы намеренно бросили ошибку «не .txt» или «декод не прошёл»
            return BadRequest(new { error = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Ошибка при анализе файла {FileId}", fileId);
            return StatusCode(StatusCodes.Status500InternalServerError,
                new { error = "Внутренняя ошибка сервера" });
        }
    }
}