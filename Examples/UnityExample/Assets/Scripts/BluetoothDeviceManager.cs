using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

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
    public Button scanButton;           // Button to trigger re-scan

    private CoreBluetoothManager manager;
    private readonly DeviceListWrapper discoveredDevices = new();

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
        Debug.Log($"Adding device to list: {deviceName}");

        // Instantiate a new Button element from prefab
        GameObject newButton = Instantiate(buttonPrefab, scrollViewContent.transform);
        Debug.Log("Button instantiated.");

        // Get the TextMeshProUGUI component from the Button prefab
        TextMeshProUGUI textComponent = newButton.GetComponentInChildren<TextMeshProUGUI>();
        if (textComponent == null)
        {
            Debug.LogError("TextMeshProUGUI component not found in ButtonPrefab! Check prefab structure.");
            return; // Exit function if TextMeshProUGUI component is missing
        }

        // Set the text to display the device name
        textComponent.text = deviceName;

        // Set text color: if the device name is in specificNames, use black; otherwise, use gray.
        if (specificNames.Contains(deviceName))
        {
            textComponent.color = Color.black;
        }
        else
        {
            textComponent.color = Color.gray;
        }
        Debug.Log($"Set button text: {deviceName} with color {(textComponent.color == Color.black ? "black" : "gray")}");

        // Add functionality to the button
        Button buttonComponent = newButton.GetComponent<Button>();
        buttonComponent?.onClick.AddListener(() =>
            {
                Debug.Log($"Button clicked for device: {deviceName}");
                // [TODO] Add logic to handle device selection or connection here
            });
    }

    void OnDestroy()
    {
        manager?.Stop();
    }
}
#endif
