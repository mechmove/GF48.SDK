// See https://aka.ms/new-console-template for more information
using Microsoft.Extensions.DependencyInjection;
using System.Security.Cryptography;

Console.WriteLine("GF 48 SDK Implementation Examples");

HardwareEventLoggerTests.HardwareEventLogger();
//FakeGoFlightDemo.FakeGoFlight();
//DisplayWriter.DisplayWriterTests();

ConsoleKeyInfo keyInfo;
do
{
	keyInfo = Console.ReadKey(true); // 'true' hides the key
} while (keyInfo.Key != ConsoleKey.E);
