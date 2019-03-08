using System;
using System.Collections.Generic;
using System.Linq;

namespace MagicHomeController.Timers
{
    public static class TimerSerializer
    {
        public static IEnumerable<Timer> Deserialize(byte[] bytes, DeviceType deviceType)
        {
            if (bytes.Length == 88)
            {
                // RGB(WW) controller
                var timers = new List<Timer>();
                for (int i = 0; i < 6; i++)
                {
                    var timerBytes = new byte[14];
                    Array.Copy(bytes, 2 + (i * 14), timerBytes, 0, 14);

                    var timer = RgbWwFromBytes(timerBytes);

                    if (timer != null)
                        timers.Add(timer);
                }

                return timers;
            }
            else if (bytes.Length == 94)
            {
                //RGB(WWCW) controller
                var timers = new List<Timer>();
                for (int i = 0; i < 6; i++)
                {
                    var timerBytes = new byte[15];
                    Array.Copy(bytes, 2 + (i * 15), timerBytes, 0, 15);

                    var timer = RgbWwCwFromBytes(timerBytes);

                    if (timer != null)
                        timers.Add(timer);
                }

                return timers;
            }
            else
            {
                throw new Exception("Controller sent wrong number of bytes while getting timers");
            }
        }

        private static Timer RgbWwFromBytes(byte[] timerBytes)
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

        private static Timer RgbWwCwFromBytes(byte[] timerBytes)
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

            if (timerBytes[14] == 0xF0)
            {
                deviceStatus.PowerState = PowerState.PowerOn;

                deviceStatus.Mode = (PresetMode)timerBytes[8];

                if (deviceStatus.Mode == PresetMode.NormalRgb)
                {
                    deviceStatus.Red = timerBytes[9];
                    deviceStatus.Green = timerBytes[10];
                    deviceStatus.Blue = timerBytes[11];
                    deviceStatus.White1 = timerBytes[12];
                    deviceStatus.White2 = timerBytes[13];
                }
                else if (deviceStatus.Mode == PresetMode.None)
                {

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

        public static byte[] Serialize(IEnumerable<Timer> timers, DeviceType deviceType)
        {
            var timersToSend = timers.ToList();

            if (timersToSend.Count > 6)
            {
                throw new Exception("only 6 timers can be set");
            }

            while (timersToSend.Count < 6)
            {
                timersToSend.Add(new Timer());
            }

            if (deviceType == DeviceType.Rgb || deviceType == DeviceType.RgbWarmwhite || deviceType == DeviceType.Bulb)
            {
                var msg = new byte[87];
                msg[0] = 0x21;

                for (int i = 0; i < 6; i++)
                {
                    var bytes = RgbWwToBytes(timersToSend[i]);
                    Array.Copy(bytes, 0, msg, 1 + (i * 14), 14);
                }

                msg[85] = 0x00;
                msg[86] = 0xF0;

                return msg;
            }
            else if (deviceType == DeviceType.RgbWarmwhiteColdwhite)
            {
                var msg = new byte[93];
                msg[0] = 0x21;

                for (int i = 0; i < 6; i++)
                {
                    var bytes = RgbWwCwToBytes(timersToSend[i]);
                    Array.Copy(bytes, 0, msg, 1 + (i * 15), 15);
                }

                msg[91] = 0x00;
                msg[92] = 0xF0;

                return msg;
            }
            else
            {
                throw new Exception($"DeviceType {deviceType} not supported");
            }
        }

        private static byte[] RgbWwToBytes(Timer timer)
        {
            var toReturn = new byte[14];

            toReturn[0] = (byte)(timer.Active ? 0xF0 : 0x0F);

            if (!timer.Active)
                return toReturn;

            if (timer.RepeatDays == TimerDays.None)
            {
                toReturn[1] = (byte)(timer.Date.Year - 2000);
                toReturn[2] = (byte)timer.Date.Month;
                toReturn[3] = (byte)timer.Date.Day;
            }

            toReturn[4] = (byte)timer.Date.Hour;
            toReturn[5] = (byte)timer.Date.Minute;
            toReturn[6] = (byte)timer.Date.Second;

            toReturn[7] = (byte)timer.RepeatDays;

            DeviceStatus deviceStatus = timer.Status;
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
                if (deviceStatus.White2 != null)
                    throw new NotSupportedException("white2 not supported for rgbww devices");
            }
            else
            {
                toReturn[9] = deviceStatus.PresetDelay;
            }

            return toReturn;
        }

        private static byte[] RgbWwCwToBytes(Timer timer)
        {
            var toReturn = new byte[15];

            toReturn[0] = (byte)(timer.Active ? 0xF0 : 0x0F);

            if (!timer.Active)
                return toReturn;

            if (timer.RepeatDays == TimerDays.None)
            {
                toReturn[1] = (byte)(timer.Date.Year - 2000);
                toReturn[2] = (byte)timer.Date.Month;
                toReturn[3] = (byte)timer.Date.Day;
            }

            toReturn[4] = (byte)timer.Date.Hour;
            toReturn[5] = (byte)timer.Date.Minute;
            toReturn[6] = (byte)timer.Date.Second;

            toReturn[7] = (byte)timer.RepeatDays;

            DeviceStatus deviceStatus = timer.Status;
            switch (deviceStatus.PowerState)
            {
                case PowerState.PowerOff:
                    toReturn[14] = 0x0F;
                    return toReturn;
                case PowerState.PowerOn:
                    toReturn[14] = 0xF0;
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
                toReturn[13] = deviceStatus.White2 ?? 0;
            }
            else
            {
                toReturn[9] = deviceStatus.PresetDelay;
            }

            return toReturn;
        }
    }
}
