namespace Djlastnight.Input.Internal;

using System;

internal class Win32_PnPEntity
{
    public ushort Availability { get; private set; }

    public string Caption { get; private set; }

    public string ClassGuid { get; private set; }

    public string[] CompatibleID { get; private set; }

    public uint ConfigManagerErrorCode { get; private set; }

    public bool ConfigManagerUserConfig { get; private set; }

    public string CreationClassName { get; private set; }

    public string Description { get; private set; }

    public string DeviceID { get; private set; }

    public bool ErrorCleared { get; private set; }

    public string ErrorDescription { get; private set; }

    public string[] HardwareID { get; private set; }

    public DateTime InstallDate { get; private set; }

    public uint LastErrorCode { get; private set; }

    public string Manufacturer { get; private set; }

    public string Name { get; private set; }

    public string PNPClass { get; private set; }

    public string PNPDeviceID { get; private set; }

    public ushort[] PowerManagementCapabilities { get; private set; }

    public bool PowerManagementSupported { get; private set; }

    public bool Present { get; private set; }

    public string Service { get; private set; }

    public string Status { get; private set; }

    public ushort StatusInfo { get; private set; }

    public string SystemCreationClassName { get; private set; }

    public string SystemName { get; private set; }

    public void SetValue(string propertyName, object value)
    {
        switch (propertyName)
        {
            case "Availability":
                Availability = (ushort)value;
                break;
            case "Caption":
                Caption = (string)value;
                break;
            case "ClassGuid":
                ClassGuid = (string)value;
                break;
            case "CompatibleID":
                CompatibleID = (string[])value;
                break;
            case "ConfigManagerErrorCode":
                ConfigManagerErrorCode = (uint)value;
                break;
            case "ConfigManagerUserConfig":
                ConfigManagerUserConfig = (bool)value;
                break;
            case "CreationClassName":
                CreationClassName = (string)value;
                break;
            case "Description":
                Description = (string)value;
                break;
            case "DeviceID":
                DeviceID = (string)value;
                break;
            case "ErrorCleared":
                ErrorCleared = (bool)value;
                break;
            case "ErrorDescription":
                ErrorDescription = (string)value;
                break;
            case "HardwareID":
                HardwareID = (string[])value;
                break;
            case "InstallDate":
                InstallDate = (DateTime)value;
                break;
            case "LastErrorCode":
                LastErrorCode = (uint)value;
                break;
            case "Manufacturer":
                Manufacturer = (string)value;
                break;
            case "Name":
                Name = (string)value;
                break;
            case "PNPClass":
                PNPClass = (string)value;
                break;
            case "PNPDeviceID":
                PNPDeviceID = (string)value;
                break;
            case "PowerManagementCapabilities":
                PowerManagementCapabilities = (ushort[])value;
                break;
            case "PowerManagementSupported":
                PowerManagementSupported = (bool)value;
                break;
            case "Present":
                Present = (bool)value;
                break;
            case "Service":
                Service = (string)value;
                break;
            case "Status":
                Status = (string)value;
                break;
            case "StatusInfo":
                StatusInfo = (ushort)value;
                break;
            case "SystemCreationClassName":
                SystemCreationClassName = (string)value;
                break;
            case "SystemName":
                SystemName = (string)value;
                break;
            default:
                throw new NotImplementedException(string.Format("Non existring property: '{0}'", propertyName));
        }
    }
}