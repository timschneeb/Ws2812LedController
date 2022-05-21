using System.Drawing;
using ScreenCapture.Base;

namespace Ws2812LedController.Ambilight;

using System;


	///
	/// Result structure of the detected blackborder.
	///
	public class BlackBorder
	{
		/// Flag indicating if the border is unknown
		public bool unknown;

		/// The size of the detected horizontal border
		public int horizontalSize;

		/// The size of the detected vertical border
		public int verticalSize;

        public BlackBorder(bool unknown, int horizontalSize, int verticalSize)
        {
            this.unknown = unknown;
            this.horizontalSize = horizontalSize;
            this.verticalSize = verticalSize;
        }

        public BlackBorder(int horizontalSize, int verticalSize)
        {
            this.horizontalSize = horizontalSize;
            this.verticalSize = verticalSize;
        }

        ///
		/// Compares this BlackBorder to the given other BlackBorder
		///
		/// @param[in] other  The other BlackBorder
		///
		/// @return True if this is the same border as other
		///
        public static bool operator ==(BlackBorder @this, BlackBorder other)
        {
            if (@this.unknown)
            {
                return other.unknown;
            }

            return (other.unknown == false) && (@this.horizontalSize == other.horizontalSize) && (@this.verticalSize == other.verticalSize);
        }

        public static bool operator !=(BlackBorder @this, BlackBorder other)
        {
            return !(@this == other);
        }
    }

	///
	/// The BlackBorderDetector performs detection of black-borders on a single image.
	/// The detector will search for the upper left corner of the picture in the frame.
	/// Based on detected black pixels it will give an estimate of the black-border.
	///
	public class BlackBorderDetector
	{
		///
		/// Constructs a black-border detector
		/// @param[in] threshold The threshold which the black-border detector should use
		///
		public BlackBorderDetector(double threshold)
		{
            _blackborderThreshold = calculateThreshold(threshold);
        }

		///
		/// Performs the actual black-border detection on the given image
		///
		/// @param[in] image  The image on which detection is performed
		///
		/// @return The detected (or not detected) black border info
		///
        public byte calculateThreshold(double threshold)
		{
			var rgbThreshold = (int)Math.Ceiling(threshold * 255);

            rgbThreshold = rgbThreshold switch
            {
                < 0 => 0,
                > 255 => 255,
                _ => rgbThreshold
            };

            //Debug(Logger::getInstance("BLACKBORDER"), "threshold set to %f (%d)", threshold , int(blackborderThreshold));

			return (byte)rgbThreshold;
		}

		///
		/// default detection mode (3lines 4side detection)
		public BlackBorder process(IImage image)
		{
			// test centre and 33%, 66% of width/height
			// 33 and 66 will check left and top
			// centre will check right and bottom sides
            var width = image.Width;
            var height = image.Height;
            var width33percent = width / 3;
            var height33percent = height / 3;
            var width66percent = width33percent * 2;
            var height66percent = height33percent * 2;
            var xCenter = width / 2;
            var yCenter = height / 2;


            var firstNonBlackXPixelIndex = -1;
            var firstNonBlackYPixelIndex = -1;

			width--; // remove 1 pixel to get end pixel index
			height--;

			// find first X pixel of the image
			for (int x = 0; x < width33percent; ++x)
			{
				if (!isBlack(image.GetPixel((width - x), yCenter)) || !isBlack(image.GetPixel(x, height33percent)) || !isBlack(image.GetPixel(x, height66percent)))
				{
					firstNonBlackXPixelIndex = x;
					break;
				}
			}

			// find first Y pixel of the image
			for (int y = 0; y < height33percent; ++y)
			{
				if (!isBlack(image.GetPixel(xCenter, (height - y))) || !isBlack(image.GetPixel(width33percent, y)) || !isBlack(image.GetPixel(width66percent, y)))
				{
					firstNonBlackYPixelIndex = y;
					break;
				}
			}

			// Construct result
            return new BlackBorder(unknown: firstNonBlackXPixelIndex == -1 || firstNonBlackYPixelIndex == -1,
                horizontalSize: firstNonBlackYPixelIndex, verticalSize: firstNonBlackXPixelIndex);
		}

		///
		/// classic detection mode (topleft single line mode)

		///
		/// classic detection mode (topleft single line mode)
//C++ TO C# CONVERTER WARNING: 'const' methods are not available in C#:
//ORIGINAL LINE: BlackBorder process_classic(const Image<ColorRgb>& image) const
		public BlackBorder process_classic(IImage image)
		{
			// only test the topleft third of the image
            var width = (image.Width / 3);
            var height = (image.Height / 3);
            var maxSize = Math.Max(width, height);

            var firstNonBlackXPixelIndex = -1;
            var firstNonBlackYPixelIndex = -1;

			// find some pixel of the image
			for (var i = 0; i < maxSize; ++i)
			{
                var x = Math.Min(i, width);
                var y = Math.Min(i, height);

				var color = image.GetPixel(x, y);
				if (!isBlack(color))
				{
					firstNonBlackXPixelIndex = x;
					firstNonBlackYPixelIndex = y;
					break;
				}
			}

			// expand image to the left
			for (; firstNonBlackXPixelIndex > 0; --firstNonBlackXPixelIndex)
			{
				var color = image.GetPixel(firstNonBlackXPixelIndex - 1, firstNonBlackYPixelIndex);
				if (isBlack(color))
				{
					break;
				}
			}

			// expand image to the top
			for (; firstNonBlackYPixelIndex > 0; --firstNonBlackYPixelIndex)
			{
                var color = image.GetPixel(firstNonBlackXPixelIndex, firstNonBlackYPixelIndex - 1);
				if (isBlack(color))
				{
					break;
				}
			}

			// Construct result
			return new BlackBorder(unknown: firstNonBlackXPixelIndex == -1 || firstNonBlackYPixelIndex == -1,
                horizontalSize: firstNonBlackYPixelIndex, verticalSize: firstNonBlackXPixelIndex);
        }
        
		///
		/// osd detection mode (find x then y at detected x to avoid changes by osd overlays)

		///
		/// osd detection mode (find x then y at detected x to avoid changes by osd overlays)
        public BlackBorder process_osd(IImage image)
		{
			// find X position at height33 and height66 we check from the left side, Ycenter will check from right side
			// then we try to find a pixel at this X position from top and bottom and right side from top
            var width = image.Width;
            var height = image.Height;
            var width33percent = width / 3;
            var height33percent = height / 3;
            var height66percent = height33percent * 2;
            var yCenter = height / 2;


            var firstNonBlackXPixelIndex = -1;
            var firstNonBlackYPixelIndex = -1;

			width--; // remove 1 pixel to get end pixel index
			height--;

			// find first X pixel of the image
			int x;
			for (x = 0; x < width33percent; ++x)
			{
				if (!isBlack(image.GetPixel((width - x), yCenter)) || !isBlack(image.GetPixel(x, height33percent)) || !isBlack(image.GetPixel(x, height66percent)))
				{
					firstNonBlackXPixelIndex = x;
					break;
				}
			}

			// find first Y pixel of the image
			for (var y = 0; y < height33percent; ++y)
			{
				// left side top + left side bottom + right side top  +  right side bottom
				if (!isBlack(image.GetPixel(x, y)) || !isBlack(image.GetPixel(x, (height - y))) || !isBlack(image.GetPixel((width - x), y)) || !isBlack(image.GetPixel((width - x), (height - y))))
				{
					firstNonBlackYPixelIndex = y;
					break;
				}
			}

			// Construct result
		    return new BlackBorder(unknown: firstNonBlackXPixelIndex == -1 || firstNonBlackYPixelIndex == -1,
                horizontalSize: firstNonBlackYPixelIndex, verticalSize: firstNonBlackXPixelIndex);
		}


		///
		/// letterbox detection mode (5lines top-bottom only detection)
        public BlackBorder process_letterbox(IImage image)
		{
			// test center and 25%, 75% of width
			// 25 and 75 will check both top and bottom
			// center will only check top (minimise false detection of captions)
            var width = image.Width;
            var height = image.Height;
            var width25percent = width / 4;
            var height33percent = height / 3;
            var width75percent = width25percent * 3;
			var xCenter = width / 2;
            
            var firstNonBlackYPixelIndex = -1;

			height--; // remove 1 pixel to get end pixel index

			// find first Y pixel of the image
			for (var y = 0; y < height33percent; ++y)
			{
				if (!isBlack(image.GetPixel(xCenter, y)) || !isBlack(image.GetPixel(width25percent, y)) || !isBlack(image.GetPixel(width75percent, y)) || !isBlack(image.GetPixel(width25percent, (height - y))) || !isBlack(image.GetPixel(width75percent, (height - y))))
				{
					firstNonBlackYPixelIndex = y;
					break;
				}
			}

			// Construct result
			return new BlackBorder(unknown: firstNonBlackYPixelIndex == -1, horizontalSize: firstNonBlackYPixelIndex,
                verticalSize: 0);
        }
        
		///
		/// Checks if a given color is considered black and therefore could be part of the border.
		///
		/// @param[in] color  The color to check
		///
		/// @return True if the color is considered black else false
		///
        private bool isBlack(Color color)
		{
			// Return the simple compare of the color against black
			return (color.R < _blackborderThreshold) && (color.G < _blackborderThreshold) && (color.B < _blackborderThreshold);
		}

		/// Threshold for the black-border detector [0 .. 255]
		private readonly byte _blackborderThreshold;

	}