using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(menuName = "Eclipse/Weapons/MobilityProfile")]
public class MobilityProfile : ScriptableObject
{
    public float walkSpeedMultiplier, sprintSpeedMultiplier;
    public float aimSpeed;
    public float worldAimFOV = 60, viewAimFOV = 45;
    public AnimationCurve aimCurve;
    public float reloadingMultiplier;
}
