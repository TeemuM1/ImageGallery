using GalleryApi.Domain.Interfaces;

namespace GalleryApi.Application.UseCases.Albums;

public class DeleteAlbumUseCase
{
    private readonly IAlbumRepository _albumRepository;

    public DeleteAlbumUseCase(IAlbumRepository albumRepository)
    {
        _albumRepository = albumRepository;
    }

    public async Task ExecuteAsync(Guid id)
    {
        var album = await _albumRepository.GetByIdAsync(id);
        if (album is null)
            throw new KeyNotFoundException($"Albumia {id} ei löydy.");

        await _albumRepository.DeleteAsync(id);
    }
}
