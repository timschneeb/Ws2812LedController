using System.Text.Json;
using Microsoft.AspNetCore.Mvc;
using Ws2812LedController.Core;
using Ws2812LedController.Core.Effects.Base;
using Ws2812LedController.Core.Model;
using Ws2812LedController.Core.Model.Serializable;
using Ws2812LedController.Core.Utils;

namespace Ws2812LedController.WebApi.Controllers;

[ApiController]
[Route("api/segment/{segmentName}/layer/{layer}")]
public class EffectController : ControllerBase, IJsonOptionHelper
{
    private readonly Ref<LedManager> _manager;
    private readonly ILogger<SegmentController> _logger;

    public EffectController(Ref<LedManager> manager, ILogger<SegmentController> logger)
    {
        _manager = manager;
        _logger = logger;
    }
    
    [HttpGet]
    [Route("[controller]s")]
    public ActionResult<IEnumerable<EffectDescriptor>> GetAvailableEffects([FromRoute] string segmentName, [FromRoute] LayerId layer)
    {
        var segment = _manager.FromSegmentName(segmentName);
        if (segment == null)
        {
            return NotFound();
        }
        
        return EffectDescriptorList.Descriptors;
    }
   
    [HttpGet]
    [Route("[controller]")]
    public ActionResult<EffectDescriptor> GetActiveEffect([FromRoute] string segmentName, [FromRoute] LayerId layer)
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

        var desc = EffectDescriptorList.Create(segment.CurrentEffects[(int)layer]);
        if (desc == null)
        {
            return new StatusCodeResult(500);
        }

        return desc;
    }
        
    [HttpPost]
    [Route("[controller]")]
    public async Task<ActionResult<EffectDescriptor>> Post([FromRoute] string segmentName, [FromBody] SetEffectData data, [FromRoute] LayerId layer)
    {
        var segment = _manager.FromSegmentName(segmentName);
        if (segment == null)
        {
            return NotFound("Unknown segment name");
        }
        
        var descList = EffectDescriptorList.Descriptors;
        var desc = descList.FirstOrDefault(x => x.Name == data.Name);
        if (desc == null)
        {
            return NotFound("Unknown effect class"); 
        }
        
        var temp = Activator.CreateInstance(desc.InternalType);
        if (temp is not BaseEffect effect)
        {
            return new StatusCodeResult(500); 
        }

        foreach (var property in data.Properties ?? Array.Empty<SetEffectData.Property>())
        {
            if (property.Name == null || property.Value is not JsonElement value)
            {
                continue;
            }
            
            var propertyInfo = desc.InternalType.GetProperty(property.Name);
            if (propertyInfo == null)
            {
                _logger.LogWarning($"Post: Property '{property.Name}' is unknown, skipping...");
                continue;
            }

            propertyInfo.SetValue(effect, value.Deserialize(propertyInfo.PropertyType, IJsonOptionHelper.GlobalJsonOptions));
        }
        
        var cancel = data.CancellationMethod?.Inflate();
        var cancelProp = desc.InternalType.GetProperty("CancellationMethod");
        if (cancel != null && cancelProp != null)
        {
            cancelProp.SetValue(effect, cancel);
        }

        // Fire-and-forget call; only power on automatically if the base layer is modified
        var _ = segment.SetEffectAsync(effect, data.PrevCancelMode ?? CancelMode.Now, layer != LayerId.BaseLayer, layer);
        
        var finalDesc = EffectDescriptorList.Create(effect);
        if (finalDesc == null)
        {
            return new OkResult();
        }
        return finalDesc;
    }
    
    [HttpPost]
    [Route("[controller]/cancel")]
    public async Task<ActionResult<EffectDescriptor>> PostCancelCurrentEffect([FromRoute] string segmentName, [FromRoute] LayerId layer, [FromBody] bool nextCycle)
    {
        var segment = _manager.FromSegmentName(segmentName);
        if (segment == null)
        {
            return NotFound("Unknown segment name");
        }
        
        if (segment.CurrentEffects[(int)layer] == null)
        {
            return new OkResult();
        }

        if (nextCycle)
        {
            segment.CurrentEffects[(int)layer]?.CancellationMethod.CancelNextCycleNonBlocking();
        }
        else
        {
            segment.CurrentEffects[(int)layer]?.CancellationMethod.Cancel();
        }

        return new OkResult();
    }
}
