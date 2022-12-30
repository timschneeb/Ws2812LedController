using Microsoft.AspNetCore.Mvc;

namespace Ws2812LedController.WebApi.Controllers;

[ApiController]
[Route("api/[controller]")]
public class PingController : ControllerBase
{
    private readonly ILogger<PingController> _logger;

    public PingController(Logger<PingController> logger)
    {
        _logger = logger;
    }

    [HttpGet]
    public ActionResult Get()
    {
        return new OkResult();
    }
}
