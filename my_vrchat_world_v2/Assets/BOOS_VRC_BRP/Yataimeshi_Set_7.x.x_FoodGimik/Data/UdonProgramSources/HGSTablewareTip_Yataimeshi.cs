
using UdonSharp;
using UnityEngine;
using UnityEngine.UI;
using VRC.SDKBase;
using VRC.Udon;

namespace HenoScript
{
    public class HGSTablewareTip_Yataimeshi : UdonSharpBehaviour
    {
        [Header("This")]
        bool initialized;
        Collider thisCollider;

        [Header("HGSCore")]
        [HideInInspector] public GameObject hGSCoreGo;
        //[HideInInspector] public HGSCore hGSCoreScript;
        [HideInInspector] public Transform hGSCoreTr;
        [HideInInspector] public UdonBehaviour hGSCoreUdon;

        [Header("Debug")]
        [SerializeField] Text debugText;

        void Start()
        {
            Initialize();
        }

        public void Initialize()
        {
            thisCollider = GetComponent<Collider>();

            initialized = true;
        }

        public void OnTriggerEnter(Collider other)
        {
            if (other.gameObject.name == "HGSCoreScript" || other.gameObject.name == "HGSSubGScript")
            {
                ///other上にUdonBehaviourがなければreturn///
                UdonBehaviour _otherUdon = other.GetComponent<UdonBehaviour>();
                if (!Utilities.IsValid(_otherUdon)) { return; }

                ///foodStateとpickupableTを取得///
                int _foodState = (int)_otherUdon.GetProgramVariable("foodState");
                bool _pickupableT = (bool)_otherUdon.GetProgramVariable("pickupableT");

                if (!Utilities.IsValid(_foodState)) { return; }
                if (!Utilities.IsValid(_pickupableT)) { return; }

                if (_foodState == 2) { return; } ///箸持ち状態ならreturn
                if (_foodState == 3) { return; } ///手持ち状態ならreturn
                if (_pickupableT == false) { return; }

                hGSCoreGo = other.gameObject;
                hGSCoreTr = other.GetComponent<Transform>();
                hGSCoreUdon = other.GetComponent<UdonBehaviour>();

                ///コライダー非アクティブ、Coreのオーナー変更、親子付け変更、全員に向けてState2を呼ぶ///
                TipColliderDisable();
                Networking.SetOwner(Networking.LocalPlayer, hGSCoreGo);
                hGSCoreTr.SetParent(this.transform);
                hGSCoreUdon.SetProgramVariable<UdonBehaviour>("connectedTip", this.GetComponent<UdonBehaviour>());
                hGSCoreUdon.SendCustomNetworkEvent(VRC.Udon.Common.Interfaces.NetworkEventTarget.All, "State2_PickedUpByTableware");
            }
        }

        public void TipColliderEnable()
        {
            if (!initialized) { Initialize(); }
            thisCollider.enabled = true;
        }

        public void TipColliderDisable()
        {
            if (!initialized) { Initialize(); }
            thisCollider.enabled = false;
        }

        public void ReleaseFood()
        {
            if (!Utilities.IsValid(hGSCoreUdon)) { return; }
            hGSCoreUdon.SendCustomNetworkEvent(VRC.Udon.Common.Interfaces.NetworkEventTarget.All, "State4_FreeFall");
        }

        public void ClearVariables()
        {
            hGSCoreGo = null;
            hGSCoreUdon = null;
            hGSCoreTr = null;
        }
    }
}
