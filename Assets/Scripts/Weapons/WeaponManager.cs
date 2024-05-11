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
        public NetworkObject primaryWeapon, secondaryWeapon;
        public PlayerMotor pm;
        [SerializeField] Weapon currentWeapon;
        [SerializeField] Weapon stashedWeapon;
        public AnimationHelper animationHelper;
        [SerializeField, Tooltip("When not performing any actions, this transform will be repositioned to accommodate for grips.")] Transform forwardHand;
        [SerializeField] Vector3 forwardHandOriginalPosition;
        [SerializeField] Quaternion forwardHandOriginalRotation;
        float viewRoll;
        [SerializeField] Transform recoilPositionTransform;
        [SerializeField] Transform viewCamTarget, worldCamTarget;
        [SerializeField] float viewRecoilPosMultiplier, viewRecoilRotMultiplier, weaponRecoilPosMultiplier, weaponRecoilRotMultiplier;
        [SerializeField] Vector3 recoilpos, recoilRot;
        private Vector3 posVelocity, rotVelocity;
        private float sds;
        [SerializeField] internal RecoilProfile recoilProfile;
        internal float recoilAngleAdditive;
        public Quaternion viewRecoilOrientation = Quaternion.identity, aimedRecoilOrientation = Quaternion.identity;
        public Quaternion WeaponRecoilOrientation = Quaternion.identity, aimedRecoilOrientationWeapon = Quaternion.identity;

        [SerializeField] float recoilReturnTime;
        [SerializeField] float currentRecoilReturn;
        Vector3 peakRecoilPosition, recoilPositionDamped;
        internal Vector3 recoilAimRotation, peakRecoilAimRotation;
        internal Vector3 aimRotationDamped;
        Vector3 aimRotationVelocity;
        [SerializeField] bool aiming;
        [SerializeField] CinemachineCamera worldCamera, viewCamera;
        [SerializeField] float worldDefaultFOV = 70, viewDefaultFOV = 50;
        public Weapon CurrentWeapon => currentWeapon;
        [SerializeField] internal Transform fireOrigin;

        AnimatorOverrideController aoc;
        AnimationClipOverrides overrides;

        public override void OnStartClient()
        {
            base.OnStartClient();
            print($"{Owner.IsHost} : {Owner.IsLocalClient}");
            if (Owner.IsLocalClient || Owner.IsHost)
                SpawnWeapon();

        }

        [ServerRpc]
        public void SpawnWeapon()
        {
            GameObject weap = Instantiate(primaryWeapon.gameObject, parent: weaponRoot);
            weap.name = primaryWeapon.name;
            currentWeapon = weap.GetComponent<Weapon>();
            currentWeapon.transform.SetLocalPositionAndRotation(Vector3.zero, Quaternion.identity);
            foreach (var item in currentWeapon.GetComponentsInChildren<Renderer>())
            {
                item.gameObject.layer = LayerMask.NameToLayer("Character");
            }

            weap = Instantiate(secondaryWeapon.gameObject, parent: weaponRoot);
            weap.name = secondaryWeapon.name;
            stashedWeapon = weap.GetComponent<Weapon>();
            stashedWeapon.transform.SetLocalPositionAndRotation(Vector3.zero, Quaternion.identity);
            foreach (var item in stashedWeapon.GetComponentsInChildren<Renderer>())
            {
                item.gameObject.layer = LayerMask.NameToLayer("Character");
            }
            weap.SetActive(false);
            ServerManager.Spawn(currentWeapon.gameObject, Owner);
            ServerManager.Spawn(stashedWeapon.gameObject, Owner);
            SetWeaponPosition(currentWeapon.NetworkObject, stashedWeapon.NetworkObject);
        }
        [ObserversRpc]
        void SetWeaponPosition(NetworkObject c, NetworkObject s)
        {
            c.gameObject.SetActive(true);
            s.gameObject.SetActive(true);
            currentWeapon = c.GetComponent<Weapon>();
            stashedWeapon = s.GetComponent<Weapon>();


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
        float aimLerp;
        void AimCameraCalculations()
        {
            if (currentWeapon && currentWeapon.aimPoint)
            {

                currentWeapon.aimAmount = Mathf.MoveTowards(currentWeapon.aimAmount, aiming ? 1 : 0, currentWeapon.mobilityProfile.aimSpeed * Time.fixedDeltaTime);
                aimLerp = currentWeapon.mobilityProfile.aimCurve.Evaluate(currentWeapon.aimAmount);
                viewCamTarget.SetLocalPositionAndRotation(Vector3.zero, Quaternion.identity);
                viewCamTarget.SetPositionAndRotation(Vector3.Lerp(viewCamTarget.position, currentWeapon.aimPoint.position, aimLerp),
                    viewCamTarget.rotation * Quaternion.Euler(recoilAimRotation * viewRecoilRotMultiplier));
                viewCamTarget.localPosition += viewRecoilOrientation * recoilPositionDamped;
                worldCamera.Lens.FieldOfView = Mathf.Lerp(worldDefaultFOV, currentWeapon.mobilityProfile.worldAimFOV, aimLerp);
                viewCamera.Lens.FieldOfView = Mathf.Lerp(viewDefaultFOV, currentWeapon.mobilityProfile.viewAimFOV, aimLerp);
            }
        }
        void RecoilCalculations()
        {
            if (currentWeapon)
            {
                float recoverSpeed = currentWeapon.recoilProfile.recoverSpeed * Time.fixedDeltaTime;
                viewRoll = recoilRot.z;

                if (recoilReturnTime < currentWeapon.recoilProfile.recoilIdleTime)
                {
                    recoilReturnTime += Time.fixedDeltaTime;
                    peakRecoilPosition = recoilPositionTransform.localPosition;
                    peakRecoilAimRotation = recoilAimRotation;
                    currentRecoilReturn = 0;

                    recoilRot -= recoverSpeed * recoilRot;
                    recoilpos -= recoverSpeed * recoilpos;
                    recoilAimRotation -= recoverSpeed * recoilAimRotation;
                }
                else
                {
                    if (currentRecoilReturn < 1)
                    {
                        currentRecoilReturn += Time.fixedDeltaTime * currentWeapon.recoilProfile.idleRecoilReturnSpeed;
                        recoilpos = Vector3.LerpUnclamped(peakRecoilPosition, Vector3.zero, currentWeapon.recoilProfile.recoilReturnCurve.Evaluate(currentRecoilReturn));
                        recoilAimRotation = Vector3.LerpUnclamped(peakRecoilAimRotation, Vector3.zero, currentWeapon.recoilProfile.recoilViewReturnCurve.Evaluate(currentRecoilReturn));
                    }
                }
                recoilPositionDamped = Vector3.SmoothDamp(recoilPositionTransform.localPosition,  recoilpos * weaponRecoilPosMultiplier, ref posVelocity, currentWeapon.recoilProfile.weaponRecoilSmoothness);
                aimRotationDamped = Vector3.SmoothDamp(aimRotationDamped, recoilAimRotation, ref aimRotationVelocity, currentWeapon.recoilProfile.viewRecoilSmoothness);
                recoilPositionTransform.SetLocalPositionAndRotation(recoilPositionDamped,
                Quaternion.Euler(aimRotationDamped * weaponRecoilRotMultiplier));
                
            }
        }
        public void SwitchWeapon()
        {
            if (IsOwner)
            {
                SwapWeaponRPC(currentWeapon, stashedWeapon);
            }
        }
        [ObserversRpc()]
        void SwapWeaponRPC(Weapon wNew, Weapon wOld)
        {
            print("networked weapon swap");
            (stashedWeapon, currentWeapon) = (wNew, wOld);
            stashedWeapon.gameObject.SetActive(false);
            currentWeapon.gameObject.SetActive(true);

            SwapAnimations();
            recoilProfile = currentWeapon.recoilProfile;
        }

        public void SwapAnimations()
        {
            //Create the override controller
            if (!aoc)
            {
                aoc = new(animator.runtimeAnimatorController);
                //assign the override controller
                animator.runtimeAnimatorController = aoc;
                overrides = new(aoc.overridesCount);
                aoc.GetOverrides(overrides);
            }
            if (currentWeapon.animations)
            {
                for (int i = 0; i < currentWeapon.animations.overrides.Count; i++)
                {
                    AnimationOverrides a = currentWeapon.animations.overrides[i];
                    overrides[a.name] = a.overrideClip;
                }
                aoc.ApplyOverrides(overrides);
            }
            netAnimator.SetController(aoc);
        }
        private void LateUpdate()
        {
            if (currentWeapon)
            {
                forwardHand.SetLocalPositionAndRotation(forwardHandOriginalPosition, forwardHandOriginalRotation);
                if(animationHelper.forwardHandWeight > 0)
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
        public void GetSwapInput(InputAction.CallbackContext context)
        {
            if (context.performed)
            {
                netAnimator.SetTrigger("Switch");
            }
        }
        internal void ReceiveRecoilImpulse()
        {
            if (!currentWeapon)
                return;
            float aimLerp = Mathf.Lerp(1, currentWeapon.recoilProfile.aimedRecoilMultiplier, currentWeapon.aimAmount);
            recoilpos += WeaponRecoilOrientation * new Vector3(Random.Range(-currentWeapon.recoilProfile.recoilPerShot.x, currentWeapon.recoilProfile.recoilPerShot.x),
                Random.Range(-currentWeapon.recoilProfile.recoilPerShot.y, currentWeapon.recoilProfile.recoilPerShot.y),
                currentWeapon.recoilProfile.recoilPerShot.z) * aimLerp;
            recoilReturnTime = 0;
            recoilAimRotation += new Vector3()
            {
                x = currentWeapon.recoilProfile.aimRotationPerShot.x,
                y = Random.Range(-currentWeapon.recoilProfile.aimRotationPerShot.y, currentWeapon.recoilProfile.aimRotationPerShot.y),
                z = Random.Range(-currentWeapon.recoilProfile.aimRotationPerShot.z, currentWeapon.recoilProfile.aimRotationPerShot.z),
            } * aimLerp;
            netAnimator.SetTrigger("Fire");
        }
    }
}