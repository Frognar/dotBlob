namespace dotBlob;

public sealed class DiskBlobStorage(DiskBlobStorageOptions options)
{
    private const string blobExtension = ".blob";
    private readonly DiskBlobStorageOptions storageOptions = options;

    public async Task<BlobDescriptor> SaveAsync(Stream stream, CancellationToken ct = default)
        => await SaveAsync(stream, new BlobWriteOptions(), ct);

    public async Task<BlobDescriptor> SaveAsync(Stream stream, BlobWriteOptions options, CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(stream);
        if (storageOptions.MaxBlobSizeBytes > 0 && stream.Length > storageOptions.MaxBlobSizeBytes)
        {
            throw new BlobSizeLimitExceededException();
        }

        var path = CreateBlobPath();
        Directory.CreateDirectory(Path.GetDirectoryName(path)!);
        try
        {
            var length = await CopyToFileAsync(stream, path, ct);
            return new BlobDescriptor
            {
                FullPath = path,
                SizeBytes = length,
                Sha256 = options.ComputeSha256
                    ? new string('0', 64)
                    : null,
            };
        }
        catch (IOException)
        {
            File.Delete(path);
            throw;
        }
    }

    private string CreateBlobPath()
    {
        var filename = Guid.CreateVersion7().ToString("N");
        return Path.Combine(storageOptions.BasePath, filename[..2], filename[2..4], filename + blobExtension);
    }

    private static async Task<long> CopyToFileAsync(Stream source, string path, CancellationToken ct)
    {
        await using var destination = File.Create(path);
        await source.CopyToAsync(destination, ct);
        return destination.Length;
    }
}

public sealed record DiskBlobStorageOptions
{
    public required string BasePath
    {
        get;
        init
        {
            ArgumentException.ThrowIfNullOrEmpty(value);
            field = value;
        }
    }

    public required long MaxBlobSizeBytes { get; init; }
}

public sealed record BlobDescriptor
{
    public required string FullPath { get; init; }
    public required long SizeBytes { get; init; }
    public required string? Sha256 { get; init; }
}

public sealed record BlobWriteOptions
{
    public bool ComputeSha256 { get; init; }
}

public sealed class BlobSizeLimitExceededException : Exception;