using FishNet.CodeGenerating;
using FishNet.Object;
using FishNet.Object.Synchronizing;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Weapon : NetworkBehaviour
{
    public readonly SyncVar<bool> fireInput = new(new SyncTypeSettings(ReadPermission.ExcludeOwner));
    bool fired;

    [ServerRpc(RunLocally = true)]
    public void SetFireInput(bool fireInput)
    {
        this.fireInput.Value = fireInput;
    }
    
    private void FixedUpdate()
    {
        WeaponEval();
    }
    [Server(Logging = FishNet.Managing.Logging.LoggingType.Common)]
    void WeaponEval()
    {
        if (fireInput.Value)
        {
            print("This weapon should fire!");
        }
    }
    [ServerRpc()]
    public void UseWeapon()
    {
        print("Fired a weapon!");
    }
    
}
