namespace MagicHomeController
{
	public class DeviceStatus
	{
		public DeviceStatus()
		{
			Mode = PresetMode.NormalRgb;
		}

		internal DeviceStatus(bool on, PresetMode mode, bool presetPaused, byte presetDelay, byte red, byte green, byte blue, byte white1, byte? white2)
		{
			On = on;
			Mode = mode;
			PresetPaused = presetPaused;
			PresetDelay = presetDelay;
			Red = red;
			Green = green;
			Blue = blue;
			White1 = white1;
			White2 = white2;
		}

		public bool On { get; set; }
		public PresetMode Mode { get; set; }
		public bool PresetPaused { get; set; }
		public byte PresetDelay { get; set; }
		public byte Red { get; set; }
		public byte Green { get; set; }
		public byte Blue { get; set; }
		public byte? White1 { get; set; }
		public byte? White2 { get; set; }
	}
}