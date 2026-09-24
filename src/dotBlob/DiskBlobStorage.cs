using System.Security.Cryptography;

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
        AssertSizeWithinLimit(stream);
        var path = CreateBlobPath();
        var directory = Path.GetDirectoryName(path)!;
        Directory.CreateDirectory(directory);
        
        var temporaryPath = Path.Combine(
            directory, $".upload-{Guid.NewGuid():N}.tmp");
        try
        {
            var (length, hash) = await CopyToFileAsync(stream, temporaryPath, options.ComputeSha256, ct);
            ct.ThrowIfCancellationRequested();
            File.Move(temporaryPath, path, overwrite: false);
            return new BlobDescriptor
            {
                FullPath = path,
                SizeBytes = length,
                Sha256 = hash,
            };
        }
        catch (IOException)
        {
            File.Delete(path);
            throw;
        }
    }
    
    private void AssertSizeWithinLimit(Stream stream)
    {
        if (storageOptions.MaxBlobSizeBytes > 0 && stream.Length > storageOptions.MaxBlobSizeBytes)
        {
            throw new BlobSizeLimitExceededException();
        }
    }

    private string CreateBlobPath()
    {
        var filename = Guid.CreateVersion7().ToString("N");
        return Path.Combine(storageOptions.BasePath, filename[..2], filename[2..4], filename + blobExtension);
    }
    
    private static async Task<(long Length, string? Sha256)> CopyToFileAsync(
        Stream source,
        string path,
        bool computeHash,
        CancellationToken ct)
    {
        await using var destination = File.Create(path);
        string? hash = null;

        if (computeHash)
        {
            using var sha256 = SHA256.Create();
            await using var hashingStream = new CryptoStream(source, sha256, CryptoStreamMode.Read);
            await hashingStream.CopyToAsync(destination, ct);
            hash = Convert.ToHexString(sha256.Hash!).ToLowerInvariant();
        }
        else
        {
            await source.CopyToAsync(destination, ct);
        }

        return (destination.Length, hash);
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