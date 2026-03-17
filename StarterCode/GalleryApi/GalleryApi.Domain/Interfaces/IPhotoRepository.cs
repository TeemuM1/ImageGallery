using GalleryApi.Domain.Entities;

namespace GalleryApi.Domain.Interfaces;

public interface IPhotoRepository
{
    Task<IEnumerable<Photo>> GetByAlbumIdAsync(Guid albumId);
    Task<Photo?> GetByIdAsync(Guid id);
    Task<Photo> CreateAsync(Photo photo);
    Task DeleteAsync(Guid id);
}
