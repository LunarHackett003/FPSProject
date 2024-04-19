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
        public readonly SyncVar<bool> fireInput = new(new SyncTypeSettings(ReadPermission.ExcludeOwner));
        bool fired;
        [SerializeField] AttachmentManager attachmentManager;

        [SerializeField] internal Transform aimPoint, gripPoint;
        private void Awake()
        {
            if(!attachmentManager)
                attachmentManager = GetComponent<AttachmentManager>();
            attachmentManager.SpawnAttachments();
        }
        [ServerRpc(RunLocally = true)]
        public void SetFireInput(bool fireInput)
        {
            this.fireInput.Value = fireInput;
        }

        private void FixedUpdate()
        {
            WeaponEval();
        }
        [Server(Logging = FishNet.Managing.Logging.LoggingType.Common)]
        void WeaponEval()
        {
            if (fireInput.Value)
            {
                if (!fired)
                {
                    print("This weapon should fire!");
                    fired = true;
                }
            }
            else
            {
                fired = false;
            }
        }
        [ServerRpc()]
        public void UseWeapon()
        {
            print("Fired a weapon!");
        }

    }
}