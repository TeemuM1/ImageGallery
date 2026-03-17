using GalleryApi.Application.DTOs;
using GalleryApi.Domain.Interfaces;

namespace GalleryApi.Application.UseCases.Photos;

public class GetPhotosByAlbumUseCase
{
    private readonly IPhotoRepository _photoRepository;
    private readonly IAlbumRepository _albumRepository;

    public GetPhotosByAlbumUseCase(IPhotoRepository photoRepository, IAlbumRepository albumRepository)
    {
        _photoRepository = photoRepository;
        _albumRepository = albumRepository;
    }

    public async Task<IEnumerable<PhotoDto>> ExecuteAsync(Guid albumId)
    {
        var album = await _albumRepository.GetByIdAsync(albumId);
        if (album is null)
            throw new KeyNotFoundException($"Albumia {albumId} ei löydy.");

        var photos = await _photoRepository.GetByAlbumIdAsync(albumId);
        return photos.Select(p => new PhotoDto(
            p.Id, p.AlbumId, p.Title, p.ImageUrl,
            p.ContentType, p.FileSizeBytes, p.UploadedAt));
    }
}
