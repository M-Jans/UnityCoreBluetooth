using System;
using UnityEngine;
using UnityEngine.UI;
#if UNITY_EDITOR_OSX || UNITY_IOS
using UnityCoreBluetooth;

public class SampleUser : MonoBehaviour
{

    public Text text;

    private CoreBluetoothManager manager;
    private CoreBluetoothCharacteristic characteristic;

    // Use this for initialization
    void Start()
    {
        manager = CoreBluetoothManager.Shared;

        manager.OnUpdateState(state =>
        {
            Debug.Log("state: " + state);
            if (state != "poweredOn") return;
            manager.StartScan();
        });

        manager.OnDiscoverPeripheral(peripheral =>
        {
            if (peripheral.name != "")
                Debug.Log("discover peripheral name: " + peripheral.name); 
            if ((peripheral.name != "Daydream controller") && (peripheral.name != "M5Stack") && (peripheral.name != "M5StickC")) return;

            manager.StopScan();
            manager.ConnectToPeripheral(peripheral);
        });

        manager.OnConnectPeripheral(peripheral =>
        {
            Debug.Log("connected peripheral name: " + peripheral.name);
            peripheral.discoverServices();
        });

        manager.OnDiscoverService(service =>
        {
            Debug.Log("discover service uuid: " + service.uuid);
            if (service.uuid != "FE55") return;
            service.discoverCharacteristics();
        });


        manager.OnDiscoverCharacteristic(characteristic =>
        {
            this.characteristic = characteristic;
            string uuid = characteristic.Uuid;
            string[] usage = characteristic.Propertis;
            Debug.Log("discover characteristic uuid: " + uuid + ", usage: " + usage);
            for (int i = 0; i < usage.Length; i++)
            {
                Debug.Log("discover characteristic uuid: " + uuid + ", usage: " + usage[i]);
                if (usage[i] == "notify")
                    characteristic.SetNotifyValue(true);
            }
        });

        manager.OnUpdateValue((characteristic, data) =>
        {
            value = data;
            flag = true;
        });
        manager.Start();
    }

    private bool flag = false;
    private byte[] value = new byte[20];

    private float vy = 0.0f;

    // Update is called once per frame
    void Update()
    {
        if (transform.position.y < 0)
        {
            vy = 0.0f;
            transform.position = new Vector3(0, 0, 0);
        }
        else
        {
            vy -= 0.006f;
            transform.position += new Vector3(0, vy, 0);
        }
        transform.Rotate(2, -3, 4);
        if (flag == false) return;
        flag = false;
        text.text = $"Notify: {BitConverter.ToInt32(value, 0)}";
        vy += 0.1f;
        transform.position += new Vector3(0, vy, 0);
    }

    void OnDestroy()
    {
        manager.Stop();
    }

    private int counter = 0;

    public void Write()
    {
        characteristic.Write(System.Text.Encoding.UTF8.GetBytes($"{counter}"));
        counter++;
    }
}
#endif
