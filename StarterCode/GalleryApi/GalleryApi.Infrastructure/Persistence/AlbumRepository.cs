using GalleryApi.Domain.Entities;
using GalleryApi.Domain.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace GalleryApi.Infrastructure.Persistence;

public class AlbumRepository : IAlbumRepository
{
    private readonly GalleryDbContext _context;

    public AlbumRepository(GalleryDbContext context)
    {
        _context = context;
    }

    public async Task<IEnumerable<Album>> GetAllAsync()
        => await _context.Albums.Include(a => a.Photos).ToListAsync();

    public async Task<Album?> GetByIdAsync(Guid id)
        => await _context.Albums.Include(a => a.Photos).FirstOrDefaultAsync(a => a.Id == id);

    public async Task<Album> CreateAsync(Album album)
    {
        _context.Albums.Add(album);
        await _context.SaveChangesAsync();
        return album;
    }

    public async Task DeleteAsync(Guid id)
    {
        var album = await _context.Albums.FindAsync(id);
        if (album is not null)
        {
            _context.Albums.Remove(album);
            await _context.SaveChangesAsync();
        }
    }
}
