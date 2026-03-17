using GalleryApi.Application.Common;
using GalleryApi.Application.DTOs;
using GalleryApi.Domain.Entities;
using GalleryApi.Domain.Interfaces;

namespace GalleryApi.Application.UseCases.Photos;

public class UploadPhotoUseCase
{
    private readonly IPhotoRepository _photoRepository;
    private readonly IAlbumRepository _albumRepository;
    private readonly IStorageService _storageService;

    // Sallitut kuvatyypit
    private static readonly string[] AllowedContentTypes =
        ["image/jpeg", "image/png", "image/webp", "image/gif"];

    // Maksimikoko: 10 MB
    private const long MaxFileSizeBytes = 10 * 1024 * 1024;

    public UploadPhotoUseCase(
        IPhotoRepository photoRepository,
        IAlbumRepository albumRepository,
        IStorageService storageService)
    {
        _photoRepository = photoRepository;
        _albumRepository = albumRepository;
        _storageService = storageService;
    }

    public async Task<Result<PhotoDto>> ExecuteAsync(UploadPhotoRequest request)
    {
        // TODO (Vaihe 7): Toteuta kuvan latauslogiikka.
        //
        // Vaiheet:
        // 1. Tarkista että albumi on olemassa:
        //       var album = await _albumRepository.GetByIdAsync(request.AlbumId);
        //       if (album is null) return Result<PhotoDto>.Failure("Albumia ... ei löydy.");
        //
        // 2. Validoi tiedostotyyppi:
        //       if (!AllowedContentTypes.Contains(request.ContentType))
        //           return Result<PhotoDto>.Failure("Tiedostotyyppi ei ole sallittu.");
        //
        // 3. Validoi tiedoston koko:
        //       if (request.FileSize > MaxFileSizeBytes)
        //           return Result<PhotoDto>.Failure("Tiedosto on liian suuri.");
        //
        // 4. Lataa tiedosto tallennuspalveluun — kääri try-catchiin:
        //       string imageUrl;
        //       try
        //       {
        //           imageUrl = await _storageService.UploadAsync(
        //               request.FileStream, request.FileName,
        //               request.ContentType, request.AlbumId);
        //       }
        //       catch (Exception ex)
        //       {
        //           return Result<PhotoDto>.Failure($"Tiedoston tallennus epäonnistui: {ex.Message}");
        //       }
        //
        // 5. Luo Photo-entiteetti ja tallenna tietokantaan — VAIN onnistuneen uploadin jälkeen:
        //       var photo = new Photo { ... };
        //       var saved = await _photoRepository.CreateAsync(photo);
        //
        // 6. Palauta onnistunut tulos:
        //       return Result<PhotoDto>.Success(new PhotoDto(...));

        throw new NotImplementedException("UploadPhotoUseCase ei ole vielä toteutettu. Katso TODO-kommentit.");
    }
}
