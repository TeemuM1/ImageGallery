using GalleryApi.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace GalleryApi.Infrastructure.Persistence;

public class GalleryDbContext : DbContext
{
    public GalleryDbContext(DbContextOptions<GalleryDbContext> options) : base(options) { }

    public DbSet<Album> Albums => Set<Album>();
    public DbSet<Photo> Photos => Set<Photo>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Album>(entity =>
        {
            entity.HasKey(a => a.Id);
            entity.Property(a => a.Name).IsRequired().HasMaxLength(200);
            entity.Property(a => a.Description).HasMaxLength(1000);

            entity.HasMany(a => a.Photos)
                  .WithOne(p => p.Album)
                  .HasForeignKey(p => p.AlbumId)
                  .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<Photo>(entity =>
        {
            entity.HasKey(p => p.Id);
            entity.Property(p => p.Title).IsRequired().HasMaxLength(200);
            entity.Property(p => p.FileName).IsRequired().HasMaxLength(500);
            entity.Property(p => p.ImageUrl).IsRequired().HasMaxLength(1000);
            entity.Property(p => p.ContentType).IsRequired().HasMaxLength(100);
        });
    }
}
