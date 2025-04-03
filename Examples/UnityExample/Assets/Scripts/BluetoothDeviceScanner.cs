using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

#if UNITY_EDITOR_OSX || UNITY_IOS
using UnityCoreBluetooth;

[Serializable]
public class DeviceListWrapper
{
    public List<string> deviceNames = new();
}

public class BluetoothDeviceScanner : MonoBehaviour
{
    public GameObject scrollViewContent; // Reference to Content object in Scroll View
    public GameObject textPrefab;       // Prefab for list items
    public Button scanButton;           // Button to trigger re-scan

    private CoreBluetoothManager manager;
    [SerializeField] private DeviceListWrapper discoveredDevices = new();

    [SerializeField] private List<string> specificNamesList = new() { "Device1"}; // Editable in Inspector
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
        // Clear existing list and reset discovered devices
        discoveredDevices.deviceNames.Clear();
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

        manager.OnUpdateState(state =>
        {
            Debug.Log("Bluetooth state: " + state);
            if (state != "poweredOn") return;
            manager.StartScan();
        });

        manager.OnDiscoverPeripheral(peripheral =>
        {
            if (!string.IsNullOrEmpty(peripheral.name) && !discoveredDevices.deviceNames.Contains(peripheral.name))
            {
                discoveredDevices.deviceNames.Add(peripheral.name);
                AddDeviceToList(peripheral.name);
                Debug.Log("Discovered device: " + peripheral.name);
            }
        });

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
        // Instantiate a new Text element from prefab
        GameObject newText = Instantiate(textPrefab, scrollViewContent.transform);
        Text textComponent = newText.GetComponent<Text>();

        textComponent.text = deviceName;

        // Ensure proper comparison logic using runtime HashSet
        if (specificNames.Contains(deviceName.Trim()))
        {
            textComponent.color = Color.black; // Set text color to black for specific names
            Debug.Log($"Specific name detected: {deviceName}");
        }
        else
        {
            textComponent.color = Color.grey; // Set text color to grey for non-specific names
            Debug.Log($"Non-specific name detected: {deviceName}");
        }
    }

    void OnDestroy()
    {
        manager?.Stop();
    }
}
#endif
