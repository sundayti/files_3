using FileAnalysisService.Application.DTOs;
using MediatR;
namespace FileAnalysisService.Application.Commands;

/// <summary>
/// Запрос на анализ файла
/// </summary>
public class AnalyzeFileCommand : IRequest<AnalysisResultDto>
{
    public Guid FileId { get; }
    public AnalyzeFileCommand(Guid fileId) => FileId = fileId;
}
