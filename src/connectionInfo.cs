using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
public sealed class ConnectionInfo
{
	string fln = "GF48.SDK.Init.txt";
	public string PID { get; set; }
	public string VID { get; set; }
	public string Desc { get; set; }
	public int DisplayId { get; set; } // 1 = Real displays, 2 = fake
	public void GetConfiguration()
	{
		var contents = File.ReadAllLines(fln);
		if (contents.Length < 4)
			throw new InvalidOperationException("Config file missing required lines.");

		VID = StripOutComment(contents[0]);
		PID = StripOutComment(contents[1]);
		Desc = StripOutComment(contents[2]);
		DisplayId = Convert.ToInt16(StripOutComment(contents[3]));
	}
	static string StripOutComment(string s)
	{
		int idx = s.IndexOf(';');
		return idx >= 0 ? s[..idx] : s;
	}

}