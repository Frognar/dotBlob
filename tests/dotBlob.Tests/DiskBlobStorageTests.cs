using System.Text;

namespace dotBlob.Tests;

public class DiskBlobStorageTests
{
    [Fact]
    public async Task SaveAsync_EmptyStream_CreatesBlobWithZeroSize()
    {
        using var root = new TempDirectory();
        var sut = new DiskBlobStorage(new DiskBlobStorageOptions { BasePath = root.Path });

        var result = await sut.SaveAsync(new MemoryStream());

        Assert.Equal(0, result.SizeBytes);
        Assert.True(File.Exists(result.FullPath));
    }

    [Fact]
    public async Task SaveAsync_NonEmptyStream_WritesCorrectSizeAndContent()
    {
        using var root = new TempDirectory();
        var sut = new DiskBlobStorage(new DiskBlobStorageOptions { BasePath = root.Path });
        var bytes = "hello blob"u8.ToArray();

        var result = await sut.SaveAsync(new MemoryStream(bytes));

        Assert.Equal(bytes.Length, result.SizeBytes);
        Assert.Equal(bytes, await File.ReadAllBytesAsync(result.FullPath));
    }

    [Fact]
    public async Task SaveAsync_NullStream_ThrowsArgumentNullException()
    {
        using var root = new TempDirectory();
        var sut = new DiskBlobStorage(new DiskBlobStorageOptions { BasePath = root.Path });

        await Assert.ThrowsAsync<ArgumentNullException>(() => sut.SaveAsync(null!));
    }

    [Fact]
    public void Ctor_EmptyBasePath_ThrowsArgumentException()
    {
        Assert.Throws<ArgumentException>(
            () => new DiskBlobStorage(new DiskBlobStorageOptions { BasePath = "" }));
    }
}
