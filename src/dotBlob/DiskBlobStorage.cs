namespace dotBlob;

public sealed class DiskBlobStorage(DiskBlobStorageOptions options)
{
    public async Task<BlobDescriptor> SaveAsync(Stream stream, CancellationToken ct = default)
    {
        var gid = Guid.CreateVersion7();
        var path = Path.Combine(options.BasePath, gid.ToString("N"));
        await using var fStream = File.Create(path);
        await stream.CopyToAsync(fStream, ct);
        return new BlobDescriptor
        {
            FullPath = path,
            SizeBytes = fStream.Length,
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