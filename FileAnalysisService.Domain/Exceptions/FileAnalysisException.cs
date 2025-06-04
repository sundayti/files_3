namespace FileAnalysisService.Domain.Exceptions;

public class FileAnalysisException : Exception
{
    public FileAnalysisException(string message) : base(message)
    { }

    public FileAnalysisException(string message, Exception inner) : base(message, inner) 
    { }
}