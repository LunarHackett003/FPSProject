using System.Collections;
using System.Collections.Generic;
using UnityEngine;
namespace Eclipse.Weapons.Attachments
{
    [CreateAssetMenu(menuName = "Eclipse/Weapons/Attachment")]
    public class AttachmentData : ScriptableObject
    {
        /// <summary>
        /// The type of attachment
        /// </summary>
        public enum AttachmentType
        {
            /// <summary>
            /// This is not a valid attachment.
            /// </summary>
            none = 0,
            /// <summary>
            /// Aids in acquiring targets when aiming
            /// </summary>
            optic = 1,
            /// <summary>
            /// Mounted under the barrel, affects control of the weapon
            /// </summary>
            underbarrel = 2,
            /// <summary>
            /// Mounted on the end of the barrel, affects the bullets
            /// </summary>
            muzzle = 3,
            /// <summary>
            /// Mounted on a rail, typically, and affects control and hip-fire targeting
            /// </summary>
            laser = 4,
            /// <summary>
            /// Held by the dominant hand, usually the right in this game.
            /// </summary>
            grip = 5,
            /// <summary>
            /// The part of the weapon that is placed against the shoulder
            /// </summary>
            stock = 6,
            /// <summary>
            /// Affects the fire rate and handling of some weapons
            /// </summary>
            trigger = 7,
            /// <summary>
            /// The ammo container on this weapon
            /// </summary>
            magazine = 8
        }
        public bool requiresMount;
        public AttachmentType attachmentType;
        public GameObject attachmentPrefab;
        public Vector3 posOffset, eulerOffset;
        public List<Weapon.WeaponProperty> modifiedProperties;
        private void OnValidate()
        {
            for (int i = 0; i < modifiedProperties.Count; i++)
            {
                if (string.IsNullOrEmpty(modifiedProperties[i].name) || modifiedProperties[i].name.ToLower() == "none")
                    modifiedProperties[i].name = System.Enum.GetName(typeof(Weapon.WeaponProperty.PropertyType), modifiedProperties[i].property);
            }
        }
    }
}