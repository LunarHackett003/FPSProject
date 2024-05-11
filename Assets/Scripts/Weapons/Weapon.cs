using FishNet.CodeGenerating;
using FishNet.Object;
using FishNet.Object.Synchronizing;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Eclipse.Weapons.Attachments;
using FishNet;
using UnityEngine.Animations;
namespace Eclipse.Weapons
{
    public class Weapon : NetworkBehaviour
    {
        [System.Serializable]
        public class TracerObject
        {
            public GameObject tracer;
            public Vector3 end;
            public Vector3 start;
            public float lerp;
            public float timeIncrement;
        }
        List<TracerObject> tracers = new List<TracerObject>();
        [SerializeField] protected Transform firePosition;
        [SerializeField] protected GameObject shotEffect;
        [SerializeField] protected float tracerSpeed;


        [System.Serializable]
        public class WeaponProperty
        {
            [HideInInspector] public string name;
            public enum PropertyType
            {
                /// <summary>
                /// Invalid Weapon Property
                /// </summary>
                none = 0,
                /// <summary>
                /// How fast the weapon is reloaded
                /// </summary>
                reloadSpeed = 1, 
                /// <summary>
                /// How many rounds the weapon starts with
                /// </summary>
                ammoCount = 2,
                /// <summary>
                /// The horizontal recoil multiplier
                /// </summary>
                horizontalRecoil = 3, 
                /// <summary>
                /// Vertical recoil multiplier
                /// </summary>
                verticalRecoil = 4,
                /// <summary>
                /// A multiplier to all recoil<br></br>
                /// the higher it is, the stronger the recoil.
                /// </summary>
                recoilControl = 5,
                /// <summary>
                /// A multiplier to the smoothness of recoil<br></br>
                /// The higher this is, the smoother the recoil.
                /// </summary>
                recoilSmoothness = 6,
                /// <summary>
                /// multiplier to damage at the most effective range
                /// </summary>
                maxDamageMult = 7,
                /// <summary>
                /// multiplier to damage at the least effective range
                /// </summary>
                minDamageMult = 8,
                /// <summary>
                /// The range at which damage starts to dropoff
                /// </summary>
                minDamageRange = 9,
                /// <summary>
                /// The range at which damage stops falling off
                /// </summary>
                maxDamageRange = 10,
                
            }
            public PropertyType property;
            public float amount = 1;
        }
        public List<WeaponProperty> properties = new();
        public WeaponAnimationSetScriptable animations;
        public readonly SyncVar<bool> fireInput = new(new SyncTypeSettings(ReadPermission.ExcludeOwner));
        internal bool fired;
        [SerializeField] AttachmentManager attachmentManager;

        [SerializeField] internal Transform aimPoint, gripPoint;
        [SerializeField] internal ParticleSystem muzzleParticle;
        [SerializeField] bool delayBeforeShot;

        [SerializeField, Tooltip("How many rounds to fire before preventing the player from firing again")] int burstCount;
        int currentBurstCount;
        [SerializeField] float shotCooldown, burstCooldown;
        float currentCooldown;
        [SerializeField] bool autoBurst;
        [SerializeField] internal bool recoiling;

        internal float currentSpread;
        [SerializeField] AnimationCurve spreadRamp;
        [SerializeField] float spreadRadiusAtMax;
        [SerializeField] float spreadRadiusAtMin;

        [SerializeField] internal float aimAmount, aimSpeed, aimRecoilModifier;
        [SerializeField] internal AnimationCurve aimCurve;
        [SerializeField] WeaponManager wmParent;
        [SerializeField] internal LayerMask bulletLayermask;
        [SerializeField] protected float maxRange;
        
        [SerializeField, Tooltip("This weapons Recoil Profile scriptable object")] internal RecoilProfile recoilProfile;
        [SerializeField, Tooltip("How strong the per-shot recoil is")] internal float recoilPerShot;
        [SerializeField] internal MobilityProfile mobilityProfile;
        [SerializeField] int fireIterations;
        [System.Serializable]
        public class Magazine
        {
            public Transform magazine;
            public Vector3 startPos;
            public Quaternion startRot;
        }
        public Magazine newMag, oldMag;
        private void Awake()
        {
            if(!attachmentManager)
                attachmentManager = GetComponent<AttachmentManager>();
            if(attachmentManager && attachmentManager.isActiveAndEnabled && Owner.IsLocalClient)
                AttachmentSpawn();
            wmParent = GetComponentInParent<WeaponManager>();
            transform.SetLocalPositionAndRotation(Vector3.zero, Quaternion.identity);
        }
        public void AttachmentSpawn()
        {
            attachmentManager.SpawnAttachments();
            wmParent = GetComponentInParent<WeaponManager>();

            //Now that we've grabbed the Attachment Manager, we need to get all the attachments and then modify this weapon's properties based on the attachments properties
            for (int i = 0; i < attachmentManager.attachPoints.Length; i++)
            {
                //Cache the one we're looking at
                AttachmentManager.AttachPoint ap = attachmentManager.attachPoints[i];
                //Check if the Current Attachment data container exists, otherwise we're likely using the default attachment, or one is not specified.
                if (ap.currentAttachment)
                {
                    //Iterate through the properties this attachment modifies
                    for (int j = 0; j < ap.currentAttachment.modifiedProperties.Count; j++)
                    {
                        //Cache the modified property 
                        WeaponProperty mp = ap.currentAttachment.modifiedProperties[j];
                        //Find the property that matches this one, then add the amount for the modifier.
                        properties.Find(x => x.property == mp.property).amount *= mp.amount;
                    }
                }
            }
            attachmentManager.weapon = this;
            //Clone the recoil profile so we don't overwrite the original
            recoilProfile = recoilProfile.Clone();
            recoilProfile.name += "_CLONED";

            // ASSIGN MODIFIED VALUES
            // ASSIGN MODIFIED VALUES

            //Assign the temp variable "prop" to each value if it needs modifying multiple times
            float prop = properties.Find(x => x.property == WeaponProperty.PropertyType.recoilSmoothness).amount;
            recoilProfile.viewRecoilSmoothness *= prop;
            recoilProfile.weaponRecoilSmoothness *= prop;
            prop = properties.Find(x => x.property == WeaponProperty.PropertyType.horizontalRecoil).amount;
            recoilProfile.recoilPerShot.x *= prop;
            recoilProfile.aimRotationPerShot.y *= prop;
            prop = properties.Find(x => x.property == WeaponProperty.PropertyType.verticalRecoil).amount;
            recoilProfile.recoilPerShot.y *= prop;
            recoilProfile.aimRotationPerShot.x *= prop;
            if (wmParent)
                wmParent.recoilProfile = recoilProfile;
            prop = properties.Find(x => x.property == WeaponProperty.PropertyType.recoilControl).amount;
            recoilProfile.recoilPerShot *= prop;
            recoilProfile.aimRotationPerShot *= prop;
        }
        [ServerRpc(RunLocally = true)]
        public void SetFireInput(bool fireInput)
        {
            this.fireInput.Value = fireInput;
        }

        private void FixedUpdate()
        {
            WeaponEval();
            //Update tracers for bullets this gun has fired
            for (int i = tracers.Count - 1; i >= 0; i--)
            {
                if (tracers[i].tracer)
                    tracers[i].tracer.transform.position = Vector3.Lerp(tracers[i].start, tracers[i].end, tracers[i].lerp);
                else
                {
                    tracers.RemoveAt(i);
                    i = Mathf.Min(i + 1, tracers.Count - 1);
                    continue;
                }
                tracers[i].lerp += tracers[i].timeIncrement;
            }
        }
        void WeaponEval()
        {
            if (!IsOwner)
                return;
            if(fireInput.Value && !fired)
            {
                UseWeapon();
            }
            currentSpread = Mathf.Clamp01(currentSpread - (Time.fixedDeltaTime * recoilProfile.hipSpreadDecaySpeed));
        }
        public void UseWeapon()
        {
            //If we need to burst fire this weapon, we'll start the burst fire Coroutine
            if(burstCount > 0)
            {
                StartCoroutine(BurstFire());
            }
            else
            {
                //If we're not burst-firing, we'll use the AutoFire coroutine
                StartCoroutine(AutoFire());
            }
        }
        [ServerRpc(RunLocally = true)]
        void FireWeapon()
        {
            print("Fired a weapon!");
            if (muzzleParticle)
                muzzleParticle.Play();
            if (wmParent)
            {
                wmParent.ReceiveRecoilImpulse();
            }
            //Finally time to actually shoot shit
            if (fireIterations <= 0)
                fireIterations = 1;
            Vector3[] tracerEndPoints = new Vector3[fireIterations];
            for (int i = 0; i < fireIterations; i++)
            {
                var ran = Random.insideUnitCircle;
                Vector3 spreadvec = (Mathf.Lerp(1, 0, aimAmount) * recoilProfile.spreadCurve.Evaluate(currentSpread) * new Vector3()
                {
                    x = Mathf.Lerp(recoilProfile.minHipSpread.x, recoilProfile.maxHipSpread.x, ran.x),
                    y = Mathf.Lerp(recoilProfile.minHipSpread.y, recoilProfile.maxHipSpread.y, ran.y),
                }) + new Vector3()
                {
                    x = Mathf.Lerp(recoilProfile.minConstantSpread.x, recoilProfile.maxConstantSpread.x, ran.x),
                    y = Mathf.Lerp(recoilProfile.minConstantSpread.y, recoilProfile.maxConstantSpread.y, ran.y)
                } + (Vector3.forward * maxRange);
                Vector3 dir = wmParent.fireOrigin.TransformDirection(spreadvec);
                if (Physics.Raycast(wmParent.fireOrigin.position, dir, out RaycastHit hit, maxRange, bulletLayermask))
                {
                    tracerEndPoints[i] = hit.point;
                    Debug.DrawLine(firePosition.position, hit.point, Color.green);
                }
                else
                {
                    Debug.DrawRay(firePosition.position, dir, Color.red);
                    tracerEndPoints[i] = wmParent.fireOrigin.position + dir;
                }    
            }
            if(shotEffect)
                SendTracersToClientsRPC(tracerEndPoints);

            currentSpread = Mathf.Clamp01(currentSpread + recoilProfile.hipSpreadPerShot);
        }
        [ObserversRpc]
        void SendTracersToClientsRPC(Vector3[] ends)
        {
            for (int i = 0; i < ends.Length; i++)
            {
                GameObject shotObject = Instantiate(shotEffect, firePosition.position, firePosition.rotation);
                var t = new TracerObject()
                {
                    tracer = shotObject,
                    start = firePosition.position,
                    end = ends[i],
                    lerp = 0,
                };
                t.timeIncrement = (tracerSpeed * Time.fixedDeltaTime) / Vector3.Distance(t.start, t.end);
                tracers.Add(t);
            }
        }

        /// <summary>
        /// Iteratively fires the weapon and waits between each shot<br></br>
        /// If AutoBurst is enabled, this coroutine immediately re-enables firing. Otherwise, firing is disabled until fireInput is released.
        /// </summary>
        /// <returns></returns>
        IEnumerator BurstFire()
        {
            fired = true;
            currentBurstCount = 0;
            if (delayBeforeShot)
                yield return new WaitForSeconds(shotCooldown);
            while (currentBurstCount < burstCount)
            {
                FireWeapon();
                currentBurstCount++;
                yield return new WaitForSeconds(currentBurstCount == burstCount ? burstCooldown : shotCooldown);
            }
            if(!autoBurst)
                yield return new WaitUntil(() => fireInput.Value == false);
            fired = false;
            yield break;
        }
        /// <summary>
        /// Fires the weapon, waits based on the shotCooldown, then allows the gun to fire again
        /// </summary>
        /// <returns></returns>
        IEnumerator AutoFire()
        {
            if (!delayBeforeShot)
                FireWeapon();
            fired = true;
            yield return new WaitForSeconds(shotCooldown);
            if (delayBeforeShot)
                FireWeapon();
            fired = false;

            yield break;
        }
        protected override void OnValidate()
        {
            base.OnValidate();
            //If we haven't set up the weapon properties yet, we need to do that now.
            if (properties.Count != System.Enum.GetNames(typeof(WeaponProperty.PropertyType)).Length - 1)
            {
                string[] enumnames = System.Enum.GetNames(typeof(WeaponProperty.PropertyType));
                //If we have less properties than needed, we'll clear the list and recreate it.
                if (properties.Count < enumnames.Length - 1)
                {
                    while (properties.Count < enumnames.Length - 1)
                    {
                        var prop = new WeaponProperty()
                        {
                            property = (WeaponProperty.PropertyType)properties.Count + 1,
                            amount = 1,
                        };
                        prop.name = System.Enum.GetName(typeof(WeaponProperty.PropertyType), prop.property);
                        properties.Add(prop);
                    }
                }
                else
                {
                    while (properties.Count > enumnames.Length -1)
                        properties.RemoveAt(properties.Count - 1);
                }
            }

            if (newMag.magazine)
            {
                newMag.startPos = newMag.magazine.localPosition;
                newMag.startRot = newMag.magazine.localRotation;
            }
            if (oldMag.magazine)
            {
                oldMag.startPos = oldMag.magazine.localPosition;
                oldMag.startRot = oldMag.magazine.localRotation;
            }
        }
    }
}