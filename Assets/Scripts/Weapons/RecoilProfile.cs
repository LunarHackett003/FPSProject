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
    }
}