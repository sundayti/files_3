using FileAnalysisService.Domain.Entities;
using FileAnalysisService.Domain.ValueObjects;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;

namespace FileAnalysisService.Infrastructure.Persistence;

public class FileAnalyticsDbContext : DbContext
{
    public FileAnalyticsDbContext(DbContextOptions<FileAnalyticsDbContext> options)
        : base(options)
    { }

    public DbSet<FileAnalysisRecord> FileAnalyses { get; set; } = null!;

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<FileAnalysisRecord>(entity =>
        {
            entity.ToTable("file_analyses");
            entity.HasKey(x => x.FileId);

            // Конвертер для FileId <-> Guid
            var fileIdConverter = new ValueConverter<FileId, Guid>(
                v => v.Value,
                v => new FileId(v));

            // Конвертер для ImageLocation <-> string
            var imageLocationConverter = new ValueConverter<ImageLocation, string>(
                v => v.Value,
                v => new ImageLocation(v));

            entity.Property(x => x.FileId)
                .HasColumnName("file_id")
                .HasConversion(fileIdConverter)
                .IsRequired();

            entity.Property(x => x.CloudImageLocation)
                .HasColumnName("image_location")
                .HasConversion(imageLocationConverter)
                .IsRequired();

            entity.Property(x => x.ParagraphCount)
                .HasColumnName("paragraph_count")
                .IsRequired();

            entity.Property(x => x.WordCount)
                .HasColumnName("word_count")
                .IsRequired();

            entity.Property(x => x.CharacterCount)
                .HasColumnName("character_count")
                .IsRequired();

            entity.Property(x => x.CreatedAtUtc)
                .HasColumnName("created_at_utc")
                .IsRequired();
        });
    }
}