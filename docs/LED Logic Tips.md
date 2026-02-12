**LED Logic Tips (or: The Bitmask That Bites Back)**
If you’re working with the eight LEDs on the GF‑48, remember that the values 0–255 aren’t just numbers — they’re eight tiny lightbulbs living inside a binary apartment complex. Each bit controls one LED, and they all have opinions about when they want to turn on or off.

**A few practical things to keep in mind:**
Track the whole LED state, not just one light at a time
The hardware won’t maintain state for you. If you want the LEDs to behave like the real aircraft, you’ll need to keep a running bitmask of all eight LEDs and update it as a group.

**Simulator logic matters more than button logic**
For example, if LNAV is already illuminated and active, pressing the LNAV button again won’t magically turn the LED off.
The only way to extinguish it is to change lateral steering mode — say, by selecting HDG.
This is normal. It’s also the moment many developers realize they’ve accidentally built a Christmas tree instead of an autopilot panel.

**Decide which LED represents which function**
If LNAV is mapped to one of the eight LEDs, you’ll need to choose which bit represents it.
Bit 0? Bit 3? Bit 7?
There’s no wrong answer — only inconsistent ones.
None of this is impossible — it just takes a little thought

**LED bitmasks are simple in theory, but once you mix:**
- simulator state
- hardware state
- mode logic
- and user input
…it becomes a small architectural puzzle. A fun one, but still a puzzle.
Treat the LEDs as a collective system rather than eight independent bulbs, and everything falls into place.