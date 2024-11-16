using System.Drawing;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Ws2812LedController.WebApi.Converters;

public class ColorConverter : JsonConverter<Color>
{
    public override Color Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        => FromHtmlArgb(reader.GetString() ?? string.Empty);

    public override void Write(Utf8JsonWriter writer, Color value, JsonSerializerOptions options)
        => writer.WriteStringValue("#" + value.A.ToString("X2") + value.R.ToString("X2") + value.G.ToString("X2") + value.B.ToString("X2"));
    
    public static Color FromHtmlArgb(string? htmlColor)
    {
        // empty color
        if ((htmlColor == null) || (htmlColor.Length == 0))
            return Color.Empty;

        // #AARRGGBB
        if (htmlColor[0] == '#' &&
            htmlColor.Length == 9)
        {
            return Color.FromArgb(Convert.ToInt32(htmlColor.Substring(1, 2), 16),
                Convert.ToInt32(htmlColor.Substring(3, 2), 16),
                Convert.ToInt32(htmlColor.Substring(5, 2), 16),
                Convert.ToInt32(htmlColor.Substring(7, 2), 16));
        }

        return ColorTranslator.FromHtml(htmlColor);
    }
}