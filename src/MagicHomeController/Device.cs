using System;
using System.Net;
using System.Net.Sockets;

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
		}

		public DeviceStatus GetDeviceStatus()
		{
			if (_deviceType == DeviceType.RgbWarmwhiteColdWhite)
				throw new NotImplementedException();

			var message = new byte[] {0x81, 0x8A, 0x8B, 0x96};

			var response = SendMessage(message, false);

			if(response.Length != 14)
				throw new Exception("Controller sent wrong number of bytes while getting status");

			var toReturn = new DeviceStatus
			{
				On = response[2] == 0x23,
				Mode = (PresetMode) response[3],
				Speed = response[5],
				Red = response[6],
				Green = response[7],
				Blue = response[8],
				White1 = response[9]
			};
			return toReturn;
		}

		public void TurnOn()
		{
			if (_deviceType == DeviceType.LegacyBulb)
				SendMessage(new byte[] {0xCC, 0x23, 0x33}, false);
			else
				SendMessage(new byte[] {0x71, 0x23, 0x0F, 0xA3}, false);
		}

		public void TurnOff()
		{
			if (_deviceType == DeviceType.LegacyBulb)
				SendMessage(new byte[] {0xCC, 0x24, 0x33}, false);
			else
				SendMessage(new byte[] {0x71, 0x24, 0x0F, 0xA4}, false);
		}

		public void SetColor(byte red, byte green, byte blue, byte? white1 = 0, byte? white2 = 0)
		{
			byte[] message;

			switch (_deviceType)
			{
				case DeviceType.Rgb:
				case DeviceType.RgbWarmwhite:
					message = new byte[] {0x31, red, green, blue, white1 ?? 0, 0x00, 0x0f};
					break;
				case DeviceType.RgbWarmwhiteColdWhite:
					message = new byte[] {0x31, red, green, blue, white1 ?? 0, white2 ?? 0, 0x0f, 0x0f};
					break;
				case DeviceType.Bulb:
					message = white1 != null ? new byte[] {0x31, 0x00, 0x00, 0x00, white1.Value, 0x0f, 0x0f} : new byte[] {0x31, red, green, blue, 0x00, 0xf0, 0x0f};
					break;
				case DeviceType.LegacyBulb:
					message = white1 != null ? new byte[] {0x56, 0x00, 0x00, 0x00, white1.Value, 0x0f, 0xaa, 0x56, 0x00, 0x00, 0x00, white1.Value, 0x0f, 0xaa} : new byte[] {0x56, red, green, blue, 0x00, 0xf0, 0xaa};
					break;
				default:
					throw new ArgumentOutOfRangeException();
			}

			SendMessage(message, true);
		}

		public void SetPreset(PresetMode presetMode, byte speed)
		{
			if (speed > 24)
				speed = 24;
			if (speed < 1)
				speed = 1;

			if (_deviceType == DeviceType.LegacyBulb)
			{
				SendMessage(new byte[] {0xBB, (byte) presetMode, speed, 0x44}, false);
			}
			else
			{
				SendMessage(new byte[] {0x61, (byte) presetMode, speed, 0x0F}, true);
			}
		}

		private byte[] SendMessage(byte[] bytes, bool sendChecksum)
		{
			if (!_socket.Connected)
				_socket.Connect(_endPoint);

			_socket.Send(bytes);
			if (sendChecksum)
				_socket.Send(new[] {CalculateChecksum(bytes)});

			var buffer = new byte[2048];
			var readBytes = _socket.Receive(buffer);

			Array.Resize(ref buffer, readBytes);

			return buffer;
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
		
		public void Dispose()
		{
			if (_socket.Connected)
				_socket.Disconnect(false);
			_socket.Dispose();
		}
	}
}