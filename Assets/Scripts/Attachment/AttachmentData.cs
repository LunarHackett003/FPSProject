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
        }
        public bool requiresMount;
        public AttachmentType attachmentType;
        public GameObject attachmentPrefab;
        public Vector3 posOffset, eulerOffset;
    }
}