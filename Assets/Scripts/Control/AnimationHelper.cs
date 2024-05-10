using System.Collections;
using System.Collections.Generic;
using UnityEngine;
namespace Eclipse.Weapons
{
    public class AnimationHelper : MonoBehaviour
    {
        public WeaponManager wm;
        public float forwardHandWeight;
        public Transform leftHand1, leftHand2, rightHand1, rightHand2;
        Vector3 leftHandStartPos, rightHandStartPos;
        Quaternion leftHandStartRot, rightHandStartRot;
        /// <summary>
        /// Key is magazine, value is holder
        /// </summary>
        private List<KeyValuePair<Transform, Transform>> grabbedMags = new();

        #region Magazine Grabbing
        /// <summary>
        /// 
        /// </summary>
        /// <param name="mag">The magazine being grabbed</param>
        /// <param name="hand">The hand grabbing the magazine</param>
        /// <param name="useMagPos">Should the magazine be held in its current orientation</param>
        void GrabMagazine(Weapon.Magazine mag, Transform hand, bool useMagPos)
        {
            if (useMagPos)
                hand.SetPositionAndRotation(mag.magazine.position, mag.magazine.rotation);
            grabbedMags.Add(new KeyValuePair<Transform, Transform>(mag.magazine, hand));
        }
        void ReleaseMagazine(Weapon.Magazine mag, bool leaveInPosition)
        {
            grabbedMags.RemoveAll(x => x.Key.transform == mag.magazine);
            if (!leaveInPosition)
            {
                mag.magazine.SetLocalPositionAndRotation(mag.startPos, mag.startRot);
            }
        }
        void ReplaceMagazine(Weapon.Magazine mag)
        {
            mag.magazine.SetLocalPositionAndRotation(mag.startPos, mag.startRot);
        }
        public void GrabOldMagLeft()
        {
            GrabMagazine(wm.CurrentWeapon.oldMag, leftHand1.transform, true);
        }
        public void GrabNewMagLeft()
        {
            GrabMagazine(wm.CurrentWeapon.newMag, leftHand2.transform, false);
        }
        public void GrabOldMagRight()
        {
            GrabMagazine(wm.CurrentWeapon.oldMag, rightHand1.transform, true);
        }
        public void GrabNewMagRight()
        {
            GrabMagazine(wm.CurrentWeapon.newMag, rightHand2.transform, false);
        }
        public void ReleaseOldMagInPosition()
        {
            ReleaseMagazine(wm.CurrentWeapon.oldMag, true);
        }
        public void ReleaseOldMagInDefault()
        {
            ReleaseMagazine(wm.CurrentWeapon.oldMag, false);
        }
        public void ReleaseNewMagInPosition()
        {
            ReleaseMagazine(wm.CurrentWeapon.newMag, true);
        }
        public void ReleaseNewMagInDefault()
        {
            ReleaseMagazine(wm.CurrentWeapon.newMag, false);
        }
        public void ReplaceOldMag()
        {
            ReplaceMagazine(wm.CurrentWeapon.oldMag);
        }
        public void ReplaceNewMag()
        {
            ReplaceMagazine(wm.CurrentWeapon.newMag);
        }
        #endregion Magazine Grabbing

        public void SwitchWeaponCallback()
        {
            wm.SwitchWeapon();
        }
        private void Update()
        {
            if(grabbedMags.Count > 0)
            {
                for (int i = 0; i < grabbedMags.Count; i++)
                {
                    KeyValuePair<Transform, Transform> pair = grabbedMags[i];
                    pair.Key.SetPositionAndRotation(pair.Value.position, pair.Value.rotation);
                }
            }
        }
    }
}