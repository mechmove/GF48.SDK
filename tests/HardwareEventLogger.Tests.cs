using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using USB_Communication;

internal static class HardwareEventLoggerTests
{
	public static void HardwareEventLogger()
	{
		Console.WriteLine("GF 48 SDK State and Logging Examples");

		if (!Directory.Exists("UsbHid"))
		{
			Directory.CreateDirectory("UsbHid");
		}

		if (!Directory.Exists("DeviceEvents"))
		{
			Directory.CreateDirectory("DeviceEvents");
		}

		var services = new ServiceCollection();

		services.AddSingleton<GF48ProtocolHandler>();
		services.AddSingleton<GF48Device>();

		var provider = services.BuildServiceProvider();

		var protocol = provider.GetRequiredService<GF48ProtocolHandler>();
		var device = provider.GetRequiredService<GF48Device>();

		protocol.ReportReceived += report =>
		{
			string HIDReport = (BitConverter.ToString(report));
			device.ProcessMessage(HIDReport).Wait();
			string detailReport = "RAW HID:" + DateTime.Now + ":" + HIDReport + Environment.NewLine;
			File.AppendAllText("UsbHid\\HidEvents.txt", detailReport);
			Console.WriteLine(detailReport);
		};

		device.OnHardwareStateChanged += state =>
		{
			string CurrentState = RtnCurrentState(state);
			File.AppendAllText("DeviceEvents\\EventLog.txt", CurrentState);
			Console.WriteLine(CurrentState);
		};
		Console.WriteLine("Now start manipulating the controls....");
		Console.ReadKey();

		ConsoleKeyInfo keyInfo;
		do
		{
			keyInfo = Console.ReadKey(true); // 'true' hides the key
		} while (keyInfo.Key != ConsoleKey.E);

	}
	static string RtnCurrentState(HardwareState state)
	{
		string controlChanges = string.Empty;
		if (state.Btn1) controlChanges += DateTime.Now + ":" + "Button 1 Pressed" + Environment.NewLine;
		if (state.Btn2) controlChanges += DateTime.Now + ":" + "Button 2 Pressed" + Environment.NewLine;
		if (state.Btn3) controlChanges += DateTime.Now + ":" + "Button 3 Pressed" + Environment.NewLine;
		if (state.Btn4) controlChanges += DateTime.Now + ":" + "Button 4 Pressed" + Environment.NewLine;
		if (state.Btn5) controlChanges += DateTime.Now + ":" + "Button 5 Pressed" + Environment.NewLine;
		if (state.Btn6) controlChanges += DateTime.Now + ":" + "Button 6 Pressed" + Environment.NewLine;
		if (state.Btn7) controlChanges += DateTime.Now + ":" + "Button 7 Pressed" + Environment.NewLine;
		if (state.Btn8) controlChanges += DateTime.Now + ":" + "Button 8 Pressed" + Environment.NewLine;
		controlChanges += "Rotary1 " + DateTime.Now + " Value: " + state.Rotary1+ Environment.NewLine;
		controlChanges += "Rotary2 " + DateTime.Now + " Value: " + state.Rotary2 + Environment.NewLine;
		controlChanges += "Rotary3 " + DateTime.Now + " Value: " + state.Rotary3 + Environment.NewLine;
		controlChanges += "Rotary4 " + DateTime.Now + " Value: " + state.Rotary4 + Environment.NewLine;
		return controlChanges + Environment.NewLine;
	}
}
