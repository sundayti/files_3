namespace FileAnalysisService.Application.DTOs;

public class AnalysisResultDto
{
    public string FileId       { get; set; } = default!;
    public string ImageLocation{ get; set; } = default!;
    public DateTime CreatedAtUtc { get; set; }

    // Новые поля статистики:
    public int ParagraphCount  { get; set; }
    public int WordCount       { get; set; }
    public int CharacterCount  { get; set; }
}