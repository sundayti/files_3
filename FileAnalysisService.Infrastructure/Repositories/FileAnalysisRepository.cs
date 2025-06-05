using FileAnalysisService.Domain.Entities;
using FileAnalysisService.Domain.Interfaces;
using FileAnalysisService.Domain.ValueObjects;
using FileAnalysisService.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace FileAnalysisService.Infrastructure.Repositories;

public class FileAnalysisRepository : IFileAnalysisRepository
{
    private readonly FileAnalyticsDbContext _dbContext;

    public FileAnalysisRepository(FileAnalyticsDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<FileAnalysisRecord?> GetByFileIdAsync(FileId fileId, CancellationToken ct = default)
    {
        return await _dbContext.FileAnalyses
            .AsNoTracking()
            .FirstOrDefaultAsync(x => x.FileId == fileId, ct);
    }

    public async Task AddAsync(FileAnalysisRecord record, CancellationToken ct = default)
    {
        _dbContext.FileAnalyses.Add(record);
        await _dbContext.SaveChangesAsync(ct);
    }
}