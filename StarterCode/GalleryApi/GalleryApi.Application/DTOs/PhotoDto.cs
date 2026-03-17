namespace GalleryApi.Application.DTOs;

public record PhotoDto(
    Guid Id,
    Guid AlbumId,
    string Title,
    string ImageUrl,
    string ContentType,
    long FileSizeBytes,
    DateTime UploadedAt
);
