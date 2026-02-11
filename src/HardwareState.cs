public class HardwareState
{
	public string Rotary1 { get; set; }
	public string Rotary2 { get; set; }
	public string Rotary3 { get; set; }
	public string Rotary4 { get; set; }
	public bool Btn1 { get; set; }
	public bool Btn2 { get; set; }
	public bool Btn3 { get; set; }
	public bool Btn4 { get; set; }
	public bool Btn5 { get; set; }
	public bool Btn6 { get; set; }
	public bool Btn7 { get; set; }
	public bool Btn8 { get; set; }


	// Clone method for immutability
	public HardwareState Clone()
	{
		return new HardwareState
		{
			Rotary1 = this.Rotary1,
			Rotary2 = this.Rotary2,
			Rotary3 = this.Rotary3,
			Rotary4 = this.Rotary4,
			Btn1 = this.Btn1,
			Btn2 = this.Btn2,
			Btn3 = this.Btn3,
			Btn4 = this.Btn4,
			Btn5 = this.Btn5,
			Btn6 = this.Btn6,
			Btn7 = this.Btn7,
			Btn8 = this.Btn8
		};
	}
}