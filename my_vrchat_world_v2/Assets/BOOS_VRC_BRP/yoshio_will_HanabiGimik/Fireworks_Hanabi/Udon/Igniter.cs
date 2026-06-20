
using UdonSharp;
using UnityEngine;
using VRC.Dynamics;
using VRC.SDKBase;
using VRC.Udon;

namespace yoshio_will.fireworks
{
    [UdonBehaviourSyncMode(BehaviourSyncMode.Manual)]
    public class Igniter : UdonSharpBehaviour
    {
        private float IgniteTimer = float.PositiveInfinity;
        [SerializeField] private float IgniteTime = 0.5f;
        [SerializeField] private UdonBehaviour IgniteTarget;
        [SerializeField] private ParticleSystem[] ParticleSystems;
        [SerializeField] private AudioSource AudioSource;
        private ContactSenderProxy _fireContact;

        const float SonicSpeed = 340f;

        /*
        private void OnTriggerEnter(Collider other)
        {
            if (other == null) return;
            if (other.gameObject.layer == 1)  // 1: TransparentFX
            {
                if (IgniteTimer == float.PositiveInfinity)
                {
                    IgniteTimer = Time.time + IgniteTime;
                }
            }
            if (other.gameObject.layer == 4)  // 4: Water
            {
                IgniteTarget.SendCustomEvent("Extinguish");
            }
        }

        private void OnTriggerExit(Collider other)
        {
            if (other == null) return;
            if (other.gameObject.layer == 1)  // 1: TransparentFX
            {
                IgniteTimer = float.PositiveInfinity;
            }
        }
        */

        public override void OnContactEnter(ContactEnterInfo contactInfo)
        {
            ContactSenderProxy sender = contactInfo.contactSender;
            bool isCatchFire = false;
            bool isCatchWater = false;
            foreach (var tag in contactInfo.matchingTags)
            {
                if (tag == "Fire")
                {
                    isCatchFire = true;
                    _fireContact = sender;
                    break;
                }
                if (tag == "Water")
                {
                    isCatchWater = true;
                    break;
                }
            }

            if (isCatchFire && float.IsInfinity(IgniteTimer))
            {
                IgniteTimer = Time.time + IgniteTime;
            }

            if (isCatchWater)
            {
                IgniteTarget.SendCustomEvent("Extinguish");
            }
        }

        public override void OnContactExit(ContactExitInfo contactInfo)
        {
            ContactSenderProxy sender = contactInfo.contactSender;
            if (_fireContact.Equals(sender))
            {
                IgniteTimer = float.PositiveInfinity;
            }
        }

        private void Update()
        {
            if (IgniteTimer > Time.time) return;
            IgniteTarget.SendCustomEvent("Ignite");
            IgniteTimer = float.PositiveInfinity;
        }

        public void Ignite2()
        {
            // 三号玉の爆発
            foreach (var particle in ParticleSystems) particle.Play();
            if (AudioSource)
            {
                VRCPlayerApi player = Networking.LocalPlayer;
                float distance = Vector3.Distance(transform.position, player.GetPosition());
                float delay = distance / SonicSpeed;
                SendCustomEventDelayedSeconds(nameof(PlayAudio), delay);
            }
        }

        public void PlayAudio()
        {
            if (AudioSource) AudioSource.Play();
        }
    }
}