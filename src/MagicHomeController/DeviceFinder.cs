using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.NetworkInformation;
using System.Net.Sockets;
using System.Text;

namespace MagicHomeController
{
	public static class DeviceFinder
	{
		private const int BroadcastPort = 48899;

		public static List<DeviceFindResult> FindDevices(IPEndPoint endPoint = null, int timeout = 5)
		{
			if (endPoint == null)
				endPoint = new IPEndPoint(new IPAddress(new byte[] {255, 255, 255, 255}), BroadcastPort);

			var endTime = DateTime.UtcNow.AddSeconds(timeout);

			var socket = new Socket(endPoint.AddressFamily, SocketType.Dgram, ProtocolType.Udp);

			socket.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.Broadcast, 1);
			socket.ReceiveTimeout = 200;

			var resultHashSet = new HashSet<DeviceFindResult>(DeviceFindResult.MacAddressEqualityComparer);
			var message = new byte[] {0x48, 0x46, 0x2d, 0x41, 0x31, 0x31, 0x41, 0x53, 0x53, 0x49, 0x53, 0x54, 0x48, 0x52, 0x45, 0x41, 0x44};
			var buffer = new byte[64];
			while (DateTime.UtcNow < endTime)
			{
				socket.SendTo(message, SocketFlags.DontRoute, endPoint);
				while (DateTime.UtcNow < endTime)
				{
					int numBytes;
					try
					{
						numBytes = socket.Receive(buffer);
					}
					catch (SocketException ex)
					{
						if (ex.SocketErrorCode == SocketError.TimedOut)
							break;
						throw;
					}

					var response = Encoding.ASCII.GetString(buffer, 0, numBytes);

					var splitResponse = response.Split(',');

					if (splitResponse.Length != 3)
						continue;

					resultHashSet.Add(new DeviceFindResult
					{
						IpAddress = IPAddress.Parse(splitResponse[0]),
						MacAddress = PhysicalAddress.Parse(splitResponse[1]),
						Model = splitResponse[2]
					});
				}
			}

			return resultHashSet.ToList();
		}
	}
}