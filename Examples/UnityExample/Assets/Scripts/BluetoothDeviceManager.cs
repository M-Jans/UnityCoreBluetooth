using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Text;


#if UNITY_EDITOR_OSX || UNITY_IOS
using UnityCoreBluetooth;

[Serializable]
public class DeviceListWrapper
{
    public List<string> deviceNames = new();
}

public class BluetoothDeviceManager : MonoBehaviour
{
    public GameObject scrollViewContent; // Reference to Content object in Scroll View
    public GameObject buttonPrefab;       // Prefab for list items
    public Button scanButton;             // Button to trigger re-scan

    private CoreBluetoothManager manager;
    private readonly DeviceListWrapper discoveredDevices = new();

    public static readonly Guid CustomCharacteristicUuid = new("72737C42-0FC3-49C6-B27E-8D19D6A0C1FA");

    // Dictionary to store discovered peripheral objects keyed by device name
    private Dictionary<string, CoreBluetoothPeripheral> discoveredPeripherals = new();

    [SerializeField] private List<string> specificNamesList = new() { "Device1" }; // Editable in Inspector
    private HashSet<string> specificNames; // Runtime HashSet for fast lookups

    void Start()
    {
        // Convert List to HashSet at runtime for efficient lookups
        specificNames = new HashSet<string>(specificNamesList);

        // Initialize Bluetooth manager and start scanning
        InitializeBluetoothManager();
        StartScan();
    }

    public void StartScan()
    {
        // Clear existing list, reset discovered devices and dictionary
        discoveredDevices.deviceNames.Clear();
        discoveredPeripherals.Clear();
        foreach (Transform child in scrollViewContent.transform)
        {
            Destroy(child.gameObject);
        }

        // Start scanning for devices again
        manager.StartScan();
        Debug.Log("Scanning for devices...");
    }

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

            // Discover services
            peripheral.discoverServices();

            // Set up service discovery callback
            manager.OnDiscoverService(service =>
            {
                Debug.Log("Discovered service with UUID: " + service.uuid);

                // Discover characteristics for the service
                service.discoverCharacteristics();

                // Set up characteristic discovery callback
                manager.OnDiscoverCharacteristic(characteristic =>
                {
                    Debug.Log("Discovered characteristic with UUID: " + characteristic.Uuid);

                    // Check if the UUID matches the desired characteristic
                    if (characteristic.Uuid == CustomCharacteristicUuid.ToString())
                    {
                        // Enable notifications for the characteristic
                        characteristic.SetNotifyValue(true);
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
                });
            });
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

    void AddDeviceToList(string deviceName)
    {
        Debug.Log($"Adding device to list: {deviceName}");

        // Instantiate a new Button element from prefab under the scroll view content
        GameObject newButton = Instantiate(buttonPrefab, scrollViewContent.transform);
        Debug.Log("Button instantiated.");

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

    void OnDestroy()
    {
        manager?.Stop();
    }
}
#endif
