namespace dotBlob;

public sealed record BlobWriteOptions
{
    public bool ComputeSha256 { get; init; }
}