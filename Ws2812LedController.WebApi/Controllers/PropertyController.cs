using System.Text.Json;
using Microsoft.AspNetCore.Mvc;
using Ws2812LedController.Core;
using Ws2812LedController.Core.Model;
using Ws2812LedController.Core.Utils;
using Ws2812LedController.WebApi.Converters;
using Ws2812LedController.WebApi.Serializable;

namespace Ws2812LedController.WebApi.Controllers;

[ApiController]
[Route("api/segment/{segmentName}/layer/{layer}/effect/prop/{propertyName}")]
public class PropertyController : ControllerBase, IJsonOptionHelper
{
    private readonly Ref<LedManager> _manager;
    private readonly ILogger<SegmentController> _logger;

    public PropertyController(Ref<LedManager> manager, ILogger<SegmentController> logger)
    {
        _manager = manager;
        _logger = logger;
    }
    
    [HttpGet]
    public ActionResult<EffectProperty> Get([FromRoute] string segmentName, [FromRoute] string propertyName, LayerId layer)
    {
        var segment = _manager.FromSegmentName(segmentName);
        if (segment == null)
        {
            return NotFound();
        }
        
        if (segment.CurrentEffects[(int)layer] == null)
        {
            return new NoContentResult();
        }

        var prop = segment.FromPropertyName(propertyName, layer);
        if (prop == null)
        {
            return NotFound();
        }

        return prop;
    }

    [HttpPost]
    public ActionResult<SegmentData> Post([FromRoute] string segmentName, [FromRoute] string propertyName, [FromBody] JsonElement value, LayerId layer)
    {
        var segment = _manager.FromSegmentName(segmentName);
        if (segment == null)
        {
            return NotFound();
        }
        
        if (segment.CurrentEffects[(int)layer] == null)
        {
            return new NoContentResult();
        }

        var effectType = segment.CurrentEffects[(int)layer]?.GetType();
        var propertyInfo = effectType?.GetProperty(propertyName);
        if (propertyInfo == null)
        {
            return NotFound();
        }

        propertyInfo.SetValue(segment.CurrentEffects[(int)layer], value.Deserialize(propertyInfo.PropertyType, IJsonOptionHelper.GlobalJsonOptions));

        return new OkResult();
    }
}
