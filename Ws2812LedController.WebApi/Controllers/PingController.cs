using Microsoft.AspNetCore.Mvc;
using Ws2812LedController.Core;
using Ws2812LedController.Core.Model;
using Ws2812LedController.Core.Utils;
using Ws2812LedController.WebApi.Serializable;

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
