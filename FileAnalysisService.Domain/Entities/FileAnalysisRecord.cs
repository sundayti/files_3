using FileAnalysisService.Domain.ValueObjects;

namespace FileAnalysisService.Domain.Entities;

public sealed class FileAnalysisRecord
{
    public FileId FileId { get; private set; }
    public ImageLocation CloudImageLocation { get; private set; }

    // Новые поля:
    public int ParagraphCount   { get; private set; }
    public int WordCount        { get; private set; }
    public int CharacterCount   { get; private set; }

    public DateTime CreatedAtUtc { get; private set; }

    private FileAnalysisRecord() { }  // для EF

    // Обновлённый конструктор «с полной инициализацией»
    public FileAnalysisRecord(
        FileId fileId,
        ImageLocation imageLocation,
        int paragraphCount,
        int wordCount,
        int characterCount,
        DateTime createdAtUtc
    )
    {
        FileId = fileId;
        CloudImageLocation = imageLocation;
        ParagraphCount    = paragraphCount;
        WordCount         = wordCount;
        CharacterCount    = characterCount;
        CreatedAtUtc      = createdAtUtc;
    }

    public static FileAnalysisRecord CreateNew(
        FileId fileId,
        ImageLocation imageLocation,
        int paragraphCount,
        int wordCount,
        int characterCount
    )
    {
        return new FileAnalysisRecord(
            fileId,
            imageLocation,
            paragraphCount,
            wordCount,
            characterCount,
            DateTime.UtcNow
        );
    }
}
