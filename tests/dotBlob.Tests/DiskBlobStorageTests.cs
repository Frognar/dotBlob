namespace dotBlob.Tests;

public class DiskBlobStorageTests
{
    [Fact]
    public async Task SaveAsync_EmptyStream_CreatesBlobWithZeroSize()
    {
        using var root = new TempDirectory();
        var sut = new DiskBlobStorage(new() { BasePath = root.Path });

        var result = await sut.SaveAsync(new MemoryStream());

        Assert.Equal(0, result.SizeBytes);
        Assert.True(File.Exists(result.FullPath));
    }
}
