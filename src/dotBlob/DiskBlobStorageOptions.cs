namespace dotBlob;

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