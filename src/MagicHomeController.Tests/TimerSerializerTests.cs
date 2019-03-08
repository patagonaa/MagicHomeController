using MagicHomeController;
using MagicHomeController.Timers;
using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Tests
{
    public class TimerSerializerTests
    {
        [SetUp]
        public void Setup()
        {
        }

        [Test]
        public void Test_RgbWwCw_Deserialize()
        {
            var hexString = "0f22f0000000150000fe000000000000f0f0000000150100fe61ff00ff0000f0f0000000150100fe61000000ff00f0f0000000150100fe6100000000fff0f0000000150100fe2c1000000000f00f0000000000000000000000000000005e";
            var responseBytes = StringToByteArray(hexString);

            var timers = TimerSerializer.Deserialize(responseBytes, DeviceType.RgbWarmwhiteColdwhite).ToList();

            Assert.AreEqual(5, timers.Count);

            Assert.AreEqual(PresetMode.None, timers[0].Status.Mode);
            Assert.AreEqual(PowerState.PowerOn, timers[0].Status.PowerState);

            Assert.AreEqual(PresetMode.NormalRgb, timers[1].Status.Mode);
            Assert.AreEqual(255, timers[1].Status.Red);
            Assert.AreEqual(0, timers[1].Status.Green);
            Assert.AreEqual(255, timers[1].Status.Blue);

            Assert.AreEqual(PresetMode.NormalRgb, timers[2].Status.Mode);
            Assert.AreEqual(255, timers[2].Status.White1);

            Assert.AreEqual(PresetMode.NormalRgb, timers[3].Status.Mode);
            Assert.AreEqual(255, timers[3].Status.White2);

            Assert.AreEqual(PresetMode.WhitePulse, timers[4].Status.Mode);
            Assert.AreEqual(16, timers[4].Status.PresetDelay);
        }

        [Test]
        public void Test_RgbWwCw_Serialize()
        {
            var hexString = "21f0000000150000fe000000000000f0f0000000150100fe61ff00ff0000f0f0000000150100fe61000000ff00f0f0000000150100fe6100000000fff0f0000000150100fe2c1000000000f00f000000000000000000000000000000f0";
            var expected = StringToByteArray(hexString);

            var timers = new List<Timer>
            {
                new Timer
                {
                    Active = true,
                    Status =
                    {
                        Mode = PresetMode.None,
                        PowerState = PowerState.PowerOn
                    },
                    Date = new DateTime(1, 1, 1, 21, 00, 00),
                    RepeatDays = TimerDays.Everyday
                },
                new Timer
                {
                    Active = true,
                    Status =
                    {
                        Mode = PresetMode.NormalRgb,
                        PowerState = PowerState.PowerOn,
                        Red = 255,
                        Green = 0,
                        Blue = 255
                    },
                    Date = new DateTime(1, 1, 1, 21, 01, 00),
                    RepeatDays = TimerDays.Everyday
                },
                new Timer
                {
                    Active = true,
                    Status =
                    {
                        Mode = PresetMode.NormalRgb,
                        PowerState = PowerState.PowerOn,
                        White1 = 255
                    },
                    Date = new DateTime(1, 1, 1, 21, 01, 00),
                    RepeatDays = TimerDays.Everyday
                },
                new Timer
                {
                    Active = true,
                    Status =
                    {
                        Mode = PresetMode.NormalRgb,
                        PowerState = PowerState.PowerOn,
                        White2 = 255
                    },
                    Date = new DateTime(1, 1, 1, 21, 01, 00),
                    RepeatDays = TimerDays.Everyday
                },
                new Timer
                {
                    Active = true,
                    Status =
                    {
                        Mode = PresetMode.WhitePulse,
                        PresetDelay = 16,
                        PowerState = PowerState.PowerOn
                    },
                    Date = new DateTime(1, 1, 1, 21, 01, 00),
                    RepeatDays = TimerDays.Everyday
                }
            };

            var returnedBytes = TimerSerializer.Serialize(timers, DeviceType.RgbWarmwhiteColdwhite);

            CollectionAssert.AreEqual(expected, returnedBytes);
        }

        private byte[] StringToByteArray(string hex)
        {
            return Enumerable.Range(0, hex.Length)
                .Where(x => x % 2 == 0)
                .Select(x => Convert.ToByte(hex.Substring(x, 2), 16))
                .ToArray();
        }
    }
}