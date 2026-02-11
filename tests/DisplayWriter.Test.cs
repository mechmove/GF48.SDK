using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

internal static class DisplayWriter
{
	public static void DisplayWriterTests()
	{
		ConnectionInfo connectionInfo = new ConnectionInfo();
		connectionInfo.GetConfiguration();

		Console.WriteLine("GF 48 SDK Device Tests");

		var services = new ServiceCollection();

		services.AddSingleton<IGoFlightModules,FakeGoFlight>();
		services.AddSingleton<IGoFlightModules,GoFlight>();
		services.AddSingleton<IGoFlight>(sp =>
		{
			var all = sp.GetServices<IGoFlightModules>();
			return all.First(p => p.DisplayId == connectionInfo.DisplayId);
		});

		var provider = services.BuildServiceProvider();

		var GFDev = provider.GetRequiredService<IGoFlight>();

		Console.WriteLine("press any key to watch LEDs, you should step through code slowly");
		Console.ReadKey();

		// set all indicators OFF if ON
		if (GFDev.Init().Equals(0)) GFDev.SetIndicators(0, (IntPtr)0);

		// write values to LEDs:
		// 8 switches will carry a binary weight,
		// we can have up to 255 (FF) representing
		// all switches on. Step through the code 
		// slowly to workout how you will use this logic

		// Note: I choose not to implement LEDs in my sim logic,
		// its here as a starting point
		for (Int32 LedPos = 0; LedPos <= 255; LedPos++)
		{
			GFDev.SetIndicators(0, (IntPtr)LedPos);
			Task.Delay(100).Wait();
		}

		Console.WriteLine("press any key");
		Console.ReadKey();

		// cleanup
		GFDev.CleanUpGoFlight(connectionInfo);

		ConsoleKeyInfo keyInfo;
		do
		{
			keyInfo = Console.ReadKey(true); // 'true' hides the key
		} while (keyInfo.Key != ConsoleKey.E);



	}
}
