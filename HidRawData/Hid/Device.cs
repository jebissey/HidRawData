namespace Djlastnight.Hid;

using Djlastnight.Hid.UsageCollection;
using Djlastnight.Win32.Win32CreateFile;
using Djlastnight.Win32.Win32Hid;
using Djlastnight.Win32.Win32RawInput;
using System;
using System.Runtime.InteropServices;
using System.Text;

public class Device : IDisposable
{
    private RID_DEVICE_INFO info;
    private HIDP_CAPS capabilities;
    private HIDP_BUTTON_CAPS[] inputButtonCapabilities;
    private HIDP_VALUE_CAPS[] inputValueCapabilities;

    /// <summary>
    /// Class constructor will fetch this object properties from HID sub system.
    /// </summary>
    /// <param name="handleToRawInputDevice">Device Handle as provided by RAWINPUTHEADER.hDevice, typically accessed as rawinput.header.hDevice</param>
    internal Device(IntPtr handleToRawInputDevice)
    {
        // Try construct and rollback if needed
        try
        {
            Construct(handleToRawInputDevice);
        }
        catch (Exception ex)
        {
            // Just rollback and propagate
            Dispose();
            throw ex;
        }
    }

    /// <summary>
    /// Make sure dispose is called even if the user forgot about it.
    /// </summary>
    ~Device()
    {
        Dispose();
    }

    /// <summary>
    /// Unique name of that HID device.
    /// Notably used as input to CreateFile.
    /// </summary>
    public string Name { get; private set; }

    /// <summary>
    /// Friendly name that people should be able to read.
    /// </summary>
    public string FriendlyName { get; private set; }

    public string Manufacturer { get; private set; }

    public string Product { get; private set; }

    public ushort VendorId { get; private set; }

    public ushort ProductId { get; private set; }

    public ushort Version { get; private set; }

    public IntPtr PreParsedData { get; private set; }

    public RID_DEVICE_INFO Info
    {
        get
        {
            return info;
        }
    }

    public HIDP_CAPS Capabilities
    {
        get
        {
            return capabilities;
        }
    }

    public string InputCapabilitiesDescription { get; private set; }

    public HIDP_BUTTON_CAPS[] InputButtonCapabilities
    {
        get
        {
            return inputButtonCapabilities;
        }
    }

    public HIDP_VALUE_CAPS[] InputValueCapabilities
    {
        get
        {
            return inputValueCapabilities;
        }
    }

    public int ButtonCount { get; private set; }

    public bool IsGamePad
    {
        get
        {
            return (UsagePage)capabilities.UsagePage == Hid.UsagePage.GenericDesktopControls && (GenericDesktop)capabilities.Usage == GenericDesktop.GamePad;
        }
    }

    public bool IsMouse
    {
        get
        {
            return Info.dwType == RawInputDeviceType.RIM_TYPEMOUSE;
        }
    }

    public bool IsKeyboard
    {
        get
        {
            return Info.dwType == RawInputDeviceType.RIM_TYPEKEYBOARD;
        }
    }

    public bool IsHid
    {
        get
        {
            return Info.dwType == RawInputDeviceType.RIM_TYPEHID;
        }
    }

    /// <summary>
    /// 
    /// </summary>
    public ushort UsagePage
    {
        get
        {
            if (Info.dwType == RawInputDeviceType.RIM_TYPEHID)
            {
                // Generic HID
                return Info.hid.usUsagePage;
            }
            else if (Info.dwType == RawInputDeviceType.RIM_TYPEKEYBOARD)
            {
                // Keyboard
                return (ushort)Hid.UsagePage.GenericDesktopControls;
            }
            else if (Info.dwType == RawInputDeviceType.RIM_TYPEMOUSE)
            {
                // Mouse
                return (ushort)Hid.UsagePage.GenericDesktopControls;
            }

            return 0;
        }
    }

    /// <summary>
    /// 
    /// </summary>
    public ushort UsageCollection
    {
        get
        {
            if (Info.dwType == RawInputDeviceType.RIM_TYPEHID)
            {
                // Generic HID
                return Info.hid.usUsage;
            }
            else if (Info.dwType == RawInputDeviceType.RIM_TYPEKEYBOARD)
            {
                // Keyboard
                return (ushort)GenericDesktop.Keyboard;
            }
            else if (Info.dwType == RawInputDeviceType.RIM_TYPEMOUSE)
            {
                // Mouse
                return (ushort)GenericDesktop.Mouse;
            }

            return 0;
        }
    }

    public uint UsageId
    {
        get
        {
            return (uint)(UsagePage << 16) | UsageCollection;
        }
    }

    /// <summary>
    /// Provide a description for the given capabilities.
    /// Notably describes axis on a gamepad/joystick.
    /// </summary>
    /// <param name="caps"></param>
    /// <returns></returns>
    public static string InputValueCapabilityDescription(HIDP_VALUE_CAPS caps)
    {
        if (!caps.IsRange && Enum.IsDefined(typeof(UsagePage), caps.UsagePage))
        {
            Type usageType = Utils.UsageType((UsagePage)caps.UsagePage);
            if (usageType == null)
            {
                return "Input Value: " + Enum.GetName(typeof(UsagePage), caps.UsagePage) + " Usage 0x" + caps.NotRange.Usage.ToString("X2");
            }

            string name = Enum.GetName(usageType, caps.NotRange.Usage);
            if (name == null)
            {
                // Could not find that usage in our enum.
                // Provide a relevant warning instead.
                name = "Usage 0x" + caps.NotRange.Usage.ToString("X2") + " not defined in " + usageType.Name;
            }
            else
            {
                // Prepend our usage type name
                name = usageType.Name + "." + name;
            }

            return "Input Value: " + name;
        }

        return null;
    }

    /// <summary>
    /// Dispose is just for unmanaged clean-up.
    /// Make sure calling disposed multiple times does not crash.
    /// See: http://stackoverflow.com/questions/538060/proper-use-of-the-idisposable-interface/538238#538238
    /// </summary>
    public void Dispose()
    {
        Marshal.FreeHGlobal(PreParsedData);
        PreParsedData = IntPtr.Zero;
    }

    public override string ToString()
    {
        return "HID Device: " + FriendlyName;
    }

    /// <summary>
    /// Private constructor.
    /// </summary>
    /// <param name="handleToRawInputDevice"></param>
    private void Construct(IntPtr handleToRawInputDevice)
    {
        PreParsedData = IntPtr.Zero;
        inputButtonCapabilities = null;
        inputValueCapabilities = null;

        // Fetch various information defining the given HID device
        Name = RawInputHelper.GetDeviceName(handleToRawInputDevice);

        // Fetch device info
        info = new RID_DEVICE_INFO();
        if (!RawInputHelper.GetDeviceInfo(handleToRawInputDevice, ref info))
        {
            throw new Exception("HidDevice: GetDeviceInfo failed: " + Marshal.GetLastWin32Error().ToString());
        }

        // Open our device from the device name/path
        var handle = Win32.Win32CreateFile.NativeMethods.CreateFile(
            Name,
            FileAccess.NONE,
            FileShare.FILE_SHARE_READ | FileShare.FILE_SHARE_WRITE,
            IntPtr.Zero,
            CreationDisposition.OPEN_EXISTING,
            FileFlagsAttributes.FILE_FLAG_OVERLAPPED,
            IntPtr.Zero);

        // Check if CreateFile worked
        if (handle.IsInvalid)
        {
            throw new Exception("HidDevice: CreateFile failed: " + Marshal.GetLastWin32Error().ToString());
        }

        // Get manufacturer string
        var manufacturerString = new StringBuilder(256);
        if (Win32.Win32Hid.NativeMethods.HidD_GetManufacturerString(handle, manufacturerString, manufacturerString.Capacity))
        {
            Manufacturer = manufacturerString.ToString();
        }

        // Get product string
        StringBuilder productString = new StringBuilder(256);
        if (Win32.Win32Hid.NativeMethods.HidD_GetProductString(handle, productString, productString.Capacity))
        {
            Product = productString.ToString();
        }

        // Get attributes
        HIDD_ATTRIBUTES attributes = new HIDD_ATTRIBUTES();
        if (Win32.Win32Hid.NativeMethods.HidD_GetAttributes(handle, ref attributes))
        {
            VendorId = attributes.VendorID;
            ProductId = attributes.ProductID;
            Version = attributes.VersionNumber;
        }

        handle.Close();

        SetFriendlyName();

        // Get our HID descriptor pre-parsed data
        PreParsedData = RawInputHelper.GetPreParsedData(handleToRawInputDevice);

        if (PreParsedData == IntPtr.Zero)
        {
            // We are done then. Some devices don't have pre-parsed data.
            return;
        }

        // Get capabilities
        var status = Win32.Win32Hid.NativeMethods.HidP_GetCaps(PreParsedData, ref capabilities);
        if (status != HidStatus.HIDP_STATUS_SUCCESS)
        {
            throw new Exception("HidDevice: HidP_GetCaps failed: " + status.ToString());
        }

        SetInputCapabilitiesDescription();

        // Get input button caps if needed
        if (Capabilities.NumberInputButtonCaps > 0)
        {
            inputButtonCapabilities = new HIDP_BUTTON_CAPS[Capabilities.NumberInputButtonCaps];
            ushort buttonCapabilitiesLength = Capabilities.NumberInputButtonCaps;
            status = Win32.Win32Hid.NativeMethods.HidP_GetButtonCaps(HIDP_REPORT_TYPE.HidP_Input, inputButtonCapabilities, ref buttonCapabilitiesLength, PreParsedData);
            if (status != HidStatus.HIDP_STATUS_SUCCESS || buttonCapabilitiesLength != Capabilities.NumberInputButtonCaps)
            {
                throw new Exception("HidDevice: HidP_GetButtonCaps failed: " + status.ToString());
            }

            ComputeButtonCount();
        }

        // Get input value caps if needed
        if (Capabilities.NumberInputValueCaps > 0)
        {
            inputValueCapabilities = new HIDP_VALUE_CAPS[Capabilities.NumberInputValueCaps];
            ushort valueCapabilitiesLength = Capabilities.NumberInputValueCaps;
            status = Win32.Win32Hid.NativeMethods.HidP_GetValueCaps(HIDP_REPORT_TYPE.HidP_Input, inputValueCapabilities, ref valueCapabilitiesLength, PreParsedData);
            if (status != HidStatus.HIDP_STATUS_SUCCESS || valueCapabilitiesLength != Capabilities.NumberInputValueCaps)
            {
                throw new Exception("HidDevice: HidP_GetValueCaps failed: " + status.ToString());
            }
        }
    }

    /// <summary>
    /// Useful for gamepads.
    /// </summary>
    private void ComputeButtonCount()
    {
        ButtonCount = 0;
        foreach (HIDP_BUTTON_CAPS bc in inputButtonCapabilities)
        {
            if (bc.IsRange)
            {
                ButtonCount += bc.Range.UsageMax - bc.Range.UsageMin + 1;
            }
        }
    }

    /// <summary>
    /// 
    /// </summary>
    private void SetInputCapabilitiesDescription()
    {
        InputCapabilitiesDescription = string.Format(
            "[ Input Capabilities ] Button: {0} - Value: {1} - Data indices: {2}",
            Capabilities.NumberInputButtonCaps,
            Capabilities.NumberInputValueCaps,
            Capabilities.NumberInputDataIndices);
    }

    /// <summary>
    /// 
    /// </summary>
    private void SetFriendlyName()
    {
        /* Work out proper suffix for our device root node.
         * That allows users to see in a glance what kind of device this is.
        */
        string suffix = string.Empty;
        Type usageCollectionType = null;
        if (Info.dwType == RawInputDeviceType.RIM_TYPEHID)
        {
            // Process usage page
            if (Enum.IsDefined(typeof(UsagePage), Info.hid.usUsagePage))
            {
                // We know this usage page, add its name
                var usagePage = (UsagePage)Info.hid.usUsagePage;
                suffix += " ( " + usagePage.ToString() + ", ";
                usageCollectionType = Utils.UsageCollectionType(usagePage);
            }
            else
            {
                // We don't know this usage page, add its value
                suffix += " ( 0x" + Info.hid.usUsagePage.ToString("X4") + ", ";
            }

            /*Process usage collection
            We don't know this usage page, add its value */
            if (usageCollectionType == null || !Enum.IsDefined(usageCollectionType, Info.hid.usUsage))
            {
                // Show Hexa
                suffix += "0x" + Info.hid.usUsage.ToString("X4") + " )";
            }
            else
            {
                // We know this usage page, add its name
                suffix += Enum.GetName(usageCollectionType, Info.hid.usUsage) + " )";
            }
        }
        else if (Info.dwType == RawInputDeviceType.RIM_TYPEKEYBOARD)
        {
            suffix = " - Keyboard";
        }
        else if (Info.dwType == RawInputDeviceType.RIM_TYPEMOUSE)
        {
            suffix = " - Mouse";
        }

        if (Product != null && Product.Length > 1)
        {
            // This device as a proper name, use it
            FriendlyName = Product + suffix;
        }
        else
        {
            // Extract friendly name from name
            char[] delimiterChars = { '#', '&' };
            string[] words = Name.Split(delimiterChars);
            if (words.Length >= 2)
            {
                // Use our name sub-string to describe this device
                FriendlyName = words[1] + " - 0x" + ProductId.ToString("X4") + suffix;
            }
            else
            {
                // No proper name just use the device ID instead
                FriendlyName = "0x" + ProductId.ToString("X4") + suffix;
            }
        }
    }
}