using System.Drawing;
using System.Runtime.Serialization;
using Ws2812LedController.Core.Utils;

namespace Ws2812LedController.Core.FastLedCompatibility;

public enum TBlendType
{
	None = 0,
	LinearBlend = 1
}

[Serializable]
public class CRGBPalette16 : ISerializable
{
	public Color ColorFromPalette(byte index, byte brightness, TBlendType blendType)
	{
		// Note: switched hi4 and lo4
		byte hi4 = (byte)(index & 0x0F);
		byte lo4 = (byte)(index >> 4);
		
		Color entry = this[hi4];
		bool blend = lo4 > 0 && (blendType != TBlendType.None);

		byte red1 = entry.R;
		byte green1 = entry.G;
		byte blue1 = entry.B;

		if (blend)
		{

			if (hi4 == 15)
			{
				entry = this[0];
			}
			else
			{
				++hi4;
				entry = this[hi4];
			}

			byte f2 = (byte)(lo4 << 4);
			byte f1 = (byte)(255 - f2);

			//    rgb1.nscale8(f1);
			byte red2 = entry.R;
			red1 = Scale.scale8(red1, f1);
			red2 = Scale.scale8(red2, f2);
			red1 += red2;

			byte green2 = entry.G;
			green1 = Scale.scale8(green1, f1);
			green2 = Scale.scale8(green2, f2);
			green1 += green2;

			byte blue2 = entry.B;
			blue1 = Scale.scale8(blue1, f1);
			blue2 = Scale.scale8(blue2, f2);
			blue1 += blue2;

		}

		if (brightness != 255)
		{
			if (brightness != 0)
			{
				++brightness; // adjust for rounding
				// Now, since brightness is nonzero, we don't need the full scale8_video logic;
				// we can just to scale8 and then add one (unless scale8 fixed) to all nonzero inputs.
				if (red1 != 0)
				{
					red1 = Scale.scale8(red1, brightness);
//#if !(FASTLED_SCALE8_FIXED==1)
//					++red1;
//#endif
				}

				if (green1 != 0)
				{
					green1 = Scale.scale8(green1, brightness);
//#if !(FASTLED_SCALE8_FIXED==1)
//					++green1;
//#endif
				}

				if (blue1 != 0)
				{
					blue1 = Scale.scale8(blue1, brightness);
//C++ TO C# CONVERTER TODO TASK: C# does not allow setting or comparing #define constants:
//#if !(FASTLED_SCALE8_FIXED==1)
//					++blue1;
//#endif
				}

			}
			else
			{
				red1 = 0;
				green1 = 0;
				blue1 = 0;
			}
		}

		return Color.FromArgb(red1, green1, blue1);
	}

	public enum Palette
	{
		Lava,
		Ocean,
		Neon,
	}
	
	public readonly Color[] Entries = new Color[16];
	public CRGBPalette16()
	{
	}
	public CRGBPalette16(Palette pal)
	{
		switch (pal)
		{
			case Palette.Ocean:
				Entries = new[]
				{
					Color.FromArgb(0x19,0x19,0x70),
					Color.FromArgb(0x00,0x00,0x8B),
					Color.FromArgb(0x19,0x19,0x70),
					Color.FromArgb(0x00,0x00,0x80),
					
					Color.FromArgb(0x00,0x00,0x8B),
					Color.FromArgb(0x00,0x00,0xCD),
					Color.FromArgb(0x2E,0x8B,0x57),
					Color.FromArgb(0x00,0x80,0x80),

					Color.FromArgb(0x5F,0x9E,0xA0),
					Color.FromArgb(0x00,0x00,0xFF),
					Color.FromArgb(0x00,0x8B,0x8B),
					Color.FromArgb(0x64,0x95,0xED),

					Color.FromArgb(0x7F,0xFF,0xD4),
					Color.FromArgb(0x2E,0x8B,0x57),
					Color.FromArgb(0x00,0xFF,0xFF),
					Color.FromArgb(0x87,0xCE,0xFA),
				};
				break;
			case Palette.Lava:
				Entries = new[]
				{
					Color.FromArgb(0x00,0x00,0x00),
					Color.FromArgb(0x80,0x00,0x00),
					Color.FromArgb(0x00,0x00,0x00),
					Color.FromArgb(0x80,0x00,0x00),
					
					Color.FromArgb(0x8B,0x00,0x00),
					Color.FromArgb(0x80,0x00,0x00),
					Color.FromArgb(0x8B,0x00,0x00),
					Color.FromArgb(0x80,0x00,0x00),

					Color.FromArgb(0x8B,0x00,0x00),
					Color.FromArgb(0x8B,0x00,0x00),
					Color.FromArgb(0xFF,0x00,0x00),
					Color.FromArgb(0xFF,0xA5,0x00),

					Color.FromArgb(0xFF,0xFF,0xFF),
					Color.FromArgb(0xFF,0xA5,0x00),
					Color.FromArgb(0xFF,0x00,0x00),
					Color.FromArgb(0x8B,0x00,0x00),
				};
				break;
			case Palette.Neon:
				fill_gradient_RGB(16, Color.FromArgb(63, 0, 255), Color.FromArgb(210, 0, 255));
				break;
		}
	}
	
	
	public CRGBPalette16(Color c1, Color c2, Color c3, Color c4)
	{
		fill_gradient_RGB( 16, c1, c2, c3, c4);
	}
	public CRGBPalette16(Color c1, Color c2, Color c3)
	{
		fill_gradient_RGB( 16, c1, c2, c3);
	}
	public CRGBPalette16(Color c1, Color c2)
	{
		fill_gradient_RGB( 16, c1, c2);
	}
	
	public CRGBPalette16(Color c1)
	{
		Array.Fill(Entries, c1);
	}

	public CRGBPalette16(Color[] colors)
	{
		if (colors.Length == Entries.Length)
		{
			Entries = colors;
			return;
		}

		var temp = new Color[16];
		var count = 0;
		while (count < 16)
		{
			for (var i = 0; i < colors.Length; i++)
			{
				temp[count] = colors[i];
				count++;

				if (count >= 16)
					break;
			}
		}

		Entries = temp;
	}
	
	private void fill_gradient_RGB(ushort numLeds, Color c1, Color c2)
	{
		ushort last = (ushort)(numLeds - 1);
		fill_gradient_RGB(0, c1, last, c2);
	}


	private void fill_gradient_RGB(ushort numLeds, Color c1, Color c2, Color c3)
	{
		ushort half = (ushort)(numLeds / 2);
		ushort last = (ushort)(numLeds - 1);
		fill_gradient_RGB(0, c1, half, c2);
		fill_gradient_RGB(half, c2, last, c3);
	}
	
	public void fill_gradient_RGB(ushort numLeds, Color c1, Color c2, Color c3, Color c4)
	{
		ushort onethird = (ushort)(numLeds / 3);
		ushort twothirds = (ushort)((numLeds * 2) / 3);
		ushort last = (ushort)(numLeds - 1);
		fill_gradient_RGB(0, c1, onethird, c2);
		fill_gradient_RGB(onethird, c2, twothirds, c3);
		fill_gradient_RGB( twothirds, c3, last, c4);
	}


	public void fill_gradient_RGB(ushort startpos, Color startcolor, ushort endpos, Color endcolor)
	{
		var rdistance87 = (endcolor.R - startcolor.R) << 7;
		var gdistance87 = (endcolor.G - startcolor.G) << 7;
		var bdistance87 = (endcolor.B - startcolor.B) << 7;

		ushort pixeldistance = (ushort)(endpos - startpos);
		int divisor = pixeldistance != 0 ? pixeldistance : 1;

		int rdelta87 = rdistance87 / divisor;
		int gdelta87 = gdistance87 / divisor;
		int bdelta87 = bdistance87 / divisor;

		rdelta87 *= 2;
		gdelta87 *= 2;
		bdelta87 *= 2;

		int r88 = startcolor.R << 8;
		int g88 = startcolor.G << 8;
		int b88 = startcolor.B << 8;
		for (ushort i = startpos; i <= endpos; ++i)
		{
			Entries[i] = Color.FromArgb(r88 >> 8, g88 >> 8, b88 >> 8);
			r88 += rdelta87;
			g88 += gdelta87;
			b88 += bdelta87;
		}
	}



	public Color this [int x]
	{
		get => Entries[(byte)x];
		set => Entries[(byte)x] = value;
	}

	private void SetChannel(int index, byte channel, byte value)
	{
		switch (channel)
		{
			case 0:
				Entries[index] = Color.FromArgb(0xFF, value, Entries[index].G, Entries[index].B);
				break;
			case 1:
				Entries[index] = Color.FromArgb(0xFF, Entries[index].R, value, Entries[index].B);
				break;
			case 2:
				Entries[index] = Color.FromArgb(0xFF, Entries[index].R, Entries[index].G, value);
				break;
		}
	}
	
	private byte GetChannel(int index, byte channel)
	{
		if (index >= Entries.Length)
		{
			return 0;
		}
		
		return channel switch
		{
			0 => Entries[index].R,
			1 => Entries[index].G,
			2 => Entries[index].B,
			_ => 0
		};
	}


	public static void nblendPaletteTowardPalette(CRGBPalette16 current, CRGBPalette16 target, byte maxChanges)
	{
		byte changes = 0;

		for (byte i = 0; i < current.Entries.Length; ++i)
		{
			for (byte channel = 0; channel < 3; ++channel)
			{
				byte currentC = current.GetChannel(i, channel); 
				byte targetC = target.GetChannel(i, channel); 
				
				// if the values are equal, no changes are needed
				if (currentC == targetC)
				{
					continue;
				}

				// if the current value is less than the target, increase it by one
				if (currentC < targetC)
				{
					current.SetChannel(i, channel, (byte)(current.GetChannel(i, channel) + 1));
					++changes;
				}

				// if the current value is greater than the target,
				// increase it by one (or two if it's still greater).
				if (currentC > targetC)
				{
					current.SetChannel(i, channel, (byte)(current.GetChannel(i, channel) - 1));
					++changes;
					if (current.GetChannel(i, channel) > target.GetChannel(i, channel))
					{
						current.SetChannel(i, channel, (byte)(current.GetChannel(i, channel) - 1));
					}
				}

				// if we've hit the maximum number of changes, exit
				if (changes >= maxChanges)
				{
					break;
				}
			}
		}
	}

	public CRGBPalette16(SerializationInfo info, StreamingContext context)
	{
		var entryArray = (uint[])(info.GetValue(nameof(Entries), typeof(uint[])) ?? 0);
		var colorArray = new Color[16];
		Array.Fill(colorArray, Color.Black);
		for (var i = 0; i < Math.Min(16, entryArray.Length); i++)
		{
			colorArray[i] = entryArray[i].ToColor();
		}
		Entries = colorArray;
	}
            
	public void GetObjectData(SerializationInfo info, StreamingContext context)
	{
		var entryArray = new uint[Entries.Length];
		for (var i = 0; i < Math.Min(Entries.Length, entryArray.Length); i++)
		{
			entryArray[i] = Entries[i].ToUInt32();
		}
		
		info.AddValue(nameof(Entries), entryArray);
	}
}

    