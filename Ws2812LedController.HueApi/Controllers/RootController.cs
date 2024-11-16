using System.Collections;
using Microsoft.AspNetCore.Mvc;
using Ws2812LedController.Core;
using Ws2812LedController.Core.Model;
using Ws2812LedController.Core.Utils;
using Ws2812LedController.HueApi.Serializable;

namespace Ws2812LedController.HueApi.Controllers;

[ApiController]
[Route("/")]
public class RootController : ControllerBase
{
    private readonly Ref<LedManager> _manager;
    private readonly DiyHueCore _core;
    private readonly ILogger<RootController> _logger;

    public RootController(Ref<LedManager> manager, DiyHueCore core, ILogger<RootController> logger)
    {
        _manager = manager;
        _core = core;
        _logger = logger;
    }

    [HttpGet]
    [Route("detect")]
    public ActionResult<HueDescriptor> Detect()
    {
        return new HueDescriptor
        {
            Mac = _core.Mac,
            Name = _core.DeviceName,
            Lights = _manager.Value.Segments.Count,
            Protocol = "native_multi",
            ModelId = "LST002",
            Type = "ws2812_strip",
            Version = "3.1"
        };
    }
    
    [HttpPut]
    [Route("state")]
    public async Task<ActionResult<Dictionary<string, HueState>>> PutStates([FromBody] Dictionary<string, HueState> states)
    {
        var tasks = new List<Task>();
        foreach (var state in states)
        {
            var light = Convert.ToInt32(state.Key);
            if (light <= 0 || light > _manager.Value.Segments.Count)
                return BadRequest("Unknown light id");

            tasks.Add(_core.ApplyState(light - 1, state.Value));
        }

        await Task.WhenAll(tasks);
        
        return states;
    }
    
    [HttpGet]
    [Route("state")]
    public ActionResult<HueState> GetState([FromQuery] int light)
    {
        if (light <= 0)
            return BadRequest();

        return _core.DetermineState(light - 1);
    }
}
