using FileAnalysisService.Domain.Entities;
using FileAnalysisService.Domain.ValueObjects;

namespace FileAnalysisService.Domain.Interfaces;

public interface IFileAnalysisRepository
{
    Task<FileAnalysisRecord?> GetByFileIdAsync(FileId fileId, CancellationToken ct = default);
    Task AddAsync(FileAnalysisRecord record, CancellationToken ct = default);
}