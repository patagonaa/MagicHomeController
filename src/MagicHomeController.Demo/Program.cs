using System.Linq;

namespace MagicHomeController.Demo
{
	class Program
	{
		static void Main(string[] args)
		{
			var allControllers = DeviceFinder.FindDevices().Select(x => new Device(x.IpAddress, DeviceType.RgbWarmwhite));

			foreach (var controller in allControllers)
			{
				controller.TurnOn();
				controller.SetColor(255, 0, 255);
			}
		}
	}
}
