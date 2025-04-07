using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Text;

#if UNITY_EDITOR_OSX || UNITY_IOS
using UnityCoreBluetooth;

/// <summary>
/// Manages Bluetooth device scanning, connection, and communication using the CoreBluetoothManager.
/// This class handles scanning for available devices, displaying them in a UI, 
/// and managing connections and data transfers to peripherals.
/// </summary>
public class BluetoothDeviceManager : MonoBehaviour
{
    #region UI Components

    /// <summary>
    /// The content object in the ScrollView where device buttons will be displayed.
    /// </summary>
    [SerializeField] private GameObject scrollViewContent;

    /// <summary>
    /// Prefab used to create buttons for each discovered Bluetooth device in the list.
    /// </summary>
    [SerializeField] private GameObject buttonPrefab;
    
    /// <summary>
    /// The button used to trigger a re-scan of Bluetooth devices.
    /// </summary>
    [SerializeField] private Button scanButton;
    
    #endregion

    #region Bluetooth Manager

    /// <summary>
    /// The instance of the CoreBluetoothManager responsible for managing Bluetooth operations like scanning, connecting, and communication.
    /// </summary>
    private CoreBluetoothManager manager; 
    
    #endregion

    #region Discovered Devices

    /// <summary>
    /// A wrapper to hold discovered device names for display and use in connection attempts.
    /// </summary>
    private readonly DeviceListWrapper discoveredDevices = new();
    
    /// <summary>
    /// A dictionary that holds the discovered peripheral devices by their name as the key.
    /// </summary>
    private Dictionary<string, CoreBluetoothPeripheral> discoveredPeripherals = new();
    
    #endregion

    #region Device Identification

    /// <summary>
    /// UUID for a custom Bluetooth characteristic that will be searched for on connected peripherals.
    /// </summary>
    public static readonly Guid CustomCharacteristicUuid = new("72737C42-0FC3-49C6-B27E-8D19D6A0C1FA");

    /// <summary>
    /// A list of specific device names to look for when scanning for Bluetooth devices. 
    /// Devices not in this list will be displayed in gray, while those in the list will be displayed in black.
    /// </summary>
    [SerializeField] private List<string> specificNamesList = new() { "Device1" };

    /// <summary>
    /// A hashset for runtime efficient lookups of device names that are considered specific devices of interest.
    /// </summary>
    private HashSet<string> specificNames;
    
    #endregion

    #region Unity Lifecycle Methods

    /// <summary>
    /// Initializes the Bluetooth manager and starts scanning for devices.
    /// </summary>
    void Start()
    {
        // Convert List to HashSet at runtime for efficient lookups
        specificNames = new HashSet<string>(specificNamesList);

        // Initialize Bluetooth manager and start scanning
        InitializeBluetoothManager();
        StartScan();
    }

    /// <summary>
    /// Stops the Bluetooth manager when the object is destroyed.
    /// </summary>
    void OnDestroy()
    {
        // Unsubscribe from events to avoid memory leaks
        manager?.Stop();
    }

    #endregion

    #region Bluetooth Scanning & Management

    /// <summary>
    /// Starts scanning for Bluetooth devices, clearing previous device lists and resetting the UI.
    /// </summary>
    public void StartScan()
    {
        // Clear existing list, reset discovered devices and dictionary
        ResetDeviceList();
        
        // Start scanning for devices again
        manager.StartScan();
        Debug.Log("Scanning for devices...");
    }

    /// <summary>
    /// Initializes the Bluetooth manager and sets up necessary callbacks for scanning, connecting, and data handling.
    /// </summary>
    private void InitializeBluetoothManager()
    {
        // Initialize Bluetooth manager
        manager = CoreBluetoothManager.Shared;

        // Set up Bluetooth state update callback
        manager.OnUpdateState(state =>
        {
            Debug.Log("Bluetooth state: " + state);
            if (state == "poweredOn")
            {
                manager.StartScan();
            }
        });

        // Set up peripheral discovery callback
        manager.OnDiscoverPeripheral(peripheral =>
        {
            if (!string.IsNullOrEmpty(peripheral.name) && !discoveredDevices.deviceNames.Contains(peripheral.name))
            {
                discoveredDevices.deviceNames.Add(peripheral.name);
                discoveredPeripherals[peripheral.name] = peripheral; // Add to the dictionary
                AddDeviceToList(peripheral.name);
                Debug.Log("Discovered device: " + peripheral.name);
            }
        });

        // Set up connection callback
        manager.OnConnectPeripheral(peripheral =>
        {
            Debug.Log("Successfully connected to peripheral: " + peripheral.name);
            DiscoverPeripheralServices(peripheral);
        });

        // Set up value update callback
        manager.OnUpdateValue((characteristic, data) =>
        {
            Debug.Log("Received data from characteristic " + characteristic.Uuid + ": " + BitConverter.ToString(data));
            // Process the received data as needed
        });

        // Start the Bluetooth manager
        manager.Start();

        // Assign button functionality
        scanButton.onClick.AddListener(() =>
        {
            InitializeBluetoothManager(); // Reinitialize callbacks before scanning
            StartScan();
        });
    }

    #endregion

    #region Device Connection & Communication

    /// <summary>
    /// Discover services for the connected peripheral.
    /// </summary>
    /// <param name="peripheral">The peripheral to discover services for.</param>
    private void DiscoverPeripheralServices(CoreBluetoothPeripheral peripheral)
    {
        peripheral.discoverServices();

        // Set up service discovery callback
        manager.OnDiscoverService(service =>
        {
            Debug.Log("Discovered service with UUID: " + service.uuid);
            service.discoverCharacteristics();

            // Set up characteristic discovery callback
            manager.OnDiscoverCharacteristic(characteristic =>
            {
                HandleCharacteristicDiscovery(characteristic);
            });
        });
    }

    /// <summary>
    /// Handles the discovery of Bluetooth characteristics.
    /// </summary>
    /// <param name="characteristic">The discovered characteristic.</param>
    private void HandleCharacteristicDiscovery(CoreBluetoothCharacteristic characteristic)
    {
        Debug.Log("Discovered characteristic with UUID: " + characteristic.Uuid);

        // Check if the UUID matches the desired characteristic
        if (characteristic.Uuid == CustomCharacteristicUuid.ToString())
        {
            characteristic.SetNotifyValue(true); // Enable notifications
            Debug.Log("Notifications enabled for characteristic: " + characteristic.Uuid);

            // Write data to the characteristic (if needed)
            byte[] dataToSend = Encoding.UTF8.GetBytes("YourData");
            characteristic.Write(dataToSend);
            Debug.Log("Data written to characteristic: " + characteristic.Uuid);
        }
        else
        {
            Debug.Log("Characteristic UUID does not match. No actions taken.");
        }
    }

    /// <summary>
    /// Attempts to connect to the selected Bluetooth device.
    /// </summary>
    /// <param name="deviceName">The name of the device to connect to.</param>
    private void ConnectToDevice(string deviceName)
    {
        if (discoveredPeripherals.TryGetValue(deviceName, out CoreBluetoothPeripheral peripheral))
        {
            Debug.Log($"Attempting connection to device: {deviceName}");
            manager.ConnectToPeripheral(peripheral);
        }
        else
        {
            Debug.LogWarning("Peripheral not found for device: " + deviceName);
        }
    }

    #endregion

    #region UI Management

    /// <summary>
    /// Adds a discovered Bluetooth device to the UI list as a button.
    /// </summary>
    /// <param name="deviceName">The name of the discovered device.</param>
    void AddDeviceToList(string deviceName)
    {
        Debug.Log($"Adding device to list: {deviceName}");

        // Instantiate a new Button element from prefab under the scroll view content
        GameObject newButton = Instantiate(buttonPrefab, scrollViewContent.transform);

        // Get the TextMeshProUGUI component from the Button prefab
        TextMeshProUGUI textComponent = newButton.GetComponentInChildren<TextMeshProUGUI>();
        if (textComponent == null)
        {
            Debug.LogError("TextMeshProUGUI component not found in ButtonPrefab! Check prefab structure.");
            return;
        }

        // Set the text to display the device name
        textComponent.text = deviceName;

        // Set text color: if the device name is in specificNames, use black; otherwise, use gray.
        textComponent.color = specificNames.Contains(deviceName) ? Color.black : Color.gray;

        // Add functionality to the button: attempt connection on click
        Button buttonComponent = newButton.GetComponent<Button>();
        buttonComponent?.onClick.AddListener(() =>
            {
                Debug.Log($"Button clicked for device: {deviceName}");
                ConnectToDevice(deviceName);
            });
    }

    #endregion

    #region Helper Methods

    /// <summary>
    /// Resets the device list UI and clears the device dictionaries.
    /// </summary>
    private void ResetDeviceList()
    {
        discoveredDevices.deviceNames.Clear();
        discoveredPeripherals.Clear();
        foreach (Transform child in scrollViewContent.transform)
        {
            Destroy(child.gameObject); // Clear the UI
        }
    }

    #endregion
}
#endif
