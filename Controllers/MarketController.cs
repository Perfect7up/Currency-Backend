using Backend.Services;
using Microsoft.AspNetCore.Mvc;

namespace Backend.Controllers;

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
    public async Task<IActionResult> GetOverview()
    {
        var data = await _marketService.GetMarketOverviewAsync();
        return Ok(data);
    }

    [HttpGet("top-gainers")]
    public async Task<IActionResult> GetTopGainers([FromQuery] int limit = 5)
    {
        var data = await _marketService.GetTopGainersAsync(limit);
        return Ok(data);
    }

    [HttpGet("top-losers")]
    public async Task<IActionResult> GetTopLosers([FromQuery] int limit = 5)
    {
        var data = await _marketService.GetTopLosersAsync(limit);
        return Ok(data);
    }

    [HttpGet("trending")]
    public async Task<IActionResult> GetTrending([FromQuery] int limit = 10)
    {
        var data = await _marketService.GetTrendingAsync(limit);
        return Ok(data);
    }
}