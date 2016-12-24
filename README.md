# MagicHomeController.NET 0.4

Port of [AceFloof's MagicHomeController](https://github.com/acefloof/MagicHomeController) to C#

With this class you can control devices that are compatible with the "Magic Home" app

Provided as is without warranty

## Usage:

### Instantiating new Devices
Use one of the constructor overloads
```
new Device(IPAddress ip, DeviceType type);
new Device(EndPoint endPoint, DeviceType type);
```

Device Types as defined in the `DeviceType` enum:
* DeviceType.Rgb
* DeviceType.RgbWarmwhite
* DeviceType.RgbWarmwhiteColdWhite
* DeviceType.Bulb // V.4+
* DeviceType.LegacyBulb // V.3-

Examples:
```
Device controller1 = new Device(IPAddress.Parse("192.168.178.123"), DeviceType.Rgb);
Device controller2 = new Device(new IPEndPoint(IPAddress.Parse("192.168.178.1"), 1234), DeviceType.RgbWarmwhite);
```

### Turning the controller on and off
Use the methods
```
void Device.TurnOn();
void Device.TurnOff();
```

### Setting colors
Use one of the method overloads
```
void Device.SetColor(byte r, byte g, byte b);
void Device.SetColor(byte r, byte g, byte b, byte ww);
void Device.SetColor(byte r, byte g, byte b, byte ww, byte cw);
```
or the extension method
```
void Device.SetColor(System.Drawing.Color color);
void Device.SetColor(System.Drawing.Color color, byte ww);
void Device.SetColor(System.Drawing.Color color, byte ww, byte cw);
```

Examples: 
```
controller1.SetColor(255, 255, 255);
controller1.SetColor(Color.DarkBlue);
```

### Using the built-in preset modes
Use the method
```
void Device.SetPreset(PresetMode presetMode, byte presetDelay);
```

a presetDelay of 1 is fastest, and 24 is slowest.

Example: 
```
controller1.SetPreset(PresetMode.RgbFade, 10);
```

Preset modes as defined in the `PresetMode` enum:

* RgbFade
* RedPulse
* GreenPulse
* BluePulse
* YellowPulse
* CyanPulse
* VioletPulse
* WhitePulse
* RedGreenAlternatePulse
* RedBlueAlternatePulse
* GreenBlueAlternatePulse
* DiscoFlash
* RedFlash
* GreenFlash
* BlueFlash
* YellowFlash
* CyanFlash
* VioletFlash
* WhiteFlash
* ColorChange
* NormalRgb *(Will be set automatically with SetColor() and only returned from GetDeviceStatus())*

### Getting the current device status
Use the method
```
DeviceStatus Device.GetDeviceStatus();
```
 
This will return a `DeviceStatus` Object with the following Properties:

* bool On // if set to false, device is off
* PresetMode Mode // if set to `PresetMode.NormalRgb`, PresetDelay property is ignored, else RGBW properties are ignored
* byte PresetDelay // described in [Using the built-in preset modes](#using-the-built-in-preset-modes)
* byte Red
* byte Green
* byte Blue
* byte? White1
* byte? White2

### Using Timers
#### Setting the current time
Use the method
```
void Device.SetTime(DateTime dateTime);
```
Example call:
```
controller1.SetTime(DateTime.Now);
```

#### Getting the time currently set
Use the method
```
DateTime Device.GetTime();
```

#### Getting all timers
Use the method
```
IEnumerable<Timer> Device.GetTimers();
```
This will return an IEnumerable of `Timer` objects further described [below](#the-timer-object).

#### Setting all timers
Use the method
```
void Device.SetTimers(IEnumerable<Timer> timers);
```
This takes an IEnumerable of `Timer` objects further described [below](#the-timer-object).

There can be up to 6 Timers at once.

#### The `Timer` object
It has the following properties:
* bool Active // Timer is active / inactive
* TimerDays RepeatDays // further described below
* DateTime Date // further described below
* DeviceStatus Status // status to set (described under [Getting the current device status](#getting-the-current-device-status))

The value of RepeatDays is described in the `TimerDays` flags enum and can be one or a combination of the following values:
* None
* Monday
* Tuesday
* Wednesday
* Thursday
* Friday
* Saturday
* Sunday
* Everyday
* Weekdays

where `Everyday` is an alias of `Monday | Tuesday | Wednesday | Thursday | Friday | Saturday | Sunday`
and `Weekdays` is an alias of `Monday | Tuesday | Wednesday | Thursday | Friday`

If `RepeatDays` is set to `None` then the Timer is only executed at the exact date and time that is set with `Date`.

If `RepeatDays` is set to anything different, only the Time Part of `Date` is relevant and the Timer executes at the specified Time every day in `RepeatDays`.

Example usage (Turn the controller on every Monday and Tuesday at 15:30 with the color blue, turn the controller off every Monday and Tuesday at 18:00 and turn the controller on (white flashing with a speed of 10) at this time tomorrow.):

```
IList<Timer> timers = new List<Timer>();
timers.Add(new Timer
{
	Active = true,
	RepeatDays = TimerDays.Monday | TimerDays.Tuesday,
	Date = new DateTime(1, 1, 1, 15, 30, 00),
	Status = new DeviceStatus
	{
		On = true,
		Mode = PresetMode.NormalRgb,
		Red = 0,
		Green = 0,
		Blue = 255,
		White1 = 0
	}
});

timers.Add(new Timer
{
	Active = true,
	RepeatDays = TimerDays.Monday | TimerDays.Tuesday,
	Date = new DateTime(1, 1, 1, 18, 00, 00),
	Status = new DeviceStatus
	{
		On = false
	}
});

timers.Add(new Timer
{
	Active = true,
	RepeatDays = TimerDays.None,
	Date = DateTime.Now.AddDays(1),
	Status = new DeviceStatus
	{
		On = true,
		Mode = PresetMode.WhiteFlash,
		PresetDelay = 10
	}
});

controller1.SetTimers(timers);
```

### Scanning for Devices

Use one of the the static method overloads
```
IEnumerable<DeviceFindResult> DeviceFinder.FindDevices();
IEnumerable<DeviceFindResult> DeviceFinder.FindDevices(Endpoint endPoint);
IEnumerable<DeviceFindResult> DeviceFinder.FindDevices(Endpoint endPoint, int timeoutMilliseconds);
```

This will return an IEnumerable of `DeviceFindResult` objects with the following Properties:

* IpAddress
* MacAddress
* Model

Example usage: 
```
IEnumerable<Device> allDevices = DeviceFinder.FindDevices().Select(x => new Device(x.IpAddress, DeviceType.RgbWarmwhite))
```