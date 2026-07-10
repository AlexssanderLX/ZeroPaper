using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;

namespace ZeroPaper.Controllers;

[ApiController]
[Route("api/public/segments")]
public class PublicSegmentsController : ControllerBase
{
    private readonly IConfiguration _config;

    public PublicSegmentsController(IConfiguration config)
    {
        _config = config;
    }

    /// <summary>
    /// Retorna a disponibilidade de cada segmento da plataforma.
    /// Configurado em appsettings via "Segments:{key}:Available".
    /// </summary>
    [HttpGet]
    [ResponseCache(Duration = 60, Location = ResponseCacheLocation.Any)]
    public IActionResult GetAvailability()
    {
        var keys = new[] { "restaurant", "retail", "petshop", "technical", "auto", "custom" };

        var result = keys.Select(key => new
        {
            key,
            available = _config.GetValue<bool>($"Segments:{key}:Available"),
        });

        return Ok(result);
    }
}
