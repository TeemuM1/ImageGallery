using GalleryApi.Application.UseCases.Albums;
using GalleryApi.Domain.Entities;
using GalleryApi.Domain.Interfaces;
using Moq;
using Xunit;

namespace GalleryApi.Tests.UseCases;

/// <summary>
/// Esimerkkitestit GetAlbumByIdUseCase-luokalle.
///
/// Nämä testit näyttävät kuinka Clean Architecture mahdollistaa
/// käyttötapausten testaamisen ilman oikeaa tietokantaa.
///
/// Käytetyt työkalut:
///   - xUnit: .NET:n suosituin testikehys
///   - Moq: mock-kirjasto, jolla voidaan luoda "vale-olioita" (mock) rajapinnoista
///
/// Mock-objekti on väärennös oikeasta toteutuksesta.
/// Tässä IAlbumRepository-rajapinnasta luodaan mock,
/// joka ei tarvitse oikeaa tietokantaa.
/// </summary>
public class GetAlbumByIdUseCaseTests
{
    [Fact]
    public async Task ExecuteAsync_PalauttaaAlbumin_KunAlbumOnOlemassa()
    {
        // Arrange — valmistele testi
        var albumId = Guid.NewGuid();
        var album = new Album
        {
            Id = albumId,
            Name = "Kesälomaalbumi",
            Description = "Kuvia kesälomalta 2024",
            CreatedAt = DateTime.UtcNow,
            Photos = []
        };

        // Luo mock-repository joka palauttaa testialbumin
        var mockRepo = new Mock<IAlbumRepository>();
        mockRepo
            .Setup(r => r.GetByIdAsync(albumId))
            .ReturnsAsync(album);

        var useCase = new GetAlbumByIdUseCase(mockRepo.Object);

        // Act — suorita käyttötapaus
        var result = await useCase.ExecuteAsync(albumId);

        // Assert — tarkista tulos
        Assert.NotNull(result);
        Assert.Equal(albumId, result.Id);
        Assert.Equal("Kesälomaalbumi", result.Name);
        Assert.Equal(0, result.PhotoCount);
    }

    [Fact]
    public async Task ExecuteAsync_PalauttaaNull_KunAlbumiaEiLoydy()
    {
        // Arrange
        var mockRepo = new Mock<IAlbumRepository>();
        mockRepo
            .Setup(r => r.GetByIdAsync(It.IsAny<Guid>()))
            .ReturnsAsync((Album?)null);

        var useCase = new GetAlbumByIdUseCase(mockRepo.Object);

        // Act
        var result = await useCase.ExecuteAsync(Guid.NewGuid());

        // Assert
        Assert.Null(result);
    }

    [Fact]
    public async Task ExecuteAsync_KutsuuRepositoryaTasmalleenKerran()
    {
        // Arrange
        var albumId = Guid.NewGuid();
        var mockRepo = new Mock<IAlbumRepository>();
        mockRepo
            .Setup(r => r.GetByIdAsync(albumId))
            .ReturnsAsync(new Album { Id = albumId, Name = "Testi", Photos = [] });

        var useCase = new GetAlbumByIdUseCase(mockRepo.Object);

        // Act
        await useCase.ExecuteAsync(albumId);

        // Assert — varmista että repository-metodia kutsuttiin täsmälleen kerran oikealla ID:llä
        mockRepo.Verify(r => r.GetByIdAsync(albumId), Times.Once);
    }
}
