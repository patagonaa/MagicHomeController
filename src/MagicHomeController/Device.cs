using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.NetworkInformation;
using System.Net.Sockets;
using System.Threading;

namespace MagicHomeController
{
	public class Device : IDisposable
	{
		private readonly EndPoint _endPoint;
		private readonly DeviceType _deviceType;
		private readonly Socket _socket;
		private const int DefaultPort = 5577;

		public Device(IPAddress ip, DeviceType deviceType)
			: this(new IPEndPoint(ip, DefaultPort), deviceType)
		{
		}

		public Device(EndPoint endPoint, DeviceType deviceType)
		{
			_endPoint = endPoint;
			_deviceType = deviceType;
			_socket = new Socket(endPoint.AddressFamily, SocketType.Stream, ProtocolType.Tcp);
            _socket.ReceiveTimeout = 100;
            _socket.SendTimeout = 100;
        }

		public DeviceStatus GetStatus()
		{
			if (_deviceType == DeviceType.RgbWarmwhiteColdWhite)
				throw new NotImplementedException();

			var message = new byte[] {0x81, 0x8A, 0x8B, 0x96};

			var response = SendMessage(message, false, true);

			if(response.Length != 14)
				throw new Exception("Controller sent wrong number of bytes while getting status");

			return new DeviceStatus(response[2] == 0x23,
				(PresetMode) response[3],
				response[5],
				response[6],
				response[7],
				response[8],
				response[9]);
		}

		public void TurnOn()
		{
			if (_deviceType == DeviceType.LegacyBulb)
				SendMessage(new byte[] {0xCC, 0x23, 0x33}, false, true);
			else
				SendMessage(new byte[] {0x71, 0x23, 0x0F}, true, true);
		}

		public void TurnOff()
		{
			if (_deviceType == DeviceType.LegacyBulb)
				SendMessage(new byte[] {0xCC, 0x24, 0x33}, false, true);
			else
				SendMessage(new byte[] {0x71, 0x24, 0x0F}, true, true);
		}

		public void SetColor(byte red, byte green, byte blue, byte? white1 = null, byte? white2 = null, bool waitForResponse = true)
		{
			byte[] message;

			switch (_deviceType)
			{
				case DeviceType.Rgb:
				case DeviceType.RgbWarmwhite:
					message = new byte[] { 0x31, red, green, blue, white1 ?? 0, 0x0f, 0x0f };
					break;
				case DeviceType.RgbWarmwhiteColdWhite:
					message = new byte[] { 0x31, red, green, blue, white1 ?? 0, white2 ?? 0, 0x0f, 0x0f };
					break;
				case DeviceType.Bulb:
					message = white1 != null ? new byte[] { 0x31, 0x00, 0x00, 0x00, white1.Value, 0x0f, 0x0f } : new byte[] { 0x31, red, green, blue, 0x00, 0xf0, 0x0f };
					break;
				case DeviceType.LegacyBulb:
					message = white1 != null ? new byte[] { 0x56, 0x00, 0x00, 0x00, white1.Value, 0x0f, 0xaa, 0x56, 0x00, 0x00, 0x00, white1.Value, 0x0f, 0xaa } : new byte[] { 0x56, red, green, blue, 0x00, 0xf0, 0xaa };
					break;
				default:
					throw new ArgumentOutOfRangeException();
			}

			SendMessage(message, true, waitForResponse);
		}

		public void SetPreset(PresetMode presetMode, byte delay)
		{
			if (delay > 24)
				delay = 24;
			if (delay < 1)
				delay = 1;

			if (_deviceType == DeviceType.LegacyBulb)
			{
				SendMessage(new byte[] { 0xBB, (byte)presetMode, delay, 0x44 }, false, true);
			}
			else
			{
				SendMessage(new byte[] { 0x61, (byte)presetMode, delay, 0x0F }, true, true);
			}
		}

		public DateTime GetTime()
		{
			var msg = new byte[] { 0x11, 0x1a, 0x1b, 0x0f };

			var response = SendMessage(msg, true, true);

			if(response.Length != 12)
				throw new Exception("Controller sent wrong number of bytes while getting time");

			return new DateTime(response[3] + 2000, response[4], response[5], response[6], response[7], response[8]);
		}

		public void SetTime(DateTime time)
		{
			byte[] msg;

			checked
			{
				msg = new byte[]
				{
					0x10,
					0x14,
					(byte)(time.Year - 2000),
					(byte)time.Month,
					(byte)time.Day,
					(byte)time.Hour,
					(byte)time.Minute,
					(byte)time.Second,
					(byte)(time.DayOfWeek == 0 ? 7 : (int)time.DayOfWeek),
					0x00,
					0x0F
				};
			}

			SendMessage(msg, true, false);
		}

		public IEnumerable<Timer> GetTimers()
		{
			var msg = new byte[] { 0x22, 0x2a, 0x2b, 0x0f };

			var response = SendMessage(msg, true, true);
			if (response.Length != 88)
				throw new Exception("Controller sent wrong number of bytes while getting timers");

			for (int i = 0; i < 6; i++)
			{
				var timerBytes = new byte[14];
				Array.Copy(response, 2 + (i * 14), timerBytes, 0, 14);

				var timer = Timer.FromBytes(timerBytes);

				if (timer != null)
					yield return timer;
			}
		}

		public void SetTimers(IEnumerable<Timer> timers)
		{
			var timersToSend = new Timer[6];

			var enumerator = timers.GetEnumerator();

			for (int i = 0; i < 6; i++)
			{
				if (enumerator.MoveNext())
					timersToSend[i] = enumerator.Current;
				else
					timersToSend[i] = new Timer();
			}

			if (enumerator.MoveNext())
				throw new Exception("only 6 timers can be set");

			enumerator.Dispose();

			var msg = new byte[87];
			msg[0] = 0x21;

			for (int i = 0; i < 6; i++)
			{
				var bytes = timersToSend[i].ToBytes();
				Array.Copy(bytes, 0, msg, 1 + (i * 14), 14);
			}

			msg[85] = 0x00;
			msg[86] = 0xF0;

			SendMessage(msg, true, false);
		}

		private byte[] SendMessage(byte[] bytes, bool sendChecksum, bool waitForResponse)
		{
			if (!_socket.Connected)
				_socket.Connect(_endPoint);

			if (sendChecksum)
			{
				var checksum = CalculateChecksum(bytes);
				Array.Resize(ref bytes, bytes.Length + 1);
				bytes[bytes.Length - 1] = checksum;
			}

			var buffer = new byte[256];

			if (waitForResponse)
			{
				_socket.Blocking = false;

				try
				{
					while (_socket.Receive(buffer) > 0)
					{
					}
				}
				catch (SocketException ex)
				{
					if (ex.SocketErrorCode != SocketError.WouldBlock)
						throw;
				}

				_socket.Blocking = true;
			}

			const int maxSendRetries = 10;
			var retries = 0;

			while(true)
			{
				_socket.Send(bytes);

				if (!waitForResponse)
					return null;

				try
				{
					int readBytes = _socket.Receive(buffer);

					Array.Resize(ref buffer, readBytes);

					return buffer;
				}
				catch (SocketException ex)
				{
					if(ex.SocketErrorCode != SocketError.TimedOut || retries >= maxSendRetries)
						throw;
					retries++;
				}
			}
		}

		private byte CalculateChecksum(byte[] bytes)
		{
			byte checksum = 0;

			foreach (var b in bytes)
			{
				unchecked
				{
					checksum += b;
				}
			}

			return checksum;
		}

		public static Device GetByMacAddress(PhysicalAddress mac, DeviceType deviceType)
		{
			var deviceFindResult = DeviceFinder.FindDevices().FirstOrDefault(x => x.MacAddress.Equals(mac));
			if (deviceFindResult == null)
				return null;

			return new Device(deviceFindResult.IpAddress, deviceType);
		}

		public override string ToString()
		{
			return string.Format("{0} Device on {1}", _deviceType, _endPoint);
		}

		public void Dispose()
		{
			if (_socket.Connected)
				_socket.Disconnect(false);
			_socket.Dispose();
		}
	}
}