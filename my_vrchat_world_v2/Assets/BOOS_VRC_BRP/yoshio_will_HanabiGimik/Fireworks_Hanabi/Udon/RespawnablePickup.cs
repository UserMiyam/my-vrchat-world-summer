
using UdonSharp;
using UnityEngine;
using VRC.SDKBase;
using VRC.Udon;
using VRC.SDK3.Components;

namespace yoshio_will.fireworks
{
    [UdonBehaviourSyncMode(BehaviourSyncMode.Continuous)]
    public class RespawnablePickup : UdonSharpBehaviour
    {
        private Vector3 InitialPosition;
        private Quaternion InitialRotation;
        private bool IsPickedUp = false;
        private Rigidbody _rigidbody;
        private VRCObjectSync _objectSync;

        void Start()
        {
            _rigidbody = GetComponent<Rigidbody>();
            _objectSync = GetComponent<VRCObjectSync>();
            InitialPosition = transform.position;
            InitialRotation = transform.rotation;
        }

        public override void OnPickup()
        {
            IsPickedUp = true;
        }

        public override void OnDrop()
        {
            IsPickedUp = false;
        }

        public void Respawn()
        {
            if (!Networking.IsOwner(gameObject)) return;
            if (IsPickedUp) return;

            _rigidbody.velocity = Vector3.zero;
            _rigidbody.angularVelocity = Vector3.zero;
            _rigidbody.position = InitialPosition;
            _rigidbody.rotation = InitialRotation;

            if (_objectSync) _objectSync.FlagDiscontinuity();
        }
    }
}