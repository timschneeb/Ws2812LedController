using System.Runtime.InteropServices;
using SoundIOSharp;

namespace Ws2812LedController.AudioReactive
{
	internal class SoundInputStream
	{
		private SoundIOInStream? _stream;
		private Task? _loop;
		private CancellationTokenSource _cancelSource = new();
		
		public string? BackendName { get; }
		public string InputName { get; }
		public double Latency { get; }
		public event EventHandler<double[][]>? NewSamplesReceived;

		public SoundInputStream(string inputName, double latency = 0.02, string? backendName = null)
		{
			BackendName = backendName;
			InputName = inputName;
			Latency = latency;
		}

		public void Start()
		{
			_cancelSource.Cancel();
			_cancelSource = new CancellationTokenSource();
			_loop = Task.Run(SoundIoLoop);
		}
		
		public async Task StopAsync()
		{
			_cancelSource.Cancel();
			await (_loop?.WaitAsync(CancellationToken.None) ?? Task.CompletedTask);
		}
		
		private void SoundIoLoop()
		{
			const bool isRaw = false;

			var api = new SoundIO ();

			var backend = BackendName == null ? SoundIOBackend.None : (SoundIOBackend)Enum.Parse (typeof (SoundIOBackend), BackendName);
			if (backend == SoundIOBackend.None)
				api.Connect ();
			else
				api.ConnectBackend (backend);
			Console.WriteLine ("SoundInputStream: Using backend: " + api.CurrentBackend);

			api.FlushEvents ();

			var in_device = Enumerable.Range (0, api.InputDeviceCount)
				.Select (i => api.GetInputDevice (i))
				.FirstOrDefault (d => d.Id == InputName && d.IsRaw == isRaw);
			if (in_device == null) {
				Console.Error.WriteLine ("SoundInputStream: Input device " + InputName + " not found.");
				return;
			}
			Console.WriteLine ("SoundInputStream: Using input device: " + in_device.Name);
			if (in_device.ProbeError != 0) {
				Console.Error.WriteLine ("SoundInputStream: Cannot probe input device " + InputName + ".");
				return;
			}
			
			var sampleRate = _prioritizedSampleRates.FirstOrDefault (sr => in_device.SupportsSampleRate (sr));

			if (sampleRate == default)
				throw new InvalidOperationException ("incompatible sample rates"); // panic()
			var fmt = _prioritizedFormats.FirstOrDefault (f => in_device.SupportsFormat (f));

			if (fmt == default)
				throw new InvalidOperationException ("incompatible sample formats"); // panic()

			_stream = in_device.CreateInStream ();
			_stream.Format = fmt;
			_stream.SampleRate = sampleRate;
			_stream.Layout = SoundIOChannelLayout.GetDefault(1);
			_stream.SoftwareLatency = Latency;
			_stream.ReadCallback = (fmin, fmax) => OnInputRead (_stream, fmin, fmax);

			_stream.Open ();
			_stream.Start ();

			while (!_cancelSource?.Token.IsCancellationRequested ?? false)
			{
				api.WaitEvents ();
			}

			_stream.Dispose ();
			in_device.RemoveReference ();
			api.Dispose ();
		}
		
		private void OnInputRead (SoundIOInStream stream, int frame_count_min, int frame_count_max)
		{
			var buffer = new double[stream.Layout.ChannelCount][];

			var framesLeft = frame_count_max;
			for (var i = 0; i < stream.Layout.ChannelCount; i++)
			{
				buffer[i] = new double[frame_count_max];
			}
			for (; ; ) {
				var frameCount = framesLeft;

				var areas = stream.BeginRead (ref frameCount);

				if (frameCount == 0)
					break;

				if (areas.IsEmpty) {
					// Due to an overflow there is a hole. Fill the ring buffer with
					// silence for the size of the hole.
					Console.Error.WriteLine ("Dropped {0} frames due to internal overflow", frameCount);
				} else {

					
					var chCount = stream.Layout.ChannelCount;
					var copySize = stream.BytesPerSample;
					for (var frame = 0; frame < frameCount; frame += 1)
					{
						for (var ch = 0; ch < chCount; ch += 1) {
							var area = areas.GetArea (ch);

							Marshal.Copy(area.Pointer, _tempBuffer, 0, 1);
							buffer[ch][frame] = _tempBuffer[0]; // quick float to double conversion
							area.Pointer += area.Step;
						}
					}
					
					
				}

				stream.EndRead ();

				framesLeft -= frameCount;
				if (framesLeft <= 0)
				{
					NewSamplesReceived?.Invoke(this, buffer);
					break;
				}
			}
		}
		
		private readonly SoundIOFormat [] _prioritizedFormats = {
			SoundIODevice.Float32NE,
			SoundIODevice.Float32FE,
			/*SoundIODevice.S32NE,
			SoundIODevice.S32FE,
			SoundIODevice.S24NE,
			SoundIODevice.S24FE,
			SoundIODevice.S16NE,
			SoundIODevice.S16FE,
			SoundIODevice.Float64NE,
			SoundIODevice.Float64FE,
			SoundIODevice.U32NE,
			SoundIODevice.U32FE,
			SoundIODevice.U24NE,
			SoundIODevice.U24FE,
			SoundIODevice.U16NE,
			SoundIODevice.U16FE,
			SoundIOFormat.S8,
			SoundIOFormat.U8,*/
			SoundIOFormat.Invalid,
		};

		private readonly int [] _prioritizedSampleRates = {
			48000,
			44100,
			96000,
			24000,
			0,
		};

		private readonly float[] _tempBuffer = new float[1];
	}
}