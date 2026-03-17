using GalleryApi.Application.Common;
using GalleryApi.Domain.Interfaces;

namespace GalleryApi.Application.UseCases.Photos;

public class DeletePhotoUseCase
{
    private readonly IPhotoRepository _photoRepository;
    private readonly IStorageService _storageService;

    public DeletePhotoUseCase(IPhotoRepository photoRepository, IStorageService storageService)
    {
        _photoRepository = photoRepository;
        _storageService = storageService;
    }

    public async Task<Result> ExecuteAsync(Guid photoId)
    {
        // TODO (Vaihe 8): Toteuta kuvan poistamislogiikka.
        //
        // Vaiheet:
        // 1. Hae kuva tietokannasta:
        //       var photo = await _photoRepository.GetByIdAsync(photoId);
        //       if (photo is null) return Result.Failure("Kuvaa ... ei löydy.");
        //
        // 2. Poista tiedosto tallennuspalvelusta:
        //       await _storageService.DeleteAsync(photo.FileName, photo.AlbumId);
        //
        // 3. Poista kuva tietokannasta:
        //       await _photoRepository.DeleteAsync(photoId);
        //
        // 4. Palauta onnistunut tulos:
        //       return Result.Success();

        throw new NotImplementedException("DeletePhotoUseCase ei ole vielä toteutettu. Katso TODO-kommentit.");
    }
}
