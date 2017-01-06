namespace MagicHomeController
{
	public enum PowerState : byte
	{
		Pause = 0x20,
		Play = 0x21,
		Toggle = 0x22,
		PowerOn = 0x23,
		PowerOff = 0x24
	}
}
