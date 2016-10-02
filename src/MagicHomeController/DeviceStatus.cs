namespace MagicHomeController
{
	public class DeviceStatus
	{
		public bool On { get; set; }
		public PresetMode Mode { get; set; }
		public byte Speed { get; set; }
		public byte Red { get; set; }
		public byte Green { get; set; }
		public byte Blue { get; set; }
		public byte? White1 { get; set; }
		public byte? White2 { get; set; }
	}
}