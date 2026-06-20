
using UdonSharp;
using UnityEngine;
using VRC.SDKBase;
using VRC.Udon;

namespace yoshio_will.fireworks
{
    [UdonBehaviourSyncMode(BehaviourSyncMode.Continuous)]
    public class Lighter : UdonSharpBehaviour
    {
        private Animator _animator;
        private int AnimParamOn;
        private Vector3 InitialPosition;
        private Quaternion InitialRotation;
        private bool IsPickedUp = false;

        private Rigidbody _rigidbody;

        void Start()
        {
            _animator = GetComponent<Animator>();
            _rigidbody = GetComponent<Rigidbody>();
            AnimParamOn = Animator.StringToHash("On");

            InitialPosition = transform.position;
            InitialRotation = transform.rotation;
        }

        public override void OnPickupUseDown()
        {
            SendCustomNetworkEvent(VRC.Udon.Common.Interfaces.NetworkEventTarget.All, nameof(LighterUseDownGlobal));
        }

        public override void OnPickupUseUp()
        {
            SendCustomNetworkEvent(VRC.Udon.Common.Interfaces.NetworkEventTarget.All, nameof(LighterUseUpGlobal));
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
        }

        public void LighterUseDownGlobal()
        {
            _animator.SetBool(AnimParamOn, true);
        }

        public void LighterUseUpGlobal()
        {
            _animator.SetBool(AnimParamOn, false);
        }
    }
}