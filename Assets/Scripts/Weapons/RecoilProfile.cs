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
        public Vector3 minHipSpread, maxHipSpread;
        /// <summary>
        /// The constant spread bounds, always applied when firing. <br></br>
        /// The maximum distance a bullet will spread over the range of the weapon.
        /// </summary>
        public Vector3 minConstantSpread, maxConstantSpread;
        public float hipSpreadPerShot, hipSpreadDecaySpeed;
        public AnimationCurve spreadCurve;
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
            minHipSpread = profile.minHipSpread;
            maxHipSpread = profile.maxHipSpread;
            minConstantSpread = profile.minConstantSpread;
            maxConstantSpread = profile.maxConstantSpread;
            hipSpreadPerShot = profile.hipSpreadPerShot;
            hipSpreadDecaySpeed = profile.hipSpreadDecaySpeed;
        }
        

    }
}