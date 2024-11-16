using System.Drawing;
using ScreenCapture.Base;

namespace Ws2812LedController.Ambilight;

public class Processor
{
    private int _mappingType = 0; // TODO
       
    
    public Color[] Process(IImage image, LedZone zone, ushort[] advanced)
		{
			List<Color> colors;
			switch (_mappingType)
			{
			case 3:
			case 2:
				colors = new List<Color>(getMeanAdvLedColor(image, zone, advanced));
				break;
			case 1:
				colors = new List<Color>(getUniLedColor(image, zone));
				break;
			default:
				colors = new List<Color>(getMeanLedColor(image, zone));
				break;
			}

            return colors.ToArray();
		}

		private Color[] getMeanLedColor(IImage image, LedZone zone)
		{
			List<Color> ledColors = new List<Color>(zone.Pixels.Length);

            // Iterate each led and compute the mean
            foreach (var pixel in zone.Pixels)
			{
				var color = calcMeanColor(image, pixel);
                ledColors.Add(color);
			}

			return ledColors.ToArray();
		}

		private Color[] getUniLedColor(IImage image, LedZone zone)
        {
            var ledColors = new Color[zone.Pixels.Length];
            Array.Fill(ledColors, calcMeanColor(image));
            return ledColors.ToArray();
		}
        
		private Color[] getMeanAdvLedColor(IImage image, LedZone zone, ushort[] lut)
		{
			var ledColors = new List<Color>(zone.Pixels.Length);
            
            foreach (var colors in zone.Pixels)
            {
                ledColors.Add(calcMeanAdvColor(image, colors, lut));
            }

            return ledColors.ToArray();
		}
		private Color calcMeanColor(IImage image, LedPixel pixel)
		{
			var colorVecSize = pixel.SubPixels.Length;

			if (colorVecSize == 0)
			{
				return Color.Black;
			}

			// Accumulate the sum of each separate color channel
			uint sumRed = 0;
			uint sumGreen = 0;
			uint sumBlue = 0;

			foreach (var colorOffset in pixel.SubPixels)
			{
                var color = image.GetPixel(colorOffset.X, colorOffset.Y);
				sumRed += color.R;
				sumGreen += color.G;
				sumBlue += color.B;
			}

			// Compute the average of each color channel
			byte avgRed = (byte)(sumRed / colorVecSize);
			byte avgGreen = (byte)(sumGreen / colorVecSize);
			byte avgBlue = (byte)(sumBlue / colorVecSize);

			// Return the computed color
			return Color.FromArgb(avgRed, avgGreen, avgBlue);
		}
        
		private Color calcMeanAdvColor(IImage image, LedPixel pixel, ushort[] lut)
		{
			var colorVecSize = pixel.SubPixels.Length;

			if (colorVecSize == 0)
			{
				return Color.Black;
			}

			// Accumulate the sum of each seperate color channel
			ulong sum1 = 0;
			ulong sumRed1 = 0;
			ulong sumGreen1 = 0;
			ulong sumBlue1 = 0;

			ulong sum2 = 0;
			ulong sumRed2 = 0;
			ulong sumGreen2 = 0;
			ulong sumBlue2 = 0;

			foreach (var subPixel in pixel.SubPixels)
			{
				if (subPixel.X >= 0 && subPixel.Y >= 0)
                {
                    var color = image.GetPixel(subPixel.X, subPixel.Y);
					sumRed1 += lut[color.R];
					sumGreen1 += lut[color.G];
					sumBlue1 += lut[color.B];
					sum1++;
				}
                // TODO remove this
				else
				{
                    var color = image.GetPixel(-subPixel.X, -subPixel.Y);
                    sumRed2 += lut[color.R];
					sumGreen2 += lut[color.G];
					sumBlue2 += lut[color.B];
					sum2++;
				}
			}


			if (sum1 > 0 && sum2 > 0)
			{
				ushort avgRed = (ushort)Math.Min((uint)Math.Sqrt(((sumRed1 * 3.0) / sum1 + sumRed2 / (double)sum2) / 4.0), (uint)255);
				ushort avgGreen = (ushort)Math.Min((uint)Math.Sqrt(((sumGreen1 * 3.0) / sum1 + sumGreen2 / (double)sum2) / 4.0), (uint)255);
				ushort avgBlue = (ushort)Math.Min((uint)Math.Sqrt(((sumBlue1 * 3.0) / sum1 + sumBlue2 / (double)sum2) / 4.0), (uint)255);

                return Color.FromArgb(avgRed, avgGreen, avgBlue);
			}
			else
			{
				ushort avgRed = (ushort)Math.Min((uint)Math.Sqrt((sumRed1 + sumRed2) / (double)(sum1 + sum2)), (uint)255);
				ushort avgGreen = (ushort)Math.Min((uint)Math.Sqrt((sumGreen1 + sumGreen2) / (double)(sum1 + sum2)), (uint)255);
				ushort avgBlue = (ushort)Math.Min((uint)Math.Sqrt((sumBlue1 + sumBlue2) / (double)(sum1 + sum2)), (uint)255);

                return Color.FromArgb(avgRed, avgGreen, avgBlue);
			}
		}
        
		private Color calcMeanColor(IImage image)
		{
			// Return the computed color
            return image.AverageColorOfRegion(image.Width, image.Height, 0, 0);
		}
}