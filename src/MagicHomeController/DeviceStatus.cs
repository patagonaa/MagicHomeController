namespace MagicHomeController
{
	public class DeviceStatus
	{
		public DeviceStatus()
		{
			Mode = PresetMode.NormalRgb;
			PowerState = PowerState.PowerOff;
		}

		public PowerState PowerState { get; set; }
		public PresetMode Mode { get; set; }
		public bool PresetPaused { get; set; }
		public byte PresetDelay { get; set; }
		public byte Red { get; set; }
		public byte Green { get; set; }
		public byte Blue { get; set; }
		public byte? White1 { get; set; }
		public byte? White2 { get; set; }
		public byte VersionNumber { get; set; }
	}
}