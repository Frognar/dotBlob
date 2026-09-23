namespace dotBlob;

public sealed class DiskBlobStorage(DiskBlobStorageOptions options)
{
    private const string BlobExtension = ".blob";

    public async Task<BlobDescriptor> SaveAsync(Stream stream, CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(stream);
        var path = CreateBlobPath();
        Directory.CreateDirectory(Path.GetDirectoryName(path)!);
        try
        {
            var length = await CopyToFileAsync(stream, path, ct);
            return new BlobDescriptor {
                FullPath = path,
                SizeBytes = length,
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
        return Path.Combine(options.BasePath, filename[..2], filename[2..4], filename + BlobExtension);
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
}

public sealed record BlobDescriptor
{
    public required string FullPath { get; init; }
    public required long SizeBytes { get; init; }
}