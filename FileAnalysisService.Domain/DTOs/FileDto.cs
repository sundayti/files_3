namespace FileAnalysisService.Domain.DTOs;

public class FileDto
{
    public byte[] ContentBytes { get; set; } = [];
    public string FileName     { get; set; } = string.Empty;
}
