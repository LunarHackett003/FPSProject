using FishNet.Transporting.UTP;
using System.Collections;
using System.Collections.Generic;
using Unity.Services.Core;
using Unity.Services.Relay;
using UnityEngine;

public class RelayManager : MonoBehaviour
{
    public FishyUnityTransport fut;
    private async void Start()
    {
        InitializationOptions io = new();
        await UnityServices.InitializeAsync(io);

        
    }
}
