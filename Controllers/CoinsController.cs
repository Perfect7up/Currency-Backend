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
    public async Task<IActionResult> GetAll([FromQuery] int page = 1, [FromQuery] int perPage = 10)
    {
        var data = await _coinService.GetCoinsAsync(page, perPage);
        return Ok(data);
    }

    [HttpGet("{id}")]
    public async Task<IActionResult> GetById(string id)
    {
        var data = await _coinService.GetCoinByIdAsync(id);
        if (data == null) return NotFound();
        return Ok(data);
    }

    [HttpGet("search")]
    public async Task<IActionResult> Search([FromQuery] string query)
    {
        if (string.IsNullOrWhiteSpace(query)) return BadRequest("Query is required");
        var data = await _coinService.SearchCoinsAsync(query);
        return Ok(data);
    }

    [HttpGet("trending")]
    public async Task<IActionResult> GetTrending()
    {
        var data = await _coinService.GetTrendingCoinsAsync();
        return Ok(data);
    }

    [HttpGet("live")]
    public async Task<IActionResult> GetLive([FromQuery] int perPage = 10)
    {
        var data = await _coinService.GetLiveCoinsAsync(perPage);
        return Ok(data);
    }

    [HttpGet("history/{id}")]
    public async Task<IActionResult> GetHistory(string id, [FromQuery] int days = 7)
    {
        var data = await _coinService.GetPriceHistoryAsync(id, days);
        return Ok(data);
    }
}