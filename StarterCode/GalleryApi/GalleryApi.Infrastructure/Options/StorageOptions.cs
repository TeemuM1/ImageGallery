namespace GalleryApi.Infrastructure.Options;

public class StorageOptions
{
    public const string SectionName = "Storage";
    public const string LocalProvider = "local";
    public const string AzureProvider = "azure";

    /// <summary>
    /// Tallennusjärjestelmä: StorageOptions.LocalProvider tai StorageOptions.AzureProvider
    /// </summary>
    public string Provider { get; set; } = LocalProvider;

    /// <summary>
    /// Paikallisen tallennuksen polku suhteessa sisältöhakemistoon (esim. "wwwroot/uploads")
    /// </summary>
    public string BasePath { get; set; } = "wwwroot/uploads";

    // Azure Blob Storage -asetukset (käytetään Part 2:ssa)
    public string AccountName { get; set; } = string.Empty;
    public string ContainerName { get; set; } = "photos";
}
