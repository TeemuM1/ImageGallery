namespace GalleryApi.Application.DTOs;

public record AlbumDto(
    Guid Id,
    string Name,
    string Description,
    DateTime CreatedAt,
    int PhotoCount
);
