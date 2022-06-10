using Microsoft.AspNetCore.Mvc;
using Ws2812LedController.Core;
using Ws2812LedController.Core.Model;
using Ws2812LedController.Core.Utils;
using Ws2812LedController.WebApi.Serializable;

namespace Ws2812LedController.WebApi.Controllers;

[ApiController]
[Route("api")]
public class RootController : ControllerBase
{
    private readonly Ref<LedManager> _manager;
    private readonly ILogger<RootController> _logger;

    public RootController(Ref<LedManager> manager, ILogger<RootController> logger)
    {
        _manager = manager;
        _logger = logger;
    }

    [HttpGet]
    [Route("brightness")]
    public ActionResult<byte> GetBrightness()
    {
        return _manager.Value.Segments.First().SourceSegment.MaxBrightness;
    }
    
    [HttpPost]
    [Route("brightness")]
    public ActionResult<byte> PostBrightness([FromBody] byte brightness)
    {
        foreach (var ctrl in _manager.Value.Segments)
        {
            ctrl.SourceSegment.MaxBrightness = brightness;
        }
        return brightness;
    }
    
    [HttpGet]
    [Route("power")]
    public ActionResult<bool> GetPower()
    {
        return _manager.Value.Segments.Any(x => x.CurrentState is PowerState.On or PowerState.PoweringOn);
    }
    
    [HttpPost]
    [Route("power")]
    public async Task<ActionResult<bool>> PostPower([FromBody] bool powered)
    {
        await _manager.Value.PowerAllAsync(powered);
        return powered;
    }
    
    [HttpGet]
    [Route("powerConsumption")]
    public ActionResult<double> GetPowerConsumption()
    {
        return Math.Round(_manager.Value.Strip.PowerConsumption, 2);
    } 
    
    [HttpGet]
    [Route("amperage")]
    public ActionResult<double> GetAmperage()
    {
        return Math.Round(_manager.Value.Strip.Amperage, 2);
    } 
    
    [HttpGet]
    [Route("voltage")]
    public ActionResult<double> GetVoltage()
    {
        return Math.Round(_manager.Value.Strip.Voltage, 2);
    }
}
