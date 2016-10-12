using System;
using System.Text;

namespace MagicHomeController
{
	public class Timer
	{
		public bool Active { get; set; }
		public TimerDays RepeatDays { get; set; }
		public DateTime Date { get; set; }
		public PresetMode Mode { get; set; }
		public byte PresetDelay { get; set; }
		public byte Red { get; set; }
		public byte Green { get; set; }
		public byte Blue { get; set; }
		public byte White1 { get; set; }
		public bool On { get; set; }

		public Timer()
		{
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

			toReturn.Mode = (PresetMode)timerBytes[8];

			if (toReturn.Mode == PresetMode.NormalRgb)
				toReturn.Red = timerBytes[9];
			else
				toReturn.PresetDelay = timerBytes[9];

			toReturn.Green = timerBytes[10];
			toReturn.Blue = timerBytes[11];
			toReturn.White1 = timerBytes[12];
			toReturn.On = timerBytes[13] == 0xF0;

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

			if(!On)
			{
				toReturn[13] = 0x0F;
				return toReturn;
			}

			toReturn[8] = (byte)Mode;

			if(Mode == PresetMode.NormalRgb)
			{
				toReturn[9] = Red;
				toReturn[10] = Green;
				toReturn[11] = Blue;
				toReturn[12] = White1;
			}
			else
			{
				toReturn[9] = PresetDelay;
			}

			toReturn[13] = 0xF0;

			return toReturn;
		}

		public override string ToString()
		{
			var sb = new StringBuilder();

			sb.Append(Active ? "active: " : "inactive: ");
			if(RepeatDays == TimerDays.None)
			{
				sb.AppendFormat("on {0} ", Date.ToString("s"));
			}
			else
			{
				sb.AppendFormat("every {0} at {1} ", RepeatDays.ToString("G"), Date.ToString("HH:mm"));
			}

			if (On)
			{
				sb.AppendFormat("set mode {0}", Mode.ToString());
				if(Mode == PresetMode.NormalRgb)
				{
					sb.AppendFormat(", R={0}, G={1}, B={2} W={3}", Red, Green, Blue, White1);
				}
				else
				{
					sb.AppendFormat(" delay {0}", PresetDelay);
				}
			}
			else
			{
				sb.AppendFormat("turn off", Mode.ToString());
			}

			return sb.ToString();
		}
	}
}