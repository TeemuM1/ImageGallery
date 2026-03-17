using GalleryApi.Domain.Interfaces;

namespace GalleryApi.Infrastructure.Storage;

// TODO (Part 2, Vaihe 2): Toteuta Azure Blob Storage -integraatio.
//
// Tarvitset NuGet-paketit:
//   dotnet add package Azure.Storage.Blobs
//   dotnet add package Azure.Identity
//
// Azure.Identity tarjoaa DefaultAzureCredential-luokan, joka toimii
// automaattisesti sekä lokaalisti (Azure CLI -kirjautuminen) että
// Azuressa (Managed Identity).
//
// Konstruktori:
//   public AzureBlobStorageService(IOptions<StorageOptions> options)
//   {
//       var accountName = options.Value.AccountName;
//       var containerName = options.Value.ContainerName;
//       var serviceClient = new BlobServiceClient(
//           new Uri($"https://{accountName}.blob.core.windows.net"),
//           new DefaultAzureCredential());
//       _containerClient = serviceClient.GetBlobContainerClient(containerName);
//   }

public class AzureBlobStorageService : IStorageService
{
    public Task<string> UploadAsync(Stream fileStream, string fileName, string contentType, Guid albumId)
    {
        throw new NotImplementedException("AzureBlobStorageService ei ole vielä toteutettu. Katso README-Part2.");
    }

    public Task DeleteAsync(string fileName, Guid albumId)
    {
        throw new NotImplementedException("AzureBlobStorageService ei ole vielä toteutettu. Katso README-Part2.");
    }
}
