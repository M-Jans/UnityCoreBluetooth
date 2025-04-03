using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

#if UNITY_EDITOR_OSX || UNITY_IOS
using UnityCoreBluetooth;

public class SampleUser : MonoBehaviour
{
    public GameObject scrollViewContent; // Reference to Content object in Scroll View
    public GameObject textPrefab;       // Prefab for list items

    private CoreBluetoothManager manager;
    private List<string> discoveredDevices = new List<string>();

    void Start()
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
            if (!string.IsNullOrEmpty(peripheral.name) && !discoveredDevices.Contains(peripheral.name))
            {
                discoveredDevices.Add(peripheral.name);
                AddDeviceToList(peripheral.name);
                Debug.Log("Discovered device: " + peripheral.name);
            }
        });

        manager.Start();
    }

    void AddDeviceToList(string deviceName)
    {
        // Instantiate a new Text element from prefab
        GameObject newText = Instantiate(textPrefab, scrollViewContent.transform);
        newText.GetComponent<Text>().text = deviceName;
    }

    void OnDestroy()
    {
        if (manager != null)
        {
            manager.Stop();
        }
    }
}
#endif
