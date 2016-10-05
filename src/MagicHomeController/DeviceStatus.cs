namespace MagicHomeController
{
	public class DeviceStatus
	{
		private readonly bool _on;
		private readonly PresetMode _mode;
		private readonly byte _speed;
		private readonly byte _red;
		private readonly byte _green;
		private readonly byte _blue;
		private readonly byte? _white1;
		private readonly byte? _white2;

		public DeviceStatus(bool on, PresetMode mode, byte speed, byte red, byte green, byte blue, byte? white1 = null, byte? white2 = null)
		{
			_on = on;
			_mode = mode;
			_speed = speed;
			_red = red;
			_green = green;
			_blue = blue;
			_white1 = white1;
			_white2 = white2;
		}

		public bool On { get { return _on; } }
		public PresetMode Mode { get { return _mode; } }
		public byte Speed { get { return _speed; } }
		public byte Red { get { return _red; } }
		public byte Green { get { return _green; } }
		public byte Blue { get { return _blue; } }
		public byte? White1 { get { return _white1; } }
		public byte? White2 { get { return _white2; } }
	}
}