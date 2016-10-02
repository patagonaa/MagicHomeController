# MagicHomeController.NET 0.4

Port of [AceFloof's MagicHomeController](https://github.com/acefloof/MagicHomeController) to C#

With this class you can control devices that are compatible with the "MagicHome" app

Provided as is without warranty
## How to use Device:
### Device types as defined in the `DeviceType` enum:

* DeviceType.Rgb
* DeviceType.RgbWarmwhite
* DeviceType.RgbWarmwhiteColdWhite
* DeviceType.Bulb // V.4+
* DeviceType.LegacyBulb // V.3-

Example call: `var controller1 = new Device(IPAddress.Parse("192.168.2.102"), DeviceType.RgbWarmwhite);`

**To turn the controller on/off use the following methods:**
    
```
controller1.TurnOn();
controller1.TurnOff();
```

**To set an RGB(+WW+CW) color use the following method:**

```
controller1.SetColor(R, G, B);
controller1.SetColor(R, G, B, WW);
controller1.SetColor(R, G, B, WW, CW);
```

**To set a preset mode use the following method:**

```
controller1.SetPreset(PresetMode, Speed);
```

A speed of 1 is fastest, and 24 is slowest.

Example call: `controller1.SetPreset(PresetMode.RgbFade, 10);`

### Preset modes as defined in the `PresetMode` enum:

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

**You can get the current status of the controller with**

```
controller1.GetDeviceStatus();
```
 
This will return a DeviceStatus Object with the following Properties:

* On
* Mode
* Speed
* Red
* Green
* Blue
* White1
* White2

## How to Scan for Devices:

**Use static method `DeviceFinder.FindDevices()`**

This will return a List of DeviceFindResult Objects with the following Properties:

* IpAddress
* MacAddress
* Model

Example usage: `var allDevices = DeviceFinder.FindDevices().Select(x => new Device(x.IpAddress, DeviceType.RgbWarmwhite))`