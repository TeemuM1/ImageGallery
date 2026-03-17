using GalleryApi.Application.DTOs;
using GalleryApi.Domain.Interfaces;

namespace GalleryApi.Application.UseCases.Albums;

public class GetAlbumsUseCase
{
    private readonly IAlbumRepository _albumRepository;

    public GetAlbumsUseCase(IAlbumRepository albumRepository)
    {
        _albumRepository = albumRepository;
    }

    public async Task<IEnumerable<AlbumDto>> ExecuteAsync()
    {
        var albums = await _albumRepository.GetAllAsync();
        return albums.Select(a => new AlbumDto(
            a.Id,
            a.Name,
            a.Description,
            a.CreatedAt,
            a.Photos.Count));
    }
}
