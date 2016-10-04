using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.NetworkInformation;

namespace MagicHomeController.Demo
{
	class Program
	{
		static void Main(string[] args)
		{

			// find single device by mac address
			var macAddr = PhysicalAddress.Parse("AA-11-22-33-44-55");
			Device deviceByMac = Device.GetByMacAddress(macAddr, DeviceType.Rgb);

			// use device by ip address
			var ip = IPAddress.Parse("192.168.0.123");
			Device deviceByIp = new Device(ip, DeviceType.Rgb); 

			// search for devices in the network and use them
			IEnumerable<DeviceFindResult> findResults = DeviceFinder.FindDevices(); // find ip, mac and model of all devices in network
			IEnumerable<Device> allDevices = findResults.Select(result => new Device(result.IpAddress, DeviceType.Rgb)); // make new rgb device instance for every found device

			foreach (var device in allDevices)
			{
				device.TurnOn(); //turn on
				device.SetColor(255, 0, 255); //set color to magenta
			}
		}
	}
}
