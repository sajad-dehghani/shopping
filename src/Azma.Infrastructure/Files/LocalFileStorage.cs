using Azma.Application.Abstractions;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Azma.Infrastructure.Files;

public sealed class FileStorageOptions
{
    public const string SectionName = "Storage";
    /// <summary>Absolute (or content-root relative) folder for uploads. Served at /uploads.</summary>
    public string UploadsPath { get; set; } = "uploads";
    public int MaxImageSizeKb { get; set; } = 3072;
}

/// <summary>
/// Stores uploaded images on disk outside wwwroot. Only whitelisted folders and image types are accepted,
/// the content signature is checked, and files get random names so user input never reaches the file system path.
/// </summary>
public sealed class LocalFileStorage(IOptions<FileStorageOptions> options, ILogger<LocalFileStorage> logger) : IFileStorage
{
    private static readonly HashSet<string> AllowedFolders = ["courses", "products", "instructors", "site"];
    private static readonly Dictionary<string, byte[][]> Signatures = new(StringComparer.OrdinalIgnoreCase)
    {
        [".jpg"] = [[0xFF, 0xD8, 0xFF]],
        [".jpeg"] = [[0xFF, 0xD8, 0xFF]],
        [".png"] = [[0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A]],
        [".webp"] = [[0x52, 0x49, 0x46, 0x46]], // "RIFF" + "WEBP" at offset 8 (checked below)
    };

    public string RootPath => options.Value.UploadsPath;

    public async Task<string> SaveImageAsync(Stream content, string originalFileName, string folder, CancellationToken cancellationToken = default)
    {
        if (!AllowedFolders.Contains(folder)) throw new FileValidationException("پوشه مقصد نامعتبر است.");
        var ext = Path.GetExtension(originalFileName).ToLowerInvariant();
        if (!Signatures.ContainsKey(ext)) throw new FileValidationException("فقط تصاویر JPG، PNG و WebP مجاز هستند.");

        var max = options.Value.MaxImageSizeKb * 1024L;
        using var buffer = new MemoryStream();
        var chunk = new byte[81920];
        int read;
        while ((read = await content.ReadAsync(chunk, cancellationToken)) > 0)
        {
            if (buffer.Length + read > max) throw new FileValidationException($"حجم تصویر نباید بیشتر از {max / 1024} کیلوبایت باشد.");
            buffer.Write(chunk, 0, read);
        }
        var bytes = buffer.ToArray();
        if (!HasValidSignature(bytes, ext)) throw new FileValidationException("محتوای فایل با نوع تصویر مطابقت ندارد.");

        var dir = Path.Combine(RootPath, folder);
        Directory.CreateDirectory(dir);
        var name = $"{Guid.NewGuid():N}{(ext == ".jpeg" ? ".jpg" : ext)}";
        await File.WriteAllBytesAsync(Path.Combine(dir, name), bytes, cancellationToken);
        logger.LogInformation("Stored upload {Folder}/{Name} ({Size} bytes)", folder, name, bytes.Length);
        return $"uploads/{folder}/{name}";
    }

    public Task DeleteAsync(string? relativePath)
    {
        if (string.IsNullOrWhiteSpace(relativePath) || !relativePath.StartsWith("uploads/", StringComparison.Ordinal)) return Task.CompletedTask;
        var root = Path.GetFullPath(RootPath);
        var full = Path.GetFullPath(Path.Combine(root, relativePath["uploads/".Length..]));
        if (!full.StartsWith(root + Path.DirectorySeparatorChar, StringComparison.Ordinal)) return Task.CompletedTask;
        try
        {
            if (File.Exists(full)) File.Delete(full);
        }
        catch (IOException ex)
        {
            logger.LogWarning(ex, "Could not delete {Path}", full);
        }
        return Task.CompletedTask;
    }

    private static bool HasValidSignature(byte[] bytes, string ext)
    {
        var ok = Signatures[ext].Any(sig => bytes.Length >= sig.Length && bytes.AsSpan(0, sig.Length).SequenceEqual(sig));
        if (ok && ext == ".webp")
            ok = bytes.Length >= 12 && bytes[8] == 'W' && bytes[9] == 'E' && bytes[10] == 'B' && bytes[11] == 'P';
        return ok;
    }
}
