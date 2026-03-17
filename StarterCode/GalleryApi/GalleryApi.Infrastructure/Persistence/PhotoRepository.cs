using GalleryApi.Domain.Entities;
using GalleryApi.Domain.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace GalleryApi.Infrastructure.Persistence;

public class PhotoRepository : IPhotoRepository
{
    private readonly GalleryDbContext _context;

    public PhotoRepository(GalleryDbContext context)
    {
        _context = context;
    }

    public async Task<IEnumerable<Photo>> GetByAlbumIdAsync(Guid albumId)
        => await _context.Photos.Where(p => p.AlbumId == albumId).ToListAsync();

    public async Task<Photo?> GetByIdAsync(Guid id)
        => await _context.Photos.FindAsync(id);

    public async Task<Photo> CreateAsync(Photo photo)
    {
        _context.Photos.Add(photo);
        await _context.SaveChangesAsync();
        return photo;
    }

    public async Task DeleteAsync(Guid id)
    {
        var photo = await _context.Photos.FindAsync(id);
        if (photo is not null)
        {
            _context.Photos.Remove(photo);
            await _context.SaveChangesAsync();
        }
    }
}
