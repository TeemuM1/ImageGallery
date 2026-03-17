namespace GalleryApi.Domain.Entities;

public class Album
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }

    public ICollection<Photo> Photos { get; set; } = [];
}
