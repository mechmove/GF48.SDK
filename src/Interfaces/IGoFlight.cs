using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection.Metadata.Ecma335;
using System.Text;
using System.Threading.Tasks;
public interface IGoFlight
{
	int DisplayId { get; }
	bool Initialized { get; set; }
	int Init();
	void SetIndicators(int nDevIndex, IntPtr szValue);
	Task CleanUpGoFlight(ConnectionInfo connectionInfo);
}
