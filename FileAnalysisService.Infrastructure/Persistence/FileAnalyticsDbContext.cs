using FileAnalysisService.Domain.Entities;
using FileAnalysisService.Domain.ValueObjects;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;
using System;

namespace FileAnalysisService.Infrastructure.Persistence;

public class FileAnalyticsDbContext : DbContext
{
    public FileAnalyticsDbContext(DbContextOptions<FileAnalyticsDbContext> options)
        : base(options) { }

    public DbSet<FileAnalysisRecord> FileAnalyses { get; set; } = null!;

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.Entity<FileAnalysisRecord>(entity =>
        {
            entity.ToTable("file_analyses");

            // ID как GUID
            var fileIdConverter = new ValueConverter<FileId, Guid>(
                v => v.Value,
                v => FileId.From(v)
            );
            entity.HasKey(x => x.FileId);
            entity.Property(x => x.FileId)
                .HasConversion(fileIdConverter)
                .HasColumnName("file_id")
                .IsRequired();

            // ImageLocation как строка
            var imageLocConverter = new ValueConverter<ImageLocation, string>(
                v => v.Value,
                v => new ImageLocation(v)
            );
            entity.Property(x => x.CloudImageLocation)
                .HasConversion(imageLocConverter)
                .HasColumnName("image_location")
                .HasMaxLength(1024)
                .IsRequired();

            // Новые колонки статистики:
            entity.Property(x => x.ParagraphCount)
                .HasColumnName("paragraph_count")
                .IsRequired();

            entity.Property(x => x.WordCount)
                .HasColumnName("word_count")
                .IsRequired();

            entity.Property(x => x.CharacterCount)
                .HasColumnName("character_count")
                .IsRequired();

            // Дата создания
            entity.Property(x => x.CreatedAtUtc)
                .HasColumnName("created_at_utc")
                .IsRequired();
        });
    }
}
