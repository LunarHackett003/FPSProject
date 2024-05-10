using FishNet;
using FishNet.Object;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace Eclipse.Weapons.Attachments
{
    public class AttachmentManager : NetworkBehaviour
    {
        [System.Serializable]
        public class AttachPoint
        {
            [Tooltip("The type of attachment this slot takes")]
            public AttachmentData.AttachmentType attachmentType;
            [Tooltip("Where this attachment, well, attaches :p")]
            public Transform point;
            public Transform magazinePoint;
            [Tooltip("If the attachment requires a mount to be attached, it'll activate this.\nWhen removing a part, it'll check if any other attachments need this mount before deactivating it.")]
            public GameObject partMount;
            [Tooltip("The default attachment, equips this when no others are equipped.")]
            public AttachmentData defaultAttachment;
            [Tooltip("The attachment currently equipped on this point.\nIf null, we'll fallback to DefaultAttachment")]
            public AttachmentData currentAttachment;
            public GameObject defaultAttachmentObject, currentAttachmentObject;
        }
        public AttachPoint[] attachPoints;
        [Tooltip("This transform indicates where the player will aim.\nIf an optic is equipped, this transform will be moved to the optic's aim point")]
        public Transform aimPoint;
        public Transform gripPoint;
        public Weapon weapon;
        [SerializeField] Vector3 defaultAimPoint;
        internal void SpawnAttachments()
        {
            if(defaultAimPoint == Vector3.zero)
                defaultAimPoint = aimPoint.localPosition;

            List<Transform> requiredMounts = new();
            //Check each attachment point if it has an attachment equipped
            foreach (var item in attachPoints)
            {
                //If an attachment is selected, we need to hide the default
                if(item.currentAttachment != null)
                {
                    //If this slot has a default attachment, disable it
                    if(item.defaultAttachmentObject)
                        item.defaultAttachmentObject.SetActive(false);
                    //Instantiate the attachment prefab and assign it
                    item.currentAttachmentObject = Instantiate(item.currentAttachment.attachmentPrefab, item.point);
                    print("attach placed - 0");
                    //Set the position and rotation to its offset
                    item.currentAttachmentObject.transform.SetLocalPositionAndRotation(item.currentAttachment.posOffset, Quaternion.Euler(item.currentAttachment.eulerOffset));
                    print("attach positioned - 1");
                    //If it needs a mount, add the mount
                    if (item.currentAttachment.requiresMount)
                    {
                        item.partMount.SetActive(true);
                        requiredMounts.Add(item.partMount.transform);
                    }
                    print("Checking attach type - 2");
                    switch (item.attachmentType)
                    {
                        case AttachmentData.AttachmentType.none:
                            break;
                        case AttachmentData.AttachmentType.optic:
                            aimPoint.position = item.currentAttachmentObject.transform.Find("AimPoint").position;
                            break;
                        case AttachmentData.AttachmentType.underbarrel:
                            Transform gp = item.currentAttachmentObject.transform.Find("GripPoint");
                            gripPoint.SetPositionAndRotation(gp.position, gp.rotation);
                            break;
                        case AttachmentData.AttachmentType.muzzle:
                            ParticleSystem ps = item.currentAttachmentObject.GetComponentInChildren<ParticleSystem>();
                            if (ps)
                                weapon.muzzleParticle = ps;
                            break;
                        case AttachmentData.AttachmentType.laser:
                            break;
                        case AttachmentData.AttachmentType.magazine:
                            if (item.magazinePoint)
                            {
                                print("trying to duplicate magazine");
                                var secondmag = Instantiate(item.currentAttachmentObject, item.magazinePoint);
                                secondmag.transform.SetLocalPositionAndRotation(Vector3.zero, Quaternion.identity);
                                ServerManager.Spawn(secondmag, Owner);
                                print("dupe successful");
                            }
                            break;
                        default:
                            break;
                    }
                    ServerManager.Spawn(item.currentAttachmentObject, Owner);

                }
                else
                {
                    if (item.currentAttachmentObject)
                        Destroy(item.currentAttachmentObject);
                    if(item.defaultAttachmentObject)
                        item.defaultAttachmentObject.SetActive(true);

                    if (item.attachmentType == AttachmentData.AttachmentType.optic)
                        aimPoint.localPosition = defaultAimPoint;
                }
            }
            if (IsOwner)
            {
                var renderers = GetComponentsInChildren<Renderer>();
                for (int i = 0; i < renderers.Length; i++)
                {
                    renderers[i].gameObject.layer = LayerMask.NameToLayer("Character");

                }

            }
        }
        override protected void OnValidate()
        {
            base.OnValidate();
            for (int i = 0; i < attachPoints.Length; i++)
            {
                AttachPoint item = attachPoints[i];
                if (item.defaultAttachment && !item.defaultAttachmentObject)
                {
                    item.defaultAttachmentObject = Instantiate(item.defaultAttachment.attachmentPrefab, item.point);
                    item.defaultAttachmentObject.transform.SetLocalPositionAndRotation(item.defaultAttachment.posOffset, Quaternion.Euler(item.defaultAttachment.eulerOffset));
                }
            }

        }
    }
}
