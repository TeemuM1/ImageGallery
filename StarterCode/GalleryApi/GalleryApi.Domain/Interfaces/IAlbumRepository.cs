using GalleryApi.Domain.Entities;

namespace GalleryApi.Domain.Interfaces;

public interface IAlbumRepository
{
    Task<IEnumerable<Album>> GetAllAsync();
    Task<Album?> GetByIdAsync(Guid id);
    Task<Album> CreateAsync(Album album);
    Task DeleteAsync(Guid id);
}
