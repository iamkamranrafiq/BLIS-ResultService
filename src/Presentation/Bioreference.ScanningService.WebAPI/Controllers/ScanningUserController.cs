using Bioreference.ScanningService.Application.Services.Interfaces;
using Microsoft.AspNetCore.Mvc;

namespace Bioreference.ScanningService.WebAPI.Controllers;

[Route("api/[controller]")]
[ApiController]
public class ScanningUserController : ControllerBase
{
    private readonly IScanningUserService _userService;
    private readonly ILogger<ScanningUserController> _logger;

    public ScanningUserController(IScanningUserService userService, ILogger<ScanningUserController> logger)
    {
        _logger = logger;
        _userService = userService;
    }

    [HttpGet]
    public async Task<IActionResult> GetUsers()
    {
        _logger.LogInformation("GET /api/ScanningUser called");
        var response = await _userService.GetActiveUsers();

        return response.Success ? Ok(response) : StatusCode(500, response);
    }

    [HttpGet("getusergroups")]
    public async Task<IActionResult> GetUserGroups()
    {
        _logger.LogInformation("GET /api/ScanningUser/GetUserGroups called");
        var response = await _userService.GetUserGroups();

        return response.Success ? Ok(response) : StatusCode(500, response);
    }
}
