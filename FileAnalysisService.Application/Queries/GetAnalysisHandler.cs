using FileAnalysisService.Application.DTOs;
using FileAnalysisService.Domain.Interfaces;
using FileAnalysisService.Domain.ValueObjects;
using MediatR;

namespace FileAnalysisService.Application.Queries;

public class GetAnalysisHandler : IRequestHandler<GetAnalysisQuery, AnalysisResultDto?>
{
    private readonly IFileAnalysisRepository _repository;

    public GetAnalysisHandler(IFileAnalysisRepository repository)
    {
        _repository = repository;
    }

    public async Task<AnalysisResultDto?> Handle(GetAnalysisQuery request, CancellationToken ct)
    {
        var fileIdVo = FileId.From(request.FileId);
        var record = await _repository.GetByFileIdAsync(fileIdVo, ct);
        if (record is null)
            return null;

        return new AnalysisResultDto
        {
            FileId = record.FileId.Value.ToString(),
            ImageLocation = record.CloudImageLocation.Value,
            CreatedAtUtc = record.CreatedAtUtc
        };
    }
}