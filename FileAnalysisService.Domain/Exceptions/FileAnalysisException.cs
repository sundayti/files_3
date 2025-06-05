namespace FileAnalysisService.Domain.Exceptions;

/// <summary>
/// Специальное исключение для ошибок анализа (например, несоответствие формата).
/// </summary>
public class FileAnalysisException(string message) : Exception(message);