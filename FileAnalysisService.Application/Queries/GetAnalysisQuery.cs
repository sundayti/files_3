using FileAnalysisService.Application.DTOs;
using MediatR;

namespace FileAnalysisService.Application.Queries;

/// <summary>
/// Запрос только для получения уже созданного результата анализа (не генерировать заново).
/// Если результата нет → можно вернуть автоматически NotFound.
/// </summary>
public class GetAnalysisQuery : IRequest<AnalysisResultDto?>
{
    public Guid FileId { get; }

    public GetAnalysisQuery(Guid fileId) => FileId = fileId;
}
