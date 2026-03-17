using GalleryApi.Application.UseCases.Albums;
using GalleryApi.Application.UseCases.Photos;
using Microsoft.Extensions.DependencyInjection;

namespace GalleryApi.Application;

public static class DependencyInjection
{
    public static IServiceCollection AddApplication(this IServiceCollection services)
    {
        // Album-käyttötapaukset
        services.AddScoped<GetAlbumsUseCase>();
        services.AddScoped<GetAlbumByIdUseCase>();
        services.AddScoped<CreateAlbumUseCase>();
        services.AddScoped<DeleteAlbumUseCase>();

        // Kuva-käyttötapaukset
        services.AddScoped<GetPhotosByAlbumUseCase>();
        services.AddScoped<UploadPhotoUseCase>();
        services.AddScoped<DeletePhotoUseCase>();

        return services;
    }
}
