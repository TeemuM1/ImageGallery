namespace GalleryApi.Domain.Interfaces;

/// <summary>
/// Rajapinta tiedostojen tallennukseen. Toimii sekä lokaalilla levyllä
/// että Azure Blob Storagessa riippuen konfiguraatiosta.
/// </summary>
public interface IStorageService
{
    /// <summary>
    /// Tallentaa kuvatiedoston ja palauttaa URL:n jolla kuva on saatavilla.
    /// </summary>
    Task<string> UploadAsync(Stream fileStream, string fileName, string contentType, Guid albumId);

    /// <summary>
    /// Poistaa kuvatiedoston tallennuspaikasta.
    /// </summary>
    Task DeleteAsync(string fileName, Guid albumId);
}
