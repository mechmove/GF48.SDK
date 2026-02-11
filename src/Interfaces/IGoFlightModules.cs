using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection.Metadata.Ecma335;
using System.Text;
using System.Threading.Tasks;
// If you do not know what this is, read: https://github.com/mechmove/mechmove.github.io/discussions/2
// We can create a "collection of interfaces" administered by MisterBigContract
// and identify at runtime by user-defined ID:
public interface IGoFlightModules : IGoFlight { }
