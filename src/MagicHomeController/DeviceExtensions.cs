using System.Drawing;

namespace MagicHomeController
{
	public static class DeviceExtensions
	{
		public static void SetColor(this Device device, Color color, byte? white1 = null, byte? white2 = null)
		{
			device.SetColor(color.R, color.G, color.B, white1, white2);
		}
	}
}