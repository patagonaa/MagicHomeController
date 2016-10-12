using System.Collections.Generic;
using System.Net;
using System.Net.NetworkInformation;

namespace MagicHomeController
{
	public class DeviceFindResult
	{
		private readonly IPAddress _ipAddress;
		private readonly PhysicalAddress _macAddress;
		private readonly string _model;

		public DeviceFindResult(IPAddress ipAddress, PhysicalAddress macAddress, string model)
		{
			_ipAddress = ipAddress;
			_macAddress = macAddress;
			_model = model;
		}

		private class MacAddressComparer : IEqualityComparer<DeviceFindResult>
		{
			public bool Equals(DeviceFindResult x, DeviceFindResult y)
			{
				if (x == null || y == null)
					return x == y;

				return x.MacAddress.Equals(y.MacAddress);
			}

			public int GetHashCode(DeviceFindResult obj)
			{
				return obj.MacAddress.GetHashCode();
			}
		}

		internal static IEqualityComparer<DeviceFindResult> MacAddressEqualityComparer
		{
			get { return new MacAddressComparer(); }
		}

		public IPAddress IpAddress { get { return _ipAddress; } }
		public PhysicalAddress MacAddress { get { return _macAddress; } }
		public string Model { get { return _model; } }

		public override string ToString()
		{
			return string.Format("{0},{1},{2}", IpAddress, MacAddress, Model);
		}
	}
}