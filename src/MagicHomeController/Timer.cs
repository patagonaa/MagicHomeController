using System;
using System.Text;

namespace MagicHomeController
{
	public class Timer
	{
		private DeviceStatus _status;
		public bool Active { get; set; }
		public TimerDays RepeatDays { get; set; }
		public DateTime Date { get; set; }

		public DeviceStatus Status
		{
			get
			{
				if (_status == null)
					return _status = new DeviceStatus();

				return _status;
			}
			set { _status = value; }
		}

		internal static Timer FromBytes(byte[] timerBytes)
		{
			var active = timerBytes[0] == 0xF0;

			if (!active && timerBytes[1] == 0 && timerBytes[2] == 0 && timerBytes[3] == 0 && timerBytes[4] == 0 && timerBytes[5] == 0 && timerBytes[6] == 0)
			{
				return null;
			}

			var toReturn = new Timer();

			toReturn.Active = active;

			toReturn.RepeatDays = (TimerDays)timerBytes[7];

			if (toReturn.RepeatDays == TimerDays.None)
			{
				toReturn.Date = new DateTime(timerBytes[1] + 2000, timerBytes[2], timerBytes[3], timerBytes[4], timerBytes[5], timerBytes[6]);
				if (toReturn.Date < DateTime.Now)
					return null;
			}
			else
			{
				toReturn.Date = new DateTime(1, 1, 1, timerBytes[4], timerBytes[5], timerBytes[6]);
			}

			var deviceStatus = new DeviceStatus();

			if (timerBytes[13] == 0xF0)
			{
				deviceStatus.PowerState = PowerState.PowerOn;

				deviceStatus.Mode = (PresetMode)timerBytes[8];

				if (deviceStatus.Mode == PresetMode.NormalRgb)
				{
					deviceStatus.Red = timerBytes[9];
					deviceStatus.Green = timerBytes[10];
					deviceStatus.Blue = timerBytes[11];
					deviceStatus.White1 = timerBytes[12];
				}
				else
				{
					deviceStatus.PresetDelay = timerBytes[9];
				}
			}
			else
			{
				deviceStatus.PowerState = PowerState.PowerOff;
			}

			toReturn.Status = deviceStatus;

			return toReturn;
		}

		internal byte[] ToBytes()
		{
			var toReturn = new byte[14];

			toReturn[0] = (byte)(Active ? 0xF0 : 0x0F);

			if (!Active)
				return toReturn;

			if (RepeatDays == TimerDays.None)
			{
				toReturn[1] = (byte)(Date.Year - 2000);
				toReturn[2] = (byte)Date.Month;
				toReturn[3] = (byte)Date.Day;
			}

			toReturn[4] = (byte)Date.Hour;
			toReturn[5] = (byte)Date.Minute;
			toReturn[6] = (byte)Date.Second;

			toReturn[7] = (byte)RepeatDays;

			DeviceStatus deviceStatus = Status;
			switch (deviceStatus.PowerState)
			{
				case PowerState.PowerOff:
					toReturn[13] = 0x0F;
					return toReturn;
				case PowerState.PowerOn:
					toReturn[13] = 0xF0;
					break;
				default:
					throw new NotSupportedException(string.Format("PowerState {0} cannot be used with timers", deviceStatus.PowerState));
			}

			toReturn[8] = (byte)deviceStatus.Mode;

			if (deviceStatus.Mode == PresetMode.NormalRgb)
			{
				toReturn[9] = deviceStatus.Red;
				toReturn[10] = deviceStatus.Green;
				toReturn[11] = deviceStatus.Blue;
				toReturn[12] = deviceStatus.White1 ?? 0;
				if(deviceStatus.White2 != null)
					throw new NotSupportedException("RGBWWCW currently not supported for timers"); // TODO
			}
			else
			{
				toReturn[9] = deviceStatus.PresetDelay;
			}

			return toReturn;
		}

		public override string ToString()
		{
			var sb = new StringBuilder();

			sb.Append(Active ? "active: " : "inactive: ");
			if (RepeatDays == TimerDays.None)
			{
				sb.AppendFormat("on {0} ", Date.ToString("s"));
			}
			else
			{
				sb.AppendFormat("every {0} at {1} ", RepeatDays.ToString("G"), Date.ToString("HH:mm"));
			}

			DeviceStatus deviceStatus = Status;
			if (deviceStatus.PowerState == PowerState.PowerOn)
			{
				sb.AppendFormat("set mode {0}", deviceStatus.Mode);
				if (deviceStatus.Mode == PresetMode.NormalRgb)
				{
					sb.AppendFormat(", R={0}, G={1}, B={2} W={3}", deviceStatus.Red, deviceStatus.Green, deviceStatus.Blue, deviceStatus.White1);
				}
				else
				{
					sb.AppendFormat(" delay {0}", deviceStatus.PresetDelay);
				}
			}
			else
			{
				sb.Append("turn off");
			}

			return sb.ToString();
		}
	}
}