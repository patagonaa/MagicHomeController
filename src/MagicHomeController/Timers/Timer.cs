using System;
using System.Text;

namespace MagicHomeController.Timers
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
					sb.AppendFormat(", R={0}, G={1}, B={2}, WW={3}, CW={4}", deviceStatus.Red, deviceStatus.Green, deviceStatus.Blue, deviceStatus.White1, deviceStatus.White2);
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