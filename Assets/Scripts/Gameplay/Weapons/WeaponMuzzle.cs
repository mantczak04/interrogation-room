using System.Collections;
using UnityEngine;

namespace InterrogationRoom.Gameplay.Weapons
{
    [DisallowMultipleComponent]
    public sealed class WeaponMuzzle : MonoBehaviour
    {
        [SerializeField] private Transform muzzleSocket;
        [SerializeField] private ParticleSystem muzzleFlash;
        [SerializeField] private Light muzzleLight;
        [SerializeField, Min(0.01f)] private float lightDuration = 0.05f;

        private Coroutine lightRoutine;

        public Vector3 Position => muzzleSocket != null ? muzzleSocket.position : transform.position;

        public bool IsConfigured => muzzleSocket != null && muzzleFlash != null;

        public void PlayFlash()
        {
            if (muzzleFlash == null)
            {
                return;
            }

            muzzleFlash.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
            muzzleFlash.Play(true);

            if (muzzleLight != null)
            {
                if (lightRoutine != null)
                {
                    StopCoroutine(lightRoutine);
                }

                lightRoutine = StartCoroutine(PulseLight());
            }
        }

        private IEnumerator PulseLight()
        {
            muzzleLight.enabled = true;
            yield return new WaitForSeconds(lightDuration);
            muzzleLight.enabled = false;
            lightRoutine = null;
        }

        private void OnDisable()
        {
            if (muzzleLight != null)
            {
                muzzleLight.enabled = false;
            }

            lightRoutine = null;
        }
    }
}
