using Avalonia.Collections;

namespace Ws2812RealtimeDesktopClient.Models;

public class EffectAssignment
{
    public string SegmentName { set; get; }
    public string EffectName { set; get; }

    public AvaloniaList<PropertyRow> Properties { set; get; }
    
    public void InflateProperties()
    {
        var desc =
            ReactiveEffectDescriptorList.Instance.Descriptors.FirstOrDefault(x => x.Name == EffectName);
        if (desc == null)
        {
            Console.WriteLine("");
            return;
        }
        
        Properties = new AvaloniaList<PropertyRow>(Properties.ToList().Where(x => x != null!));
        foreach (var prop in desc.Properties)
        {
            var propInfo = Properties.FirstOrDefault(x => x.Name == prop.Name);
            if (propInfo != null)
            {
                Console.WriteLine(prop.Name + "=" + prop.Value);
                propInfo.Update(prop, true);
            }
            else
            {
                Properties.Add(new PropertyRow(prop));
            }
        }
    }
}