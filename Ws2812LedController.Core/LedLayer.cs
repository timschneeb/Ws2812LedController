using System.Diagnostics;
using System.Drawing;

namespace Ws2812LedController.Core;

public class LedLayer
{
    public bool IsActive { get; set; } = true;
    public bool InvertX { set; get; }
    public LedMask? Mask { set; get; }

    public int AbsEnd => Width - 1;
    public int Width { get; }
    public Color[] LayerState { get; }
    
    public LedLayer(int length)
    {
        Width = length;
        LayerState = new Color[length];
        Array.Fill(LayerState, Color.FromArgb(0, 0, 0, 0));
    }

    public void SetPixel(int i, Color color)
    {
        if (!IsActive)
        {
            return;
        }
        
        if (InvertX)
        {
            i = AbsEnd - i;
        }

        var finalColor = Mask?.Condition(color, i, Width) ?? color;
        SetPixelInternal(i, finalColor);
    }

    private void SetPixelInternal(int i, Color color)
    {
        Debug.Assert(i >= 0 && i < Width);
        LayerState[i] = color;
    }

    public Color PixelAt(int i)
    {
        if (InvertX)
        {
            i = AbsEnd - i;
        }
        Debug.Assert(i >= 0 && i < Width);
        return LayerState[i];
    }
    
    public void CopyPixels(int destIndex, int srcIndex, int length)
    {
        if (!IsActive)
        {
            return;
        }
        
        if (InvertX)
        {
            destIndex = AbsEnd - destIndex - length;
            srcIndex = AbsEnd - srcIndex - length;
        }
        
        var buffer = new Color[srcIndex + length];
        for (var i = srcIndex; i < srcIndex + length; i++)
        {
            if (srcIndex + i < Width && srcIndex + i >= 0)
            {
                buffer[i] = PixelAt(srcIndex + i);
            }
            else
            {
                /* Out of bounds */
                buffer[i] = Color.Black;
            }
        }
        for (var i = srcIndex; i < srcIndex + length; i++)
        {
            Color color;
            if (destIndex + i < Width && destIndex + i >= 0)
            {
                color = buffer[i];
            }
            else
            {
                /* Out of bounds */
                color = Color.Black;
            }
            SetPixel(destIndex + i, color);
        }
    }
    
    public void Fill(int start, int length, Color color)
    {
        if (!IsActive)
        {
            return;
        }
        
        if (InvertX)
        {
            start = AbsEnd - start - length;
        }
        
        for (var i = start; i < start + length; i++)
        {
            SetPixel(i, color);
        }
    }
    
    public void Clear(Color? color = null)
    {
        if (!IsActive)
        {
            return;
        }
        
        for (var i = 0; i < Width; i++)
        {
            SetPixel(i, color ?? Color.Black);
        }
    }

    public byte[] DumpBytes()
    {
        var buffer = new byte[Width * 4];
        var i = 0;
        foreach(var color in LayerState)
        {
            buffer[i * 4] = color.R;
            buffer[i * 4 + 1] = color.G;
            buffer[i * 4 + 2] = color.B;
            buffer[i * 4 + 3] = color.A;
            i++;
        }

        return buffer;
    }

    public void UpdateBytes(byte[] bytes)
    {
        if (!IsActive)
        {
            return;
        }
        
        Debug.Assert(bytes.Length % 4 == 0);
        
        for(var index = 0; index < Width; index++)
        {
            SetPixel(index, Color.FromArgb(bytes[index * 4 + 3], bytes[index * 4], bytes[index * 4 + 1], bytes[index * 4 + 2]));
        }
    }
}