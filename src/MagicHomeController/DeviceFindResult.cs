using System.Collections.Generic;
using System.Net;
using System.Net.NetworkInformation;

namespace MagicHomeController
{
	public class DeviceFindResult
	{
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

		public IPAddress IpAddress { get; set; }
		public PhysicalAddress MacAddress { get; set; }
		public string Model { get; set; }
	}
}