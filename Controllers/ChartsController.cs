using Backend.Models;
using Backend.Services;
using Microsoft.AspNetCore.Mvc;

namespace Backend.Controllers;

[ApiController]
[Route("api/[controller]")]
public class ChartsController : ControllerBase
{
    private readonly IChartService _chartService;

    public ChartsController(IChartService chartService)
    {
        _chartService = chartService;
    }

    [HttpGet("{coinId}/ohlcv")]
    public async Task<ActionResult<List<OhlcvPoint>>> GetOhlcv(string coinId, [FromQuery] string period = "1d")
    {
        var data = await _chartService.GetOhlcvAsync(coinId, period);
        if (data == null || !data.Any()) return NotFound("No chart data available.");

        return Ok(data);
    }
}