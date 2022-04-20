using System.Text.Json;
using Microsoft.AspNetCore.Mvc;
using Ws2812LedController.Core;
using Ws2812LedController.Core.Effects.Base;
using Ws2812LedController.Core.Model;
using Ws2812LedController.Core.Model.Serializable;
using Ws2812LedController.Core.Utils;

namespace Ws2812LedController.WebApi.Controllers;

[ApiController]
[Route("api/segment/{segmentName}")]
public class LayerController : ControllerBase, IJsonOptionHelper
{
    private readonly Ref<LedManager> _manager;
    private readonly ILogger<SegmentController> _logger;

    public LayerController(Ref<LedManager> manager, ILogger<SegmentController> logger)
    {
        _manager = manager;
        _logger = logger;
    }
    
    [HttpGet]
    [Route("[controller]")]
    [Route("[controller]s")]
    public ActionResult<IEnumerable<LayerData>> GetLayers([FromRoute] string segmentName)
    {
        var segment = _manager.FromSegmentName(segmentName);
        if (segment == null)
        {
            return NotFound();
        }

        var layers = new LayerData[segment.SourceSegment.Layers.Length];
        for (var i = 0; i < segment.SourceSegment.Layers.Length; i++)
        {
            layers[i] = new LayerData(segment, (LayerId)i);
        }
       
        return layers;
    }
   
    [HttpGet]
    [Route("[controller]/{layer}")]
    public ActionResult<LayerData> GetLayer([FromRoute] string segmentName, [FromRoute] LayerId layer)
    {
        var segment = _manager.FromSegmentName(segmentName);
        if (segment == null)
        {
            return NotFound();
        }

        return new LayerData(segment, layer);
    }
}
