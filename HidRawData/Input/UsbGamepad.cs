namespace Djlastnight.Input;

using Microsoft.Win32;
using System;
using System.Linq;

public class UsbGamepad : IIinputDevice
{
    internal UsbGamepad(string deviceId, string description)
    {
        if (deviceId == null)
        {
            throw new ArgumentNullException("deviceId");
        }

        if (description == null)
        {
            throw new ArgumentNullException("description");
        }

        DeviceID = deviceId;
        Description = description;

        int vidIndex = DeviceID.IndexOf("VID_");
        if (vidIndex == -1)
        {
            Vendor = null;
        }
        else
        {
            string startingAtVid = DeviceID.Substring(vidIndex + 4);
            Vendor = startingAtVid.Substring(0, 4);
        }

        int pidIndex = DeviceID.IndexOf("PID_");
        if (pidIndex == -1)
        {
            Product = null;
        }
        else
        {
            string startingAtPid = DeviceID.Substring(pidIndex + 4);
            Product = startingAtPid.Substring(0, 4);
        }

        var cleanDeviceId = string.Format("VID_{0}&PID_{1}", Vendor, Product);

        // Getting the gamepad name from the registry
        using (var rootKey = Registry.CurrentUser.OpenSubKey(@"SYSTEM\CurrentControlSet\Control\MediaProperties\PrivateProperties\Joystick\OEM\"))
        {
            if (rootKey == null)
            {
                return;
            }

            var subKeyNames = rootKey.GetSubKeyNames();
            if (subKeyNames.Contains(cleanDeviceId))
            {
                using (var key = rootKey.OpenSubKey(cleanDeviceId))
                {
                    if (key.GetValueNames().Contains("OEMName"))
                    {
                        FriendlyName = (string)key.GetValue("OEMName");
                    }
                }
            }
        }
    }

    public string DeviceID
    {
        get;
        private set;
    }

    public string Description
    {
        get;
        private set;
    }

    public string Vendor
    {
        get;
        private set;
    }

    public string Product
    {
        get;
        private set;
    }

    public string FriendlyName
    {
        get;
        private set;
    }

    public DeviceType DeviceType
    {
        get { return DeviceType.Gamepad; }
    }

    public override string ToString()
    {
        return string.Format(
            "USB Gamepad:{0}Friendly name: {1}{0}DeviceID: {2}{0}VID: {3}{0}PID: {4}{0}Description: {5}",
            Environment.NewLine,
            FriendlyName,
            DeviceID,
            Vendor,
            Product,
            Description);
    }
}