using Backend.Models;
using Backend.Services;
using Microsoft.AspNetCore.Mvc;

namespace Backend.Controllers;

[ApiController]
[Route("api/[controller]")]
public class ToolsController : ControllerBase
{
    private readonly IToolsService _toolsService;
    private readonly ILogger<ToolsController> _logger;

    public ToolsController(IToolsService toolsService, ILogger<ToolsController> logger)
    {
        _toolsService = toolsService;
        _logger = logger;
    }

    [HttpGet("convert")]
    public async Task<ActionResult<decimal>> ConvertCurrency(
        [FromQuery] string from,
        [FromQuery] string to,
        [FromQuery] decimal amount = 1)
    {
        try
        {
            var result = await _toolsService.ConvertCurrencyAsync(from, to, amount);
            return Ok(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Controller error in ConvertCurrency");
            return StatusCode(500, "Internal server error during conversion.");
        }
    }

    [HttpGet("/api/coins/compare")]
    public async Task<ActionResult<List<Coin>>> CompareCoins([FromQuery] string ids)
    {
        if (string.IsNullOrEmpty(ids)) return BadRequest("IDs parameter is required.");

        try
        {
            var result = await _toolsService.CompareCoinsAsync(ids);
            return Ok(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Controller error in CompareCoins");
            return StatusCode(500, "Internal server error during comparison.");
        }
    }
}