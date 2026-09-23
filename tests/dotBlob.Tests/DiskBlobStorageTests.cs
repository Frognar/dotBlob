namespace dotBlob.Tests;

public class DiskBlobStorageTests
{
    private static DiskBlobStorage PrepareSut(string basePath, long maxBlobSizeBytes = 0)
        => new(new DiskBlobStorageOptions { BasePath = basePath, MaxBlobSizeBytes = maxBlobSizeBytes });

    [Fact]
    public async Task SaveAsync_EmptyStream_CreatesBlobWithZeroSize()
    {
        using var root = new TempDirectory();
        var sut = PrepareSut(root.Path);

        var result = await sut.SaveAsync(new MemoryStream());

        Assert.Equal(0, result.SizeBytes);
        Assert.True(File.Exists(result.FullPath));
    }

    [Fact]
    public async Task SaveAsync_NonEmptyStream_WritesCorrectSizeAndContent()
    {
        using var root = new TempDirectory();
        var sut = PrepareSut(root.Path);
        var bytes = "hello blob"u8.ToArray();

        var result = await sut.SaveAsync(new MemoryStream(bytes));

        Assert.Equal(bytes.Length, result.SizeBytes);
        Assert.Equal(bytes, await File.ReadAllBytesAsync(result.FullPath));
    }

    [Fact]
    public async Task SaveAsync_CreatesFileWithBlobExtension()
    {
        using var root = new TempDirectory();
        var sut = PrepareSut(root.Path);

        var result = await sut.SaveAsync(new MemoryStream([1, 2, 3]));

        Assert.EndsWith(".blob", result.FullPath);
        Assert.True(File.Exists(result.FullPath));
    }

    [Fact]
    public async Task SaveAsync_NullStream_ThrowsArgumentNullException()
    {
        using var root = new TempDirectory();
        var sut = PrepareSut(root.Path);

        await Assert.ThrowsAsync<ArgumentNullException>(() => sut.SaveAsync(null!));
    }

    [Fact]
    public void Ctor_EmptyBasePath_ThrowsArgumentException()
    {
        Assert.Throws<ArgumentException>(() => PrepareSut(""));
    }

    [Fact]
    public async Task SaveAsync_FailureDuringCopy_NoFinalBlobCreated()
    {
        using var root = new TempDirectory();
        var sut = PrepareSut(root.Path);
        await using var failingStream = new FailingReadStream(failAfter: 3);

        await Assert.ThrowsAsync<IOException>(() => sut.SaveAsync(failingStream));
        Assert.Empty(Directory.GetFiles(root.Path, "*.blob"));
    }

    [Fact]
    public async Task SaveAsync_CreatesShardedDirectoryStructure()
    {
        using var root = new TempDirectory();
        var sut = PrepareSut(root.Path);

        var result = await sut.SaveAsync(new MemoryStream([1, 2, 3]));

        var expectedDirs = Path.GetDirectoryName(result.FullPath)![root.Path.Length..]
            .Split(Path.DirectorySeparatorChar, StringSplitOptions.RemoveEmptyEntries);
        
        Assert.Equal(2, expectedDirs.Length);
        Assert.Matches("^[0-9a-f]{2}$", expectedDirs[0]);
        Assert.Matches("^[0-9a-f]{2}$", expectedDirs[1]);
    }

    [Fact]
    public async Task SaveAsync_ExceedsMaxBlobSize_ThrowsAndLeavesNoFile()
    {
        using var root = new TempDirectory();
        var sut = PrepareSut(root.Path, maxBlobSizeBytes: 5);
        var data = new byte[10];

        await Assert.ThrowsAsync<BlobSizeLimitExceededException>(() => sut.SaveAsync(new MemoryStream(data)));

        Assert.Empty(Directory.GetFiles(root.Path, "*.blob", SearchOption.AllDirectories));
    }
}
