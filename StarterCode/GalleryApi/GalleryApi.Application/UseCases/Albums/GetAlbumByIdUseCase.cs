using GalleryApi.Application.DTOs;
using GalleryApi.Domain.Interfaces;

namespace GalleryApi.Application.UseCases.Albums;

public class GetAlbumByIdUseCase
{
    private readonly IAlbumRepository _albumRepository;

    public GetAlbumByIdUseCase(IAlbumRepository albumRepository)
    {
        _albumRepository = albumRepository;
    }

    public async Task<AlbumDto?> ExecuteAsync(Guid id)
    {
        var album = await _albumRepository.GetByIdAsync(id);
        if (album is null) return null;

        return new AlbumDto(
            album.Id,
            album.Name,
            album.Description,
            album.CreatedAt,
            album.Photos.Count);
    }
}
