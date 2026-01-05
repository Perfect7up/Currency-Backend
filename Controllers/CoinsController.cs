using Backend.Models;
using Backend.Services;
using Microsoft.AspNetCore.Mvc;

namespace Backend.Controllers;

[ApiController]
[Route("api/[controller]")]
public class CoinsController : ControllerBase
{
    private readonly ICoinService _coinService;

    public CoinsController(ICoinService coinService)
    {
        _coinService = coinService;
    }

    [HttpGet]
    [ProducesResponseType(typeof(List<Coin>), 200)]
    public async Task<IActionResult> GetAll([FromQuery] int page = 1, [FromQuery] int perPage = 10)
    {
        var data = await _coinService.GetCoinsAsync(page, perPage);
        return Ok(data);
    }

    [HttpGet("{id}")]
    [ProducesResponseType(typeof(Coin), 200)]
    public async Task<IActionResult> GetById(string id)
    {
        var data = await _coinService.GetCoinByIdAsync(id);
        if (data == null) return NotFound();
        return Ok(data);
    }

    [HttpGet("trending")]
    [ProducesResponseType(typeof(List<Coin>), 200)]
    public async Task<IActionResult> GetTrending()
    {
        var data = await _coinService.GetTrendingCoinsAsync();
        return Ok(data);
    }

    [HttpGet("history/{id}")]
    [ProducesResponseType(typeof(List<PriceHistory>), 200)]
    public async Task<IActionResult> GetHistory(string id, [FromQuery] int days = 7)
    {
        var data = await _coinService.GetPriceHistoryAsync(id, days);
        return Ok(data);
    }
}