using GalleryApi.Application.DTOs;
using GalleryApi.Domain.Entities;
using GalleryApi.Domain.Interfaces;

namespace GalleryApi.Application.UseCases.Albums;

public class CreateAlbumUseCase
{
    private readonly IAlbumRepository _albumRepository;

    public CreateAlbumUseCase(IAlbumRepository albumRepository)
    {
        _albumRepository = albumRepository;
    }

    public async Task<AlbumDto> ExecuteAsync(CreateAlbumRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.Name))
            throw new ArgumentException("Albumin nimi ei voi olla tyhjä.");

        var album = new Album
        {
            Id = Guid.NewGuid(),
            Name = request.Name.Trim(),
            Description = request.Description?.Trim() ?? string.Empty,
            CreatedAt = DateTime.UtcNow
        };

        var created = await _albumRepository.CreateAsync(album);
        return new AlbumDto(created.Id, created.Name, created.Description, created.CreatedAt, 0);
    }
}
