using ScreenCapture.Base;

namespace Ws2812LedController.Ambilight;

	public class BlackBorderProcessor
	{
        public BlackBorderProcessor()
        {
            _detector = new BlackBorderDetector(0.05);
            // TODO handleSettingsUpdate(settings::type::BLACKBORDER, _hyperhdr->getSetting(settings::type::BLACKBORDER));
        }
        
		///
		/// Return the current (detected) border
		/// @return The current border
		///
        private BlackBorder getCurrentBorder()
		{
            // TODO: copy
			return _currentBorder;
		}

		///
		/// Return activation state of black border detector
		/// @return The current border
		//
        private bool enabled()
		{
			return _enabled;
		}

		///
		/// Set activation state of black border detector
		/// @param enable current state
		///
		private void setEnabled(bool enable)
		{
			_enabled = enable;
		}

		///
		/// Sets the _hardDisabled state, if True prevents the enable from COMP_BLACKBORDER state emit (mimics wrong state to external!)
		/// It's not possible to enable black-border detection from this method, if the user requested a disable!
		/// @param disable  The new state
		///
		private void setHardDisable(bool disable)
		{
			if (disable)
			{
				_enabled = false;
			}
			else
			{
				// the user has the last word to enable
				if (_userEnabled)
				{
					_enabled = true;
				}
			}
			_hardDisabled = disable;
		}

		///
		/// Processes the image. This performs detection of black-border on the given image and
		/// updates the current border accordingly. If the current border is updated the method call
		/// will return true else false
		///
		/// @param image The image to process
		///
		/// @return True if a different border was detected than the current else false
		///
		private bool process(IImage image)
		{
			// get the border for the single image
			var imageBorder = new BlackBorder(horizontalSize: 0, verticalSize: 0);

            if (!enabled())
			{
				imageBorder.unknown = true; 
                _currentBorder = imageBorder;
				return true;
			}

            imageBorder = _detectionMode switch
            {
                "default" => _detector.process(image),
                "classic" => _detector.process_classic(image),
                "osd" => _detector.process_osd(image),
                "letterbox" => _detector.process_letterbox(image),
                _ => imageBorder
            };
            // add blur to the border
			if (imageBorder.horizontalSize > 0)
			{
				imageBorder.horizontalSize = (int)(_blurRemoveCnt + imageBorder.horizontalSize);
			}
			if (imageBorder.verticalSize > 0)
			{
				imageBorder.verticalSize += (int)(_blurRemoveCnt + imageBorder.verticalSize);
			}

			return updateBorder(imageBorder);
		}

		///
		/// @brief Handle component state changes, it's not possible for BB to be enabled, when a hardDisable is active
		///
		private void handleCompStateChangeRequest(bool enable)
		{
            _userEnabled = enable;
			if (enable)
			{
				// eg effects and probably other components don't want a BB, mimik a wrong comp state to the comp register
				if (!_hardDisabled)
				{
					_enabled = enable;
				}
			}
			else
			{
				_enabled = enable;
			}
		}
        
        /*TODO public void handleSettingsUpdate(settings.type type, QJsonDocument config)
        {
            if (type == settings.type.BLACKBORDER)
            {
                var obj = config.@object();
                _unknownSwitchCnt = obj["unknownFrameCnt"].toInt(600);
                _borderSwitchCnt = obj["borderFrameCnt"].toInt(50);
                _maxInconsistentCnt = obj["maxInconsistentCnt"].toInt(10);
                _blurRemoveCnt = obj["blurRemoveCnt"].toInt(1);
                _detectionMode = obj["mode"].toString("default");
                double newThreshold = obj["threshold"].toDouble(5.0) / 100.0;
    
                if (_oldThreshold != newThreshold)
                {
                    _oldThreshold = newThreshold;
    
                    _detector = null;
    
                    _detector = new BlackBorderDetector(newThreshold);
                }
    
                //Info(Logger.getInstance("BLACKBORDER"), "Set mode to: %s", QSTRING_CSTR(_detectionMode));
    
                // eval the comp state
                handleCompStateChangeRequest(obj["enable"].toBool(true));
            }
        }*/

		///
		/// Updates the current border based on the newly detected border. Returns true if the
		/// current border has changed.
		///
		/// @param newDetectedBorder  The newly detected border
		/// @return True if the current border changed else false
		///
		private bool updateBorder(BlackBorder newDetectedBorder)
		{
			// the new changes ignore false small borders (no reset of consistance)
			// as long as the previous stable state returns within 10 frames
			// and will only switch to a new border if it is realy detected stable >50 frames

			// sometimes the grabber delivers "bad" frames with a smaller black border (looks like random number every few frames and even when freezing the image)
			// maybe some interferences of the power supply or bad signal causing this effect - not exactly sure what causes it but changing the power supply of the converter significantly increased that "random" effect on my system
			// (you can check with the debug output below or if you want i can provide some output logs)
			// this "random effect" caused the old algorithm to switch to that smaller border immediatly, resulting in a too small border being detected
			// makes it look like the border detectionn is not working - since the new 3 line detection algorithm is more precise this became a problem specialy in dark scenes
			// wisc

			//	std::cout << "c: " << setw(2) << _currentBorder.verticalSize << " " << setw(2) << _currentBorder.horizontalSize << " p: " << setw(2) << _previousDetectedBorder.verticalSize << " " << setw(2) << _previousDetectedBorder.horizontalSize << " n: " << setw(2) << newDetectedBorder.verticalSize << " " << setw(2) << newDetectedBorder.horizontalSize << " c:i " << setw(2) << _consistentCnt << ":" << setw(2) << _inconsistentCnt << std::endl;

				// set the consistency counter
			if (newDetectedBorder == _previousDetectedBorder)
			{
				++_consistentCnt;
				_inconsistentCnt = 0;
			}
			else
			{
				++_inconsistentCnt;
				if (_inconsistentCnt <= _maxInconsistentCnt) // only few inconsistent frames
				{
					//discard the newDetectedBorder -> keep the consistent count for previousDetectedBorder
					return false;
				}
				// the inconsistency threshold is reached
				// -> give the newDetectedBorder a chance to proof that its consistent

                // TODO: copy
                _previousDetectedBorder = newDetectedBorder;
				_consistentCnt = 0;
			}

			// check if there is a change
			if (_currentBorder == newDetectedBorder)
			{
				// No change required
				_inconsistentCnt = 0; // we have found a consistent border -> reset _inconsistentCnt
				return false;
			}

			bool borderChanged = false;
			if (newDetectedBorder.unknown)
			{
				// apply the unknown border if we consistently can't determine a border
				if (_consistentCnt == _unknownSwitchCnt)
				{
                    // TODO: copy
                    _currentBorder = newDetectedBorder;
					borderChanged = true;
				}
			}
			else
			{
				// apply the detected border if it has been detected consistently
				if (_currentBorder.unknown || _consistentCnt == _borderSwitchCnt)
				{
                    // TODO: copy
                    _currentBorder = newDetectedBorder;
					borderChanged = true;
				}
			}

			return borderChanged;
		}

		/// flag for black-border detector usage
		private bool _enabled = false;

		/// The number of unknown-borders detected before it becomes the current border
		private uint _unknownSwitchCnt = 100;

		/// The number of horizontal/vertical borders detected before it becomes the current border
		private uint _borderSwitchCnt = 50;

		// The number of frames that are "ignored" before a new border gets set as _previousDetectedBorder
		private uint _maxInconsistentCnt = 10;

		/// The number of pixels to increase a detected border for removing blurry pixels
		private uint _blurRemoveCnt = 1;

		/// The border detection mode
		private string _detectionMode = "default";

		/// The black-border detector
		private BlackBorderDetector _detector;

		/// The current detected border
		private BlackBorder _currentBorder = new(true, -1, -1);

		/// The border detected in the previous frame
		private BlackBorder _previousDetectedBorder = new(true, -1, -1);

		/// The number of frame the previous detected border matched the incoming border
		private uint _consistentCnt = 0;
		/// The number of frame the previous detected border NOT matched the incoming border
		private uint _inconsistentCnt = 10;
		/// old threshold
		private double _oldThreshold = -0.1;
		/// True when disabled in specific situations, this prevents to enable BB when the visible priority requested a disable
		private bool _hardDisabled = false;
		/// Reflect the last component state request from user (comp change)
		private bool _userEnabled = false;

	}