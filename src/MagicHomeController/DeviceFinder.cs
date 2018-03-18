using System;
using System.Collections;
using System.Collections.Generic;
using System.Net;
using System.Net.NetworkInformation;
using System.Net.Sockets;
using System.Text;

namespace MagicHomeController
{
	public static class DeviceFinder
	{
		private const int BroadcastPort = 48899;

		private class DeviceFindEnumerable : IEnumerable<DeviceFindResult> 
		{
			private readonly EndPoint _endPoint;
			private readonly int _timeoutMs;

			public DeviceFindEnumerable(EndPoint endPoint, int timeoutMs)
			{
				_endPoint = endPoint;
				_timeoutMs = timeoutMs;
			}
			
			private class DeviceFindEnumerator : IEnumerator<DeviceFindResult>
			{
				private readonly EndPoint _endPoint;
				private readonly Socket _socket;
				private readonly DateTime _endTime;
				private static readonly byte[] Message = Encoding.ASCII.GetBytes("HF-A11ASSISTHREAD");
				private readonly HashSet<DeviceFindResult> _foundDevices;
				private bool _tryReceiveNextTime;
				private const int ReceiveTriesTotal = 5;
				private const int SendTriesTotal = 5;

				public DeviceFindEnumerator(EndPoint endPoint, int timeoutMs)
				{
					_endPoint = endPoint;

					_socket = new Socket(endPoint.AddressFamily, SocketType.Dgram, ProtocolType.Udp);
					_socket.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.Broadcast, 1);

					_socket.ReceiveTimeout = timeoutMs / ReceiveTriesTotal / SendTriesTotal;

					_foundDevices = new HashSet<DeviceFindResult>(DeviceFindResult.MacAddressEqualityComparer);

					_endTime = DateTime.UtcNow.AddMilliseconds(timeoutMs);

					_tryReceiveNextTime = false;
				}

				public bool MoveNext()
				{
					if (DateTime.UtcNow > _endTime)
						return false;

					DeviceFindResult result;

					if (_tryReceiveNextTime)
					{
						do
						{
							result = TryReceive();

							if (result != null && _foundDevices.Add(result))
							{
								Current = result;
								return true;
							}
						} while (result != null);
					}

					while (DateTime.UtcNow < _endTime)
					{
						_socket.SendTo(Message, SocketFlags.DontRoute, _endPoint);
						var receiveTries = 0;
						while (DateTime.UtcNow < _endTime && receiveTries < ReceiveTriesTotal)
						{
							result = TryReceive();

							if (result != null && _foundDevices.Add(result))
							{
								Current = result;
								_tryReceiveNextTime = true;
								return true;
							}

							receiveTries++;
						}
					}
					return false;
				}

				private DeviceFindResult TryReceive()
				{
					var buffer = new byte[64];

					var receivedBytes = 0;
					try
					{
						receivedBytes = _socket.Receive(buffer);
					}
					catch (SocketException ex)
					{
						if (ex.SocketErrorCode != SocketError.TimedOut)
							throw;
					}

					if (receivedBytes == 0)
						return null;

					var response = Encoding.ASCII.GetString(buffer, 0, receivedBytes);

					var splitResponse = response.Split(',');

					if (splitResponse.Length != 3)
						return null;

					try
					{
						return new DeviceFindResult(IPAddress.Parse(splitResponse[0]), PhysicalAddress.Parse(splitResponse[1]), splitResponse[2]);
					}
					catch (Exception)
					{
						return null;
					}
				}

				public void Reset()
				{
					throw new NotSupportedException();
				}

				public DeviceFindResult Current { get; private set; }

				object IEnumerator.Current
				{
					get { return Current; }
				}

				public void Dispose()
				{
					_socket.Dispose();
				}
			}

			public IEnumerator<DeviceFindResult> GetEnumerator()
			{
				return new DeviceFindEnumerator(_endPoint, _timeoutMs);
			}

			IEnumerator IEnumerable.GetEnumerator()
			{
				return GetEnumerator();
			}
		}

		public static IEnumerable<DeviceFindResult> FindDevices(EndPoint endPoint = null, int timeout = 5000)
		{
			if (endPoint == null)
				endPoint = new IPEndPoint(IPAddress.Broadcast, BroadcastPort);

			return new DeviceFindEnumerable(endPoint, timeout);
		}
	}
}