using System;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

/// <summary>
public class GF48ProtocolHandler : IDisposable
{
	public event Action<byte[]> ReportReceived;

	// Synopsis: This class facilitates communication with a USB HID device using Windows API calls.
	// It includes methods for enumerating connected HID devices, opening a connection to a specific device,
	// as denoted by product ID and vendor ID, and reading input reports from the device asynchronously.
	// This code was sourced from a free download at https://www.codeproject.com/, but was optimized for 
	// correct operation (source contained repeated device connections) and converted from Form to a Console App.
	// I tried to isolate logic based on sound architectural principles. 

	#region WinAPI

	[DllImport("setupapi.dll")]
	static extern IntPtr SetupDiGetClassDevs(ref Guid ClassGuid, IntPtr Enumerator, IntPtr hwndParent, int Flags);

	[DllImport("setupapi.dll")]
	static extern bool SetupDiEnumDeviceInterfaces(IntPtr hDevInfo, IntPtr devInfo, ref Guid interfaceClassGuid, int memberIndex, ref SP_DEVICE_INTERFACE_DATA deviceInterfaceData);

	[DllImport(@"setupapi.dll")]
	static extern bool SetupDiGetDeviceInterfaceDetail(IntPtr DeviceInfoSet, ref SP_DEVICE_INTERFACE_DATA DeviceInterfaceData, ref SP_DEVICE_INTERFACE_DETAIL_DATA DeviceInterfaceDetailData, int DeviceInterfaceDetailDataSize, ref int RequiredSize, IntPtr DeviceInfoData);

	[DllImport(@"setupapi.dll")]
	static extern bool SetupDiGetDeviceInterfaceDetail(IntPtr DeviceInfoSet, ref SP_DEVICE_INTERFACE_DATA DeviceInterfaceData, IntPtr DeviceInterfaceDetailData, int DeviceInterfaceDetailDataSize, ref int RequiredSize, IntPtr DeviceInfoData);

	[DllImport(@"kernel32.dll")]
	static extern IntPtr CreateFile(string fileName, uint fileAccess, uint fileShare, FileMapProtection securityAttributes, uint creationDisposition, uint flags, IntPtr overlapped);

	[DllImport("hid.dll")]
	static extern void HidD_GetHidGuid(ref Guid Guid);

	[DllImport("hid.dll")]
	static extern bool HidD_GetPreparsedData(IntPtr HidDeviceObject, ref IntPtr PreparsedData);

	[DllImport("hid.dll")]
	static extern bool HidD_GetAttributes(IntPtr DeviceObject, ref HIDD_ATTRIBUTES Attributes);

	[DllImport("hid.dll")]
	static extern uint HidP_GetCaps(IntPtr PreparsedData, ref HIDP_CAPS Capabilities);

	[DllImport("hid.dll")]
	static extern int HidP_GetButtonCaps(HIDP_REPORT_TYPE ReportType, [In, Out] HIDP_BUTTON_CAPS[] ButtonCaps, ref ushort ButtonCapsLength, IntPtr PreparsedData);

	[DllImport("hid.dll")]
	static extern int HidP_GetValueCaps(HIDP_REPORT_TYPE ReportType, [In, Out] HIDP_VALUE_CAPS[] ValueCaps, ref ushort ValueCapsLength, IntPtr PreparsedData);

	[DllImport("hid.dll")]
	static extern int HidP_MaxUsageListLength(HIDP_REPORT_TYPE ReportType, ushort UsagePage, IntPtr PreparsedData);

	[DllImport("hid.dll")]
	static extern int HidP_SetUsages(HIDP_REPORT_TYPE ReportType, ushort UsagePage, short LinkCollection, short Usages, ref int UsageLength, IntPtr PreparsedData, IntPtr HID_Report, int ReportLength);

	[DllImport("hid.dll")]
	static extern int HidP_SetUsageValue(HIDP_REPORT_TYPE ReportType, ushort UsagePage, short LinkCollection, ushort Usage, ulong UsageValue, IntPtr PreparsedData, IntPtr HID_Report, int ReportLength);

	[DllImport("setupapi.dll")]
	static extern bool SetupDiDestroyDeviceInfoList(IntPtr DeviceInfoSet);

	[DllImport("kernel32.dll")]
	static extern bool CloseHandle(IntPtr hObject);

	[DllImport("hid.dll")]
	static extern bool HidD_FreePreparsedData(ref IntPtr PreparsedData);

	[DllImport("hid.dll")]
	private static extern bool HidD_GetProductString(IntPtr HidDeviceObject, IntPtr Buffer, uint BufferLength);

	[DllImport("hid.dll")]
	static extern bool HidD_GetSerialNumberString(IntPtr HidDeviceObject, IntPtr Buffer, Int32 BufferLength);

	[DllImport("hid.dll")]
	static extern Boolean HidD_GetManufacturerString(IntPtr HidDeviceObject, IntPtr Buffer, Int32 BufferLength);

	[DllImport("kernel32.dll")]
	static extern uint GetLastError();

	#endregion

	#region DLL Var

	IntPtr hardwareDeviceInfo;

	const int DIGCF_DEFAULT = 0x00000001;
	const int DIGCF_PRESENT = 0x00000002;
	const int DIGCF_ALLCLASSES = 0x00000004;
	const int DIGCF_PROFILE = 0x00000008;
	const int DIGCF_DEVICEINTERFACE = 0x00000010;

	const uint GENERIC_READ = 0x80000000;
	const uint GENERIC_WRITE = 0x40000000;
	const uint GENERIC_EXECUTE = 0x20000000;
	const uint GENERIC_ALL = 0x10000000;

	const uint FILE_SHARE_READ = 0x00000001;
	const uint FILE_SHARE_WRITE = 0x00000002;
	const uint FILE_SHARE_DELETE = 0x00000004;

	const uint CREATE_NEW = 1;
	const uint CREATE_ALWAYS = 2;
	const uint OPEN_EXISTING = 3;
	const uint OPEN_ALWAYS = 4;
	const uint TRUNCATE_EXISTING = 5;

	const int HIDP_STATUS_SUCCESS = 1114112;
	const int DEVICE_PATH = 260;
	const int INVALID_HANDLE_VALUE = -1;

	enum FileMapProtection : uint
	{
		PageReadonly = 0x02,
		PageReadWrite = 0x04,
		PageWriteCopy = 0x08,
		PageExecuteRead = 0x20,
		PageExecuteReadWrite = 0x40,
		SectionCommit = 0x8000000,
		SectionImage = 0x1000000,
		SectionNoCache = 0x10000000,
		SectionReserve = 0x4000000,
	}

	enum HIDP_REPORT_TYPE : ushort
	{
		HidP_Input = 0x00,
		HidP_Output = 0x01,
		HidP_Feature = 0x02,
	}

	[StructLayout(LayoutKind.Sequential)]
	struct LIST_ENTRY
	{
		public IntPtr Flink;
		public IntPtr Blink;
	}

	[StructLayout(LayoutKind.Sequential)]
	struct DEVICE_LIST_NODE
	{
		public LIST_ENTRY Hdr;
		public IntPtr NotificationHandle;
		public HID_DEVICE HidDeviceInfo;
		public bool DeviceOpened;
	}

	[StructLayout(LayoutKind.Sequential)]
	struct SP_DEVICE_INTERFACE_DATA
	{
		public Int32 cbSize;
		public Guid interfaceClassGuid;
		public Int32 flags;
		private UIntPtr reserved;
	}

	[StructLayout(LayoutKind.Sequential, CharSet = CharSet.Ansi)]
	struct SP_DEVICE_INTERFACE_DETAIL_DATA
	{
		public int cbSize;
		[MarshalAs(UnmanagedType.ByValTStr, SizeConst = DEVICE_PATH)]
		public string DevicePath;
	}

	[StructLayout(LayoutKind.Sequential)]
	struct SP_DEVINFO_DATA
	{
		public int cbSize;
		public Guid classGuid;
		public UInt32 devInst;
		public IntPtr reserved;
	}

	[StructLayout(LayoutKind.Sequential)]
	struct HIDP_CAPS
	{
		[MarshalAs(UnmanagedType.U2)]
		public UInt16 Usage;
		[MarshalAs(UnmanagedType.U2)]
		public UInt16 UsagePage;
		[MarshalAs(UnmanagedType.U2)]
		public UInt16 InputReportByteLength;
		[MarshalAs(UnmanagedType.U2)]
		public UInt16 OutputReportByteLength;
		[MarshalAs(UnmanagedType.U2)]
		public UInt16 FeatureReportByteLength;
		[MarshalAs(UnmanagedType.ByValArray, SizeConst = 17)]
		public UInt16[] Reserved;
		[MarshalAs(UnmanagedType.U2)]
		public UInt16 NumberLinkCollectionNodes;
		[MarshalAs(UnmanagedType.U2)]
		public UInt16 NumberInputButtonCaps;
		[MarshalAs(UnmanagedType.U2)]
		public UInt16 NumberInputValueCaps;
		[MarshalAs(UnmanagedType.U2)]
		public UInt16 NumberInputDataIndices;
		[MarshalAs(UnmanagedType.U2)]
		public UInt16 NumberOutputButtonCaps;
		[MarshalAs(UnmanagedType.U2)]
		public UInt16 NumberOutputValueCaps;
		[MarshalAs(UnmanagedType.U2)]
		public UInt16 NumberOutputDataIndices;
		[MarshalAs(UnmanagedType.U2)]
		public UInt16 NumberFeatureButtonCaps;
		[MarshalAs(UnmanagedType.U2)]
		public UInt16 NumberFeatureValueCaps;
		[MarshalAs(UnmanagedType.U2)]
		public UInt16 NumberFeatureDataIndices;
	};

	[StructLayout(LayoutKind.Sequential)]
	struct HIDD_ATTRIBUTES
	{
		public Int32 Size;
		public UInt16 VendorID;
		public UInt16 ProductID;
		public Int16 VersionNumber;
	}

	[StructLayout(LayoutKind.Sequential)]
	struct ButtonData
	{
		public Int32 UsageMin;
		public Int32 UsageMax;
		public Int32 MaxUsageLength;
		public Int16 Usages;
	}

	[StructLayout(LayoutKind.Sequential)]
	struct ValueData
	{
		public ushort Usage;
		public ushort Reserved;

		public ulong Value;
		public long ScaledValue;
	}

	[StructLayout(LayoutKind.Explicit)]
	struct HID_DATA
	{
		[FieldOffset(0)]
		public bool IsButtonData;
		[FieldOffset(1)]
		public byte Reserved;
		[FieldOffset(2)]
		public ushort UsagePage;
		[FieldOffset(4)]
		public Int32 Status;
		[FieldOffset(8)]
		public Int32 ReportID;
		[FieldOffset(16)]
		public bool IsDataSet;

		[FieldOffset(17)]
		public ButtonData ButtonData;
		[FieldOffset(17)]
		public ValueData ValueData;
	}

	[StructLayout(LayoutKind.Sequential)]
	struct HIDP_Range
	{
		public ushort UsageMin, UsageMax;
		public ushort StringMin, StringMax;
		public ushort DesignatorMin, DesignatorMax;
		public ushort DataIndexMin, DataIndexMax;
	}

	[StructLayout(LayoutKind.Sequential)]
	struct HIDP_NotRange
	{
		public ushort Usage, Reserved1;
		public ushort StringIndex, Reserved2;
		public ushort DesignatorIndex, Reserved3;
		public ushort DataIndex, Reserved4;
	}

	[StructLayout(LayoutKind.Explicit)]
	struct HIDP_BUTTON_CAPS
	{
		[FieldOffset(0)]
		public ushort UsagePage;
		[FieldOffset(2)]
		public byte ReportID;
		[FieldOffset(3), MarshalAs(UnmanagedType.U1)]
		public bool IsAlias;
		[FieldOffset(4)]
		public short BitField;
		[FieldOffset(6)]
		public short LinkCollection;
		[FieldOffset(8)]
		public short LinkUsage;
		[FieldOffset(10)]
		public short LinkUsagePage;
		[FieldOffset(12), MarshalAs(UnmanagedType.U1)]
		public bool IsRange;
		[FieldOffset(13), MarshalAs(UnmanagedType.U1)]
		public bool IsStringRange;
		[FieldOffset(14), MarshalAs(UnmanagedType.U1)]
		public bool IsDesignatorRange;
		[FieldOffset(15), MarshalAs(UnmanagedType.U1)]
		public bool IsAbsolute;
		[FieldOffset(16), MarshalAs(UnmanagedType.ByValArray, SizeConst = 10)]
		public int[] Reserved;
		[FieldOffset(56)]
		public HIDP_Range Range;
		[FieldOffset(56)]
		public HIDP_NotRange NotRange;
	}

	[StructLayout(LayoutKind.Explicit)]
	struct HIDP_VALUE_CAPS
	{
		[FieldOffset(0)]
		public ushort UsagePage;
		[FieldOffset(2)]
		public byte ReportID;
		[FieldOffset(3), MarshalAs(UnmanagedType.U1)]
		public bool IsAlias;
		[FieldOffset(4)]
		public ushort BitField;
		[FieldOffset(6)]
		public ushort LinkCollection;
		[FieldOffset(8)]
		public ushort LinkUsage;
		[FieldOffset(10)]
		public ushort LinkUsagePage;
		[FieldOffset(12), MarshalAs(UnmanagedType.U1)]
		public bool IsRange;
		[FieldOffset(13), MarshalAs(UnmanagedType.U1)]
		public bool IsStringRange;
		[FieldOffset(14), MarshalAs(UnmanagedType.U1)]
		public bool IsDesignatorRange;
		[FieldOffset(15), MarshalAs(UnmanagedType.U1)]
		public bool IsAbsolute;
		[FieldOffset(16), MarshalAs(UnmanagedType.U1)]
		public bool HasNull;
		[FieldOffset(17)]
		public byte Reserved;
		[FieldOffset(18)]
		public short BitSize;
		[FieldOffset(20)]
		public short ReportCount;
		[FieldOffset(22)]
		public ushort Reserved2a;
		[FieldOffset(24)]
		public ushort Reserved2b;
		[FieldOffset(26)]
		public ushort Reserved2c;
		[FieldOffset(28)]
		public ushort Reserved2d;
		[FieldOffset(30)]
		public ushort Reserved2e;
		[FieldOffset(32)]
		public int UnitsExp;
		[FieldOffset(36)]
		public int Units;
		[FieldOffset(40)]
		public int LogicalMin;
		[FieldOffset(44)]
		public int LogicalMax;
		[FieldOffset(48)]
		public int PhysicalMin;
		[FieldOffset(52)]
		public int PhysicalMax;
		[FieldOffset(56)]
		public HIDP_Range Range;
		[FieldOffset(56)]
		public HIDP_NotRange NotRange;
	}

	[StructLayout(LayoutKind.Sequential, CharSet = CharSet.Ansi)]
	struct HID_DEVICE
	{
		public String Manufacturer;
		public String Product;
		public Int32 SerialNumber;
		public UInt16 VersionNumber;

		[MarshalAs(UnmanagedType.ByValTStr, SizeConst = DEVICE_PATH)]
		public String DevicePath;
		public IntPtr Pointer;

		public Boolean OpenedForRead;
		public Boolean OpenedForWrite;
		public Boolean OpenedOverlapped;
		public Boolean OpenedExclusive;

		public IntPtr Ppd;
		public HIDP_CAPS Caps;
		public HIDD_ATTRIBUTES Attributes;

		public IntPtr[] InputReportBuffer;
		public HID_DATA[] InputData;
		public Int32 InputDataLength;
		public HIDP_BUTTON_CAPS[] InputButtonCaps;
		public HIDP_VALUE_CAPS[] InputValueCaps;

		public IntPtr[] OutputReportBuffer;
		public HID_DATA[] OutputData;
		public Int32 OutputDataLength;
		public HIDP_BUTTON_CAPS[] OutputButtonCaps;
		public HIDP_VALUE_CAPS[] OutputValueCaps;

		public IntPtr[] FeatureReportBuffer;
		public HID_DATA[] FeatureData;
		public Int32 FeatureDataLength;
		public HIDP_BUTTON_CAPS[] FeatureButtonCaps;
		public HIDP_VALUE_CAPS[] FeatureValueCaps;
	}

	#endregion

	struct HIDReadData
	{
		public static Boolean State;

		public static HID_DEVICE[] Device;
		public static Int32 iDevice;

		public static UInt16 VendorID;
		public static UInt16 ProductID;

	}
	public struct HWInterface
	{
		public static int TotalHIDDevices;
		public static int YourHIDDevice;
		public static string RawData;
	}
	private HID_DEVICE _device;
	private CancellationTokenSource _cts;
	private Task _readLoopTask;

	// -----------------------------
	// START ASYNC READ LOOP
	// -----------------------------
	public GF48ProtocolHandler()
	{
		ConnectionInfo connectionInfo = new ConnectionInfo();
		connectionInfo.GetConfiguration();

		UInt16.TryParse(connectionInfo.VID, NumberStyles.HexNumber, CultureInfo.InvariantCulture, out HIDReadData.VendorID);
		UInt16.TryParse(connectionInfo.PID, NumberStyles.HexNumber, CultureInfo.InvariantCulture, out HIDReadData.ProductID);
		CheckHIDRead(); // run outside of thread to determine total USB devices, and your device location
		HIDReadData.Device = new HID_DEVICE[HWInterface.TotalHIDDevices]; // total USB devices
		HIDReadData.iDevice = FindKnownHIDDevice(ref HIDReadData.Device, HWInterface.YourHIDDevice); // device num of your USB device

		for (int Index = 0; Index < HWInterface.TotalHIDDevices; Index++)
		{
			if (HIDReadData.Device[Index].Attributes.VendorID == HIDReadData.VendorID)
			{
				if (HIDReadData.Device[Index].Attributes.ProductID == HIDReadData.ProductID)
				{
					_device = HIDReadData.Device[Index];
					break;
				}
			}
		}

		if (_readLoopTask != null)
			return;

		_cts = new CancellationTokenSource();
		_readLoopTask = Task.Run(() => ReadLoopAsync(_cts.Token));
	}
	// -----------------------------
	// ASYNC READ LOOP
	// -----------------------------
	private async Task ReadLoopAsync(CancellationToken token)
	{
		var reportLength = _device.Caps.InputReportByteLength;
		var buffer = new byte[reportLength];

		while (!token.IsCancellationRequested)
		{
			try
			{
				uint bytesRead = 0;

				bool ok = ReadFile(
					_device.Pointer,
					buffer,
					(uint)reportLength,
					ref bytesRead,
					IntPtr.Zero
				);

				if (ok && bytesRead > 0)
				{
					var actual = new byte[bytesRead];
					Array.Copy(buffer, actual, bytesRead);

						ReportReceived?.Invoke(actual);
				}

				//await Task.Delay(1, token);
			}
			catch (OperationCanceledException)
			{
				break;
			}
			catch (Exception ex)
			{
				Console.WriteLine($"[HID] Read error: {ex.Message}");
				await Task.Delay(10, token);
			}
		}
	}

	// -----------------------------
	// SEND HID REPORT
	// -----------------------------
	public bool SendReport(byte[] report)
	{
		uint written = 0;

		return WriteFile(
			_device.Pointer,
			report,
			(uint)report.Length,
			ref written,
			IntPtr.Zero
		);
	}

	void CheckHIDRead()
	{
		if (HIDReadData.State == false)
		{
			var nbrDevice = FindDeviceNumber();
			HIDReadData.Device = new HID_DEVICE[nbrDevice];
			FindKnownHIDDevices(ref HIDReadData.Device);
			HWInterface.TotalHIDDevices = nbrDevice;
			for (var Index = 0; Index < nbrDevice; Index++)
			{
				if (HIDReadData.VendorID != 0)
				{
					var Count = 0;

					if (HIDReadData.Device[Index].Attributes.VendorID == HIDReadData.VendorID)
					{
						Count++;
					}
					if (HIDReadData.Device[Index].Attributes.ProductID == HIDReadData.ProductID)
					{
						Count++;
					}

					if (Count == 2)
					{
						HWInterface.YourHIDDevice = Index;
						HIDReadData.iDevice = Index;
						HIDReadData.State = true;
						Console.WriteLine("Device found!");
						break;
					}
					else
					{
						HIDReadData.State = false;
					}
				}
			}
		}
	}
	Int32 FindDeviceNumber()
	{
		var hidGuid = new Guid();
		var deviceInfoData = new SP_DEVICE_INTERFACE_DATA();

		HidD_GetHidGuid(ref hidGuid);

		//
		// Open a handle to the plug and play dev node.
		//
		SetupDiDestroyDeviceInfoList(hardwareDeviceInfo);
		hardwareDeviceInfo = SetupDiGetClassDevs(ref hidGuid, IntPtr.Zero, IntPtr.Zero, DIGCF_PRESENT | DIGCF_DEVICEINTERFACE);
		deviceInfoData.cbSize = Marshal.SizeOf(typeof(SP_DEVICE_INTERFACE_DATA));

		var Index = 0;
		while (SetupDiEnumDeviceInterfaces(hardwareDeviceInfo, IntPtr.Zero, ref hidGuid, Index, ref deviceInfoData))
		{
			Index++;
		}

		return (Index);
	}
	Int32 FindKnownHIDDevice(ref HID_DEVICE[] HID_Devices, int iDevice)
	{
		var hidGuid = new Guid();
		var deviceInfoData = new SP_DEVICE_INTERFACE_DATA();
		var functionClassDeviceData = new SP_DEVICE_INTERFACE_DETAIL_DATA();

		HidD_GetHidGuid(ref hidGuid);

		//
		// Open a handle to the plug and play dev node.
		//
		SetupDiDestroyDeviceInfoList(hardwareDeviceInfo);
		hardwareDeviceInfo = SetupDiGetClassDevs(ref hidGuid, IntPtr.Zero, IntPtr.Zero, DIGCF_PRESENT | DIGCF_DEVICEINTERFACE);
		deviceInfoData.cbSize = Marshal.SizeOf(typeof(SP_DEVICE_INTERFACE_DATA));

		var iHIDD = iDevice;
		while (SetupDiEnumDeviceInterfaces(hardwareDeviceInfo, IntPtr.Zero, ref hidGuid, iHIDD, ref deviceInfoData))
		{
			var RequiredLength = 0;

			//
			// Allocate a function class device data structure to receive the
			// goods about this particular device.
			//
			SetupDiGetDeviceInterfaceDetail(hardwareDeviceInfo, ref deviceInfoData, IntPtr.Zero, 0, ref RequiredLength, IntPtr.Zero);

			if (IntPtr.Size == 8)
			{
				functionClassDeviceData.cbSize = 8;
			}
			else if (IntPtr.Size == 4)
			{
				functionClassDeviceData.cbSize = 5;
			}

			//
			// Retrieve the information from Plug and Play.
			//
			SetupDiGetDeviceInterfaceDetail(hardwareDeviceInfo, ref deviceInfoData, ref functionClassDeviceData, RequiredLength, ref RequiredLength, IntPtr.Zero);

			//
			// Open device with just generic query abilities to begin with
			//
			OpenHIDDevice(functionClassDeviceData.DevicePath, ref HID_Devices, iHIDD);
			break;
		}
		return iHIDD;
	}

	void OpenHIDDevice(String DevicePath, ref HID_DEVICE[] HID_Device, Int32 iHIDD)
	{
		//
		// RoutineDescription:
		// Given the HardwareDeviceInfo, representing a handle to the plug and
		// play information, and deviceInfoData, representing a specific hid device,
		// open that device and fill in all the relivant information in the given
		// HID_DEVICE structure.
		//
		HID_Device[iHIDD].DevicePath = DevicePath;

		//
		// The hid.dll api's do not pass the overlapped structure into deviceiocontrol
		// so to use them we must have a non overlapped device.  If the request is for
		// an overlapped device we will close the device below and get a handle to an
		// overlapped device
		//
		CloseHandle(HID_Device[iHIDD].Pointer);
		HID_Device[iHIDD].Pointer = CreateFile(HID_Device[iHIDD].DevicePath, GENERIC_READ | GENERIC_WRITE, FILE_SHARE_READ | FILE_SHARE_WRITE, 0, OPEN_EXISTING, 0, IntPtr.Zero);
		HID_Device[iHIDD].Caps = new HIDP_CAPS();
		HID_Device[iHIDD].Attributes = new HIDD_ATTRIBUTES();

		//
		// If the device was not opened as overlapped, then fill in the rest of the
		// HID_Device structure.  However, if opened as overlapped, this handle cannot
		// be used in the calls to the HidD_ exported functions since each of these
		// functions does synchronous I/O.
		//
		HidD_FreePreparsedData(ref HID_Device[iHIDD].Ppd);
		HID_Device[iHIDD].Ppd = IntPtr.Zero;

		HidD_GetPreparsedData(HID_Device[iHIDD].Pointer, ref HID_Device[iHIDD].Ppd);
		HidD_GetAttributes(HID_Device[iHIDD].Pointer, ref HID_Device[iHIDD].Attributes);
		HidP_GetCaps(HID_Device[iHIDD].Ppd, ref HID_Device[iHIDD].Caps);

		var Buffer = Marshal.AllocHGlobal(126);
		{
			if (HidD_GetManufacturerString(HID_Device[iHIDD].Pointer, Buffer, 126))
			{
				HID_Device[iHIDD].Manufacturer = Marshal.PtrToStringAuto(Buffer);
			}
			if (HidD_GetProductString(HID_Device[iHIDD].Pointer, Buffer, 126))
			{
				HID_Device[iHIDD].Product = Marshal.PtrToStringAuto(Buffer);
			}
			if (HidD_GetSerialNumberString(HID_Device[iHIDD].Pointer, Buffer, 126))
			{
				Int32.TryParse(Marshal.PtrToStringAuto(Buffer), out HID_Device[iHIDD].SerialNumber);
			}
		}
		Marshal.FreeHGlobal(Buffer);

		//
		// At this point the client has a choice.  It may chose to look at the
		// Usage and Page of the top level collection found in the HIDP_CAPS
		// structure.  In this way --------*it could just use the usages it knows about.
		// If either HidP_GetUsages or HidP_GetUsageValue return an error then
		// that particular usage does not exist in the report.
		// This is most likely the preferred method as the application can only
		// use usages of which it already knows.
		// In this case the app need not even call GetButtonCaps or GetValueCaps.
		//
		// In this example, however, wSendHID_PIDe will call FillDeviceInfo to look for all
		//    of the usages in the device.
		//
		//FillDeviceInfo(ref HID_Device, iHIDD);
	}
	Int32 FindKnownHIDDevices(ref HID_DEVICE[] HID_Devices)
	{
		var hidGuid = new Guid();
		var deviceInfoData = new SP_DEVICE_INTERFACE_DATA();
		var functionClassDeviceData = new SP_DEVICE_INTERFACE_DETAIL_DATA();

		HidD_GetHidGuid(ref hidGuid);

		//
		// Open a handle to the plug and play dev node.
		//
		SetupDiDestroyDeviceInfoList(hardwareDeviceInfo);
		hardwareDeviceInfo = SetupDiGetClassDevs(ref hidGuid, IntPtr.Zero, IntPtr.Zero, DIGCF_PRESENT | DIGCF_DEVICEINTERFACE);
		deviceInfoData.cbSize = Marshal.SizeOf(typeof(SP_DEVICE_INTERFACE_DATA));

		var iHIDD = 0;
		while (SetupDiEnumDeviceInterfaces(hardwareDeviceInfo, IntPtr.Zero, ref hidGuid, iHIDD, ref deviceInfoData))
		{
			var RequiredLength = 0;

			//
			// Allocate a function class device data structure to receive the
			// goods about this particular device.
			//
			SetupDiGetDeviceInterfaceDetail(hardwareDeviceInfo, ref deviceInfoData, IntPtr.Zero, 0, ref RequiredLength, IntPtr.Zero);

			if (IntPtr.Size == 8)
			{
				functionClassDeviceData.cbSize = 8;
			}
			else if (IntPtr.Size == 4)
			{
				functionClassDeviceData.cbSize = 5;
			}

			//
			// Retrieve the information from Plug and Play.
			//
			SetupDiGetDeviceInterfaceDetail(hardwareDeviceInfo, ref deviceInfoData, ref functionClassDeviceData, RequiredLength, ref RequiredLength, IntPtr.Zero);

			//
			// Open device with just generic query abilities to begin with
			//
			OpenHIDDevice(functionClassDeviceData.DevicePath, ref HID_Devices, iHIDD);

			iHIDD++;
		}

		return iHIDD;
	}

	// -----------------------------
	// STOP + CLEANUP
	// -----------------------------
	public void Stop()
	{
		if (_cts == null)
			return;

		_cts.Cancel();

		try
		{
			_readLoopTask?.Wait(200);
		}
		catch { }

		_cts.Dispose();
		_cts = null;
		_readLoopTask = null;
	}

	public void Dispose()
	{
		if (_device.Pointer != IntPtr.Zero)
		{
			HidD_FreePreparsedData(ref _device.Ppd);
			_device.Ppd = IntPtr.Zero;
			_device.Pointer = IntPtr.Zero;
			CloseHandle(_device.Pointer);
		}
		Stop();
	}

	// -----------------------------
	// P/Invoke
	// -----------------------------
	[DllImport("kernel32.dll", SetLastError = true)]
	private static extern bool ReadFile(
		IntPtr hFile,
		[Out] byte[] lpBuffer,
		uint nNumberOfBytesToRead,
		ref uint lpNumberOfBytesRead,
		IntPtr lpOverlapped);

	[DllImport("kernel32.dll", SetLastError = true)]
	private static extern bool WriteFile(
		IntPtr hFile,
		byte[] lpBuffer,
		uint nNumberOfBytesToWrite,
		ref uint lpNumberOfBytesWritten,
		IntPtr lpOverlapped);
}