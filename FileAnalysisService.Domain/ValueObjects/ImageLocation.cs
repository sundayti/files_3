namespace FileAnalysisService.Domain.ValueObjects;

/// <summary>
/// Локация объекта в MinIO (например, ключ в бакете).
/// </summary>
public sealed record ImageLocation
{
    public string Value { get; }

    public ImageLocation(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
            throw new ArgumentException("Location не может быть пустой строкой.", nameof(value));

        Value = value;
    }

    public override string ToString() => Value;
}