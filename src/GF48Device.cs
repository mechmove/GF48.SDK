using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace USB_Communication
{
	public class GF48Device
	{
		private HardwareState _currentState = new HardwareState();
		
		public event Action<HardwareState> OnHardwareStateChanged;
		public async Task ProcessMessage(string RawData)
		{
			_currentState.Rotary1 = RawData.Substring(3, 2);
			_currentState.Rotary2 = RawData.Substring(6, 2);
			_currentState.Rotary3 = RawData.Substring(9, 2);
			_currentState.Rotary4 = RawData.Substring(12, 2);
			switch (RawData.Substring(15, 2))
			{
				case "00":
					{// if we don't do this, the buttons will stay "pressed"
					 // which results in a double press since there are 2 signals for press and
					 // release of each button.
						_currentState.Btn1 = false;
						_currentState.Btn2 = false;
						_currentState.Btn3 = false;
						_currentState.Btn4 = false;
						_currentState.Btn5 = false;
						_currentState.Btn6 = false;
						_currentState.Btn7 = false;
						_currentState.Btn8 = false;
					}
					break;
				case "01":
					_currentState.Btn1 = true;
					break;
				case "02":
					_currentState.Btn2 = true;
					break;
				case "04":
					_currentState.Btn3 = true;
					break;
				case "08":
					_currentState.Btn4 = true;
					break;
				case "10":
					_currentState.Btn5 = true;
					break;
				case "20":
					_currentState.Btn6 = true;
					break;
				case "40":
					_currentState.Btn7 = true;
					break;
				case "80":
					_currentState.Btn8 = true;
					break;
			}

			// we are done with mapping, now push updates to the sim
			var snapshot = _currentState.Clone();
			var handler = OnHardwareStateChanged;
			if (handler != null)
				_ = Task.Run(() => handler(snapshot));
		}
	}
}
