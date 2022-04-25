using Microsoft.AspNetCore.Mvc;
using Ws2812LedController.Core;
using Ws2812LedController.Core.Model;
using Ws2812LedController.Core.Utils;
using Ws2812LedController.WebApi.Serializable;

namespace Ws2812LedController.WebApi.Controllers;

[ApiController]
[Route("api/[controller]")]
public class SegmentController : ControllerBase
{
    private readonly Ref<LedManager> _manager;
    private readonly ILogger<SegmentController> _logger;

    public SegmentController(Ref<LedManager> manager, ILogger<SegmentController> logger)
    {
        _manager = manager;
        _logger = logger;
    }

    [HttpGet]
    public ActionResult<IEnumerable<SegmentData>> Get()
    {
        var segments = _manager.Value.Segments;
        var ret = new SegmentData[segments.Count];
        for (var i = 0; i < segments.Count; i++)
        {
            ret[i] = new SegmentData(segments[i], _manager);
        }

        return ret;
    }
    
    [HttpGet]
    [Route("{segmentName}")]
    public ActionResult<SegmentData> Get(string segmentName)
    {
        var segment = _manager.FromSegmentName(segmentName);
        if (segment == null)
        {
            return NotFound();
        }
        
        return new SegmentData(segment, _manager);
    }

    [HttpGet]
    [Route("{segmentName}/brightness")]
    public ActionResult<byte> GetBrightness(string segmentName)
    {
        var segment = _manager.FromSegmentName(segmentName);
        if (segment == null)
        {
            return NotFound();
        }
        
        return segment.SourceSegment.MaxBrightness;
    }
    
    [HttpPost]
    [Route("{segmentName}/brightness")]
    public ActionResult PostBrightness(string segmentName, [FromBody] byte brightness)
    {
        var segment = _manager.FromSegmentName(segmentName);
        if (segment == null)
        {
            return NotFound();
        }

        segment.SourceSegment.MaxBrightness = brightness;
        
        return new OkResult();
    }
    
    [HttpGet]
    [Route("{segmentName}/power")]
    public ActionResult<PowerState> GetPower(string segmentName)
    {
        var segment = _manager.FromSegmentName(segmentName);
        if (segment == null)
        {
            return NotFound();
        }
        
        return segment.CurrentState;
    }
    
    [HttpPost]
    [Route("{segmentName}/power")]
    public async Task<ActionResult> PostPower(string segmentName, [FromBody] bool powered)
    {
        var segment = _manager.FromSegmentName(segmentName);
        if (segment == null)
        {
            return NotFound();
        }

        await segment.PowerAsync(powered);
        
        return new OkResult();
    }
}
