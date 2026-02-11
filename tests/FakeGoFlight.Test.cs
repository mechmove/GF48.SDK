using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

static internal class FakeGoFlightDemo 
{
	public static void FakeGoFlight()
	{
		ConnectionInfo connectionInfo = new ConnectionInfo();
		connectionInfo.GetConfiguration();

		var services = new ServiceCollection();

		services.AddSingleton<IGoFlightModules, FakeGoFlight>();
		services.AddSingleton<IGoFlightModules, GoFlight>();
		services.AddSingleton<IGoFlight>(sp =>
		{
			var all = sp.GetServices<IGoFlightModules>();
			return all.First(p => p.DisplayId == connectionInfo.DisplayId);
		});

		var provider = services.BuildServiceProvider();

		var GFDev = provider.GetRequiredService<IGoFlight>();

		// the purpose of this test is prove we can control behavior
		// with a configuration flag and without changing code. 

		//GoFlightOpt { get; set; } // 1 = Real displays, 2 = fake
		Console.Write("GoFlight DisplayID : " + GFDev.DisplayId.ToString());

		GFDev.Init();

		Console.ReadKey();
		GFDev.CleanUpGoFlight(connectionInfo);
	}

}
