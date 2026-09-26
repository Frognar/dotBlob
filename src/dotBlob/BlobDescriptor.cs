namespace dotBlob;

public sealed record BlobDescriptor
{
    public required string FullPath { get; init; }
    public required long SizeBytes { get; init; }
    public required string? Sha256 { get; init; }
}