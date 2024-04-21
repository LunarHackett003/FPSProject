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
        [SerializeField] Transform viewCamTarget, worldCamTarget;
        [SerializeField] float viewRecoilPosMultiplier, viewRecoilRotMultiplier;
        [SerializeField] Vector3 recoilpos, recoilRot;
        private Vector3 posVelocity, rotVelocity;
        private float sds;
        [SerializeField] float recoilSmoothness;
        [SerializeField] internal RecoilProfile recoilProfile;
        internal float recoilAngleAdditive;

        [SerializeField] float recoilReturnTime;
        [SerializeField] float currentRecoilReturn;
        Vector3 peakRecoilPosition;
        [SerializeField] bool aiming;
        [SerializeField] CinemachineCamera worldCamera, viewCamera;
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
            //Only the owner needs the viewmodel camera
            AimCameraCalculations();
            worldCamera.transform.rotation = worldCamTarget.rotation;
            viewCamera.transform.rotation = viewCamTarget.rotation;
        }
        void AimCameraCalculations()
        {
            if (currentWeapon && currentWeapon.aimPoint)
            {

                currentWeapon.aimAmount = Mathf.MoveTowards(currentWeapon.aimAmount, aiming ? 1 : 0, currentWeapon.mobilityProfile.aimSpeed * Time.fixedDeltaTime);

                viewCamTarget.SetLocalPositionAndRotation(Vector3.zero, Quaternion.identity);
                viewCamTarget.SetPositionAndRotation(Vector3.Lerp(viewCamTarget.position, currentWeapon.aimPoint.position + (weaponRoot.TransformDirection(recoilpos) * viewRecoilPosMultiplier), currentWeapon.aimAmount),
                    Quaternion.Lerp(viewCamTarget.rotation, currentWeapon.aimPoint.rotation, currentWeapon.aimAmount));
            }
        }
        void RecoilCalculations()
        {
            if (currentWeapon)
            {
                float recoverSpeed = recoilProfile.recoverSpeed * Time.fixedDeltaTime;

                viewRoll = recoilRot.z;


                if (recoilReturnTime < recoilProfile.recoilIdleTime)
                {
                    recoilReturnTime += Time.fixedDeltaTime;
                    peakRecoilPosition = recoilPositionTransform.localPosition;
                    currentRecoilReturn = 0;

                    recoilRot -= recoverSpeed * recoilRot;
                    recoilpos -= recoverSpeed * recoilpos;
                }
                else
                {
                    if (currentRecoilReturn < 1)
                    {
                        currentRecoilReturn += Time.fixedDeltaTime * recoilProfile.idleRecoilReturnSpeed;
                        recoilpos = Vector3.LerpUnclamped(peakRecoilPosition, Vector3.zero, recoilProfile.recoilReturnCurve.Evaluate(currentRecoilReturn));
                    }
                }
                recoilPositionTransform.SetLocalPositionAndRotation(Vector3.SmoothDamp(recoilPositionTransform.localPosition, recoilpos, ref posVelocity, recoilSmoothness),
    Quaternion.identity);
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
        public void GetAimInput(InputAction.CallbackContext context)
        {
            aiming = context.ReadValueAsButton();
        }
        internal void ReceiveRecoilImpulse()
        {
            recoilpos += new Vector3(Random.Range(-recoilProfile.recoilPerShot.x, recoilProfile.recoilPerShot.x),
                Random.Range(-recoilProfile.recoilPerShot.y, recoilProfile.recoilPerShot.y), 
                recoilProfile.recoilPerShot.z) *Mathf.Lerp(1, currentWeapon.recoilProfile.aimedRecoilMultiplier, currentWeapon.aimAmount);
            recoilReturnTime = 0;
        }
    }
}