using FishNet.Component.Animating;
using FishNet.Object;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

public class WeaponManager : NetworkBehaviour
{
    public NetworkAnimator netAnimator;
    public Animator animator;

    public Transform weaponRoot;
    public NetworkObject spawnWeapon;

    public Weapon currentWeapon;
    public override void OnStartClient()
    {
        base.OnStartClient();
        if (IsOwner)
            SpawnWeapon();
    }
    [ServerRpc(RunLocally = false)]
    public void SpawnWeapon()
    {
        GameObject weap = Instantiate(spawnWeapon.gameObject, parent: weaponRoot);
        Spawn(weap, Owner);
        currentWeapon = weap.GetComponent<Weapon>();
        currentWeapon.transform.SetLocalPositionAndRotation(Vector3.zero, Quaternion.identity);
        foreach (var item in currentWeapon.GetComponentsInChildren<Renderer>())
        {
            item.gameObject.layer = LayerMask.NameToLayer("Character");
        }
    }
    private void FixedUpdate()
    {
        if (!IsOwner)
            return;
    }
    public void GetFireInput(InputAction.CallbackContext context)
    {
        if (context.performed)
            currentWeapon.SetFireInput(true);
        if(context.canceled)
            currentWeapon.SetFireInput(false);
    }
}
