using System;
using System.Collections.Generic;

/// <summary>
/// A wrapper class for holding a list of discovered Bluetooth device names.
/// Used to store the list of devices that the Bluetooth manager has discovered.
/// </summary>
[Serializable]
public class DeviceListWrapper
{
    // A list of device names discovered via Bluetooth
    public List<string> deviceNames = new();
}
