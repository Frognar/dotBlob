namespace dotBlob;

public sealed class DiskBlobStorage(DiskBlobStorageOptions options)
{
    public async Task<BlobDescriptor> SaveAsync(Stream stream, CancellationToken ct = default)
    {
        Guid gid = Guid.CreateVersion7();
        var path = Path.Combine(options.BasePath, gid.ToString("N"));
        File.Create(path);
        return new BlobDescriptor()
        {
            FullPath = path,
            SizeBytes = 0,
        };
    }
}

public sealed record DiskBlobStorageOptions
{
    public required string BasePath { get; init; }
}

public sealed record BlobDescriptor
{
    public required string FullPath { get; init; }
    public required long SizeBytes { get; init; }
}