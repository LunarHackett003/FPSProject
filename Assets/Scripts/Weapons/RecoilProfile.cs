using System.Collections;
using UnityEngine;

namespace Eclipse.Weapons
{
[CreateAssetMenu(menuName = "Eclipse/Weapons/RecoilProfile")]
    public class RecoilProfile : ScriptableObject
    {
        public float recoverSpeed;
        public Vector3 recoilPerShot;
        public float firingRecoilReturnSpeed, idleRecoilReturnSpeed;
        public float recoilIdleTime;
        public AnimationCurve recoilReturnCurve, recoilViewReturnCurve;
        public float aimedRecoilMultiplier;
        public Vector3 aimRotationPerShot;
        public float weaponRecoilSmoothness, viewRecoilSmoothness;

        public RecoilProfile()
        {
        }

        public RecoilProfile(RecoilProfile profile)
        {
            recoverSpeed = profile.recoverSpeed;
            recoilPerShot = profile.recoilPerShot;
            firingRecoilReturnSpeed = profile.firingRecoilReturnSpeed;
            idleRecoilReturnSpeed= profile.idleRecoilReturnSpeed;
            recoilIdleTime = profile.recoilIdleTime;
            recoilReturnCurve = profile.recoilReturnCurve;
            recoilViewReturnCurve = profile.recoilViewReturnCurve;
            aimedRecoilMultiplier = profile.aimedRecoilMultiplier;
            aimRotationPerShot = profile.aimRotationPerShot;
            weaponRecoilSmoothness = profile.weaponRecoilSmoothness;
            viewRecoilSmoothness = profile.viewRecoilSmoothness;
        }
        

    }
}