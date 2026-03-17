using GalleryApi.Domain.Interfaces;
using GalleryApi.Infrastructure.Options;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Options;

namespace GalleryApi.Infrastructure.Storage;

public class LocalStorageService : IStorageService
{
    // TODO (Vaihe 5): Lisää konstruktori ja toteutus tähän.
    //
    // Tarvitset seuraavat injektiot:
    //   IWebHostEnvironment env       — antaa ContentRootPath (sovelluksen juurihakemisto)
    //   IOptions<StorageOptions> opts — antaa Storage:BasePath konfiguraatiosta
    //
    // Esimerkki konstruktorista:
    //   public LocalStorageService(IWebHostEnvironment env, IOptions<StorageOptions> opts)
    //   {
    //       _basePath = Path.Combine(env.ContentRootPath, opts.Value.BasePath);
    //   }
    //
    // UploadAsync-metodi:
    //   1. Muodosta kansio: Path.Combine(_basePath, albumId.ToString())
    //   2. Luo kansio: Directory.CreateDirectory(albumDir)
    //   3. Muodosta tiedostopolku: Path.Combine(albumDir, fileName)
    //   4. Kirjoita Stream tiedostoon:
    //        using var output = File.Create(filePath);
    //        await fileStream.CopyToAsync(output);
    //   5. Palauta URL: $"/uploads/{albumId}/{fileName}"
    //
    // DeleteAsync-metodi:
    //   1. Muodosta polku: Path.Combine(_basePath, albumId.ToString(), fileName)
    //   2. Tarkista onko tiedosto olemassa: File.Exists(filePath)
    //   3. Poista: File.Delete(filePath)

    public Task<string> UploadAsync(Stream fileStream, string fileName, string contentType, Guid albumId)
    {
        throw new NotImplementedException("LocalStorageService.UploadAsync ei ole vielä toteutettu. Katso TODO-kommentit.");
    }

    public Task DeleteAsync(string fileName, Guid albumId)
    {
        throw new NotImplementedException("LocalStorageService.DeleteAsync ei ole vielä toteutettu. Katso TODO-kommentit.");
    }
}
