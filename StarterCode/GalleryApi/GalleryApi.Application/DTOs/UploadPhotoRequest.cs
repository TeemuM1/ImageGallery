namespace GalleryApi.Application.DTOs;

public record UploadPhotoRequest(
    Guid AlbumId,
    string Title,
    Stream FileStream,
    string FileName,
    string ContentType,
    long FileSize
);
