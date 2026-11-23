namespace Azma.Application.Abstractions;

public interface IFileStorage
{
    /// <summary>Validates and stores an image. Returns the public relative path (e.g. "uploads/courses/x.webp").</summary>
    Task<string> SaveImageAsync(Stream content, string originalFileName, string folder, CancellationToken cancellationToken = default);
    Task DeleteAsync(string? relativePath);

    /// <summary>
    /// Validates and stores a private image (e.g. a payment receipt) outside the public uploads folder.
    /// Returns an opaque path (e.g. "private/receipts/x.jpg") that can only be read back with <see cref="OpenPrivate"/>.
    /// </summary>
    Task<string> SavePrivateImageAsync(Stream content, string originalFileName, string folder, CancellationToken cancellationToken = default);

    /// <summary>Opens a private file for reading, or null if it does not exist. Returns the content type too.</summary>
    (Stream Content, string ContentType)? OpenPrivate(string? privatePath);
}

public sealed class FileValidationException(string message) : Exception(message);
