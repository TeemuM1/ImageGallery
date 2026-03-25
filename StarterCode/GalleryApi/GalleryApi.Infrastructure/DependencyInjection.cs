using GalleryApi.Domain.Interfaces;
using GalleryApi.Infrastructure.Options;
using GalleryApi.Infrastructure.Persistence;
using GalleryApi.Infrastructure.Storage;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace GalleryApi.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        // Rekisteröi tietokantayhteys — käyttää SQLiteä oletuksena
        // "Default" viittaa appsettings.json:n ConnectionStrings:Default-arvoon
        services.AddDbContext<GalleryDbContext>(options =>
            options.UseSqlite(
                configuration.GetConnectionString("Default") ?? "Data Source=gallery.db"));

        // Rekisteröi repositoriot — DI antaa näiden toteutukset automaattisesti
        // kun jokin luokka pyytää IAlbumRepository- tai IPhotoRepository-tyyppiä
        services.AddScoped<IAlbumRepository, AlbumRepository>();
        services.AddScoped<IPhotoRepository, PhotoRepository>();

        // Valitaan tallennustoteutus konfiguraatioarvon perusteella.
        // Lokaalissa kehityksessä appsettings.json:ssa on Storage:Provider = "local",
        // Azuressa App Settingsissä asetetaan Storage:Provider = "azure" (Osa 2).
        // Näin sama koodi toimii molemmissa ympäristöissä — vain yksi rivi konfiguraatiossa muuttuu.
        // StorageOptions-vakiot estävät magic stringit — kirjoitusvirhe näkyy käännösaikana
        var provider = configuration[$"{StorageOptions.SectionName}:Provider"]
            ?? StorageOptions.LocalProvider;
        if (provider == StorageOptions.AzureProvider)
            services.AddScoped<IStorageService, AzureBlobStorageService>(); // Osa 2
        else
            services.AddScoped<IStorageService, LocalStorageService>();     // Osa 1 — tämä nyt

        return services;
    }
}
