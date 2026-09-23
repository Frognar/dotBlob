namespace dotBlob;

public sealed class DiskBlobStorage(DiskBlobStorageOptions options)
{
    private const string BlobExtension = ".blob";

    public async Task<BlobDescriptor> SaveAsync(Stream stream, CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(stream);
        var gid = Guid.CreateVersion7();
        var filename = gid.ToString("N");
        var path = Path.Combine(options.BasePath, filename + BlobExtension);
        try
        {
            await using var fStream = File.Create(path);
            await stream.CopyToAsync(fStream, ct);
            return new BlobDescriptor
            {
                FullPath = path,
                SizeBytes = fStream.Length,
            };
        }
        catch (IOException)
        {
            File.Delete(path);
            throw;
        }
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
}

public sealed record BlobDescriptor
{
    public required string FullPath { get; init; }
    public required long SizeBytes { get; init; }
}