
using UdonSharp;
using UnityEngine;
using VRC.SDKBase;
using VRC.Udon;
using VRC.SDK3.Components;

namespace yoshio_will.fireworks
{
    [UdonBehaviourSyncMode(BehaviourSyncMode.Continuous)]
    public class Fireworks : UdonSharpBehaviour
    {
        [SerializeField] private float MaxAngularVelocity = 200;
        [SerializeField] private float ShotPower = 0.001f;
        [SerializeField] private float ForceDeactivateTimer = 30;
        [SerializeField] private bool IsSearchObjectsFromParent = false;
        [SerializeField] private bool IsIgniteOnUse = false;
        private Transform ForceApplyPoint;
        private Transform VariableCarrier;
        private Transform FireballInitialPosition;
        private Rigidbody _rigidbody;
        private Animator _animator;
        private int AnimParamActivate, AnimParamDeactivate;
        private GameObject[] Fireballs;
        private bool IsIgnited = false;
        private Vector3 InitialPosition;
        private Quaternion InitialRotation;
        private bool IsPickedUp = false;
        private VRCObjectSync _objectSync;

        private float _forceDeactivateTime;

        void Start()
        {
            Fireballs = new GameObject[10];
            int fireballIdx = 0;

            Transform[] trans;
            if (IsSearchObjectsFromParent)
                trans = transform.parent.GetComponentsInChildren<Transform>();
            else
                trans = GetComponentsInChildren<Transform>();
            foreach (var tran in trans)
            {
                switch (tran.name)
                {
                    case "#ForceApplyPoint": ForceApplyPoint = tran; break;
                    case "#VariableCarrier": VariableCarrier = tran; break;
                    case "#InitialPosition": FireballInitialPosition = tran; break;
                }
                if (tran.name.Length > 9)
                {
                    if (tran.name.Substring(0, 9) == "#Fireball" && fireballIdx < Fireballs.Length)
                    {
                        Fireballs[fireballIdx] = tran.gameObject;
                        Debug.Log(tran.name);
                        fireballIdx++;
                    }
                }
            }

            _rigidbody = GetComponent<Rigidbody>();
            if (_rigidbody) _rigidbody.maxAngularVelocity = MaxAngularVelocity;
            _animator = GetComponent<Animator>();
            AnimParamActivate = Animator.StringToHash("Activate");
            AnimParamDeactivate = Animator.StringToHash("Deactivate");

            InitialPosition = transform.position;
            InitialRotation = transform.rotation;
            _objectSync = GetComponent<VRCObjectSync>();

            _forceDeactivateTime = float.PositiveInfinity;
        }

        public void Ignite()
        {
            //if (!Networking.IsOwner(gameObject)) return;
            SendCustomNetworkEvent(VRC.Udon.Common.Interfaces.NetworkEventTarget.All, nameof(IgniteGlobal));
        }

        public void Extinguish()
        {
            //if (!Networking.IsOwner(gameObject)) return;
            SendCustomNetworkEvent(VRC.Udon.Common.Interfaces.NetworkEventTarget.All, nameof(ExtinguishGlobal));
        }

        public void IgniteGlobal()
        {
            if (!IsIgnited)
            {
                IsIgnited = true;
                _animator.SetTrigger(AnimParamActivate);
                _animator.ResetTrigger(AnimParamDeactivate);
                _forceDeactivateTime = Time.time + ForceDeactivateTimer;
            }
        }

        public void ExtinguishGlobal()
        {
            if (IsIgnited)
            {
                Deactivate();
            }
        }

        private void FixedUpdate()
        {
            if (_forceDeactivateTime < Time.time)
            {
                Deactivate();
            }
            if (!IsIgnited) return;
            if (!Networking.IsOwner(gameObject)) return;
            if (ForceApplyPoint == null || VariableCarrier == null) return;

            if (_rigidbody) _rigidbody.AddForceAtPosition(ForceApplyPoint.TransformVector(Vector3.forward * VariableCarrier.localPosition.x), ForceApplyPoint.position);
        }

        public void Deactivate()    // Animatorから呼ばれるやつ。名前を変えないこと
        {
            IsIgnited = false;
            _animator.ResetTrigger(AnimParamActivate);
            _animator.SetTrigger(AnimParamDeactivate);
            _forceDeactivateTime = float.PositiveInfinity;
        }

        public void Respawn()   // Animatorから呼ばれるやつ。名前を変えないこと
        {
            if (!Networking.IsOwner(gameObject)) return;
            if (IsPickedUp) return;

            if (_rigidbody)
            {
                _rigidbody.velocity = Vector3.zero;
                _rigidbody.angularVelocity = Vector3.zero;
                _rigidbody.position = InitialPosition;
                _rigidbody.rotation = InitialRotation;
            }
            if (_objectSync) _objectSync.FlagDiscontinuity();
        }

        public void PrepareFireball1() { PrepareFireball(0); }
        public void PrepareFireball2() { PrepareFireball(1); }
        public void PrepareFireball3() { PrepareFireball(2); }
        public void PrepareFireball4() { PrepareFireball(3); }
        public void PrepareFireball5() { PrepareFireball(4); }
        public void PrepareFireball6() { PrepareFireball(5); }
        public void PrepareFireball7() { PrepareFireball(6); }
        public void PrepareFireball8() { PrepareFireball(7); }
        public void PrepareFireball9() { PrepareFireball(8); }
        public void PrepareFireball10() { PrepareFireball(9); }
        public void ShotFireball1() { ShotFireball(0); }
        public void ShotFireball2() { ShotFireball(1); }
        public void ShotFireball3() { ShotFireball(2); }
        public void ShotFireball4() { ShotFireball(3); }
        public void ShotFireball5() { ShotFireball(4); }
        public void ShotFireball6() { ShotFireball(5); }
        public void ShotFireball7() { ShotFireball(6); }
        public void ShotFireball8() { ShotFireball(7); }
        public void ShotFireball9() { ShotFireball(8); }
        public void ShotFireball10() { ShotFireball(9); }

        public void PrepareFireball(int idx)
        {
            Fireballs[idx].GetComponent<Rigidbody>().isKinematic = true;
            Fireballs[idx].transform.position = FireballInitialPosition.position;
            Fireballs[idx].transform.rotation = FireballInitialPosition.rotation;
        }

        public void ShotFireball(int idx)
        {
            Rigidbody rb = Fireballs[idx].GetComponent<Rigidbody>();
            Animator am = Fireballs[idx].GetComponent<Animator>();
            rb.isKinematic = false;
            rb.AddForce(FireballInitialPosition.forward * ShotPower, ForceMode.Impulse);
            am.SetTrigger(AnimParamActivate);
        }

        public override void OnPickup()
        {
            IsPickedUp = true;
        }

        public override void OnDrop()
        {
            IsPickedUp = false;
        }

        public override void OnPickupUseDown()
        {
            if (IsIgniteOnUse) Ignite();
        }
    }
}
