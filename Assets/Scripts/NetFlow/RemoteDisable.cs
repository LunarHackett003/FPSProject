using FishNet.Object;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class RemoteDisable : NetworkBehaviour
{

    public Behaviour[] remoteDisableBehaviours, ownerDisableBehaviours;
    public override void OnStartClient()
    {
        base.OnStartClient();
        if (IsOwner)
            Local();
        else
            Remote();
    }
    void Remote()
    {
        for (int i = 0; i < remoteDisableBehaviours.Length; i++)
        {
            remoteDisableBehaviours[i].enabled = false;
        }
    }
    void Local()
    {
        for (int i = 0; i < ownerDisableBehaviours.Length; i++)
        {
            ownerDisableBehaviours[i].enabled = false;
        }
    }
}
