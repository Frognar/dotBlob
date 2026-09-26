namespace dotBlob.Tests;

public class DiskBlobStorageTests
{
    private static DiskBlobStorage CreateStorage(string basePath, long maxBlobSizeBytes = 0)
        => new(new DiskBlobStorageOptions { BasePath = basePath, MaxBlobSizeBytes = maxBlobSizeBytes });

    private static void AssertNoBlobFiles(string basePath)
        => Assert.Empty(Directory.GetFiles(basePath, "*.blob", SearchOption.AllDirectories));

    [Theory]
    [InlineData("")]
    [InlineData("hello blob")]
    public async Task SaveAsync_WritesContentAndReportsSize(string content)
    {
        using var root = new TempDirectory();
        var sut = CreateStorage(root.Path);
        var bytes = System.Text.Encoding.UTF8.GetBytes(content);
        await using var stream = new MemoryStream(bytes);

        var result = await sut.SaveAsync(stream);

        Assert.Equal(bytes.Length, result.SizeBytes);
        Assert.Equal(bytes, await File.ReadAllBytesAsync(result.FullPath));
    }
    
    [Fact]
    public async Task SaveAsync_CreatesBlobInTwoShardedDirectories()
    {
        using var root = new TempDirectory();
        var sut = CreateStorage(root.Path);
        await using var stream = new MemoryStream([.. "hello blob"u8]);

        var result = await sut.SaveAsync(stream);

        var relativePath = Path.GetRelativePath(root.Path, result.FullPath);
        var parts = relativePath.Split(Path.DirectorySeparatorChar);

        Assert.Equal(3, parts.Length);
        Assert.Matches("^[0-9a-f]{2}$", parts[0]);
        Assert.Matches("^[0-9a-f]{2}$", parts[1]);
        Assert.EndsWith(".blob", parts[2]);
        Assert.True(File.Exists(result.FullPath));
    }

    [Fact]
    public async Task SaveAsync_NullStream_ThrowsArgumentNullException()
    {
        using var root = new TempDirectory();
        var sut = CreateStorage(root.Path);

        await Assert.ThrowsAsync<ArgumentNullException>(() => sut.SaveAsync(null!));
    }

    [Fact]
    public void Ctor_EmptyBasePath_ThrowsArgumentException()
    {
        Assert.Throws<ArgumentException>(() => CreateStorage(""));
    }

    [Fact]
    public async Task SaveAsync_FailureDuringCopy_LeavesNoFinalBlob()
    {
        using var root = new TempDirectory();
        var sut = CreateStorage(root.Path);
        await using var failingStream = new FailingReadStream(failAfter: 3);

        await Assert.ThrowsAsync<IOException>(() => sut.SaveAsync(failingStream));
        AssertNoBlobFiles(root.Path);
    }
    
    [Fact]
    public async Task SaveAsync_FailureDuringCopy_LeavesNoTemporaryFile()
    {
        using var root = new TempDirectory();
        var sut = CreateStorage(root.Path);
        await using var failingStream = new FailingReadStream(failAfter: 3);

        await Assert.ThrowsAsync<IOException>(() => sut.SaveAsync(failingStream));

        Assert.Empty(Directory.GetFiles(
            root.Path, "*", SearchOption.AllDirectories));
    }

    [Fact]
    public async Task SaveAsync_ExceedsMaxBlobSize_LeavesNoFinalBlob()
    {
        using var root = new TempDirectory();
        var sut = CreateStorage(root.Path, maxBlobSizeBytes: 5);
        await using var stream = new MemoryStream(new byte[10]);

        await Assert.ThrowsAsync<BlobSizeLimitExceededException>(() => sut.SaveAsync(stream));
        AssertNoBlobFiles(root.Path);
    }
    
    [Fact]
    public async Task SaveAsync_WithSha256_ComputesCorrectHash()
    {
        using var root = new TempDirectory();
        var sut = CreateStorage(root.Path);
        await using var stream = new MemoryStream([.. "hello blob"u8]);
        const string expectedHash = "e997afd18e5f6be004fc193aed2c90291e68ab2c7599a62538c935b7fca6ab0f";

        var result = await sut.SaveAsync(
            stream,
            new BlobWriteOptions { ComputeSha256 = true });

        Assert.Equal(expectedHash, result.Sha256);
    }
}
