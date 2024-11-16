using System.Runtime.Serialization;
using Newtonsoft.Json;
using Ws2812RealtimeDesktopClient.ViewModels;

namespace Ws2812RealtimeDesktopClient.Models;

public class SegmentEntry : ViewModelBase, ISerializable
{
    public string Name { set; get; }
    public string[] MirroredTo { set; get; }
    public int Start { set; get; }
    public int Width { set; get; }
    public bool InvertX { set; get; }

    [JsonIgnore]
    public string Description
    {
        get
        {
            var desc = $"Offset: {Start}, Width: {Width}";
            if (InvertX)
            {
                desc += ", Inverted";
            }

            if (MirroredTo.Length > 0)
            {
                desc += ", Duplicated to:";
                foreach (var m in MirroredTo)
                {
                    var isLast = m == MirroredTo.Last();
                    desc += isLast ? $" {m}" : $" {m},";
                }
            }
            return desc;
        }
    }

    [JsonConstructor]
    public SegmentEntry()
    {
        Name = string.Empty;
        MirroredTo = Array.Empty<string>();
    }
    public SegmentEntry(string name, int start, int width, bool invertX = false, string[]? mirroredTo = null)
    {
        Name = name;
        InvertX = invertX;
        MirroredTo = mirroredTo ?? Array.Empty<string>();
        Start = start;
        Width = width;
    }

    public void UpdateFromViewModel()
    {
        RaisePropertyChanged(nameof(Name));
        RaisePropertyChanged(nameof(Description));
    }

    public SegmentEntry( SerializationInfo info, StreamingContext context )
    {
        Name = (string)(info.GetValue(nameof(Name), typeof(string)) ?? string.Empty);
        Start = (int)(info.GetValue(nameof(Start), typeof(int)) ?? 0);
        Width = (int)(info.GetValue(nameof(Name), typeof(int)) ?? 1);
        InvertX = (bool)(info.GetValue(nameof(InvertX), typeof(bool)) ?? false);
        MirroredTo = (string[])(info.GetValue(nameof(MirroredTo), typeof(string[])) ?? Array.Empty<string>());
    }
            
    public void GetObjectData(SerializationInfo info, StreamingContext context)
    {
        info.AddValue(nameof(Name), Name);
        info.AddValue(nameof(Start), Start);
        info.AddValue(nameof(Width), Width);
        info.AddValue(nameof(InvertX), InvertX);
        info.AddValue(nameof(MirroredTo), MirroredTo);
    }
}