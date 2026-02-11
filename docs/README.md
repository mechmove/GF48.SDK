**GF‑48 SDK**

A clean, simulator‑agnostic SDK for the GoFlight GF‑48 hardware module.
This library provides a modern, dependency‑injected architecture for reading HID reports, mapping them into strongly‑typed hardware state, and reacting to changes in real time. No simulator assumptions. Just hardware → state → events.

**Features**
- Fully simulator‑agnostic
- Strongly‑typed HardwareState snapshots
- Immutable state cloning
- Asynchronous USB HID read loop
- Real and fake device implementations (GoFlight and FakeGoFlight)
- Dependency‑injected architecture
- Logging and display examples included
- Clean separation of concerns: protocol → device → state

**Architecture Overview**
The GF‑48 SDK is built around three core components:
**GF48ProtocolHandler**
Handles USB HID enumeration, device discovery, and asynchronous input report reading.
It raises ReportReceived whenever a raw HID packet arrives.
Key behavior (from the code):
- Reads HID reports asynchronously
- Emits raw byte arrays
- Matches devices by VID/PID from GF48.SDK.Init.txt
- Uses Windows HID APIs via P/Invoke
**GF48Device**
Converts raw HID strings into meaningful state.
- Extracts rotary values via substring
- Decodes button byte via switch
- Produces immutable snapshots via Clone()
- Raises OnHardwareStateChanged asynchronously
**Example from the code:**
_currentState.Rotary1 = RawData.Substring(3, 2);


**HardwareState**
A simple, immutable snapshot of all GF‑48 controls:
- Rotary1–Rotary4 (hex strings)
- Btn1–Btn8 (booleans)
Snapshots are cloned to prevent accidental mutation.

**Device Selection (Real vs. Fake)**
The SDK uses a clean DI pattern to select the correct device at runtime:
services.AddSingleton<IGoFlightModules, FakeGoFlight>();
services.AddSingleton<IGoFlightModules, GoFlight>();
services.AddSingleton<IGoFlight>(sp =>
{
    var all = sp.GetServices<IGoFlightModules>();
    return all.First(p => p.DisplayId == connectionInfo.DisplayId);
});

***Set DisplayId in GF48.SDK.Init.txt:***
- 1 → Real hardware (GoFlight)
- 2 → Fake device (FakeGoFlight)
This allows testing without hardware and without changing code.

**Configuration**

The SDK reads VID, PID, description, and display mode from:
GF48.SDK.Init.txt

Example:
VID1234
PID5678
GF48 Module
1

Comments can be added using ; — they are stripped automatically.

**Examples**

**1. Hardware Event Logger**
Logs raw HID reports and decoded hardware state:
- Raw HID → UsbHid/HidEvents.txt
- State changes → DeviceEvents/EventLog.txt
Useful for debugging and reverse‑engineering.

**2. Display Writer**
Writes LED patterns (0–255) to the GF‑48 indicators:
for (int pos = 0; pos <= 255; pos++)
{
    GFDev.SetIndicators(0, (IntPtr)pos);
    Task.Delay(100).Wait();
}

**3. Fake Device Demo**
Demonstrates runtime selection of real vs. fake device.

**Usage**
**Initialize the SDK**
var services = new ServiceCollection();

services.AddSingleton<GF48ProtocolHandler>();
services.AddSingleton<GF48Device>();
services.AddSingleton<HardwareState>();

var provider = services.BuildServiceProvider();

**Subscribe to Events**
protocol.ReportReceived += report => { ... };
device.OnHardwareStateChanged += state => { ... };

**Process HID Reports**
string hid = BitConverter.ToString(report);
await device.ProcessMessage(hid);

**Philosophy**
This SDK is intentionally simple.
It doesn’t assume a simulator.
It doesn’t dictate how you use the hardware.
It doesn’t drag in legacy abstractions or simulator‑specific logic.
It gives you:
- clean contracts
- predictable behavior
- a stable foundation
- and the freedom to build whatever sits above it

**License**
MIT.
