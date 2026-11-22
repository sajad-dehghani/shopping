namespace Azma.Application.Abstractions;

public interface IFileStorage
{
    /// <summary>Validates and stores an image. Returns the public relative path (e.g. "uploads/courses/x.webp").</summary>
    Task<string> SaveImageAsync(Stream content, string originalFileName, string folder, CancellationToken cancellationToken = default);
    Task DeleteAsync(string? relativePath);
}

public sealed class FileValidationException(string message) : Exception(message);
