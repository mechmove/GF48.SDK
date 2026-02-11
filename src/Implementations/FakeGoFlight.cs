using System.Collections.Generic;
using System.Linq;
using System.Net.Sockets;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

public class FakeGoFlight : IGoFlightModules, IGoFlight
{
	public int DisplayId => 2;
	public int Init()
	{
		return 0;
	}

	public void SetIndicators(int nDevIndex, IntPtr szValue) { }
	public bool Initialized { get; set; }
	public async Task CleanUpGoFlight(ConnectionInfo connectionInfo)
	{
	}

}
