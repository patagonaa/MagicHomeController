using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.NetworkInformation;
using System.Net.Sockets;
using System.Threading;

namespace MagicHomeController
{
	// General Stuff based on: 
	// https://github.com/beville/flux_led
	// https://github.com/vikstrous/zengge-lightcontrol
	// https://github.com/zoot1612/plugin_mh/blob/master/MH_API.txt
	// some own reverse engineering via tcpdump

	// Legacy Bulb Stuff based on: 
	// https://docs.google.com/document/d/1IJ2l5GvphzQFpe22I6L5qjZG0DXvY12nHl6hHSQ7I2s/pub
	
	public class Device : IDisposable
	{
		private readonly EndPoint _endPoint;
		private readonly DeviceType _deviceType;
		private Socket _socket;
		private const int DefaultPort = 5577;

		public Device(IPAddress ip, DeviceType deviceType)
			: this(new IPEndPoint(ip, DefaultPort), deviceType)
		{
		}

		public Device(EndPoint endPoint, DeviceType deviceType)
		{
			_endPoint = endPoint;
			_deviceType = deviceType;
        }

		public DeviceStatus GetStatus()
		{
			if (_deviceType == DeviceType.LegacyBulb)
			{
				var message = new byte[] { 0xEF, 0x01, 0x77 };

				var response = SendMessage(message, false, true);

				if (response.Length != 12)
					throw new Exception("Controller sent wrong number of bytes while getting status");


				return new DeviceStatus
				{
					PowerState = (PowerState) response[2],
					Mode = response[3] == 0x41 ? PresetMode.NormalRgb : (PresetMode) response[3],
					PresetPaused = response[4] == 0x20,
					PresetDelay = response[5],
					Red = response[6],
					Green = response[7],
					Blue = response[8],
					White1 = response[9],
					VersionNumber = response[10]
				};
			}
			else
			{
				var message = new byte[] { 0x81, 0x8A, 0x8B, 0x96 };

				var response = SendMessage(message, false, true);

				if (response.Length != 14)
					throw new Exception("Controller sent wrong number of bytes while getting status");

				return new DeviceStatus
				{
					PowerState = (PowerState) response[2],
					Mode = (PresetMode) response[3],
					PresetPaused = false,
					PresetDelay = response[5],
					Red = response[6],
					Green = response[7],
					Blue = response[8],
					White1 = response[9],
					VersionNumber = response[10],
					White2 = response[11] //TODO: check if this is correct
				};
			}
		}

		public void SetPowerState(PowerState state)
		{
			if (_deviceType == DeviceType.LegacyBulb)
				SendMessage(new byte[] { 0xCC, (byte) state, 0x33 }, false, true);
			else
				SendMessage(new byte[] { 0x71, (byte) state, 0x0F }, true, true);
		}

		public void TurnOn()
		{
			SetPowerState(PowerState.PowerOn);
		}

		public void TurnOff()
		{
			SetPowerState(PowerState.PowerOff);
		}

		public void SetColor(byte? red = null, byte? green = null, byte? blue = null, byte? white1 = null, byte? white2 = null, bool waitForResponse = true, bool persist = true)
		{
			byte[] message;

			bool sendChecksum;

			bool rgbSet = red.HasValue || green.HasValue || blue.HasValue;
			bool white1Set = white1.HasValue;
			bool white2Set = white2.HasValue;

			if (rgbSet && (!red.HasValue || !green.HasValue || !blue.HasValue))
				throw new InvalidOperationException("All color values (rgb) must be set");

			if(_deviceType == DeviceType.Rgb && white1Set)
				throw new InvalidOperationException("device type Rgb doesn't have white1");

			if(_deviceType != DeviceType.RgbWarmwhiteColdwhite && white2Set)
				throw new InvalidOperationException("only device type RgbWarmwhiteColdwhite has white2");

			if((_deviceType == DeviceType.Bulb || _deviceType == DeviceType.LegacyBulb || _deviceType == DeviceType.RgbWarmwhiteColdwhite) && rgbSet && white1Set)
				throw new InvalidOperationException("only rgb or white can be set at once if using bulbs or RGBWWCW");

			switch (_deviceType)
			{
				case DeviceType.Rgb:
				case DeviceType.RgbWarmwhite:
				case DeviceType.Bulb:
					message = new byte[] {(byte) (persist ? 0x31 : 0x41), red ?? 0, green ?? 0, blue ?? 0, white1 ?? 0, 0x0f, 0x0f};
					sendChecksum = true;
					break;
				case DeviceType.RgbWarmwhiteColdwhite:
					message = new byte[] {(byte) (persist ? 0x31 : 0x41), red ?? 0, green ?? 0, blue ?? 0, white1 ?? 0, white2 ?? 0, (byte) (rgbSet ? 0xf0 : 0x0f), 0x0f};
					sendChecksum = true;
					break;
				case DeviceType.LegacyBulb:
					sendChecksum = false;
					message = new byte[] {(byte) (persist ? 0x56 : 0x77), red ?? 0, green ?? 0, blue ?? 0, white1 ?? 0, (byte) (rgbSet ? 0xf0 : 0x0f), 0xaa};
					break;
				default:
					throw new ArgumentOutOfRangeException();
			}

			SendMessage(message, sendChecksum, waitForResponse);
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
			if (_socket == null || !_socket.Connected)
				Reconnect();

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
					Thread.Sleep(10);
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

		public void Reconnect()
		{
			if (_socket != null)
				_socket.Dispose();

			_socket = new Socket(_endPoint.AddressFamily, SocketType.Stream, ProtocolType.Tcp);
            _socket.ReceiveTimeout = 100;
            _socket.SendTimeout = 100;
			_socket.Connect(_endPoint);
		}

		public void Dispose()
		{
			if (_socket.Connected)
				_socket.Disconnect(false);
			_socket.Dispose();
		}
	}
}