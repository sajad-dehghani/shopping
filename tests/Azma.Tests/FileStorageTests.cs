using Azma.Infrastructure.Files;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;

namespace Azma.Tests;

public class FileStorageTests : IDisposable
{
    private readonly string _root = Path.Combine(Path.GetTempPath(), "azma-tests-" + Guid.NewGuid().ToString("N"));
    private static readonly byte[] Png = [0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A, 0, 0, 0, 0];

    private LocalFileStorage Storage => new(
        Options.Create(new FileStorageOptions { UploadsPath = Path.Combine(_root, "uploads"), PrivatePath = Path.Combine(_root, "private") }),
        NullLogger<LocalFileStorage>.Instance);

    /// <summary>Every folder an admin ImageUpload component uses must be accepted by the storage whitelist.</summary>
    [Theory]
    [InlineData("courses")]
    [InlineData("products")]
    [InlineData("instructors")]
    [InlineData("site")]
    [InlineData("slides")]
    public async Task Admin_upload_folders_are_allowed(string folder)
    {
        var path = await Storage.SaveImageAsync(new MemoryStream(Png), "image.png", folder);
        Assert.StartsWith($"uploads/{folder}/", path);
        Assert.True(File.Exists(Path.Combine(_root, path.Replace('/', Path.DirectorySeparatorChar))));
    }

    [Fact]
    public async Task Unknown_folder_is_rejected()
        => await Assert.ThrowsAsync<Azma.Application.Abstractions.FileValidationException>(
            () => Storage.SaveImageAsync(new MemoryStream(Png), "image.png", "../etc"));

    public void Dispose()
    {
        try { Directory.Delete(_root, recursive: true); } catch (IOException) { }
    }
}
