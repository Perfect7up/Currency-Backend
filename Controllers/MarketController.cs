using Backend.Models;
using Backend.Services;
using Microsoft.AspNetCore.Mvc;

[ApiController]
[Route("api/[controller]")]
public class MarketController : ControllerBase
{
    private readonly IMarketService _marketService;

    public MarketController(IMarketService marketService)
    {
        _marketService = marketService;
    }

    [HttpGet("overview")]
    [ProducesResponseType(typeof(MarketOverview), 200)]
    public async Task<IActionResult> GetOverview()
    {
        var data = await _marketService.GetMarketOverviewAsync();
        return Ok(data);
    }

    [HttpGet("top-gainers")]
    [ProducesResponseType(typeof(List<Coin>), 200)]
    public async Task<IActionResult> GetTopGainers([FromQuery] int limit = 5)
    {
        var data = await _marketService.GetTopGainersAsync(limit);
        return Ok(data);
    }

    [HttpGet("top-losers")]
    [ProducesResponseType(typeof(List<Coin>), 200)]
    public async Task<IActionResult> GetTopLosers([FromQuery] int limit = 5)
    {
        var data = await _marketService.GetTopLosersAsync(limit);
        return Ok(data);
    }

    [HttpGet("trending")]
    [ProducesResponseType(typeof(List<Coin>), 200)]
    public async Task<IActionResult> GetTrending([FromQuery] int limit = 10)
    {
        var data = await _marketService.GetTrendingAsync(limit);
        return Ok(data);
    }
}