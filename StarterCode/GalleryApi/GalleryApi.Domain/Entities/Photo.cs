namespace GalleryApi.Domain.Entities;

public class Photo
{
    public Guid Id { get; set; }
    public Guid AlbumId { get; set; }
    public Album Album { get; set; } = null!;

    public string Title { get; set; } = string.Empty;

    // Tiedostonimi tallennettuna (esim. "photo.jpg")
    public string FileName { get; set; } = string.Empty;

    // URL tai polku kuvaan (esim. "/uploads/album-id/photo.jpg" tai Azure Blob URL)
    public string ImageUrl { get; set; } = string.Empty;

    public string ContentType { get; set; } = string.Empty;
    public long FileSizeBytes { get; set; }
    public DateTime UploadedAt { get; set; }
}
