using GalleryApi.Application.DTOs;
using GalleryApi.Application.UseCases.Albums;
using Microsoft.AspNetCore.Mvc;

namespace GalleryApi.WebApi.Controllers;

[ApiController]
[Route("api/[controller]")]
public class AlbumsController : ControllerBase
{
    private readonly GetAlbumsUseCase _getAlbums;
    private readonly GetAlbumByIdUseCase _getAlbumById;
    private readonly CreateAlbumUseCase _createAlbum;
    private readonly DeleteAlbumUseCase _deleteAlbum;

    public AlbumsController(
        GetAlbumsUseCase getAlbums,
        GetAlbumByIdUseCase getAlbumById,
        CreateAlbumUseCase createAlbum,
        DeleteAlbumUseCase deleteAlbum)
    {
        _getAlbums = getAlbums;
        _getAlbumById = getAlbumById;
        _createAlbum = createAlbum;
        _deleteAlbum = deleteAlbum;
    }

    /// <summary>Palauttaa kaikki albumit</summary>
    [HttpGet]
    public async Task<ActionResult<IEnumerable<AlbumDto>>> GetAll()
        => Ok(await _getAlbums.ExecuteAsync());

    /// <summary>Palauttaa yksittäisen albumin</summary>
    [HttpGet("{id:guid}")]
    public async Task<ActionResult<AlbumDto>> GetById(Guid id)
    {
        var album = await _getAlbumById.ExecuteAsync(id);
        return album is null ? NotFound() : Ok(album);
    }

    /// <summary>Luo uuden albumin</summary>
    [HttpPost]
    public async Task<ActionResult<AlbumDto>> Create([FromBody] CreateAlbumRequest request)
    {
        try
        {
            var album = await _createAlbum.ExecuteAsync(request);
            return CreatedAtAction(nameof(GetById), new { id = album.Id }, album);
        }
        catch (ArgumentException ex)
        {
            return BadRequest(new { Error = ex.Message });
        }
    }

    /// <summary>Poistaa albumin ja kaikki sen kuvat</summary>
    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> Delete(Guid id)
    {
        try
        {
            await _deleteAlbum.ExecuteAsync(id);
            return NoContent();
        }
        catch (KeyNotFoundException)
        {
            return NotFound();
        }
    }
}
