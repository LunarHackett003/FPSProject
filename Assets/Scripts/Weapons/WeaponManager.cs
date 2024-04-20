using FishNet.Component.Animating;
using FishNet.Object;
using GameKit.Dependencies.Utilities;
using System.Collections;
using System.Collections.Generic;
using System.Transactions;
using Unity.Cinemachine;
using UnityEngine;
using UnityEngine.InputSystem;

namespace Eclipse.Weapons
{
    public class WeaponManager : NetworkBehaviour
    {
        public NetworkAnimator netAnimator;
        public Animator animator;

        public Transform weaponRoot;
        public NetworkObject spawnWeapon;
        public PlayerMotor pm;
        [SerializeField] Weapon currentWeapon;
        [SerializeField] Weapon stashedWeapon;
        public AnimationHelper animationHelper;
        [SerializeField, Tooltip("When not performing any actions, this transform will be repositioned to accommodate for grips.")] Transform forwardHand;
        [SerializeField] Vector3 forwardHandOriginalPosition;
        [SerializeField] Quaternion forwardHandOriginalRotation;
        float viewRoll;
        [SerializeField] Transform recoilPositionTransform, recoilAngularTarget;
        [SerializeField] Transform viewmodelCamera;
        [SerializeField] float viewRecoilPosMultiplier, weaponRecoilRotMultiplier, viewmodelRollMultiplier;
        [SerializeField] Vector3 recoilpos, recoilRot;
        private Vector3 posVelocity, rotVelocity;
        private float sds;
        [SerializeField] float recoilSmoothness;
        private void Awake()
        {
            
        }
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
            //Recoil needs to be visible for all players
            RecoilCalculations();

            if (!IsOwner)
                return;

        }
        void RecoilCalculations()
        {
            if (currentWeapon)
            {
                float recoverSpeed = currentWeapon.recoilRecoverSpeed * Time.fixedDeltaTime;
                recoilRot -=  recoverSpeed * recoilRot;
                recoilpos -= recoverSpeed * recoilpos;
                viewRoll = recoilRot.z;
                recoilAngularTarget.SetLocalPositionAndRotation(Vector3.SmoothDamp(recoilAngularTarget.localPosition, new(recoilRot.x, recoilRot.y, 0), ref rotVelocity, recoilSmoothness),
                    Quaternion.Euler(0, 0, viewRoll * viewmodelRollMultiplier ));
                recoilPositionTransform.SetLocalPositionAndRotation(Vector3.SmoothDamp(recoilPositionTransform.localPosition, recoilpos, ref posVelocity, recoilSmoothness),
                    Quaternion.LerpUnclamped(Quaternion.identity, viewmodelCamera.localRotation, weaponRecoilRotMultiplier));
            }
        }

        private void LateUpdate()
        {
            if (currentWeapon)
            {


                forwardHand.SetLocalPositionAndRotation(forwardHandOriginalPosition, forwardHandOriginalRotation);
                forwardHand.SetPositionAndRotation(Vector3.Lerp(forwardHand.position, currentWeapon.gripPoint.position, animationHelper.forwardHandWeight),
                    Quaternion.Lerp(forwardHand.rotation, currentWeapon.gripPoint.rotation, animationHelper.forwardHandWeight));
            }
        }
        public void GetFireInput(InputAction.CallbackContext context)
        {
            if (context.performed)
                currentWeapon.SetFireInput(true);
            if (context.canceled)
                currentWeapon.SetFireInput(false);
        }
        [ObserversRpc()]
        internal void ReceiveShot(Vector3 recoilAngular, Vector3 recoilLinear)
        {
            recoilpos += recoilLinear;
            recoilRot += recoilAngular;
        }
        internal void ReceiveShotNoSync(Vector3 recoilAngular, Vector3 recoilLinear)
        {
            recoilpos += recoilLinear;
            recoilRot += recoilAngular;
        }
    }
}