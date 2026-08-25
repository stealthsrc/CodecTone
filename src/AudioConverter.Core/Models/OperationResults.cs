namespace AudioConverter.Core.Models;

public sealed record OperationProgress(double Fraction, string Status);

public sealed record FileResult(string InputPath, string? OutputPath, string? Error)
{
    public bool Succeeded => Error is null;
}

public sealed record BatchResult(IReadOnlyList<FileResult> Files)
{
    public int Succeeded => Files.Count(file => file.Succeeded);
    public int Failed => Files.Count - Succeeded;
}
