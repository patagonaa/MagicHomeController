using System;

namespace MagicHomeController
{
	public class Timer
	{
		public bool Active { get; set; }
		public TimerDays RepeatDays { get; set; }
		public DateTime? Date { get; set; }
		public PresetMode Mode { get; set; }
		public byte PresetDelay { get; set; }
		public byte Red { get; set; }
		public byte Green { get; set; }
		public byte Blue { get; set; }
		public byte White1 { get; set; }
		public bool On { get; set; }

		internal Timer()
		{
		}

		internal Timer(byte[] timerBytes)
		{
			Active = timerBytes[0] == 0xF0;

			RepeatDays = (TimerDays)timerBytes[7];

			if (RepeatDays == TimerDays.None)
			{
				try
				{
					Date = new DateTime(timerBytes[1] + 2000, timerBytes[2], timerBytes[3], timerBytes[4], timerBytes[5], timerBytes[6]);
				}
				catch (ArgumentOutOfRangeException)
				{
					Date = null;
				}
			}

			Mode = (PresetMode)timerBytes[8];

			if (Mode == PresetMode.NormalRgb)
				Red = timerBytes[9];
			else
				PresetDelay = timerBytes[9];

			Green = timerBytes[10];
			Blue = timerBytes[11];
			White1 = timerBytes[12];
			On = timerBytes[13] == 0xF0;
		}

		internal byte[] ToBytes()
		{
			var toReturn = new byte[14];

			toReturn[0] = (byte)(Active ? 0xF0 : 0x0F);

			if (!Active)
				return toReturn;

			if (RepeatDays == TimerDays.None)
			{
				if (Date == null)
					throw new Exception("non-repeated timer must have date set");

				toReturn[1] = (byte)Date.Value.Year;
				toReturn[2] = (byte)Date.Value.Month;
				toReturn[3] = (byte)Date.Value.Day;
				toReturn[4] = (byte)Date.Value.Hour;
				toReturn[5] = (byte)Date.Value.Minute;
				toReturn[6] = (byte)Date.Value.Second;
			}

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
	}
}