using FishNet.CodeGenerating;
using FishNet.Object;
using FishNet.Object.Synchronizing;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Eclipse.Weapons.Attachments;

namespace Eclipse.Weapons
{
    public class Weapon : NetworkBehaviour
    {
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

        public readonly SyncVar<bool> fireInput = new(new SyncTypeSettings(ReadPermission.ExcludeOwner));
        internal bool fired;
        [SerializeField] AttachmentManager attachmentManager;

        [SerializeField] internal Transform aimPoint, gripPoint;
        [SerializeField] ParticleSystem muzzleParticle;
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
        WeaponManager wmParent;

        
        [SerializeField, Tooltip("This weapons Recoil Profile scriptable object")] internal RecoilProfile recoilProfile;
        [SerializeField, Tooltip("How strong the per-shot recoil is")] internal float recoilPerShot;
        [SerializeField] internal MobilityProfile mobilityProfile;

        private void Awake()
        {
            if(!attachmentManager)
                attachmentManager = GetComponent<AttachmentManager>();
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

            wmParent.recoilProfile = recoilProfile;
        }
        [ServerRpc(RunLocally = true)]
        public void SetFireInput(bool fireInput)
        {
            this.fireInput.Value = fireInput;
        }

        private void FixedUpdate()
        {
            //Server-side check against the fire input.
            WeaponEval();
        }
        [Server(Logging = FishNet.Managing.Logging.LoggingType.Common)]
        void WeaponEval()
        {
            //Double check, not sure if this is necessary or not tbqh
            if (!IsOwnerOrServer)
                return;
            //If this gun is attempting to fire, is still yet to fire AND the server is initialised (triple checking, apparently, still new to fishNet), then we'll fire!
            if(fireInput.Value && !fired && IsServerInitialized)
            {
                UseWeapon();
            }
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
        void FireWeapon()
        {
            print("Fired a weapon!");
            if (muzzleParticle)
                muzzleParticle.Play();
            if (wmParent)
            {
                wmParent.ReceiveRecoilImpulse();
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
        }
    }
}